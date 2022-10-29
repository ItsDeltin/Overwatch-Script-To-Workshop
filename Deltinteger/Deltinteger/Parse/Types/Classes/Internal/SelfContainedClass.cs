using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public interface ISelfContainedClass
    {
        string Name { get; }
        MarkupBuilder Documentation { get; }
        void Setup(SetupSelfContainedClass setup);
        void New(ActionSet actionSet, NewClassInfo newClassInfo);
    }

    class SelfContainedType : ISelfContainedClass
    {
        public static SelfContainedType Create(string name, MarkupBuilder documentation, Action<SetupSelfContainedClass> setup) =>
            new SelfContainedType(name, documentation, setup);

        public string Name { get; }
        public MarkupBuilder Documentation { get; }
        readonly Action<SetupSelfContainedClass> setup;

        private SelfContainedType(string name, MarkupBuilder documentation, Action<SetupSelfContainedClass> setup)
        {
            Name = name;
            Documentation = documentation;
            this.setup = setup;
        }

        public void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            throw new NotImplementedException();
        }

        public void Setup(SetupSelfContainedClass setup) => this.setup(setup);

        public SCClassProvider AsClass(DeltinScript deltinScript) => new SCClassProvider(deltinScript, this);

        public SCStructProvider AsStruct(DeltinScript deltinScript) => new SCStructProvider(deltinScript, this);
    }

    public class SetupSelfContainedClass
    {
        public CodeType TypeInstance { get; }

        public IEnumerable<Constructor> Constructors => constructors;

        readonly ISelfContainedTypeMaker typeMaker;
        readonly List<Constructor> constructors = new List<Constructor>();

        public SetupSelfContainedClass(ISelfContainedTypeMaker typeMaker, CodeType typeInstance)
        {
            this.typeMaker = typeMaker;
            TypeInstance = typeInstance;
        }

        /// <summary>Creates an ObjectVariable and adds it to the object scope.</summary>
        public ITypeVariable AddObjectVariable(IVariableInstance variableInstance) => typeMaker.CreateObjectVariable(variableInstance);
        public ITypeVariable AddObjectVariable(string name, CodeType type) => AddObjectVariable(new InternalVar(name, type, StoreType.FullVariable));
        public void AddObjectMethod(FuncMethodBuilder method) => typeMaker.AddObjectMethod(method);
        public void AddStaticMethod(FuncMethodBuilder method) => typeMaker.AddStaticMethod(method);
        public void AddStaticVariable(IVariableInstance variable) => typeMaker.AddStaticVariable(variable);
        public void AddConstructor(Constructor constructor) => constructors.Add(constructor);

        public IWorkshopTree CreateInstanceWithValues(ActionSet actionSet, params IWorkshopTree[] values) => typeMaker.CreateInstanceWithValues(actionSet, values);

        public T Match<T>(Func<T> isClass, Func<T> isStruct) => typeMaker.Match(isClass, isStruct);
    }

    public interface ISelfContainedTypeMaker
    {
        ITypeVariable CreateObjectVariable(IVariableInstance variableInstance);

        void AddStaticMethod(FuncMethodBuilder builder);

        void AddObjectMethod(FuncMethodBuilder builder);

        void AddStaticVariable(IVariableInstance variable);

        IWorkshopTree CreateInstanceWithValues(ActionSet actionSet, params IWorkshopTree[] values);

        T Match<T>(Func<T> isClass, Func<T> isStruct);
    }

    public interface ITypeVariable
    {
        Element Get(ActionSet actionSet);
        Element GetWithReference(ToWorkshop toWorkshop, IWorkshopTree reference);
        void Set(ActionSet actionSet, IWorkshopTree value, params Element[] index);
        void SetWithReference(ActionSet actionSet, IWorkshopTree reference, IWorkshopTree value);
        void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, params Element[] index);
    }

    public class SCClassProvider : ClassInitializer
    {
        public ClassType Instance { get; }
        readonly ISelfContainedClass typeInfo;
        readonly DeltinScript deltinScript;

        public SCClassProvider(DeltinScript deltinScript, ISelfContainedClass typeInfo) : base(typeInfo.Name)
        {
            this.typeInfo = typeInfo;
            this.deltinScript = deltinScript;
            GenericTypes = new AnonymousType[0];
            Instance = (ClassType)GetInstance();
        }

        public override CodeType GetInstance() => new SCClassInstance(deltinScript, typeInfo, this);

        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new SCClassInstance(deltinScript, typeInfo, this);

        class SCClassInstance : ClassType, IGetMeta, ISelfContainedTypeMaker
        {
            readonly ISelfContainedClass typeInfo;
            readonly List<ObjectVariable> objectVariables = new List<ObjectVariable>();

            public SCClassInstance(DeltinScript deltinScript, ISelfContainedClass typeInfo, SCClassProvider provider) : base(typeInfo.Name, provider)
            {
                this.typeInfo = typeInfo;
                ObjectScope = new Scope();
                StaticScope = new Scope();
                deltinScript.StagedInitiation.On(this);
            }

            public void GetMeta()
            {
                var setup = new SetupSelfContainedClass(this, this);
                typeInfo.Setup(setup);
                Constructors = setup.Constructors.ToArray();
            }

            protected override void New(ActionSet actionSet, NewClassInfo classInfo) => typeInfo.New(actionSet, classInfo);

            ITypeVariable ISelfContainedTypeMaker.CreateObjectVariable(IVariableInstance variableInstance)
            {
                this._variables.Add(variableInstance);
                var objectVariable = new ObjectVariable(this, variableInstance);
                objectVariables.Add(objectVariable);
                ObjectScope.AddNativeVariable(variableInstance);
                return new SelfContainedClassVariable(objectVariable);
            }

            IWorkshopTree ISelfContainedTypeMaker.CreateInstanceWithValues(ActionSet actionSet, params IWorkshopTree[] values)
            {
                var reference = Create(actionSet, actionSet.DeltinScript.GetComponent<ClassData>());

                for (int i = 0; i < objectVariables.Count; i++)
                    objectVariables[i].Set(actionSet, values[i]);

                return reference.Get();
            }

            T ISelfContainedTypeMaker.Match<T>(Func<T> isClass, Func<T> isStruct) => isClass();

            void ISelfContainedTypeMaker.AddStaticMethod(FuncMethodBuilder builder)
            {
                StaticScope.AddNativeMethod(builder.GetMethod());
            }

            void ISelfContainedTypeMaker.AddObjectMethod(FuncMethodBuilder builder)
            {
                ObjectScope.AddNativeMethod(builder.GetMethod());
            }

            void ISelfContainedTypeMaker.AddStaticVariable(IVariableInstance variable)
            {
                StaticScope.AddNativeVariable(variable);
            }

            class SelfContainedClassVariable : ITypeVariable
            {
                readonly ObjectVariable objectVariable;
                public SelfContainedClassVariable(ObjectVariable objectVariable) => this.objectVariable = objectVariable;

                public Element Get(ActionSet actionSet) => objectVariable.Get(actionSet);

                public Element GetWithReference(ToWorkshop toWorkshop, IWorkshopTree reference) => objectVariable.Get(toWorkshop, reference);

                public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, params Element[] index) =>
                    objectVariable.Modify(actionSet, operation, value, index);

                public void Set(ActionSet actionSet, IWorkshopTree value, params Element[] index) => objectVariable.Set(actionSet, value, index);

                public void SetWithReference(ActionSet actionSet, IWorkshopTree reference, IWorkshopTree value) => objectVariable.SetWithReference(actionSet, reference, value);
            }
        }
    }

    class SCStructProvider : StructInitializer, ISelfContainedTypeMaker, IGetMeta
    {
        readonly ISelfContainedClass typeInfo;
        readonly DeltinScript deltinScript;
        readonly StructInstance structInstance;

        public SCStructProvider(DeltinScript deltinScript, ISelfContainedClass typeInfo) : base(typeInfo.Name)
        {
            this.typeInfo = typeInfo;
            this.deltinScript = deltinScript;
            GenericTypes = new AnonymousType[0];
            structInstance = GetInstance();
        }

        public override void DependContent() { }
        public override void DependMeta() => deltinScript.StagedInitiation.Meta.Depend(this);

        public override StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker) => new StructInstance(this, typeLinker);

        ITypeVariable ISelfContainedTypeMaker.CreateObjectVariable(IVariableInstance variableInstance)
        {
            Variables.Add(variableInstance.Provider);
            return new SCStructVariable(variableInstance);
        }

        IWorkshopTree ISelfContainedTypeMaker.CreateInstanceWithValues(ActionSet actionSet, params IWorkshopTree[] values)
        {
            var structValues = this.Variables.Zip(values, (var, value) => new { var.Name, value }).ToDictionary(v => v.Name, v => v.value);
            return new LinkedStructAssigner(structValues);
        }

        T ISelfContainedTypeMaker.Match<T>(Func<T> isClass, Func<T> isStruct) => isStruct();

        void ISelfContainedTypeMaker.AddStaticMethod(FuncMethodBuilder builder) => StaticMethods.Add(builder.AsProvider(true));
        void ISelfContainedTypeMaker.AddObjectMethod(FuncMethodBuilder builder) => Methods.Add(builder.AsProvider(false));
        void ISelfContainedTypeMaker.AddStaticVariable(IVariableInstance variable) => StaticVariables.Add(variable.Provider);

        void IGetMeta.GetMeta()
        {
            var setup = new SetupSelfContainedClass(this, structInstance);
            typeInfo.Setup(setup);
        }

        class SCStructVariable : ITypeVariable
        {
            readonly IVariableInstance variable;

            public SCStructVariable(IVariableInstance variable)
            {
                this.variable = variable;
            }

            public Element Get(ActionSet actionSet) => (Element)GetGettable(actionSet).GetVariable();

            public Element GetWithReference(ToWorkshop toWorkshop, IWorkshopTree reference)
            {
                throw new Exception("Cannot get struct variable with reference.");
            }

            public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, params Element[] index)
            {
                GetGettable(actionSet).Modify(actionSet, operation, value, null, index);
            }

            public void Set(ActionSet actionSet, IWorkshopTree value, params Element[] index)
            {
                GetGettable(actionSet).Set(actionSet, value, null, index);
            }

            public void SetWithReference(ActionSet actionSet, IWorkshopTree reference, IWorkshopTree value)
            {
                throw new NotImplementedException("Cannot set struct variable with reference.");
            }

            private IGettable GetGettable(ActionSet actionSet)
            {
                return StructHelper.ExtractStructValue(actionSet.CurrentObject).GetGettable(variable.Name);
            }
        }
    }
}