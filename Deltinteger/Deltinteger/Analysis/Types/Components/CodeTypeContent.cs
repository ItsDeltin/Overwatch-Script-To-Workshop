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


        public ScopeSource ScopeSource => GenerateSource();
        ScopeSource scopeSource;

        ScopeSource GenerateSource()
        {
            if (scopeSource == null)
            {
                scopeSource = new ScopeSource();
                foreach (var element in Elements)
                    scopeSource.AddScopedElement(element.ScopedElement);
            }
            return scopeSource;
        }
    }
}