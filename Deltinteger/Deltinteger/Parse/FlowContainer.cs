namespace Deltin.Deltinteger.Parse
{
    public interface IContinueContainer
    {
        void AddContinue(ActionSet actionSet, string comment);
    }

    public interface IBreakContainer
    {
        void AddBreak(ActionSet actionSet, string coment);
    }
}