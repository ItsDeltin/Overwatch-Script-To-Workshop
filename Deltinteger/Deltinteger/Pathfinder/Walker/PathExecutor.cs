using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder.Walker
{
    interface IPathRule
    {
        Element GetNodeArray(Element player);

        Element GetAttributesArray(Element player);

        bool NodesMayBeNull();
    }

    interface IPathExecutor
    {
        void Pathfind(ExecutorArgs executorArgs);
    }

    class ClassReferenceRule : IPathRule
    {
        readonly ExecutorInstanceSetup instance;
        readonly IndexReference pathmapReference;
        readonly PathmapClass pathmapClass;

        public ClassReferenceRule(ExecutorInstanceSetup instance)
        {
            this.instance = instance;
            pathmapClass = instance.DeltinScript.GetComponent<PathfinderTypesComponent>().Pathmap;
            pathmapReference = instance.DeltinScript.VarCollection.Assign("pathmapReference", false, false);
            instance.Setup(this);
        }

        public IPathExecutor WithReference(Element reference) => new WithReferenceExecutor(this, reference);

        // IPathRule
        Element IPathRule.GetNodeArray(Element player) => pathmapClass.Nodes.GetWithReference(
            instance.DeltinScript.WorkshopConverter,
            pathmapReference.Get(player)
        );

        Element IPathRule.GetAttributesArray(Element player) => pathmapClass.Attributes.GetWithReference(
            instance.DeltinScript.WorkshopConverter,
            pathmapReference.Get(player)
        );

        bool IPathRule.NodesMayBeNull() => instance.PathExecutorComponent.PotentiallyNullNodes;

        // IPathExecutor
        record WithReferenceExecutor(ClassReferenceRule rules, Element reference) : IPathExecutor
        {
            public void Pathfind(ExecutorArgs executorArgs)
            {
                // Set target's pathmap reference.
                executorArgs.ActionSet.AddAction(rules.pathmapReference.SetVariable(
                    value: reference,
                    targetPlayer: executorArgs.Players
                ));
                rules.instance.Pathfind(executorArgs);
            }
        }
    }

    class StructPathExecutor : IPathRule, IPathExecutor
    {
        /// <summary>The pathmap being used to pathfind.</summary>
        public IWorkshopTree Source { get; private set; }

        readonly ExecutorInstanceSetup instance;

        public StructPathExecutor(ExecutorInstanceSetup instance, IWorkshopTree source)
        {
            Source = source;
            this.instance = instance;
            instance.Setup(this);
        }

        public void Pathfind(ExecutorArgs executorArgs)
        {
            instance.Pathfind(executorArgs);
        }

        Element IPathRule.GetNodeArray(Element player) => (Element)((IStructValue)Source).GetValue(BakemapStruct.NODES_VAR_NAME);

        Element IPathRule.GetAttributesArray(Element player) => (Element)((IStructValue)Source).GetValue(BakemapStruct.ATTRIBUTES_VAR_NAME);

        bool IPathRule.NodesMayBeNull() => false;
    }
}