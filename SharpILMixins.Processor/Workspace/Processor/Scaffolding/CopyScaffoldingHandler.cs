using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using dnlib.DotNet;
using NLog;
using SharpILMixins.Annotations;
using SharpILMixins.Annotations.Inline;
using SharpILMixins.Processor.Utils;
using SharpILMixins.Processor.Workspace.Generator;
using SharpILMixins.Processor.Workspace.Obfuscation;
using SharpILMixins.Processor.Workspace.Processor.Actions;
using SharpILMixins.Processor.Workspace.Processor.Scaffolding.Redirects;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding
{
    public class CopyScaffoldingHandler
    {
        public CopyScaffoldingHandler(MixinWorkspace workspace)
        {
            Workspace = workspace;
            RedirectManager = new RedirectManager(this);
        }

        public Logger Logger { get; } = LoggerUtils.LogFactory.GetLogger(nameof(CopyScaffoldingHandler));

        public MixinWorkspace Workspace { get; }

        public RedirectManager RedirectManager { get; set; }

        public void ProcessType(TypeDef targetType, TypeDef mixinType)
        {
            ProcessInterfaces(targetType, mixinType);
            ProcessFields(targetType, mixinType);
            ProcessMethods(targetType, mixinType);
            ProcessProperties(targetType, mixinType);
        }

        private void ProcessInterfaces(TypeDef targetType, TypeDef mixinType)
        {
            if (mixinType.HasInterfaces)
                foreach (var impl in mixinType.Interfaces)
                {
                    var implUser =
                        new InterfaceImplUser(
                            RedirectManager.ResolveTypeDefIfNeeded(impl.Interface, targetType.DefinitionAssembly));
                    targetType.Interfaces.Add(implUser);
                    Logger.Info(
                        $"Mixin {mixinType.Name} provided Target Type {targetType.Name} the implementation of Interface {impl.Interface.Name}");
                }
        }

        #region Fields

        

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

        #endregion


        private bool ShouldCopyProperty(PropertyDef prop)
        {
            //Ignore shadowed fields
            return prop.GetCustomAttribute<ShadowAttribute>() == null;
        }

        private void ProcessProperties(TypeDef targetType, TypeDef mixinType)
        {
            var props = mixinType.Properties.Where(ShouldCopyProperty).ToList();
            ProcessShadowElements(mixinType.Properties, targetType.Properties.Cast<IMemberRef>().ToList());
            CopyProperties(targetType, props);

            //These elements might be used if the Mixin extends the class they're targeting and accesses from super
            var superElements = targetType.BaseType.ResolveTypeDef().Fields;
            ProcessShadowElements(superElements, superElements.Cast<IMemberRef>().ToList());
        }

        private void CopyProperties(TypeDef targetType, List<PropertyDef> props)
        {
            foreach (var prop in props)
            {
                var copyProp = new PropertyDefUser(prop.Name, prop.PropertySig, prop.Attributes);

                if (prop.GetCustomAttribute<UniqueAttribute>() != null)
                    copyProp.Name = Utilities.GenerateRandomName(Workspace.Settings.MixinHandlerName);

                CopyPropertyMethods(prop, copyProp);

                targetType.Properties.Add(copyProp);
                RedirectManager.RegisterRedirect(prop, copyProp);
            }
        }

        private void CopyPropertyMethods(PropertyDef prop, PropertyDefUser copyProp)
        {
            foreach (var def in prop.GetMethods) copyProp.GetMethods.Add(RedirectManager.ProcessMemberRedirect(def));
            foreach (var def in prop.SetMethods) copyProp.SetMethods.Add(RedirectManager.ProcessMemberRedirect(def));
            foreach (var def in prop.OtherMethods) copyProp.OtherMethods.Add(RedirectManager.ProcessMemberRedirect(def));

            foreach (var methodDef in copyProp.GetMethods.Concat(copyProp.SetMethods).Concat(copyProp.OtherMethods).Where(m => m.HasBody))
            {
                RedirectManager.ProcessRedirects(methodDef, methodDef.Body);
            }
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

        public void CopyNonMixinClasses(ModuleDefMD mixinModule, ModuleDefMD targetModule,
            ObfuscationMap? obfuscationMap = null)
        {
            var nonMixinTypes = mixinModule.Types
                .SelectMany(t => t.NestedTypes.Concat(new[] {t})).Where(mixinType =>
                    mixinType.GetCustomAttribute<MixinAttribute>() == null && mixinType.FullName != "<Module>")
                .ToList();

            foreach (var typeDef in nonMixinTypes)
            {
                //Skip empty targets classes generated by SharpILMixins
                if (typeDef.GetCustomAttribute<DescriptionAttribute>()?.Description ==
                    GeneratorMixinRelation.GeneratedByAttribute) continue;

                var newDeclaringType = FindNewDeclaringType(typeDef, targetModule, obfuscationMap);
                typeDef.DeclaringType = null;
                mixinModule.Types.Remove(typeDef);

                if (newDeclaringType != null)
                    newDeclaringType.NestedTypes.Add(typeDef);
                else
                    targetModule.Types.Add(typeDef);

                foreach (var method in typeDef.Methods) RedirectManager.ProcessRedirects(method, method.Body);
            }
        }

        private static TypeDef? FindNewDeclaringType(TypeDef typeDef, ModuleDefMD targetModule,
            ObfuscationMap? obfuscationMap = null)
        {
            var mixinAttribute = typeDef.DeclaringType?.GetCustomAttribute<MixinAttribute>();
            var target = mixinAttribute?.Target;
            if (obfuscationMap != null)
            {
                var entry = obfuscationMap.GetEntriesForType(ObfuscationMapEntryType.Type)
                    .FirstOrDefault(c => c.TargetMember == target);
                target = entry != null ? GetNamespace(entry.TargetMember) + entry.DeObfuscatedName : target;
            }

            var ownerType = targetModule.Find(target, false) ?? typeDef.DeclaringType;
            return ownerType;
        }

        private static string GetNamespace(string? fullName)
        {
            if (fullName == null) return "";
            return fullName.Contains('.') ? fullName.Substring(0, fullName.LastIndexOf('.') + 1) : fullName;
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
            var methodDoesNotHaveByRefParams = !method.GetParams().Any(p => p.IsByRef);
            var forceInlineHandlers = Workspace.Settings.ExperimentalInlineHandlers;
            var userRequestedInline = inlineOptionAttribute?.Setting ==
                                      InlineSetting.DoInline;
            var isOverwriteAndUserWantsInline = inlineOptionAttribute?.Setting ==
                InlineSetting.NoInline && method.GetCustomAttribute<OverwriteAttribute>() != null;

            return methodDoesNotHaveByRefParams && (forceInlineHandlers ||
                                                    userRequestedInline ||
                                                    isOverwriteAndUserWantsInline);
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