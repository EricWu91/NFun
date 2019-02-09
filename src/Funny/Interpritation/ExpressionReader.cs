using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Parsing;
using Funny.Tokenization;

namespace Funny.Interpritation
{
    public class ExpressionReader
    {
        private readonly IEnumerable<LexEquatation> _lexEquatations;
        private readonly Dictionary<string, FunctionBase> _predefinedfunctions;

        private readonly Dictionary<string, VariableExpressionNode> _variables 
            = new Dictionary<string, VariableExpressionNode>();
        
        private readonly Dictionary<string, Equatation> _equatations 
            = new Dictionary<string, Equatation>();
        
        public static Runtime.Runtime Interpritate(
            LexEquatation[] lexEquatations, 
            IEnumerable<FunctionBase> predefinedfunctions)
        {
            var funDic = predefinedfunctions.ToDictionary((f) => f.Name.ToLower());
            
            var ans = new ExpressionReader(lexEquatations, funDic);
            ans.Interpritate();
            var result = OrderEquatationsOrThrow(lexEquatations, ans);
            return new Runtime.Runtime(result,  ans._variables);
        }

        private static Equatation[] OrderEquatationsOrThrow(LexEquatation[] lexEquatations, ExpressionReader ans)
        {
            //now build dependencies map
            int[][] dependencyGraph = new int[lexEquatations.Length][];

            for (int i = 0; i < lexEquatations.Length; i++)
            {
                if (ans._variables.TryGetValue(lexEquatations[i].Id.ToLower(), out var outvar))
                {
                    outvar.IsOutput = true;
                    ans._equatations.Values.ElementAt(i).ReusingWithOtherEquatations = true;
                    
                    dependencyGraph[i] = outvar.usedInOutputs.ToArray();
                }
                else
                    dependencyGraph[i] = Array.Empty<int>();
            }

            var sortResults = GraphTools.SortTopology(dependencyGraph);
            if (sortResults.HasCycle)
                throw new ParseException("Cycle dependencies: "
                                         + string.Join(',', sortResults.NodeNames));

            //Equatations calculation order
            var result = new List<Equatation>(dependencyGraph.Length);
            //applying sort order to equatations
            for (int i = 0; i < sortResults.NodeNames.Length; i++)
            {
                //order is reversed:
                var index =  sortResults.NodeNames[sortResults.NodeNames.Length - i-1];
                var element = ans._equatations.Values.ElementAt(index);
                result.Add(element);
            }
            return result.ToArray();
        }

        private ExpressionReader(IEnumerable<LexEquatation> lexEquatations,
            Dictionary<string, FunctionBase> predefinedfunctions)
        {
            _lexEquatations = lexEquatations;
            _predefinedfunctions = predefinedfunctions;
        }

        private void Interpritate()
        {
            int equatationNum = 0;
            foreach (var equatation in _lexEquatations)
            {
                var expression = ReadNode(equatation.Expression, equatationNum);
                _equatations.Add(equatation.Id.ToLower(), new Equatation
                {
                    Expression = expression,
                    Id = equatation.Id,
                });
                equatationNum++;
            }
        }
        private IExpressionNode ReadNode(LexNode node, int equatationNum)
        {
            if(node.Is(LexNodeType.Var))
                return GetOrAddVariableNode(node.Value, equatationNum);
            if(node.Is(LexNodeType.Fun))
                return GetFunNode(node, equatationNum);
            if(node.Is(LexNodeType.IfThanElse))
                return GetIfThanElseNode(node, equatationNum);
            if(node.Is(LexNodeType.Number))
                return GetValueNode(node);
            if (_ops.ContainsKey(node.Type))
                return GetOpNode(node, equatationNum);
            
            throw new ArgumentException($"Unknown lexnode type {node.Type}");
        }

        private IExpressionNode GetOpNode(LexNode node, int equatationNum)
        {
            var op = _ops[node.Type];
            var left = node.Children.ElementAtOrDefault(0);
            if (left == null)
                throw new ParseException("\"a\" node is missing");

            var right = node.Children.ElementAtOrDefault(1);
            if (right == null)
                throw new ParseException("\"b\" node is missing");

            var leftExpr = ReadNode(left, equatationNum);
            var rightExpr = ReadNode(right, equatationNum);
            
            return new OpExpressionNode(leftExpr, rightExpr, op);
        }

