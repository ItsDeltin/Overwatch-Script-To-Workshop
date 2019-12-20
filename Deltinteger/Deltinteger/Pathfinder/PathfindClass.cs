using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class PathmapClass : CodeType
    {
        public PathmapClass() : base("Pathmap")
        {
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this)
            };
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues)
        {
            var classData = actionSet.Translate.DeltinScript.SetupClasses();
            var classReference = actionSet.VarCollection.Assign("_new_PathMap", actionSet.IsGlobal, true);
            DefinedType.GetClassIndex(classReference, actionSet, classData);

            ((PathmapClassConstructor)constructor).Parse(actionSet, classReference, constructorValues);

            return classReference.GetVariable();
        }

        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = "Pathmap",
            Kind = CompletionItemKind.Class
        };
    }

    class PathmapClassConstructor : Constructor
    {
        public PathmapClassConstructor(PathmapClass pathMapClass) : base(pathMapClass, null, AccessLevel.Public)
        {
            Parameters = new CodeParameter[] {
                new CodeParameter("pathmapFile", null, null, "File path of the pathmap to use.")
            };
        }

        public void Parse(ActionSet actionSet, IndexReference classReference, IWorkshopTree[] parameterValues)
        {
            
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues) => throw new NotImplementedException();
    }
}