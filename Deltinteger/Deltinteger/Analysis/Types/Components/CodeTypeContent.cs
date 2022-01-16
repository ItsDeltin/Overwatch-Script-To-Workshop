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
                    var scopeBuilder = new CodeTypeScopeBuilder();

                    foreach (var element in Elements)
                        element.Construct(scopeBuilder);

                    scopeSource = scopeBuilder.ToScope();
                }
                return scopeSource;
            }
        }
        ScopeSource scopeSource;


        public static readonly CodeTypeContent Empty = new CodeTypeContent();
    }
}