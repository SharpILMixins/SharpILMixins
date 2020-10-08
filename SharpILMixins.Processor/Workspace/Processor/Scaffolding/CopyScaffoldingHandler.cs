using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using SharpILMixins.Annotations;
using SharpILMixins.Processor.Utils;

namespace SharpILMixins.Processor.Workspace.Processor.Scaffolding
{
    public class CopyScaffoldingHandler
    {
        public MixinWorkspace Workspace { get; }

        public RedirectManager RedirectManager { get; set; }

        public CopyScaffoldingHandler(MixinWorkspace workspace)
        {
            Workspace = workspace;
            RedirectManager = new RedirectManager(this);
        }

        public void ProcessType(TypeDef targetType, TypeDef mixinType)
        {
            ProcessFields(targetType, mixinType);
            ProcessMethods(targetType, mixinType);
        }

        private void ProcessFields(TypeDef targetType, TypeDef mixinType)
        {
            var fields = mixinType.Fields.Where(ShouldCopyField).ToList();
            ProcessShadowElements(mixinType.Fields, targetType.Fields.Cast<IMemberRef>().ToList());
            CopyFields(targetType, fields);
        }

        private void CopyFields(TypeDef targetType, List<FieldDef> fields)
        {
            foreach (var field in fields)
            {
                var copyField = new FieldDefUser(field.Name, field.FieldSig, field.Attributes);

                if (field.GetCustomAttribute<UniqueAttribute>() != null)
                {
                    copyField.Name = Utilities.GenerateRandomName();
                }

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
            {
                if (element is IHasCustomAttribute customAttribute &&
                    customAttribute.GetCustomAttribute<ShadowAttribute>() != null)
                {
                    var targetMethod =
                        targetElements.FirstOrDefault(m => RedirectManager.SigComparer.Equals(m, element)) ??
                        throw new MixinApplyException(
                            $"Unable to find target for Shadow element \"{element.FullName}\"");
                    RedirectManager.RegisterRedirect(element, targetMethod);
                }
            }
        }

        #region Methods

        private void ProcessMethods(TypeDef targetType, TypeDef mixinType)
        {
            var mixinMethods = mixinType.Methods.Where(ShouldCopyMethod).ToList();
            ProcessShadowElements(mixinType.Methods, targetType.Methods.Cast<IMemberRef>().ToList());
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
                var newMethod = CreateNewMethodCopy(targetType, mixinMethod);
                RedirectManager.RegisterRedirect(mixinMethod, newMethod);
               
                if (mixinMethod.GetCustomAttribute<OverwriteAttribute>() != null)
                {
                    newMethod.Name = Utilities.GenerateRandomName("overwrite");
                }
            }
        }

        private MethodDefUser CreateNewMethodCopy(TypeDef targetType, MethodDef method)
        {
            var newMethod = CopyUtils.CopyMethod(method, targetType, false);
            if (method.GetCustomAttribute<UniqueAttribute>() != null)
            {
                newMethod.Name = Utilities.GenerateRandomName();
            }

            RedirectManager.ProcessRedirects(method.Body);

            return newMethod;
        }

        #endregion

        public void CopyNonMixinClasses(ModuleDefMD mixinModule, ModuleDefMD targetModule)
        {
            var mixinTypes = mixinModule.Types.Where(mixinType =>
                mixinType.GetCustomAttribute<MixinAttribute>() == null && mixinType.FullName != "<Module>").ToList();

            foreach (var mixinType in mixinTypes)
            {
                mixinModule.Types.Remove(mixinType);
                targetModule.Types.Add(mixinType);
            }
        }
    }
}