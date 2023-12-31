namespace Deltin.Deltinteger.Compiler.SyntaxTree;

record class WorkshopRuleContent(IWorkshopExpression[] InnerItems);

interface IWorkshopRuleElement { }

record class WorkshopRuleEvent : IWorkshopRuleElement;

record class WorkshopRuleConditions(IWorkshopExpression[] Conditions) : IWorkshopRuleElement;

record class WorkshopRuleActions(IWorkshopExpression[] Actions) : IWorkshopRuleElement;

interface IWorkshopExpression { }