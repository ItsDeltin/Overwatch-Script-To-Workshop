using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Animation
{
    class PlayAnimationFunction : IMethod
    {
        public string Name => "PlayAnimation";
        public string Documentation => "";
        public CodeParameter[] Parameters { get; } = new CodeParameter[] {
            new CodeParameter("actionName", "The name of the animation action.")
        };
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public bool Static => false;
        public bool WholeContext => true;
        public bool DoesReturnValue => false;
        public CodeType ReturnType => null;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => IMethod.GetLabel(this);

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            // Get the AnimationTick component.
            var component = actionSet.DeltinScript.GetComponent<AnimationTick>();
            // Add the blendobject reference and the action identifier.
            component.AddAnimation(actionSet, (Element)actionSet.CurrentObject, methodCall.Get(0));
            return null;
        }
    }
}