using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class StagedInitiation
    {
        public InitiationCollection<IGetSemantics> Semantics { get; } = new InitiationCollection<IGetSemantics>(getSemantics => getSemantics.GetSemantics());
        public InitiationCollection<IGetMeta> Meta { get; } = new InitiationCollection<IGetMeta>(getMeta => getMeta.GetMeta());
        public InitiationCollection<IGetContent> Content { get; } = new InitiationCollection<IGetContent>(getContent => getContent.GetContent());
        public InitiationCollection<IPostContent> PostContent { get; } = new InitiationCollection<IPostContent>(postContent => postContent.PostContent());

        public void Start()
        {
            Semantics.Set();
            Meta.Set();
            Content.Set();
            PostContent.Set();
        }

        public void On(InitiationStage stage, Action action)
        {
            var executor = new GenericExecutor(action);

            switch (stage)
            {
                case InitiationStage.Semantics: Semantics.Execute(executor); break;
                case InitiationStage.Meta: Meta.Execute(executor); break;
                case InitiationStage.Content: Content.Execute(executor); break;
                case InitiationStage.PostContent: PostContent.Execute(executor); break;
            }
        }

        public void On(IGetSemantics getSemantics) => Semantics.Execute(getSemantics);
        public void On(IGetMeta getMeta) => Meta.Execute(getMeta);
        public void On(IGetContent getContent) => Content.Execute(getContent);
        public void On(IPostContent postContent) => PostContent.Execute(postContent);

        public class InitiationCollection<T>
        {
            readonly HashSet<T> _executed = new HashSet<T>();
            readonly Action<T> _executor;
            HashSet<T> _wait = new HashSet<T>();
            bool _isSet;

            public InitiationCollection(Action<T> executor) => _executor = executor;

            public void Execute(T key)
            {
                if (_isSet)
                    Run(key);
                else
                    _wait.Add(key);
            }

            public void Depend(T key)
            {
                if (!_isSet)
                    throw new Exception("Cannot depend on key when the InitiationCollection is not yet set.");
                
                Run(key);
            }

            public void Set()
            {
                _isSet = true;
                foreach (var key in _wait)
                    Run(key);
                
                _wait = null;
            }

            void Run(T key)
            {
                if (_executed.Add(key))
                    _executor(key);
            }
        }

        class GenericExecutor : IGetSemantics, IGetMeta, IGetContent, IPostContent
        {
            readonly Action _action;
            public GenericExecutor(Action action) => _action = action;
            void IGetContent.GetContent() => _action();
            void IGetMeta.GetMeta()  => _action();
            void IGetSemantics.GetSemantics() => _action();
            void IPostContent.PostContent() => _action();
        }
    }

    public enum InitiationStage
    {
        Semantics,
        Meta,
        Content,
        PostContent
    }

    public interface IGetSemantics { void GetSemantics(); }
    public interface IGetMeta { void GetMeta(); }
    public interface IGetContent { void GetContent(); }
    public interface IPostContent { void PostContent(); }
}