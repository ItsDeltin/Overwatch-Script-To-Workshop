using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public class BlockTreeScan
    {
        private readonly ParseInfo _parseInfo;
        private readonly string _objectName;
        private readonly DocRange _genericErrorRange;
        public BlockAction Block { get; }
        public ReturnAction[] Returns { get; }
        public bool ReturnsValue { get; protected set; }
        public bool MultiplePaths { get; private set; }
        public CodeType ReturnType { get; private set; }

        public BlockTreeScan(ParseInfo parseInfo, BlockAction block, string objectName, DocRange genericErrorRange)
        {
            _parseInfo = parseInfo;
            _objectName = objectName;
            _genericErrorRange = genericErrorRange;
            Block = block;
            Returns = GetReturns();
            ReturnsValue = Returns.Any(r => r.ReturningValue != null);
        }
        public BlockTreeScan(ParseInfo parseInfo, DefinedMethodProvider method) : this(parseInfo, method.Block, method.Name, method.Context.Identifier.Range)
        {
            ReturnsValue = method.ReturnType != null;
        }

        // Makes sure each return statement returns a value if the method returns a value and that each path returns a value.
        public void ValidateReturns()
        {
            if (ReturnsValue)
            {
                // If there is only one return statement, return the reference to
                // the return statement to reduce the number of actions.
                MultiplePaths = Returns.Length > 1;

                // Syntax error if there are any paths that don't return a value.
                CheckPath(new PathInfo(Block, _genericErrorRange, true));

                // Syntax error if a return statement does not return a value.
                foreach (var ret in Returns)
                    if (ret.ReturningValue == null)
                        _parseInfo.Script.Diagnostics.Error("Must return a value.", ret.ErrorRange);

                // Syntax error if the return type is constant and there are more than one returns.
                if (ReturnType != null && ReturnType.IsConstant() && MultiplePaths)
                    _parseInfo.Script.Diagnostics.Error("Cannot have more than one return statement if the function's return type is constant.", _genericErrorRange);
            }
            else
            {
                // Syntax error on any return statement that returns a value.
                foreach (var ret in Returns)
                    if (ret.ReturningValue != null)
                        _parseInfo.Script.Diagnostics.Error(_objectName + " is void, so no value can be returned.", ret.ErrorRange);
            }
        }

        // Gets all return statements in a method.
        private ReturnAction[] GetReturns()
        {
            List<ReturnAction> returns = new List<ReturnAction>();
            getReturns(returns, Block);
            return returns.ToArray();

            void getReturns(List<ReturnAction> actions, IStatement block)
            {
                // Loop through each statement in the block.
                if (block is BlockAction action)
                {
                    foreach (var statement in action.Statements)
                        // If the current statement is a return statement, add it to the list.
                        if (statement is ReturnAction returnAction)
                        {
                            actions.Add(returnAction);

                            if (returnAction.ReturningValue != null && ReturnType == null)
                                ReturnType = returnAction.ReturningValue.Type();
                        }

                        // If the current statement contains sub-blocks, get the return statements in those blocks recursively.
                        else if (statement is IBlockContainer)
                            foreach (var path in ((IBlockContainer)statement).GetPaths())
                                getReturns(actions, path.Block);
                }
                else if (block is ReturnAction singleReturn)
                {
                    actions.Add(singleReturn);

                    if (singleReturn.ReturningValue != null && ReturnType == null)
                        ReturnType = singleReturn.ReturningValue.Type();
                }
            }
        }
        
        // Makes sure each path returns a value.
        private void CheckPath(PathInfo path)
        {
            bool blockReturns = false;
            // Check the statements backwards.
            if (path.Block is BlockAction action)
                for (int i = action.Statements.Length - 1; i >= 0; i--)
                {
                    if (action.Statements[i] is ReturnAction)
                    {
                        blockReturns = true;
                        break;
                    }
                    
                    if (action.Statements[i] is IBlockContainer)
                    {
                        // If any of the paths in the block container has WillRun set to true,
                        // set blockReturns to true. The responsibility of checking if this
                        // block will run is given to the block container.
                        if (((IBlockContainer)action.Statements[i]).GetPaths().Any(containerPath => containerPath.WillRun))
                            blockReturns = true;

                        CheckContainer((IBlockContainer)action.Statements[i]);
                        break;
                    }
                }
            if (!blockReturns)
                _parseInfo.Script.Diagnostics.Error("Path does not return a value.", path.ErrorRange);
        }

        private void CheckContainer(IBlockContainer container)
        {
            foreach (var path in container.GetPaths()) CheckPath(path);
        }
    }
}