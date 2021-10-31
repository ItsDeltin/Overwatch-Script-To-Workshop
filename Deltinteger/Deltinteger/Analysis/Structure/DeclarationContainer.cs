using System;

namespace DS.Analysis.Structure
{
    class DeclarationContainer : IDisposable
    {
        // Methods, classes, structs, enums, type alias
        readonly AbstractDeclaredElement[] _declaredElements;

        public DeclarationContainer(AbstractDeclaredElement[] declaredElements)
        {
            _declaredElements = declaredElements;
        }

        public void GetStructure(StructureInfo structureInfo)
        {
            foreach (var declaredElement in _declaredElements)
            {
                structureInfo.ScopeAppender.AddScopedElement(declaredElement.MakeScopedElement(default(ScopedElementParameters)));
                declaredElement.GetStructure(structureInfo);
            }
        }

        public void GetMeta(ContextInfo context)
        {
            foreach (var declaredElement in _declaredElements)
                declaredElement.GetMeta(context);
        }

        public void GetContent(ContextInfo context)
        {
            foreach (var declaredElement in _declaredElements)
                declaredElement.GetContent(context);
        }

        public void Dispose()
        {
            foreach (var declaredElement in _declaredElements)
                declaredElement.Dispose();
        }
    }
}