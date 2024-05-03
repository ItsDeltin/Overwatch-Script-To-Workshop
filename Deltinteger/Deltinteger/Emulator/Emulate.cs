#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Emulator;

public class EmulateScript
{
    readonly EmulateState state;
    readonly EmulateRule[] rules;

    public EmulateScript(IList<Rule> rules, IEmulateLogger logger)
    {
        state = new(logger, rules);
        this.rules = rules.Select(rule => new EmulateRule(state, rule)).ToArray();
    }

    public void TickOne()
    {
        foreach (var rule in rules)
            rule.Tick();
    }

    public EmulateValue GetGlobalVariableValue(string name) => state.GetGlobalVariable(name).Value;
}

public class EmulateState(IEmulateLogger logger, IList<Rule> rules)
{
    public IEmulateLogger Logger = logger;
    readonly IList<Rule> rules = rules;
    readonly EmulateVariableSet globalVariableSet = new();

    public EmulateVariable GetGlobalVariable(string name) => globalVariableSet.GetVariable(name);

    public Rule? RuleFromSubroutineName(string name) => rules.FirstOrDefault(r => r.Subroutine == name);
}

class EmulateRule(EmulateState state, Rule rule)
{
    readonly EmulateState state = state;
    readonly Rule rule = rule;
    readonly Stack<EmulateStack> stack = [];
    bool isExecuting = false;

    public void Tick()
    {
        if (!isExecuting && ConditionsOk())
        {
            // Start rule.
            stack.Push(new EmulateStack(state, rule));
            isExecuting = true;
        }

        while (isExecuting)
        {
            if (stack.TryPeek(out var currentStack))
            {
                var runResult = currentStack.Continue();
                switch (runResult)
                {
                    case ExecutedAction.Completed:
                        stack.Pop();
                        break;
                    case ExecutedAction.Wait: throw new NotImplementedException();
                    case ExecutedAction.CallRule call:
                        var rule = state.RuleFromSubroutineName(call.SubroutineName).Unwrap($"Failed to find rule with subroutine named '{call.SubroutineName}'");
                        stack.Push(new(state, rule));
                        break;
                }
            }
            else
            {
                isExecuting = false;
                break;
            }
        }
    }

    public bool ConditionsOk() => true;
}

class EmulateStack(EmulateState state, Rule rule)
{
    readonly EmulateState state = state;
    readonly Rule rule = rule;
    readonly Stack<IBlockStack> loopStack = [];
    int action = 0;
    int skipCount = 0;

    Element ConsumeAction() => rule.Actions[action++];

    Element CurrentAction() => rule.Actions[action];

    static IWorkshopTree P(Element element, int param) => element.ParameterValues[param];

    public ExecutedAction Continue()
    {
        while (action < rule.Actions.Length)
        {
            var (callRule, abort) = ExecuteAction();

            if (callRule is not null)
                return new ExecutedAction.CallRule(callRule);
            else if (abort)
                break;
        }
        return new ExecutedAction.Completed();
    }

    (string? CallRule, bool Abort) ExecuteAction()
    {
        var act = ConsumeAction();

        if (skipCount > 0)
        {
            skipCount--;
            switch (act.Function.Name)
            {
                case "If":
                case "While":
                case "For Global Variable":
                case "For Player Variable":
                    loopStack.Push(new SkippedBlockStack());
                    break;

                case "End":
                    loopStack.Pop();
                    break;
            }
            return (null, false);
        }

        switch (act.Function.Name)
        {
            case "Set Global Variable":
                {
                    var name = EmulateHelper.ExtractVariableName(P(act, 0)).Unwrap();
                    var variable = state.GetGlobalVariable(name);
                    var value = Evaluate(P(act, 1));
                    variable.Value = value;
                    break;
                }

            case "Set Global Variable At Index":
                {
                    var name = EmulateHelper.ExtractVariableName(P(act, 0)).Unwrap();
                    var variable = state.GetGlobalVariable(name);
                    var index = Evaluate(P(act, 1)).AsNumber();
                    var value = Evaluate(P(act, 2));
                    variable.Modify(var => var.SetAtIndex(index, value));
                    break;
                }

            case "Modify Global Variable":
                {
                    var name = EmulateHelper.ExtractVariableName(P(act, 0)).Unwrap();
                    var operation = EmulateHelper.ExtractOperation(P(act, 1)).Unwrap();
                    var value = Evaluate(P(act, 2));
                    var variable = state.GetGlobalVariable(name);
                    variable.Modify(var => var.Modify(operation, value));
                    break;
                }

            case "Modify Global Variable At Index":
                {
                    var name = EmulateHelper.ExtractVariableName(P(act, 0)).Unwrap();
                    var index = Evaluate(P(act, 1));
                    var operation = EmulateHelper.ExtractOperation(P(act, 2)).Unwrap();
                    var value = Evaluate(P(act, 3));
                    var variable = state.GetGlobalVariable(name);
                    variable.Modify(var => var.ModifyAtIndex(index, operation, value));
                    break;
                }

            case "If":
                ExecuteIf(act);
                break;

            case "Else If":
                {
                    bool tryElseIf = loopStack.TryPeek(out var stack) && stack is IfBlockStack ifStack && ifStack.RunElseBlock;

                    if (tryElseIf)
                    {
                        loopStack.Pop();
                        ExecuteIf(act);
                    }
                    else
                    {
                        ProgressToEndOfBlock(BlockType.IfChain);
                    }
                    break;
                }

            case "Else":
                {
                    bool runElseBlock = loopStack.TryPeek(out var stack) && stack is IfBlockStack ifStack && ifStack.RunElseBlock;
                    if (!runElseBlock)
                        ProgressToEndOfBlock(BlockType.Simple);

                    break;
                }

            case "While":
                ExecuteWhile(act);
                break;

            case "For Global Variable":
                string varName = EmulateHelper.ExtractVariableName(P(act, 0)).Unwrap();
                ExecuteFor(
                    state.GetGlobalVariable(varName),
                    Evaluate(P(act, 1)),
                    Evaluate(P(act, 2)),
                    Evaluate(P(act, 3))
                );
                break;

            case "End":
                {
                    if (loopStack.TryPeek(out var stack) && stack.ReachedEnd())
                        loopStack.Pop();
                    break;
                }

            case "Skip":
                {
                    skipCount = Math.Max((int)Evaluate(P(act, 0)), 0);
                    break;
                }

            case "Skip If":
                {
                    if (Evaluate(P(act, 0)))
                        skipCount = Math.Max((int)Evaluate(P(act, 1)), 0);
                    break;
                }

            case "Small Message" or "Big Message":
                state.Logger.Log(Evaluate(P(act, 1)).ToString());
                break;

            case "Log To Inspector":
                state.Logger.Log(Evaluate(P(act, 0)).ToString());
                break;

            case "Call Subroutine":
                var subroutineName = EmulateHelper.ExtractSubroutineName(P(act, 0)).Unwrap();
                return (subroutineName, false);

            case "Abort":
                return (null, true);

            default:
                throw new Exception("Unhandled action: " + act.Function.Name);
        }

        return (null, false);
    }

