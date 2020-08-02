using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.CustomMethods;
using Deltin.Deltinteger.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Parse
{
    public class VectorType : CodeType
    {
        public static VectorType Instance { get; } = new VectorType();

        private Scope objectScope = new Scope("Vector");
        private Scope staticScope = new Scope("Vector");

        private InternalVar X;
        private InternalVar Y;
        private InternalVar Z;

        private InternalVar HorizontalAngle;
        private InternalVar VerticalAngle;

        private InternalVar Zero;

        public VectorType() : base("Vector")
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            Kind = "struct";
            TokenType = TokenType.Struct;
        }

        public void ResolveElements()
        {
            X = CreateInternalVar("X", "The X component of the vector.");
            Y = CreateInternalVar("Y", "The Y component of the vector.");
            Z = CreateInternalVar("Z", "The Z component of the vector.");
            HorizontalAngle = CreateInternalVar("HorizontalAngle", "The horizontal angle of the vector.");
            VerticalAngle = CreateInternalVar("VerticalAngle", "The vertical angle of the vector.");
            Zero = CreateInternalVar("Zero", "Equal to `Vector(0, 0, 0)`.", true);

            objectScope.AddNativeMethod(DistanceTo);
            objectScope.AddNativeMethod(CrossProduct);
            objectScope.AddNativeMethod(DotProduct);
            objectScope.AddNativeMethod(Normalize);
            objectScope.AddNativeMethod(DirectionTowards);
            objectScope.AddNativeMethod(FarthestPlayer);
            objectScope.AddNativeMethod(ClosestPlayer);
            objectScope.AddNativeMethod(IsInLineOfSight);
            objectScope.AddNativeMethod(Towards);
            objectScope.AddNativeMethod(AsLocalVector);
            objectScope.AddNativeMethod(AsWorldVector);
        }

        private InternalVar CreateInternalVar(string name, string documentation, bool isStatic = false)
        {
            // Create the variable.
            InternalVar newInternalVar = new InternalVar(name, CompletionItemKind.Property);

            // Make the variable unsettable.
            newInternalVar.IsSettable = false;

            // Set the documentation.
            newInternalVar.Documentation = documentation;

            // Add the variable to the object scope.
            if (!isStatic) objectScope.AddNativeVariable(newInternalVar);
            // Add the variable to the static scope.
            else staticScope.AddNativeVariable(newInternalVar);

            return newInternalVar;
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            translateInfo.DefaultIndexAssigner.Add(Zero, Element.Vector(0, 0, 0));
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(X, Element.XOf(reference));
            assigner.Add(Y, Element.YOf(reference));
            assigner.Add(Z, Element.ZOf(reference));

            assigner.Add(HorizontalAngle, Element.Part("Horizontal Angle From Direction", reference));
            assigner.Add(VerticalAngle, Element.Part("Vertical Angle From Direction", reference));
        }

        public override Scope GetObjectScope() => objectScope;
        public override Scope ReturningScope() => staticScope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };

        private static FuncMethod DistanceTo = new FuncMethodBuilder() {
            Name = "DistanceTo",
            Documentation = "Gets the distance between 2 vectors.",
            DoesReturnValue = true,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector or player to get the distance to.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.DistanceBetween(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod CrossProduct = new FuncMethodBuilder() {
            Name = "CrossProduct",
            Documentation = "The cross product of the specified vector.",
            DoesReturnValue = true,
            ReturnType = Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the cross product to.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.CrossProduct(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod DotProduct = new FuncMethodBuilder() {
            Name = "DotProduct",
            Documentation = "Returns what amount of one vector goes in the direction of another.",
            DoesReturnValue = true,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the dot product to.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.DotProduct(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod Normalize = new FuncMethodBuilder() {
            Name = "Normalize",
            Documentation = "The unit-length normalization of the vector.",
            DoesReturnValue = true,
            ReturnType = Instance,
            Action = (ActionSet actionSet, MethodCall call) => Element.Normalize(actionSet.CurrentObject)
        };

        private static FuncMethod DirectionTowards = new FuncMethodBuilder() {
            Name = "DirectionTowards",
            Documentation = "The unit-length direction vector to another vector.",
            DoesReturnValue = true,
            ReturnType = Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the direction towards.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.DirectionTowards(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod FarthestPlayer = new FuncMethodBuilder() {
            Name = "FarthestPlayer",
            Documentation = "The farthest player from the vector, optionally restricted by team.",
            DoesReturnValue = true,
            Parameters = new CodeParameter[] { new CodeParameter("team", "The team to get the farthest player with.", new ExpressionOrWorkshopValue(ElementEnumMember.Team(Team.All))) },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Farthest Player From", actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod ClosestPlayer = new FuncMethodBuilder() {
            Name = "ClosestPlayer",
            Documentation = "The closest player to the vector, optionally restricted by team.",
            DoesReturnValue = true,
            Parameters = new CodeParameter[] { new CodeParameter("team", "The team to get the closest player with.", new ExpressionOrWorkshopValue(ElementEnumMember.Team(Team.All))) },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Closest Player To", actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod IsInLineOfSight = new FuncMethodBuilder() {
            Name = "IsInLineOfSight",
            Documentation = "Whether the vector has line of sight with the specified vector.",
            DoesReturnValue = true,
            Parameters = new CodeParameter[] {
                new CodeParameter("other", "The vector to determine line of site."),
                new CodeParameter("barriers", "Defines how barriers affect line of sight.", ValueGroupType.GetEnumType("BarrierLOS"), new ExpressionOrWorkshopValue(ElementRoot.Instance.GetEnumValue("BarrierLOS", "NoBarriersBlock")))
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Is In Line Of Sight", actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };

        private static FuncMethod Towards = new FuncMethodBuilder() {
            Name = "Towards",
            Documentation = "The displacement vector from the vector to another.",
            DoesReturnValue = true,
            ReturnType = Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the displacement towards.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Vector Towards", actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod AsLocalVector = new FuncMethodBuilder() {
            Name = "AsLocalVector",
            Documentation = "The vector in local coordinates corresponding to the vector in world coordinates.",
            DoesReturnValue = true,
            ReturnType = Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("relativePlayer", "The player to whom the resulting vector will be relative."),
                new CodeParameter("transformation", "Specifies whether the vector should receive a rotation and a translation (usually applied to positions) or only a rotation (usually applied to directions and velocities).", ValueGroupType.GetEnumType("Transformation"))
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Local Vector Of", actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };

        private static FuncMethod AsWorldVector = new FuncMethodBuilder() {
            Name = "AsWorldVector",
            Documentation = "The vector in world coordinates corresponding to the vector in local coordinates.",
            DoesReturnValue = true,
            ReturnType = Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("relativePlayer", "The player to whom the resulting vector will be relative."),
                new CodeParameter("transformation", "Specifies whether the vector should receive a rotation and a translation (usually applied to positions) or only a rotation (usually applied to directions and velocities).", ValueGroupType.GetEnumType("Transformation"))
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("World Vector Of", actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };
    }
}