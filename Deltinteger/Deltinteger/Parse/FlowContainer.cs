namespace Deltin.Deltinteger.Parse
{
    public interface IContinueContainer
    {
        void AddContinue(SkipStartMarker continueMarker);
    }

    public interface IBreakContainer
    {
        void AddBreak(SkipStartMarker continueMarker);
    }
}