namespace Deltin.Deltinteger.Parse
{
    public interface IContinueContainer
    {
        void AddContinue(ActionSet actionSet);
    }

    public interface IBreakContainer
    {
        void AddBreak(ActionSet actionSet);
    }
}