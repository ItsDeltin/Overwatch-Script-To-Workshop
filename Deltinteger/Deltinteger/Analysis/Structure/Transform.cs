using System.Collections.Generic;
using System.Linq;

namespace DS.Analysis.Structure
{
    struct Transform
    {
        public IEnumerable<string> Path;

        public Transform(string name) => Path = new[] { name };
        public Transform(Transform parent, string name) => Path = parent.Path.Append(name);
    }
}