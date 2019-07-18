using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger
{
    public interface IWorkshopTree
    {
        string ToWorkshop();
        void DebugPrint(Log log, int depth = 0);
        double ServerLoadWeight();
    }

    public interface IMethod : ILanguageServerInfo
    {
        string Name { get; }
        ParameterBase[] Parameters { get; }
        WikiMethod Wiki { get; }
    }

    public interface ILanguageServerInfo
    {
        //string Label { get; }
        //string MarkdownLabel { get; }
        string GetLabel(bool markdown);
    }

    public interface ISkip
    {
        int SkipParameterIndex();
    }
}