using System;
using System.IO;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.CustomMethods;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Pathfinder
{
    public class PathmapClass : CodeType
    {
        protected override string TypeKindString => "class";
        private Scope ObjectScope { get; }
        private Scope StaticScope { get; }

        public PathmapClass() : base("Pathmap")
        {
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this)
            };
            Description = "A pathmap can be used for pathfinding.";

            ObjectScope = new Scope("class Pathmap");
            ObjectScope.AddMethod(CustomMethodData.GetCustomMethod<Pathfind>(), null, null);
            ObjectScope.AddMethod(CustomMethodData.GetCustomMethod<PathfindAll>(), null, null);
            ObjectScope.AddMethod(CustomMethodData.GetCustomMethod<GetPath>(), null, null);

            StaticScope = new Scope("class Pathmap");
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<StopPathfind>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<IsPathfinding>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<IsPathfindStuck>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<FixPathfind>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<NextNode>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<WalkPath>(), null, null);
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Create the class.
            var objectData = actionSet.Translate.DeltinScript.SetupClasses().CreateObject(actionSet, "_new_PathMap");

            // Get the pathmap data.
            PathMap pathMap = (PathMap)additionalParameterData[0];

            actionSet.AddAction(objectData.ClassObject.SetVariable(pathMap.NodesAsWorkshopData(), null, 0));
            actionSet.AddAction(objectData.ClassObject.SetVariable(pathMap.SegmentsAsWorkshopData(), null, 1));

            return objectData.ClassReference.GetVariable();
        }

        public override IndexReference GetObjectSource(DeltinScript translateInfo, IWorkshopTree element)
        {
            return translateInfo.SetupClasses().ClassArray.CreateChild((Element)element);
        }

        public override Scope GetObjectScope() => ObjectScope;
        public override Scope ReturningScope() => StaticScope;

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
                new PathmapFileParameter("pathmapFile", "File path of the pathmap to use. Must be a `.pathmap` file.")
            };
            Documentation = Extras.GetMarkupContent("Creates a pathmap from a `.pathmap` file.");
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues) => throw new NotImplementedException();
    }

    class PathmapFileParameter : FileParameter
    {
        public PathmapFileParameter(string parameterName, string description) : base("pathmap", parameterName, description)
        {
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            string filepath = base.Validate(script, value, valueRange) as string;
            if (filepath == null) return null;

            PathMap map;
            try
            {
                map = PathMap.ImportFromXML(filepath);
            }
            catch (InvalidOperationException)
            {
                script.Diagnostics.Error("Failed to deserialize the PathMap.", valueRange);
                return null;
            }

            return map;
        }
    }
}