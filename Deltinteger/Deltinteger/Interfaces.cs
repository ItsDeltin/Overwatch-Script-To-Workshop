using System.Text;

namespace Deltin.Deltinteger
{
    public interface IWorkshopTree
    {
        string ToWorkshop();
        void DebugPrint(int depth = 0);
    }
}