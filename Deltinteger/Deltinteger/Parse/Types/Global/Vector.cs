using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class VectorType : CodeType, IGetMeta
    {
        private Scope objectScope = new Scope("Vector");
        private Scope staticScope = new Scope("Vector");

        private InternalVar X;
        private InternalVar Y;
        private InternalVar Z;

        private InternalVar HorizontalAngle;
        private InternalVar VerticalAngle;
        private InternalVar Magnitude;

        private InternalVar Zero;

        private readonly ITypeSupplier _typeSupplier;

        public VectorType(DeltinScript deltinScript, ITypeSupplier supplier) : base("Vector")
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            TokenType = SemanticTokenType.Struct;
            _typeSupplier = supplier;

            deltinScript.StagedInitiation.On(this);
        }

        public void GetMeta()
        {
            X = CreateInternalVar("X", "The X component of the vector.", _typeSupplier.Number());
            Y = CreateInternalVar("Y", "The Y component of the vector.", _typeSupplier.Number());
            Z = CreateInternalVar("Z", "The Z component of the vector.", _typeSupplier.Number());
            HorizontalAngle = CreateInternalVar("HorizontalAngle", "The horizontal angle of the vector.", _typeSupplier.Number());
            VerticalAngle = CreateInternalVar("VerticalAngle", "The vertical angle of the vector.", _typeSupplier.Number());
            Magnitude = CreateInternalVar("Magnitude", "The magnitude of the vector.", _typeSupplier.Number());
            Zero = CreateInternalVar("Zero", "Equal to `Vector(0, 0, 0)`.", _typeSupplier.Vector(), Element.Vector(0, 0, 0), true);

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

            Operations.AddTypeOperation(new TypeOperation[] {
                new TypeOperation(TypeOperator.Add, this, this), // Vector + vector
                new TypeOperation(TypeOperator.Subtract, this, this), // Vector - vector
                new TypeOperation(TypeOperator.Multiply, this, this), // Vector * vector
                new TypeOperation(TypeOperator.Divide, this, this), // Vector / vector
                new TypeOperation(TypeOperator.Multiply, _typeSupplier.Number(), this), // Vector * number
                new TypeOperation(TypeOperator.Divide, _typeSupplier.Number(), this), // Vector / number
            });
            Operations.AddTypeOperation(new[] {
                new AssignmentOperation(AssignmentOperator.AddEqual, this), // += vector
                new AssignmentOperation(AssignmentOperator.SubtractEqual, this), // -= vector
                new AssignmentOperation(AssignmentOperator.MultiplyEqual, this), // *= vector
                new AssignmentOperation(AssignmentOperator.DivideEqual, this), // /= vector
                new AssignmentOperation(AssignmentOperator.MultiplyEqual, _typeSupplier.Number()), // *= number
                new AssignmentOperation(AssignmentOperator.DivideEqual, _typeSupplier.Number()) // /= number
            });
        }

        private InternalVar CreateInternalVar(string name, string documentation, CodeType type, bool isStatic = false)
        {
            // Create the variable.
            InternalVar newInternalVar = new InternalVar(name, CompletionItemKind.Property) {
                // IsSettable = false, // Make the variable unsettable.
                Documentation = documentation, // Set the documentation.
                CodeType = type // Set the type.
            };

            // Add the variable to the object scope.
            if (!isStatic) objectScope.AddNativeVariable(newInternalVar);
            // Add the variable to the static scope.
            else staticScope.AddNativeVariable(newInternalVar);

            return newInternalVar;
        }

        private InternalVarValue CreateInternalVar(string name, string documentation, CodeType type, IWorkshopTree value, bool isStatic = false)
        {
            // Create the variable.
            InternalVarValue newInternalVar = new InternalVarValue(name, type, value, CompletionItemKind.Property) {
                // IsSettable = false, // Make the variable unsettable.
                Documentation = documentation // Set the documentation.
            };

            // Add the variable to the object scope.
            if (!isStatic) objectScope.AddNativeVariable(newInternalVar);
            // Add the variable to the static scope.
            else staticScope.AddNativeVariable(newInternalVar);

            return newInternalVar;
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(X, Element.XOf(reference));
            assigner.Add(Y, Element.YOf(reference));
            assigner.Add(Z, Element.ZOf(reference));
            assigner.Add(Magnitude, Element.MagnitudeOf(reference));

            assigner.Add(HorizontalAngle, Element.Part("Horizontal Angle From Direction", reference));
            assigner.Add(VerticalAngle, Element.Part("Vertical Angle From Direction", reference));
        }

        public override Scope GetObjectScope() => objectScope;
        public override Scope ReturningScope() => staticScope;

        public override CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };

        private FuncMethod DistanceTo => new FuncMethodBuilder() {
            Name = "DistanceTo",
            Documentation = "Gets the distance between 2 vectors.",
            ReturnType = _typeSupplier.Number(),
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector or player to get the distance to.", _typeSupplier.PlayerOrVector()) },
            Action = (ActionSet actionSet, MethodCall call) => Element.DistanceBetween(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private FuncMethod CrossProduct => new FuncMethodBuilder() {
            Name = "CrossProduct",
            Documentation = "The cross product of the specified vector.",
            ReturnType = this,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the cross product to.", _typeSupplier.Vector()) },
            Action = (ActionSet actionSet, MethodCall call) => Element.CrossProduct(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private FuncMethod DotProduct => new FuncMethodBuilder() {
            Name = "DotProduct",
            Documentation = "Returns what amount of one vector goes in the direction of another.",
            ReturnType = _typeSupplier.Number(),
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the dot product to.", _typeSupplier.Vector()) },
            Action = (ActionSet actionSet, MethodCall call) => Element.DotProduct(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private FuncMethod Normalize => new FuncMethodBuilder() {
            Name = "Normalize",
            Documentation = "The unit-length normalization of the vector.",
            ReturnType = this,
            Action = (ActionSet actionSet, MethodCall call) => Element.Normalize(actionSet.CurrentObject)
        };

        private FuncMethod DirectionTowards => new FuncMethodBuilder() {
            Name = "DirectionTowards",
            Documentation = "The unit-length direction vector to another vector.",
            ReturnType = this,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the direction towards.", _typeSupplier.Vector()) },
            Action = (ActionSet actionSet, MethodCall call) => Element.DirectionTowards(actionSet.CurrentObject, call.ParameterValues[0])
        };

        private FuncMethod FarthestPlayer => new FuncMethodBuilder() {
            Name = "FarthestPlayer",
            Documentation = "The farthest player from the vector, optionally restricted by team.",
            ReturnType = _typeSupplier.Player(),
            Parameters = new CodeParameter[] { new CodeParameter("team", "The team to get the farthest player with.", _typeSupplier.Team(), new ExpressionOrWorkshopValue(ElementEnumMember.Team(Team.All))) },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Farthest Player From", actionSet.CurrentObject, call.ParameterValues[0])
        };

        private FuncMethod ClosestPlayer => new FuncMethodBuilder() {
            Name = "ClosestPlayer",
            Documentation = "The closest player to the vector, optionally restricted by team.",
            ReturnType = _typeSupplier.Player(),
            Parameters = new CodeParameter[] { new CodeParameter("team", "The team to get the closest player with.", _typeSupplier.Team(), new ExpressionOrWorkshopValue(ElementEnumMember.Team(Team.All))) },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Closest Player To", actionSet.CurrentObject, call.ParameterValues[0])
        };

        private FuncMethod IsInLineOfSight => new FuncMethodBuilder() {
            Name = "IsInLineOfSight",
            Documentation = "Whether the vector has line of sight with the specified vector.",
            ReturnType = _typeSupplier.Boolean(),
            Parameters = new CodeParameter[] {
                new CodeParameter("other", "The vector to determine line of site.", _typeSupplier.Vector()),
                new CodeParameter("barriers", "Defines how barriers affect line of sight.", _typeSupplier.EnumType("BarrierLOS"), new ExpressionOrWorkshopValue(ElementRoot.Instance.GetEnumValue("BarrierLOS", "NoBarriersBlock")))
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Is In Line Of Sight", actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };

        private FuncMethod Towards => new FuncMethodBuilder() {
            Name = "Towards",
            Documentation = "The displacement vector from the vector to another.",
            ReturnType = this,
            Parameters = new CodeParameter[] { new CodeParameter("other", "The vector to get the displacement towards.", _typeSupplier.Vector()) },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Vector Towards", actionSet.CurrentObject, call.ParameterValues[0])
        };

        private FuncMethod AsLocalVector => new FuncMethodBuilder() {
            Name = "AsLocalVector",
            Documentation = "The vector in local coordinates corresponding to the vector in world coordinates.",
            ReturnType = this,
            Parameters = new CodeParameter[] {
                new CodeParameter("relativePlayer", "The player to whom the resulting vector will be relative.", _typeSupplier.Player()),
                new CodeParameter("transformation", "Specifies whether the vector should receive a rotation and a translation (usually applied to positions) or only a rotation (usually applied to directions and velocities).", _typeSupplier.EnumType("Transformation"))
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("Local Vector Of", actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };

        private FuncMethod AsWorldVector => new FuncMethodBuilder() {
            Name = "AsWorldVector",
            Documentation = "The vector in world coordinates corresponding to the vector in local coordinates.",
            ReturnType = this,
            Parameters = new CodeParameter[] {
                new CodeParameter("relativePlayer", "The player to whom the resulting vector will be relative.", _typeSupplier.Player()),
                new CodeParameter("transformation", "Specifies whether the vector should receive a rotation and a translation (usually applied to positions) or only a rotation (usually applied to directions and velocities).", _typeSupplier.EnumType("Transformation"))
            },
            Action = (ActionSet actionSet, MethodCall call) => Element.Part("World Vector Of", actionSet.CurrentObject, call.ParameterValues[0], call.ParameterValues[1])
        };
    }
}