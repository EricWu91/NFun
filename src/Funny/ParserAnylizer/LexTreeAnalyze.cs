using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation;
using Funny.Parsing;

namespace Funny.ParserAnylizer
{
    public static class LexTreeAnlyzer
    {
        public static LexTreeAnalyze Analyze(LexEquation[] lexEquations)
        {
            var vars = SearchVariables(lexEquations);
            return new LexTreeAnalyze()
            {
                AllVariables = vars.Values,
                OrderedEquations = OrderEquationsOrThrow(lexEquations, vars)
            };
        }
        
        public static Dictionary<string, LexVarAnalytics> SearchVariables(LexEquation[] lexEquations)
        {
            var vars = new Dictionary<string, LexVarAnalytics>();
            foreach (var lexEquation in lexEquations)
            {
                vars.Add( lexEquation.Id, new LexVarAnalytics(lexEquation.Id, true));
            }
            for (var i = 0; i < lexEquations.Length; i++)
            {
                var treeEquation = lexEquations[i];
                
                var varNodes = Dfs(treeEquation.Expression, node => node.Is(LexNodeType.Var));
                foreach (var variableNode in  varNodes)
                {
                    if(!vars.ContainsKey(variableNode.Value))
                        vars.Add(variableNode.Value, new LexVarAnalytics(variableNode.Value));

                    vars[variableNode.Value].UsedInOutputs.Add(i);
                }
            }

            return vars;
        }
        public static  IEnumerable<LexNode> Dfs(LexNode node, Predicate<LexNode> condition)
        {
            if (condition(node))
                yield return node;
            foreach (var nodeChild in node.Children)
            {
                foreach (var lexNode in Dfs(nodeChild, condition))
                    yield return lexNode;
            }
        }
        
        public static  LexEquationAnalytics[] OrderEquationsOrThrow(LexEquation[] lexEquations, Dictionary<string,LexVarAnalytics> vars)
        {
            //now build dependencies map
            var result =  new LexEquationAnalytics[lexEquations.Length];
            int[][] dependencyGraph = new int[lexEquations.Length][];

            for (int i = 0; i < lexEquations.Length; i++)
            {
                result[i] = new LexEquationAnalytics();
                if (vars.TryGetValue(lexEquations[i].Id, out var outvar))
                {
                    outvar.IsOutput = true;
                    result[i].UsedInOtherEquations = true;
                    dependencyGraph[i] = outvar.UsedInOutputs.ToArray();
                }
                else
                    dependencyGraph[i] = Array.Empty<int>();
            }

            var sortResults = GraphTools.SortTopology(dependencyGraph);
            if (sortResults.HasCycle)
                throw new ParseException("Cycle dependencies: "
                                         + string.Join(',', sortResults.NodeNames));

            //Equations calculation order
            //applying sort order to Equations
            for (int i = 0; i < sortResults.NodeNames.Length; i++)
            {
                //order is reversed:
                var index =  sortResults.NodeNames[sortResults.NodeNames.Length - i-1];
                var element = lexEquations.ElementAt(index);
                
                result[i].Equation =  element;
            }
            return result;
        }
        
    }

    public class LexTreeAnalyze
    {
        public LexEquationAnalytics[] OrderedEquations;
        public IEnumerable<LexVarAnalytics> AllVariables;
    }
}