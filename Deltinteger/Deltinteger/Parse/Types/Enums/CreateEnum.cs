#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Parse.Types;

class CreateEnum
{
    public static TypeProvider CreateEnumFromContext(ParseInfo parseInfo, EnumContext enumContext)
    {
        string name = enumContext.Identifier.Text;
        var scope = parseInfo.TranslateInfo.RulesetScope;
        var enumScope = new Scope("enum " + name);
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
                var defaultInstance = metaInitialization.GetDefaultInstance();

                CodeType[][] slots = new CodeType[enumContext.Values.Count][];

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

                            FuncMethod method = new FuncMethodBuilder()
                            {
                                Name = valueName,
                                Parameters = [..valueTypeInformation.Items.Select((type, i) =>
                                    new CodeParameter($"value_{i}", type))],
                                ReturnType = defaultInstance,
                                Action = (actionSet, call) =>
                                {
                                    return GetValueOfSlot(slots, variantValue.GetDefaultValue(actionSet), call.ParameterValues);
                                },
                                Documentation = ""
                            };
                            metaInitialization.AddMethodToStaticScope(method);
                        }
                        else
                        {
                            slots[i] = [];

                            // Proper construction for enum value.
                            var wrappedMemberValue = IVariableDefault.Create(actionSet => GetValueOfSlot(slots, variantValue.GetDefaultValue(actionSet), []));

                            // Create the enum member.
                            var enumMemberVariable = VariableMaker.NewPropertyLike(valueName, defaultInstance, wrappedMemberValue);
                            metaInitialization.AddVariableToStaticScope(enumMemberVariable);
                            staticVariableCollection.AddVariable(enumMemberVariable);
                        }
                    }

                return new(GetAssignerFunction: (type, attributes) => CreateAssignerForSlots(slots, attributes));
            });

            return new(anonymousTypes);
        });
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