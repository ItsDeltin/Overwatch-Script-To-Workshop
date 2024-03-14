using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class ScriptTypes : ITypeSupplier
    {
        private readonly DeltinScript _deltinScript;
        public List<ICodeTypeInitializer> AllTypes { get; } = new List<ICodeTypeInitializer>();
        public List<ICodeTypeInitializer> DefinedTypes { get; } = new List<ICodeTypeInitializer>();
        private readonly PlayerType _playerType;
        private readonly VectorType _vectorType;
        private readonly NumberType _numberType;
        private readonly StringType _stringType;
        private readonly BooleanType _booleanType;
        private readonly ArrayProvider _arrayProvider;
        private AnyType _anyType;
        private AnyType _unknownType;

        public ScriptTypes(DeltinScript deltinScript)
        {
            _deltinScript = deltinScript;
            _playerType = new PlayerType(deltinScript, this);
            _vectorType = new VectorType(deltinScript, this);
            _numberType = new NumberType(deltinScript, this);
            _stringType = new StringType(deltinScript, this);
            _booleanType = new BooleanType(this);
            _arrayProvider = new ArrayProvider(this);
        }

        public void GetDefaults()
        {
            _anyType = new AnyType(_deltinScript);
            _unknownType = new AnyType("?", true, _deltinScript);
            AddType(_anyType);
            AddType(_playerType);
            AddType(_vectorType);
            AddType(_numberType);
            AddType(_stringType);
            AddType(_booleanType);
            // Enums
            foreach (var type in ValueGroupType.GetEnumTypes(this))
                AddType(type);

            _deltinScript.GetComponent<Pathfinder.PathfinderTypesComponent>();

            _deltinScript.PlayerVariableScope = _playerType.PlayerVariableScope;
        }

        public void AddType(CodeType type) => AllTypes.Add(new GenericCodeTypeInitializer(type));
        public void AddType(ICodeTypeInitializer initializer) => AllTypes.Add(initializer);

        public void AddTypesToScope(Scope scope)
        {
            foreach (var type in AllTypes)
                scope.AddType(type);
        }

        public T GetInstance<T>() where T : CodeType => (T)AllTypes.First(type => type.BuiltInTypeMatches(typeof(T))).GetInstance();
        public CodeType GetInstanceFromInitializer<T>() where T : ICodeTypeInitializer => AllTypes.First(type => type.GetType() == typeof(T)).GetInstance();
        public T GetInitializer<T>() where T : ICodeTypeInitializer => (T)AllTypes.First(type => type.GetType() == typeof(T));

        public CodeType Default() => Any();
        public CodeType Any() => _anyType;
        public CodeType AnyArray() => new ArrayType(this, Any());
        public CodeType Boolean() => _booleanType;
        public CodeType Number() => _numberType;
        public CodeType String() => _stringType;
        public CodeType Player() => _playerType;
        public CodeType Vector() => _vectorType;
        public CodeType Unknown() => _unknownType;
        public ArrayProvider ArrayProvider() => _arrayProvider;

        public CodeType EnumType(string typeName)
        {
            foreach (var type in AllTypes)
                if (type.GetInstance() is ValueGroupType valueGroupType && type.Name == typeName)
                    return valueGroupType;
            throw new Exception("No enum type by the name of '" + typeName + "' exists.");
        }

        public CodeType VectorArray() => new ArrayType(this, Vector());
        public CodeType PlayerArray() => new ArrayType(this, Player());
        public CodeType Players() => new PipeType(Player(), PlayerArray());
        public CodeType PlayerOrVector() => new PipeType(Player(), Vector());
        public CodeType Button() => EnumType("Button");
        public CodeType Hero() => EnumType("Hero");
        public CodeType Color() => EnumType("Color");
        public CodeType Map() => EnumType("Map");
        public CodeType GameMode() => EnumType("GameMode");
        public CodeType Team() => EnumType("Team");
        public CodeType ColorOrTeam() => new PipeType(Color(), Team());
        public CodeType Array(CodeType innerType) => new ArrayType(this, innerType);
        public CodeType PipeType(CodeType a, CodeType b) => new PipeType(a, b);
    }

    public readonly struct ArrayProvider
    {
        public readonly IVariable Length;
        public readonly IVariable First;
        public readonly IVariable Last;
        public readonly AnonymousType TypeParam;

        public ArrayProvider(ITypeSupplier types)
        {
            TypeParam = new AnonymousType("T", new AnonymousTypeAttributes(false));
            Length = VariableMaker.NewUnambiguousPropertyLike("Length", types.Number());
            First = VariableMaker.NewUnambiguousPropertyLike("First", TypeParam);
            Last = VariableMaker.NewUnambiguousPropertyLike("Last", TypeParam);
        }

        /// <summary>Creates instances for the variables in array types.</summary>
        /// <param name="arrayTypeReference">A reference to the owning ArrayType.</param>
        /// <param name="arrayOfType">The type of the array.</param>
        /// <returns>(Length, First, Last)</returns>
        public (IVariableInstance, IVariableInstance, IVariableInstance) GetInstances(ArrayType arrayTypeReference, CodeType arrayOfType)
        {
            var linker = new InstanceAnonymousTypeLinker().Add(TypeParam, arrayOfType);
            return (
                Length.GetInstance(arrayTypeReference, linker),
                First.GetInstance(arrayTypeReference, linker),
                Last.GetInstance(arrayTypeReference, linker)
            );
        }
    }
}
