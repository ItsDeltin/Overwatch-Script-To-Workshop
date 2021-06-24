namespace Deltin.Deltinteger.Parse
{
    public class NewClassInfo
    {
        public IndexReference ObjectReference { get; }
        public Constructor Constructor { get; }
        public WorkshopParameter[] Parameters { get; }
        
        public NewClassInfo(IndexReference objectReference, Constructor constructor, WorkshopParameter[] parameters)
        {
            ObjectReference = objectReference;
            Constructor = constructor;
            Parameters = parameters;
        }
    }
}