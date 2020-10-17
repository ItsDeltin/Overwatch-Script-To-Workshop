namespace Deltin.Deltinteger.Parse
{
    public class NewClassInfo
    {
        public IndexReference ObjectReference { get; }
        public Constructor Constructor { get; }
        public IWorkshopTree[] ConstructorValues { get; }
        public object[] AdditionalParameterData { get; }
        
        public NewClassInfo(IndexReference objectReference, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            ObjectReference = objectReference;
            Constructor = constructor;
            ConstructorValues = constructorValues;
            AdditionalParameterData = additionalParameterData;
        }
    }
}