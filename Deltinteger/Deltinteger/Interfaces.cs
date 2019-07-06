using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger
{
    public interface IWorkshopTree
    {
        string ToWorkshop();
        void DebugPrint(Log log, int depth = 0);
    }

    public interface IMethod
    {
        string Name { get; }
        ParameterBase[] Parameters { get; }
        string Label { get; }
        WikiMethod Wiki { get; }
    }
}