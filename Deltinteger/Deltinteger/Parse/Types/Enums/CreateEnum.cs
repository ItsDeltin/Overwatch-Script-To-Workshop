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

            var anonymousTypes = creator.GetAnonymousTypesFromContext(parseInfo, enumContext.Generics);

            // Function to get inner type information,
            creator.AddMetaFunction(parseInfo, metaInitialization =>
            {
                // Add type arguments to the working scope.
                metaInitialization.AddTypeArgumentsToScope(scope);

                // Working instance.
                var defaultInstance = metaInitialization.GetDefaultInstance();

                CodeType[][] slots = new CodeType[enumContext.Values.Count][];
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
                            slots[i] = valueTypeInformation.Items;

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
                                        return GetValueOfSlot(slots, variantValue.GetDefaultValue(actionSet), call.ParameterValues);
                                    },
                                    Documentation = ""
                                }.GetMethod());
                        }
                        else
                        {
                            slots[i] = [];

                            // Proper construction for enum value.
                            var wrappedMemberValue = IVariableDefault.Create(actionSet => GetValueOfSlot(slots, variantValue.GetDefaultValue(actionSet), []));

                            // Create the enum member.
                            var enumMemberVariable = VariableMaker.NewPropertyLike(valueName, defaultInstance, wrappedMemberValue);
                            metaInitialization.AddStaticVariable(enumMemberVariable);
                            staticVariableCollection.AddVariable(enumMemberVariable);
                        }
                        enumMembers[i] = new(
                            valueName,
                            slots[i],
                            GetKey: variantValue.GetDefaultValue);
                    }

                return new(
                    // todo: 'slots' is using the original types in the definition.
                    // will not work properly for type arguments.
                    GetAssignerFunction: (type, attributes) => CreateAssignerForSlots(slots, attributes),
                    OnInstanceReady: (type, typeLinker) =>
                    {
                        ((EnumType)type).EnumMembers = [.. enumMembers.Select(
                            em => new EnumMember(
                                em.Name,
                                [.. em.Items.Select(item => item.GetRealType(typeLinker))],
                                em.GetKey)
                        )];
                    }
                );
            });

            return new(anonymousTypes);
        }, (provider, linker) => new EnumType(provider, linker));
        return type;
    }

    static StructAssigner CreateAssignerForSlots(CodeType[][] slots, AssigningAttributes attributes)
    {
        int slotCount = GetEnumStackDelta(slots);

        // Create assigner.
        var assigner = new StructAssigner(
            [
                new("variant", getAssigner => new DataTypeAssigner(attributes)),
                ..Enumerable.Range(0, slotCount).Select(slot => new StructSlot($"slot{slot}", getAssigner => new DataTypeAssigner(attributes.StepName($"slot{slot}"))))
            ],
            new(attributes),
            false
        );

        return assigner;
    }

    static LinkedStructValue GetValueOfSlot(CodeType[][] slots, IWorkshopTree variantValue, IWorkshopTree[] itemValues)
    {
        var parallelValues = new Dictionary<string, IWorkshopTree>()
        {
            ["variant"] = variantValue
        };

        int enumStackDelta = GetEnumStackDelta(slots);

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

    static int GetEnumStackDelta(CodeType[][] slots)
    {
        return slots.Max(memberItems => memberItems.Sum(item => item.GetGettableAssigner(new AssigningAttributes() { StoreType = StoreType.FullVariable }).StackDelta()));
    }

    sealed record EnumValueTypeInformation(CodeType[] Items)
    {
        public static EnumValueTypeInformation FromContext(ParseInfo parseInfo, Scope scope, EnumValueTypeContext context)
        {
            return new([.. context.Items.Select(item => TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, item))]);
        }
    }
}

class EnumType(TypeProvider provider, InstanceAnonymousTypeLinker typeLinker) : TypeProvider.TypeInstance(provider, typeLinker)
{
    public EnumMember[] EnumMembers { get; set; } = [];

    public IWorkshopTree KeyOf(IWorkshopTree value)
    {
        if (value is IStructValue asStructValue)
        {
            return asStructValue.GetValue("variant");
        }
        throw new NotImplementedException();
    }
}

readonly record struct EnumMember(string Name, CodeType[] Items, Func<ActionSet, IWorkshopTree> GetKey);