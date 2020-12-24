using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Animation
{
    class AnimationPlayFunctions
    {
        public static readonly FuncMethod Play = new FuncMethodBuilder() {
            Name = "Play",
            Documentation = "Plays an animation by its action name.",
            Parameters = new CodeParameter[] {
                new CodeParameter("actionName", "The name of the animation action."),
                new CodeParameter("loop", "Determines if the action loops when completed.")
            },
            Action = (actionSet, methodCall) => {
                // Get the AnimationTick component.
                var component = actionSet.DeltinScript.GetComponent<AnimationTick>();
                // Add the blendobject reference and the action identifier.
                component.AddAnimation(actionSet, (Element)actionSet.CurrentObject, methodCall.Get(0), methodCall.Get(1));
                return null;
            }
        };
        public static readonly FuncMethod Stop = new FuncMethodBuilder() {
            Name = "Stop",
            Documentation = "Stops an animation by the action name.",
            Parameters = new CodeParameter[] {
                new CodeParameter("actionName", "The name of the animation action.")
            },
            Action = (actionSet, methodCall) => {
                // Get the AnimationTick component.
                var component = actionSet.DeltinScript.GetComponent<AnimationTick>();
                // Add the blendobject reference and the action identifier.
                component.StopAnimation(actionSet, (Element)actionSet.CurrentObject, methodCall.Get(0));
                return null;
            }
        };
        public static readonly FuncMethod StopAll = new FuncMethodBuilder() {
            Name = "StopAll",
            Documentation = "Stops all animations being played.",
            Action = (actionSet, methodCall) => {
                // Get the AnimationTick component.
                var component = actionSet.DeltinScript.GetComponent<AnimationTick>();
                // Add the blendobject reference and the action identifier.
                component.StopAllAnimations(actionSet, (Element)actionSet.CurrentObject);
                return null;
            }
        };
    }
}