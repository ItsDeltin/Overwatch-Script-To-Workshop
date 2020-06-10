using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("DestroyDummyBot", "Destroys a dummy bot.", CustomMethodType.Action)]
    public class DestroyDummyBot : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("dummy", "The dummy bot to destroy. A reference to this can be obtained with running LastCreatedEntity() after creating a dummy bot.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            actionSet.AddAction(Element.Part<A_DestroyDummyBot>(Element.Part<V_TeamOf>(parameterValues[0]), Element.Part<V_SlotOf>(parameterValues[0])));
            return null;
        }
    }
}