using System;
using System.Linq;
using System.Collections.Generic;

namespace DS.Analysis
{
    using Core;
    using Scopes;
    using Utility;

    class DotCrumb : IDisposable
    {
        readonly ContextInfo context;
        readonly List<DotNode> nodes = new List<DotNode>();

        public DotCrumb(ContextInfo context)
        {
            this.context = context;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public void AddNode(CrumbNodeFactory crumbNodeFactory, bool isLast)
        {
            nodes.Add(new DotNode(this, crumbNodeFactory, isLast));
        }

        public void Dispose() => nodes.Dispose();

        class DotNode : IDisposable
        {
            readonly SerialScopeSource serialScope = new SerialScopeSource();
            readonly IDotCrumbNode crumbNode;

            public DotNode(DotCrumb crumb, CrumbNodeFactory crumbNodeFactory, bool isLast)
            {
                var context = crumb.context;
                var parent = crumb.nodes.FirstOrDefault();
                var isFirst = parent == null;

                // If this is not the first expression, clear tail data and set the source expression.
                if (!isFirst)
                    context = parent.crumbNode.SetChildContext(context.ClearTail().SetScope(parent.serialScope));
                // If this is not the last expression, clear head data.
                if (!isLast)
                    context = context.ClearHead();

                crumbNode = crumbNodeFactory(new CrumbNodeFactoryHelper(context, serialScope));
            }

            public void Dispose() => crumbNode.Dispose();
        }

        public delegate IDotCrumbNode CrumbNodeFactory(CrumbNodeFactoryHelper helper);

        public static IDotCrumbNode CreateDotCrumbNode(IDisposable disposable, IScopeSource scopeSource, Func<ContextInfo, ContextInfo> setChildContext)
        {
            return new AnonymousDotCrumbNode(disposable, scopeSource, setChildContext);
        }

        class AnonymousDotCrumbNode : IDotCrumbNode
        {
            public IScopeSource ScopeSource { get; }
            readonly IDisposable disposable;
            readonly Func<ContextInfo, ContextInfo> setChildContext;

            public AnonymousDotCrumbNode(IDisposable disposable, IScopeSource scopeSource, Func<ContextInfo, ContextInfo> setChildContext)
            {
                ScopeSource = scopeSource;
                this.disposable = disposable;
                this.setChildContext = setChildContext;
            }

            public void Dispose() => disposable.Dispose();

            public ContextInfo SetChildContext(ContextInfo current) => setChildContext(current);
        }
    }

    class CrumbNodeFactoryHelper
    {
        public ContextInfo ContextInfo { get; }
        readonly SerialScopeSource serialScopeSource;

        public CrumbNodeFactoryHelper(ContextInfo contextInfo, SerialScopeSource serialScopeSource)
        {
            ContextInfo = contextInfo;
            this.serialScopeSource = serialScopeSource;
        }

        public void UpdateScope(ScopedElement[] elements) => serialScopeSource.Elements = elements;
    }

    interface IDotCrumbNode : IDisposable
    {
        IScopeSource ScopeSource { get; }
        ContextInfo SetChildContext(ContextInfo current);
    }
}