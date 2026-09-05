using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    // An interface that manages restricted calls.
    // Not to be confusted with IRecursiveCallHandler.
    public interface IRestrictedCallHandler
    {
        void AddRestrictedCall(RestrictedCall restrictedCall);

        public static RestrictedCallHandler New(Action<RestrictedCall> onRestrictedCall) => new RestrictedCallHandler(onRestrictedCall);
        public sealed class RestrictedCallHandler(Action<RestrictedCall> onRestrictedCall) : IRestrictedCallHandler
        {
            public void AddRestrictedCall(RestrictedCall restrictedCall) => onRestrictedCall(restrictedCall);
        }
    }

    // A simple implementation of IRestrictedCallHandler.
    // All restricted elements obtained are just added to the list.
    public class RestrictedCallList : List<RestrictedCall>, IRestrictedCallHandler
    {
        void IRestrictedCallHandler.AddRestrictedCall(RestrictedCall restrictedCall) => Add(restrictedCall);
    }
}