using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using NLog;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Processor.Actions;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding
{
    public class CopyScaffoldingHandler
    {
        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(CopyScaffoldingHandler));

        public CopyScaffoldingHandler(MixinWorkspace workspace)
        {
            Workspace = workspace;
            RedirectManager = new RedirectManager(this);
        }

        public MixinWorkspace Workspace { get; }

        public RedirectManager RedirectManager { get; set; }

        public void ProcessType(TypeDef targetType, TypeDef mixinType)
        {
            ProcessInterfaces(targetType, mixinType);
            ProcessFields(targetType, mixinType);
            ProcessMethods(targetType, mixinType);
        }

        private void ProcessInterfaces(TypeDef targetType, TypeDef mixinType)
        {
            if (mixinType.HasInterfaces)
            {
                foreach (var impl in mixinType.Interfaces)
                {
                    var implUser = new InterfaceImplUser(RedirectManager.ResolveTypeDefIfNeeded(impl.Interface, targetType.DefinitionAssembly));
                    targetType.Interfaces.Add(implUser);
                    Logger.Info($"Mixin {mixinType.Name} provided Target Type {targetType.Name} the implementation of Interface {impl.Interface.Name}");
                }
            }
        }

        private void ProcessFields(TypeDef targetType, TypeDef mixinType)
        {
            var fields = mixinType.Fields.Where(ShouldCopyField).ToList();
            ProcessShadowElements(mixinType.Fields, targetType.Fields.Cast<IMemberRef>().ToList());
            CopyFields(targetType, fields);
            
            //These elements might be used if the Mixin extends the class they're targeting and accesses from super
            var superElements = targetType.BaseType.ResolveTypeDef().Fields;
            ProcessShadowElements(superElements, superElements.Cast<IMemberRef>().ToList());
        }

        private void CopyFields(TypeDef targetType, List<FieldDef> fields)
        {
            foreach (var field in fields)
            {
                var copyField = new FieldDefUser(field.Name, field.FieldSig, field.Attributes);

                if (field.GetCustomAttribute<UniqueAttribute>() != null)
                    copyField.Name = Utilities.GenerateRandomName(Workspace.Settings.MixinHandlerName);

                targetType.Fields.Add(copyField);
                RedirectManager.RegisterRedirect(field, copyField);
            }
        }


        private static bool ShouldCopyField(FieldDef f)
        {
            //Ignore shadowed fields
            return f.GetCustomAttribute<ShadowAttribute>() == null;
        }

        private void ProcessShadowElements(IEnumerable<IMemberRef> mixinElements, IList<IMemberRef> targetElements)
        {
            foreach (var element in mixinElements)
                if (IsShadowElement(element, targetElements))
                {
                    var targetMethod =
                        targetElements.FirstOrDefault(m => RedirectManager.SigComparer.Equals(m, element)) ??
                        throw new MixinApplyException(
                            $"Unable to find target for Shadow element \"{element.FullName}\"");
                    RedirectManager.RegisterRedirect(element, targetMethod);
                }
        }

        private static bool IsShadowElement(IMemberRef element, IList<IMemberRef> targetElements)
        {
            return targetElements.Contains(element) || element is IHasCustomAttribute customAttribute &&
                customAttribute.GetCustomAttribute<ShadowAttribute>() != null;
        }

        public void CopyNonMixinClasses(ModuleDefMD mixinModule, ModuleDefMD targetModule)
        {
            var nonMixinTypes = mixinModule.Types.Where(mixinType =>
                mixinType.GetCustomAttribute<MixinAttribute>() == null && mixinType.FullName != "<Module>").ToList();

            foreach (var typeDef in nonMixinTypes)
            {
                mixinModule.Types.Remove(typeDef);
                targetModule.Types.Add(typeDef);
            }
        }

        #region Methods

        private void ProcessMethods(TypeDef targetType, TypeDef mixinType)
        {
            var mixinMethods = mixinType.Methods.Where(ShouldCopyMethod).ToList();
            ProcessShadowElements(mixinType.Methods, targetType.Methods.Cast<IMemberRef>().ToList());

            //These elements might be used if the Mixin extends the class they're targeting and accesses from super
            var superElements = targetType.BaseType.ResolveTypeDef().Fields;
            ProcessShadowElements(superElements, superElements.Cast<IMemberRef>().ToList());
           
            CreateMethodHandlers(targetType, mixinMethods);
        }

        private static bool ShouldCopyMethod(MethodDef m)
        {
            //Ignore constructors and shadowed methods
            return !m.IsConstructor && m.GetCustomAttribute<ShadowAttribute>() == null;
        }

        private void CreateMethodHandlers(TypeDef targetType, IEnumerable<MethodDef> mixinMethods)
        {
            foreach (var mixinMethod in mixinMethods)
            {
                //Overwrite handlers are only copied if NoInline is enabled
                if (IsMethodInlined(mixinMethod))
                {
                    //Redirect mixin method to target method
                    var exception = new MixinApplyException("Unable to inline mixin method");
                    var mixinAttribute = mixinMethod.GetCustomAttribute<BaseMixinAttribute>() ?? throw exception;
                    var targetMethod =
                        MixinAction.GetTargetMethodThrow(mixinMethod, mixinAttribute, targetType, Workspace);


                    RedirectManager.RegisterRedirect(mixinMethod, targetMethod);
                    continue;
                }

                var newMethod = CreateNewMethodCopy(targetType, mixinMethod);
                RedirectManager.RegisterRedirect(mixinMethod, newMethod);

                if (mixinMethod.GetCustomAttribute<OverwriteAttribute>() != null)
                    newMethod.Name = Utilities.GenerateRandomName("overwrite");
            }
        }

        public bool IsMethodInlined(MethodDef method)
        {
            var inlineOptionAttribute = method.GetCustomAttribute<MethodInlineOptionAttribute>();

            //Either inline all methods (requested by user)
            //or because the mixin creator asked to inline their method
            //or, if nothing else was specified, only inline if it's an overwrite handler
            return !method.GetParams().Any(p => p.IsByRef) && (Workspace.Settings.ExperimentalInlineHandlers ||
                                                               inlineOptionAttribute?.Setting ==
                                                               InlineSetting.DoInline ||
                                                               method.GetCustomAttribute<OverwriteAttribute>() != null);
        }

        private MethodDefUser CreateNewMethodCopy(TypeDef targetType, MethodDef method)
        {
            var newMethod = CopyUtils.CopyMethod(method, Workspace, targetType, false);
            if (method.GetCustomAttribute<UniqueAttribute>() != null)
                newMethod.Name = Utilities.GenerateRandomName(Workspace.Settings.MixinHandlerName);

            RedirectManager.ProcessRedirects(method, method.Body);

            return newMethod;
        }

        #endregion
    }
}