using System;
using System.Collections.Generic;

namespace Femyou
{
    public interface IModel : IDisposable
    {
        string Name { get; }
        string Description { get; }

        Guid GUID { get; }

        IReadOnlyDictionary<string, IVariable> Variables { get; }

        IInstance CreateCoSimulationInstance(string name, ICallbacks callbacks = null);
    }
}