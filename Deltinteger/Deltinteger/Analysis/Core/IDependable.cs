using System;

namespace DS.Analysis.Core
{
    interface IDependable
    {
        IDisposable AddDependent(IDependent dependent);
    }
}