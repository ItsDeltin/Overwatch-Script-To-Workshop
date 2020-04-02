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
    public class PathmapClass : ClassType
    {
        public IndexReference Nodes { get; private set; }
        public IndexReference Segments { get; private set; }

        public PathmapClass() : base("Pathmap")
        {
            this.Constructors = new Constructor[] {
                new PathmapClassConstructor(this)
            };
            Description = "A pathmap can be used for pathfinding.";
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            serveObjectScope.AddMethod(CustomMethodData.GetCustomMethod<Pathfind>(), null, null);
            serveObjectScope.AddMethod(CustomMethodData.GetCustomMethod<PathfindAll>(), null, null);
            serveObjectScope.AddMethod(CustomMethodData.GetCustomMethod<GetPath>(), null, null);

            staticScope.AddMethod(CustomMethodData.GetCustomMethod<StopPathfind>(), null, null);
            staticScope.AddMethod(CustomMethodData.GetCustomMethod<IsPathfinding>(), null, null);
            staticScope.AddMethod(CustomMethodData.GetCustomMethod<IsPathfindStuck>(), null, null);
            staticScope.AddMethod(CustomMethodData.GetCustomMethod<FixPathfind>(), null, null);
            staticScope.AddMethod(CustomMethodData.GetCustomMethod<NextNode>(), null, null);
            staticScope.AddMethod(CustomMethodData.GetCustomMethod<WalkPath>(), null, null);
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            Nodes = translateInfo.VarCollection.Assign("Nodes", true, false);
            Segments = translateInfo.VarCollection.Assign("Segments", true, false);
        }

        protected override void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Get the pathmap data.
            PathMap pathMap = (PathMap)newClassInfo.AdditionalParameterData[0];

            actionSet.AddAction(Nodes.SetVariable(
                value: pathMap.NodesAsWorkshopData(),
                index: (Element)newClassInfo.ObjectReference.GetVariable()
            ));
            actionSet.AddAction(Segments.SetVariable(
                value: pathMap.SegmentsAsWorkshopData(),
                index: (Element)newClassInfo.ObjectReference.GetVariable()
            ));
        }
    }

    class PathmapClassConstructor : Constructor
    {
        public PathmapClassConstructor(PathmapClass pathMapClass) : base(pathMapClass, null, AccessLevel.Public)
        {
            Parameters = new CodeParameter[] {
                new PathmapFileParameter("pathmapFile", "File path of the pathmap to use. Must be a `.pathmap` file.")
            };
            Documentation = "Creates a pathmap from a `.pathmap` file.";
        }

        public override void Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData) => throw new NotImplementedException();
    }

    class PathmapFileParameter : FileParameter
    {
        public PathmapFileParameter(string parameterName, string description) : base(parameterName, description, ".pathmap") {}

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