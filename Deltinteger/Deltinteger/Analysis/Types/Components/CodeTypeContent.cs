using System;
using DS.Analysis.Scopes;

namespace DS.Analysis.Types.Components
{
    class CodeTypeContent
    {
        protected ICodeTypeElement[] Elements;


        public CodeTypeContent()
        {
            Elements = new ICodeTypeElement[0];
        }

        public CodeTypeContent(ICodeTypeElement[] elements)
        {
            Elements = elements;
        }


        public ScopeSource ScopeSource
        {
            get
            {
                // If the scope source is null, generate it.
                if (scopeSource == null)
                {
                    scopeSource = new ScopeSource();

                    foreach (var element in Elements)
                        scopeSource.AddScopedElement(element.ScopedElement);
                }
                return scopeSource;
            }
        }
        ScopeSource scopeSource;


        public static readonly CodeTypeContent Empty = new CodeTypeContent();
    }
}