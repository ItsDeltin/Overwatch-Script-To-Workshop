#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Types;

class CreateEnum
{
    public static TypeProvider CreateEnumFromContext(ParseInfo parseInfo, EnumContext enumContext)
    {
        string name = enumContext.Identifier.Text;
        var scope = parseInfo.TranslateInfo.RulesetScope.Child();
        var staticVariableCollection = parseInfo.TranslateInfo.GetComponent<StaticVariableCollection>();

        // Todo: Move this into creator
        parseInfo.TranslateInfo.CheckConflict(parseInfo, new(name), enumContext.Identifier.Range);

        // Create the enum.
        var type = TypeProvider.Create(name, creator =>
        {
            creator.AddDeclaration(parseInfo, enumContext.Identifier.Range);

            // Determine the kind of enum that this is.
            var enumKind = GetEnumKindFromSyntax(enumContext);

            // Get the anonymous types.
            var anonymousTypes = creator.GetAnonymousTypesFromContext(parseInfo, enumContext.Generics);

            // Function to get inner type information,
            creator.AddMetaFunction(parseInfo, metaInitialization =>
            {
                // Add type arguments to the working scope.
                metaInitialization.AddTypeArgumentsToScope(scope);

                // Get an instance of the enum type.
                var defaultInstance = metaInitialization.GetDefaultInstance();

                // 'slots' contains the types used for storage for each enum member.
                // For example, an enum such as `enum { A, B(Number, String) }`:
                // [0] will refer to the types defined for enum member 'A', which will be an empty array.
                // [1] will refer to the types defined for enum member 'B', which will be [Number, String].
                EnumMember[] enumMembers = new EnumMember[enumContext.Values.Count];

                // Get the enum members.
                for (int i = 0; i < enumContext.Values.Count; i++)
                    if (enumContext.Values[i].Identifier)
                    {
                        var currentItem = enumContext.Values[i];
                        string valueName = currentItem.Identifier.Text;

                        // Get the enum value.
                        IVariableDefault variantValue = enumContext.Values[i].Value is not null
                            ? IVariableDefault.FromExpression(parseInfo.GetExpression(scope, enumContext.Values[i].Value))!
                            : IVariableDefault.FromWorkshopValue(Element.Num(i))!;

                        // Get the value type information if provided.
                        var valueTypeInformation = currentItem.ValueType is not null
                            ? EnumValueTypeInformation.FromContext(parseInfo, scope, currentItem.ValueType)
                            : null;

                        // Create a function for the member instead.
                        if (valueTypeInformation is not null)
                        {
                            // The method factory stuff mimics the behaviour of a method provider
                            // without all the boilerplate.
                            metaInitialization.AddStaticMethod(typeLinker =>
                                new FuncMethodBuilder()
                                {
                                    Name = valueName,
                                    Parameters = [..valueTypeInformation.Items.Select((type, i) =>
                                        new CodeParameter($"value_{i}", type.GetRealType(typeLinker)))],
                                    ReturnType = defaultInstance.GetRealType(typeLinker),
                                    Action = (actionSet, call) =>
                                    {
                                        return GetValueOfSlot(enumKind, enumMembers, variantValue.GetDefaultValue(actionSet), call.ParameterValues);
                                    },
                                    Documentation = ""
                                }.GetMethod());
                        }
                        else
                        {
                            // Proper construction for enum value.
                            var wrappedMemberValue = IVariableDefault.Create(actionSet => GetValueOfSlot(enumKind, enumMembers, variantValue.GetDefaultValue(actionSet), []));

                            // Create the enum member.
                            var enumMemberVariable = VariableMaker.NewPropertyLike(valueName, defaultInstance, wrappedMemberValue);
                            metaInitialization.AddStaticVariable(enumMemberVariable);
                            staticVariableCollection.AddVariable(enumMemberVariable);
                        }
                        enumMembers[i] = new(
                            valueName,
                            valueTypeInformation?.Items ?? [],
                            GetKey: variantValue.GetDefaultValue);
                    }

                return new(
                    GetAssignerFunction: (type, attributes, willBeUsedAsArray) => CreateAssignerForEnum(
                        enumKind,
                        (EnumType)type,
                        attributes,
                        willBeUsedAsArray),
                    OnInstanceReady: (type, typeLinker) =>
                    {
                        // The provided factory garantees this is an EnumType.
                        var enumType = (EnumType)type;

                        enumType.EnumMembers = [.. enumMembers.Select(
                            em => new EnumMember(
                                em.Name,
                                [.. em.Items.Select(item => item.GetRealType(typeLinker))],
                                em.GetKey)
                        )];

                        // Return stack information about the enum.
                        return new(
                            // Is this enum a struct (parallel)?
                            IsStruct: enumKind == EnumKind.Parallel,
                            // Stack length is always 1 if not parallel.
                            StackLength: enumKind == EnumKind.Parallel ? GetEnumStackDelta(enumType.EnumMembers) : 1
                        );
                    }
                );
            });

            return new(
                anonymousTypes,
                TypeInstanceFactory: (provider, linker) => new EnumType(provider, linker, enumKind));
        });
        return type;
    }

