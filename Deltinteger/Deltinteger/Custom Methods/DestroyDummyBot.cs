using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("DestroyDummyBot", "Destroys a dummy bot.", CustomMethodType.Action, typeof(NullType))]
    public class DestroyDummyBot : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("dummy", "The dummy bot to destroy. A reference to this can be obtained with running LastCreatedEntity() after creating a dummy bot.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            actionSet.AddAction(Element.Part("Destroy Dummy Bot", Element.Part("Team Of", parameterValues[0]), Element.Part("Slot Of", parameterValues[0])));
            return null;
        }
    }
}
