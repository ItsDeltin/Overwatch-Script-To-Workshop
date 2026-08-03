#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Types;

class CreateEnum
{
    public static TypeProvider CreateEnumFromContext(ParseInfo parseInfo, EnumContext enumContext)
    {
        string name = enumContext.Identifier.GetText();
        var scope = parseInfo.TranslateInfo.RulesetScope.Child();
        var staticVariableCollection = parseInfo.TranslateInfo.GetComponent<StaticVariableCollection>();

        // Create the enum.
        var type = TypeProvider.Create(name, TypeKind.Enum, creator =>
        {
            creator.AddDocumentationFromMetaComment(enumContext.Doc);
            if (enumContext.Identifier is not null)
            {
                creator.CheckForConflict(parseInfo, enumContext.Identifier.Range);
                creator.AddDeclaration(parseInfo, enumContext.Identifier.Range);
            }

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

                // The union type of all given keys within the enum members.
                // Fall back to 'Any' if this remains undecided.
                CodeType? keyType = null;

                // Get the enum members.
                for (int i = 0; i < enumContext.Values.Count; i++)
                    if (enumContext.Values[i].Identifier)
                    {
                        var currentItem = enumContext.Values[i];
                        string valueName = currentItem.Identifier.Text;

                        // Get enum member documentation.
                        var metaComment = ParsedMetaComment.FromMetaComment(currentItem.Doc);
                        var memberDocumentation = metaComment is not null ? new MarkupBuilder(metaComment.Description) : new();

                        // Get the enum value.
                        IVariableDefault variantValue;

                        // If key is provided.
                        if (currentItem.Value is not null)
                        {
                            var keyExpression = parseInfo.GetExpression(scope, currentItem.Value);
                            variantValue = IVariableDefault.FromExpression(keyExpression)!;

                            var memberKeyType = keyExpression.Type();

                            // Ensure that the type of the given key value is assignable to Any.
                            if (!CodeTypeHelpers.IsCompatibleWithAny(memberKeyType))
                                parseInfo.Error("The key of an enum member cannot be a constant or parallel data type", currentItem.Range);
                            else
                                // Unionize with the given key type..
                                keyType = CodeTypeHelpers.UnionWith(memberKeyType, keyType);
                        }
                        else
                        {
                            variantValue = IVariableDefault.FromWorkshopValue(Element.Num(i))!;
                            // At least one enum member uses a number as its key,
                            // so unionize the accumulative key type with 'Number'.
                            keyType = CodeTypeHelpers.UnionWith(parseInfo.Types.Number(), keyType);
                        }

                        // Get the value type information if provided.
                        var valueTypeInformation = currentItem.ValueType is not null
                            ? EnumValueTypeInformation.FromContext(parseInfo, scope, currentItem.ValueType)
                            : null;

                        // Register the inner values of the enum member as a storage item.
                        if (valueTypeInformation is not null)
                            foreach (var item in valueTypeInformation.Items)
                                metaInitialization.AddStorageItem(item);

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
                                        return CreateValueOfEnumMember(enumKind, enumMembers, variantValue.GetDefaultValue(actionSet), call.ParameterValues);
                                    },
                                    Documentation = memberDocumentation
                                }.GetMethod());
                        }
                        else
                        {
                            // Proper construction for enum value.
                            var wrappedMemberValue = IVariableDefault.Create(actionSet => CreateValueOfEnumMember(enumKind, enumMembers, variantValue.GetDefaultValue(actionSet), []));

                            // Create the enum member.
                            var enumMemberVariable = VariableMaker.NewPropertyWithOptions(valueName, defaultInstance, new(wrappedMemberValue, memberDocumentation));
                            metaInitialization.AddStaticVariable(enumMemberVariable);
                            staticVariableCollection.AddVariable(enumMemberVariable);
                        }
                        enumMembers[i] = new(
                            valueName,
                            valueTypeInformation?.Items ?? [],
                            GetKey: variantValue.GetDefaultValue);
                    }

                // Add the 'key' member to instances of this enum if the enum contains inner values.
                IVariable? keyVariable = null;
                if (enumKind is not EnumKind.NoInnerValues)
                {
                    keyVariable = VariableMaker.NewPropertyLike(
                        "Key",
                        keyType ?? parseInfo.Types.Any());
                    metaInitialization.AddObjectVariable(keyVariable);
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
                            // If the enum is structured as an array, this will need array protection!
                            NeedsArrayOperationProtection: enumKind == EnumKind.Single,
                            // Function to add items to the index assigner.
                            AddObjectVariablesToAssigner: (toWorkshop, source, assigner) =>
                            {
                                if (keyVariable is not null)
                                    assigner.Add(keyVariable, KeyOf(enumKind, source.Value));
                            },
                            // Information to retrieve after all types have had a chance to look
                            // at their contents. Stack length is always 1 if not parallel.
                            GetPostMetaInformation: () => new(StackLength: enumKind == EnumKind.Parallel ? GetRequiredSlotCount(enumType.EnumMembers) + 1 : 1)
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
            int slotCount = GetRequiredSlotCount(type.EnumMembers);

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
    static IWorkshopTree CreateValueOfEnumMember(
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

            int enumStackDelta = GetRequiredSlotCount(enumMembers);

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
    static int GetRequiredSlotCount(EnumMember[] enumMembers)
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

    public static IWorkshopTree KeyOf(EnumKind enumKind, IWorkshopTree value)
    {
        return (enumKind, value) switch
        {
            (EnumKind.Single, _) => Element.FirstOf(value),
            (EnumKind.Parallel, IStructValue structValue) => structValue.GetValue("variant"),
            _ => value,
        };
    }
}

class EnumType(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker, EnumKind enumKind) : TypeProvider.TypeInstance(provider, typeLinker)
{
    public EnumMember[] EnumMembers { get; set; } = [];
    public EnumKind EnumKind { get; } = enumKind;

    public IWorkshopTree KeyOf(IWorkshopTree value) => CreateEnum.KeyOf(EnumKind, value);
}

enum EnumKind
{
    NoInnerValues,
    Single,
    Parallel
}

readonly record struct EnumMember(string Name, CodeType[] Items, Func<ActionSet, IWorkshopTree> GetKey);