    static IGettableAssigner CreateAssignerForEnum(
        EnumKind enumKind,
        EnumType type,
        AssigningAttributes attributes,
        bool willBeUsedAsArray)
    {
        // Not parallel.
        if (enumKind is EnumKind.NoInnerValues or EnumKind.Single)
        {
            return new DataTypeAssigner(attributes);
        }
        // Parallel enum.
        else
        {
            int slotCount = GetEnumStackDelta(type.EnumMembers);

            // Create assigner.
            var assigner = new StructAssigner(
                [
                    // First slot is the key of the enum member.
                    new("variant", getAssigner => new DataTypeAssigner(attributes)),
                // Create enough slots to fit any potential enum member.
                ..Enumerable.Range(0, slotCount)
                    .Select(slot =>
                        new StructSlot($"slot{slot}", getAssigner =>
                            new DataTypeAssigner(attributes.StepName($"slot{slot}"))))
                ],
                new(attributes),
                willBeUsedAsArray
            );

            return assigner;
        }
    }

    /// <summary>Generates the workshop value of an enum member.</summary>
    /// <param name="variantValue">The key of the enum member.</param>
    /// <param name="itemValues">The provided values for the enum member's inner values.</param>
    static IWorkshopTree GetValueOfSlot(
        EnumKind enumKind,
        EnumMember[] enumMembers,
        IWorkshopTree variantValue,
        IWorkshopTree[] itemValues)
    {
        // Classic enum.
        if (enumKind is EnumKind.NoInnerValues)
        {
            return variantValue;
        }
        // Single enum w/ values.
        else if (enumKind is EnumKind.Single)
        {
            return Element.CreateArray([
                variantValue,
                ..itemValues.SelectMany(StructHelper.Flatten)
            ]);
        }
        // Parallel enum
        else
        {
            var parallelValues = new Dictionary<string, IWorkshopTree>()
            {
                ["variant"] = variantValue
            };

            int enumStackDelta = GetEnumStackDelta(enumMembers);

            int slot = 0;
            foreach (var slotValue in itemValues.SelectMany(StructHelper.Flatten))
            {
                parallelValues.Add($"slot{slot}", slotValue);
                slot++;
            }

            for (; slot < enumStackDelta; slot++)
                parallelValues.Add($"slot{slot}", Element.Num(0));

            return new LinkedStructValue(parallelValues);
        }
    }

    /// <summary>Gets the number of slots needed to store an enum,
    /// not including the slot required for the key.
    /// Assumes that the enum is parallel.</summary>
    static int GetEnumStackDelta(EnumMember[] enumMembers)
    {
        return enumMembers.Max(enumMember =>
            enumMember.Items.Sum(item =>
                item.GetGettableAssigner(
                    new AssigningAttributes()
                    {
                        StoreType = StoreType.FullVariable
                    }).StackDelta()));
    }

    static EnumKind GetEnumKindFromSyntax(EnumContext syntax)
    {
        // If all of the enum members do not have any inner values, this is a one-slot enum.
        if (syntax.Values.All(member => member.ValueType is null || member.ValueType.Items.Count == 0))
            return EnumKind.NoInnerValues;

        // Otherwise, we use 'single' to determine if this is a 'single' enum or a 'parallel' enum.
        return syntax.Single ? EnumKind.Single : EnumKind.Parallel;
    }

    sealed record EnumValueTypeInformation(CodeType[] Items)
    {
        public static EnumValueTypeInformation FromContext(ParseInfo parseInfo, Scope scope, EnumValueTypeContext context)
        {
            return new([.. context.Items.Select(item => TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, item))]);
        }
    }
}

class EnumType(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker, EnumKind enumKind) : TypeProvider.TypeInstance(provider, typeLinker)
{
    public EnumMember[] EnumMembers { get; set; } = [];
    public EnumKind EnumKind { get; } = enumKind;

    public IWorkshopTree KeyOf(IWorkshopTree value)
    {
        return (EnumKind, value) switch
        {
            (EnumKind.Single, _) => Element.FirstOf(value),
            (EnumKind.Parallel, IStructValue structValue) => structValue.GetValue("variant"),
            _ => value,
        };
    }
}

enum EnumKind
{
    NoInnerValues,
    Single,
    Parallel
}

readonly record struct EnumMember(string Name, CodeType[] Items, Func<ActionSet, IWorkshopTree> GetKey);