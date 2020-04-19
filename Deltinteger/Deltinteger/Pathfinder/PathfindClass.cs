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

            serveObjectScope.AddNativeMethod(CustomMethodData.GetCustomMethod<Pathfind>());
            serveObjectScope.AddNativeMethod(CustomMethodData.GetCustomMethod<PathfindAll>());
            serveObjectScope.AddNativeMethod(CustomMethodData.GetCustomMethod<GetPath>());

            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<StopPathfind>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<IsPathfinding>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<IsPathfindStuck>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<FixPathfind>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<NextNode>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<WalkPath>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<CurrentSegmentAttribute>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<SegmentAttribute>());
            staticScope.AddNativeMethod(CustomMethodData.GetCustomMethod<RestartThottle>());
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

            Element index = (Element)newClassInfo.ObjectReference.GetVariable();
            IndexReference nodes = actionSet.VarCollection.Assign("_tempNodes", actionSet.IsGlobal, false);
            IndexReference segments = actionSet.VarCollection.Assign("_tempSegments", actionSet.IsGlobal, false);

            actionSet.AddAction(nodes.SetVariable(new V_EmptyArray()));
            actionSet.AddAction(segments.SetVariable(new V_EmptyArray()));

            foreach (var node in pathMap.Nodes)
                actionSet.AddAction(nodes.ModifyVariable(operation: Operation.AppendToArray, value: node.ToVector()));
            foreach (var segment in pathMap.Segments)
                actionSet.AddAction(segments.ModifyVariable(operation: Operation.AppendToArray, value: segment.AsWorkshopData()));
            
            actionSet.AddAction(Nodes.SetVariable((Element)nodes.GetVariable(), index: index));
            actionSet.AddAction(Segments.SetVariable((Element)segments.GetVariable(), index: index));
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