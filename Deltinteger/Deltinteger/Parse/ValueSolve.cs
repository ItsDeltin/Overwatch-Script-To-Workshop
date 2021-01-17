using System;

namespace Deltin.Deltinteger.Parse
{
    public interface IValueSolve
    {
        bool IsSet { get; }
        void OnReady(Action action);
    }

    public interface IValueSolve<T> : IValueSolve
    {
        void OnReady(Action<T> action);
    }

    public class ValueSolveSource : IValueSolve
    {
        public bool IsSet { get; private set; }
        private Action _onValueObtained;

        public ValueSolveSource() {}

        public ValueSolveSource(bool isSet)
        {
            IsSet = isSet;
        }

        public void Set()
        {
            if (IsSet) throw new Exception("ValueSolveSource already set.");
            IsSet = true;
            if (_onValueObtained != null) _onValueObtained();
            _onValueObtained = null;
        }

        public void OnReady(Action action)
        {
            if (IsSet)
                action();
            else
                _onValueObtained += action;
        }
    }

    public class ValueSolveSource<T> : IValueSolve<T>
    {
        public bool IsSet { get; private set; }
        public T Value { get; private set; }
        private Action<T> _onValueObtained;

        public void Set(T value)
        {
            if (IsSet) throw new Exception("ValueSolveSource already set.");
            IsSet = true;
            Value = value;
            _onValueObtained(value);
            _onValueObtained = null;
        }

        public void OnReady(Action<T> action)
        {
            if (IsSet)
                action(Value);
            else
                _onValueObtained += action;
        }

        public void OnReady(Action action) => OnReady(v => action());
    }
}