    void ExecuteIf(Element ifAction)
    {
        if (Evaluate(ifAction.ParameterValues[0]))
        {
            loopStack.Push(new IfBlockStack(false));
        }
        else
        {
            loopStack.Push(new IfBlockStack(true));
            ProgressToEndOfBlock(BlockType.IfChain);
        }
    }

    void ExecuteFor(EmulateVariable variable, double start, double end, double step)
    {
        var forLoop = new ForBlockStack(this, action, variable, start, end, step);
        forLoop.Start();
        loopStack.Push(forLoop);
    }

    void ExecuteWhile(Element whileAction)
    {
        var whileLoop = new WhileBlockStack(this, action, () => Evaluate(P(whileAction, 0)));
        whileLoop.Start();
        loopStack.Push(whileLoop);
    }

    void ProgressToEndOfBlock(BlockType blockType)
    {
        string currentActionName;
        while (!linkActionsToTypes[blockType].Contains(currentActionName = CurrentAction().Function.Name))
        {
            action++;
            if (balances.TryGetValue(currentActionName, out var recursiveBalanceType))
            {
                ProgressToEndOfBlock(recursiveBalanceType);
            }
        }
    }

    EmulateValue Evaluate(IWorkshopTree value) => EmulateValue.Evaluate(value, state).Unwrap();

    interface IBlockStack
    {
        bool ReachedEnd();
    }

    record IfBlockStack(bool RunElseBlock) : IBlockStack
    {
        public bool ReachedEnd() => true;
    }

    class ForBlockStack(
        EmulateStack stack,
        int forActionIndex,
        EmulateVariable variable,
        double start,
        double end,
        double step) : IBlockStack
    {
        public void Start()
        {
            variable.Value = start;
            if (!CheckCondition())
                stack.ProgressToEndOfBlock(BlockType.Simple);
        }

        public bool ReachedEnd()
        {
            if (CheckCondition())
            {
                stack.action = forActionIndex;
                variable.Value += step;
                return false;
            }
            return true;
        }

        // Todo: how does the workshop handle negative steps?
        bool CheckCondition() => variable.Value.AsNumber() < end;
    }

    class WhileBlockStack(EmulateStack stack, int whileActionIndex, Func<bool> tryCondition) : IBlockStack
    {
        public void Start()
        {
            if (!tryCondition())
            {
                stack.ProgressToEndOfBlock(BlockType.Simple);
            }
        }

        public bool ReachedEnd()
        {
            if (tryCondition())
            {
                stack.action = whileActionIndex;
                return false;
            }
            return true;
        }
    }

    class SkippedBlockStack : IBlockStack
    {
        public bool ReachedEnd() => true;
    }

    public enum BlockType
    {
        IfChain,
        Simple
    }

    public static readonly Dictionary<string, BlockType> balances = new() {
        {"If", BlockType.IfChain},
        {"Else If", BlockType.IfChain},
        {"Else", BlockType.Simple},
        {"While", BlockType.Simple},
        {"For Global Variable", BlockType.Simple},
        {"For Player Variable", BlockType.Simple}
    };

    public static readonly Dictionary<BlockType, string[]> linkActionsToTypes = new() {
        {BlockType.IfChain, ["Else If", "Else", "End"]},
        {BlockType.Simple, ["End"]},
    };
}

abstract record ExecutedAction
{
    public sealed record Completed : ExecutedAction;
    public sealed record Wait : ExecutedAction;
    public sealed record CallRule(string SubroutineName) : ExecutedAction;
}