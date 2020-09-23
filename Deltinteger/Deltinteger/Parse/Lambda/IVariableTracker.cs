using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public interface IVariableTracker
    {
        void LocalVariableAccessed(IIndexReferencer variable);
    }
}