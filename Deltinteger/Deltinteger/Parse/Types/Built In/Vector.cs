using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.CustomMethods;
using Deltin.Deltinteger.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Parse
{
    public class VectorType : CodeType, IInitOperations
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
            Inherit(Positionable.Instance, null, null);
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

        public void InitOperations()
        {
            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.Add, this, this, null, TypeOperation.Add), // Vector + vector
                new TypeOperation(TypeOperator.Subtract, this, this, null, TypeOperation.Subtract), // Vector - vector
                new TypeOperation(TypeOperator.Multiply, this, this, null, TypeOperation.Multiply), // Vector * vector
                new TypeOperation(TypeOperator.Divide, this, this, null, TypeOperation.Divide), // Vector / vector
                new TypeOperation(TypeOperator.Multiply, NumberType.Instance, this, null, TypeOperation.Multiply), // Vector * number
                new TypeOperation(TypeOperator.Divide, NumberType.Instance, this, null, TypeOperation.Divide), // Vector / number
            };
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
            translateInfo.DefaultIndexAssigner.Add(Zero, new V_Vector());
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(X, Element.Part<V_XOf>(reference));
            assigner.Add(Y, Element.Part<V_YOf>(reference));
            assigner.Add(Z, Element.Part<V_ZOf>(reference));

            assigner.Add(HorizontalAngle, Element.Part<V_HorizontalAngleFromDirection>(reference));
            assigner.Add(VerticalAngle, Element.Part<V_VerticalAngleFromDirection>(reference));
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
            ReturnType = NumberType.Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector or player to get the distance to.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_DistanceBetween>(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod CrossProduct = new FuncMethodBuilder() {
            Name = "CrossProduct",
            Documentation = "The cross product of the specified vector.",
            ReturnType = Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the cross product to.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_CrossProduct>(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod DotProduct = new FuncMethodBuilder() {
            Name = "DotProduct",
            Documentation = "Returns what amount of one vector goes in the direction of another.",
            ReturnType = NumberType.Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the dot product to.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_DotProduct>(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod Normalize = new FuncMethodBuilder() {
            Name = "Normalize",
            Documentation = "The unit-length normalization of the vector.",
            ReturnType = Instance,
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_Normalize>(actionSet.CurrentObject)
        };

        private static FuncMethod DirectionTowards = new FuncMethodBuilder() {
            Name = "DirectionTowards",
            Documentation = "The unit-length direction vector to another vector.",
            ReturnType = Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the direction towards.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_DirectionTowards>(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod FarthestPlayer = new FuncMethodBuilder() {
            Name = "FarthestPlayer",
            Documentation = "The farthest player from the vector, optionally restricted by team.",
            ReturnType = ObjectType.Instance, // TODO: Switch to player
            Parameters = new CodeParameter[] { new CodeParameter("team", "The team to get the farthest player with.", new ExpressionOrWorkshopValue(Element.Part<V_TeamVar>(EnumData.GetEnumValue(Team.All)))) },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_FarthestPlayerFrom>(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod ClosestPlayer = new FuncMethodBuilder() {
            Name = "ClosestPlayer",
            Documentation = "The closest player to the vector, optionally restricted by team.",
            ReturnType = ObjectType.Instance, // TODO: Switch to player
            Parameters = new CodeParameter[] { new CodeParameter("team", "The team to get the closest player with.", new ExpressionOrWorkshopValue(Element.Part<V_TeamVar>(EnumData.GetEnumValue(Team.All)))) },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_ClosestPlayerTo>(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod IsInLineOfSight = new FuncMethodBuilder() {
            Name = "IsInLineOfSight",
            Documentation = "Whether the vector has line of sight with the specified vector.",
            ReturnType = BooleanType.Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("other", "The vector to determine line of site."),
                new CodeParameter("barriers", "Defines how barriers affect line of sight.", ValueGroupType.GetEnumType<BarrierLOS>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(BarrierLOS.NoBarriersBlock)))
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_IsInLineOfSight>(actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };

        private static FuncMethod Towards = new FuncMethodBuilder() {
            Name = "Towards",
            Documentation = "The displacement vector from the vector to another.",
            ReturnType = Instance,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the displacement towards.") },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_VectorTowards>(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private static FuncMethod AsLocalVector = new FuncMethodBuilder() {
            Name = "AsLocalVector",
            Documentation = "The vector in local coordinates corresponding to the vector in world coordinates.",
            ReturnType = Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("relativePlayer", "The player to whom the resulting vector will be relative."),
                new CodeParameter("transformation", "Specifies whether the vector should receive a rotation and a translation (usually applied to positions) or only a rotation (usually applied to directions and velocities).", ValueGroupType.GetEnumType<Transformation>())
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_LocalVectorOf>(actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };

        private static FuncMethod AsWorldVector = new FuncMethodBuilder() {
            Name = "AsWorldVector",
            Documentation = "The vector in world coordinates corresponding to the vector in local coordinates.",
            ReturnType = Instance,
            Parameters = new CodeParameter[] {
                new CodeParameter("relativePlayer", "The player to whom the resulting vector will be relative."),
                new CodeParameter("transformation", "Specifies whether the vector should receive a rotation and a translation (usually applied to positions) or only a rotation (usually applied to directions and velocities).", ValueGroupType.GetEnumType<Transformation>())
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part<V_WorldVectorOf>(actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };
    }
}