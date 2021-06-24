using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod DestroyDummyBot(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "DestroyDummyBot",
            Documentation = "Destroys a dummy bot.",
            Parameters = new[] {
                new CodeParameter("dummy", "The dummy bot to destroy. A reference to this can be obtained with running LastCreatedEntity() after creating a dummy bot.", deltinScript.Types.Player())
            },
            Action = (actionSet, methodCall) => {
                actionSet.AddAction(Element.Part("Destroy Dummy Bot", Element.Part("Team Of", methodCall.Get(0)), Element.Part("Slot Of", methodCall.Get(0))));
                return null;
            }
        };
    }
}