        private IExpressionNode GetIfThanElseNode(LexNode node, int equatationNum)
        {
            var ifNodes = new List<IfCaseExpressionNode>();
            foreach (var ifNode in node.Children.Where(c => c.Is(LexNodeType.IfThen)))
            {
                var condition = ReadNode(ifNode.Children.First(),equatationNum);
                var expr = ReadNode(ifNode.Children.Last(), equatationNum);
                ifNodes.Add(new IfCaseExpressionNode(condition, expr));
            }

            var elseNode = ReadNode(node.Children.Last(), equatationNum);
            return new IfThanElseExpressionNode(ifNodes.ToArray(), elseNode);
        }

        private static IExpressionNode GetValueNode(LexNode node)
        {
            var val = node.Value;
            try
            {
                if (val.Length > 2)
                {
                    val = val.Replace("_", null);

                    if (val[1] == 'b')
                        return new ValueExpressionNode(Convert.ToInt32(val.Substring(2), 2));
                    if (val[1] == 'x')
                        return new ValueExpressionNode(Convert.ToInt32(val, 16));
                }

                if (val.EndsWith('.'))
                    throw new FormatException();
                return new ValueExpressionNode(double.Parse(val));
            }
            catch (FormatException e)
            {
                throw new ParseException("Cannot parse number \"" + node.Value + "\"");
            }
        }

        private static Dictionary<LexNodeType, Func<double, double, double>> _ops =
            new Dictionary<LexNodeType, Func<double, double, double>>()
            {
                    {LexNodeType.Plus,(a, b) => a + b},
                    {LexNodeType.Minus,(a, b) => a - b},
                    {LexNodeType.Div,(a, b) => a / b},
                    {LexNodeType.Mult,(a, b) => a * b},
                    {LexNodeType.Pow,Math.Pow},
                    {LexNodeType.Rema,(a, b) => a % b},
                    {LexNodeType.And,(a, b) => (a != 0 && b != 0) ? 1 : 0},
                    {LexNodeType.Or,(a, b) => (a != 0 || b != 0) ? 1 : 0},        
                    {LexNodeType.Xor,(a, b) => ((a != 0) != (b != 0)) ? 1 : 0},
                    {LexNodeType.Equal, (a, b) => (a == b) ? 1 : 0},                    
                    {LexNodeType.NotEqual,(a, b) => (a != b) ? 1 : 0},                    
                    {LexNodeType.Less,(a, b) => (a < b) ? 1 : 0},                   
                    {LexNodeType.LessOrEqual,(a, b) => (a <= b) ? 1 : 0},                    
                    {LexNodeType.More,(a, b) => (a > b) ? 1 : 0},                    
                    {LexNodeType.MoreOrEqual,(a, b) => (a >= b) ? 1 : 0},                  
            };
        private IExpressionNode GetFunNode(LexNode node, int equatationNum)
        {
            var id = node.Value.ToLower();
            if(!_predefinedfunctions.ContainsKey(id))
                throw new ParseException($"Function \"{id}\" is not defined");
            var fun = _predefinedfunctions[id];
            var children = node.Children.Select(c => ReadNode(c, equatationNum)).ToArray();
            if(children.Length!= fun.ArgsCount)
                throw new ParseException($"Args count of function \"{id}\" is wrong. Expected: {fun.ArgsCount} but was {children.Length}");
            return new FunExpressionNode(fun, children);
        }

        private IExpressionNode GetOrAddVariableNode(string varName, int equatationNum)
        {
            var lower = varName.ToLower();
            VariableExpressionNode res;
            
            if (_variables.ContainsKey(lower))
                res= _variables[lower];
            else {
                res = new VariableExpressionNode(lower);
                _variables.Add(lower, res);            
            }
            res.AddEquatationNum(equatationNum);
            return res;
        }
    }
}