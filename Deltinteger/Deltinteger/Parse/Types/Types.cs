using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse
{
    public abstract class CodeType : IExpression, ICallable
    {
        public string Name { get; }
        public Constructor[] Constructors { get; protected set; } = new Constructor[0];
        public string Description { get; protected set; }

        public CodeType(string name)
        {
            Name = name;
        }

        // Static
        public abstract Scope ReturningScope();
        // Object
        public virtual Scope GetObjectScope() => null;

        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => null;

        /// <summary>
        /// Determines if variables with this type can have their value changed.
        /// </summary>
        public virtual TypeSettable Constant() => TypeSettable.Normal;

        public virtual IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
        {
            // Classes that can't be created shouldn't have constructors.
            throw new NotImplementedException();
        }

        public virtual IndexReference GetObjectSource(DeltinScript translateInfo, IWorkshopTree element) => null;

        public virtual void AddStaticVariablesToAssigner(DeltinScript translateInfo, VarIndexAssigner assigner) {}
        public virtual void AddObjectVariablesToAssigner(IWorkshopTree source, DeltinScript translateInfo, VarIndexAssigner assigner) {}

        public virtual void Call(ScriptFile script, DocRange callRange)
        {
            if (Description != null)
                script.AddHover(callRange, Description);
        }

        public abstract CompletionItem GetCompletion();

        public static CodeType GetCodeTypeFromContext(ParseInfo parseInfo, DeltinScriptParser.Code_typeContext typeContext)
        {
            if (typeContext == null) return null;

            CodeType type = null;
            if (typeContext.PART() != null)
                parseInfo.TranslateInfo.GetCodeType(typeContext.PART().GetText(), parseInfo.Script.Diagnostics, DocRange.GetRange(typeContext));

            if (type != null)
                type.Call(parseInfo.Script, DocRange.GetRange(typeContext));

            if (typeContext.INDEX_START() != null)
                for (int i = 0; i < typeContext.INDEX_START().Length; i++)
                    type = new ArrayType(type);
            
            return type;
        }

        public static bool TypeMatches(CodeType parameterType, CodeType valueType)
        {
            return parameterType == null || parameterType.Name == valueType?.Name;
        }

        static List<CodeType> _defaultTypes;
        public static List<CodeType> DefaultTypes {
            get {
                if (_defaultTypes == null) GetDefaultTypes();
                return _defaultTypes;
            }
        }
        private static void GetDefaultTypes()
        {
            _defaultTypes = new List<CodeType>();
            foreach (var enumData in EnumData.GetEnumData())
                _defaultTypes.Add(new WorkshopEnumType(enumData));
            
            // Add custom classes here.
            _defaultTypes.Add(new Pathfinder.PathmapClass());
            _defaultTypes.Add(new Models.AssetClass());
        }
    }

    public enum TypeSettable
    {
        Normal, Convertable, Constant
    }

    public class ArrayType : CodeType
    {
        public CodeType ArrayOfType { get; }
        public Scope ObjectScope { get; }
        private InternalVar Length { get; } = new InternalVar("Length", CompletionItemKind.Property) {
            Detail = "Length",
            Documentation = "Gets the length of the array."
        };
        private InternalVar First { get; } = new InternalVar("First", CompletionItemKind.Property) {
            Detail = "First",
            Documentation = "Gets the first element in the array."
        };
        private InternalVar Last { get; } = new InternalVar("Last", CompletionItemKind.Property) {
            Detail = "Last",
            Documentation = "Gets the last element in the array."
        };

        public ArrayType(CodeType arrayOfType) : base((arrayOfType?.Name ?? "define") + "[]")
        {
            ArrayOfType = arrayOfType;
            ObjectScope = new Scope(Name);
            ObjectScope.AddNativeVariable(Length);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree source, DeltinScript translateInfo, VarIndexAssigner assigner)
        {
            assigner.Add(Length, Element.Part<V_CountOf>(source));
            assigner.Add(First, Element.Part<V_FirstOf>(source));
            assigner.Add(Last, Element.Part<V_LastOf>(source));
        }

        public override Scope GetObjectScope() => ObjectScope;
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => throw new NotImplementedException();
    }
}