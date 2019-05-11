using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public static class TopLevelParser
    {
        public static SyntaxTree Parse(TokenFlow flow)
        {
            var reader = new SyntaxNodeReader(flow);
            var nodes = new List<ISyntaxNode>();
           
            var equationNames = new List<string>();
            while (true)
            {
                flow.SkipNewLines();
                if (flow.IsDone || flow.IsCurrent(TokType.Eof))
                    break;
                VarAttribute[] attributes = new VarAttribute[0];
                if (flow.IsCurrent(TokType.Attribute))
                    attributes = ReadAttributes(flow);

                var startOfTheString = flow.IsStart || flow.IsPrevious(TokType.NewLine);

                var exprStart = flow.Current.Start;
                var e = reader.ReadExpressionOrNull();
                if (e == null)
                    throw ErrorFactory.UnknownValueAtStartOfExpression(exprStart, flow.Current);
                
                if(e is TypedVarDefSyntaxNode typed)
                {
                    
                    //Input typed var specification
                    nodes.Add(
                        new VarDefenitionSyntaxNode(typed, attributes));                        
                }
                else if (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon))
                {
                    if (e is VariableSyntaxNode variable)
                    {
                        //equatation
                        flow.MoveNext();
                        var equation = ReadEquation(flow, reader, variable.Id, attributes);
                        nodes.Add(equation);
                        equationNames.Add(equation.Id);
                    }
                    //Todo Make operator fun as separate node type
                    else if (e is FunCallSyntaxNode fun && !fun.IsOperator)
                    {
                        //fun
                        if (attributes.Any())
                            throw ErrorFactory.AttributeOnFunction(exprStart, fun);
                        nodes.Add(ReadUserFunction(exprStart, fun, flow, reader));
                    }
                    else
                        throw ErrorFactory.ExpressionBeforeTheDefenition(exprStart, e, flow.Current);
                }
                else
                {
                    //anonymous equation
                    if (equationNames.Any())
                    {
                        if (startOfTheString && equationNames[0]=="out")
                            throw ErrorFactory.OnlyOneAnonymousExpressionAllowed(exprStart, e, flow.Current);
                        else
                            throw ErrorFactory.UnexpectedExpression(e);
                    }

                    if(!startOfTheString)
                        throw ErrorFactory.AnonymousExpressionHasToStartFromNewLine(exprStart, e, flow.Current);
                        
                    //todo start
                    //anonymous
                    var equation = new EquationSyntaxNode("out",0, e, attributes);
                    equationNames.Add(equation.Id);
                    nodes.Add(equation);
                }
            }

            var tree = new SyntaxTree(nodes.ToArray());
            tree.ComeOver(new SetNodeNumberVisitor());
            return tree;
        }
        private static VarAttribute[] ReadAttributes(TokenFlow flow)
        {
            bool newLine = flow.IsStart || flow.Previous.Is(TokType.NewLine);
            var ans = new List<VarAttribute>();
            while (flow.IsCurrent(TokType.Attribute))
            {
                if (!newLine)
                    throw ErrorFactory.NowNewLineBeforeAttribute(flow);

                ans.Add(ReadAttribute(flow));
                flow.SkipNewLines();
            }
            return ans.ToArray();
        }
        private static VarAttribute ReadAttribute(TokenFlow flow)
        {
            var start = flow.Current.Start;
            flow.MoveNext();
            if (!flow.MoveIf(TokType.Id, out var id))
                throw ErrorFactory.ItIsNotAnAttribute(start, flow.Current);
            object val = null;
            if (flow.MoveIf(TokType.Obr))
            {
                var next = flow.Current;
                switch (next.Type)
                {
                    case TokType.False:
                        val = false;
                        break;
                    case TokType.True:
                        val = true;
                        break;
                    case TokType.Number:
                        val = TokenHelper.ToNumber(next.Value);
                        break;
                    case TokType.Text:
                        val = next.Value;
                        break;
                    default:
                        throw ErrorFactory.ItIsNotCorrectAttributeValue(next);
                }
                flow.MoveNext();
                if(!flow.MoveIf(TokType.Cbr))                
                    throw ErrorFactory.AttributeCbrMissed(start, flow);
            }
            if(!flow.MoveIf(TokType.NewLine))
                throw ErrorFactory.NowNewLineAfterAttribute(start, flow);

            return new VarAttribute(id.Value, val);
        }

        private static UserFunctionDefenitionSyntaxNode ReadUserFunction(int start, FunCallSyntaxNode headNode, TokenFlow flow, SyntaxNodeReader reader)
        {
            var id = headNode.Value;
            if (headNode.IsInBrackets)
                throw ErrorFactory.UnexpectedBracketsOnFunDefenition( headNode, start,flow.Previous.Finish);

            var arguments = new List<TypedVarDefSyntaxNode>();
            foreach (var headNodeChild in headNode.Args)
            {
                if (headNodeChild is TypedVarDefSyntaxNode varDef)
                    arguments.Add(varDef);
                else if(headNodeChild is VariableSyntaxNode varSyntax)
                    arguments.Add(new TypedVarDefSyntaxNode(varSyntax.Id, VarType.Real, headNodeChild.Interval));
                else    
                    throw ErrorFactory.WrongFunctionArgumentDefenition(start, headNode, headNodeChild, flow.Current);
              
                if(headNodeChild.IsInBrackets)    
                    throw ErrorFactory.FunctionArgumentInBracketDefenition(start, headNode, headNodeChild, flow.Current);
            }

            VarType outputType;
            if (flow.MoveIf(TokType.Colon, out _))
                outputType = flow.ReadVarType();
            else
                outputType = VarType.Real;

            flow.SkipNewLines();
            if (!flow.MoveIf(TokType.Def, out var def))
                throw ErrorFactory.FunDefTokenIsMissed(id, arguments, flow.Current);  

            var expression =reader.ReadExpressionOrNull();
            if (expression == null)
            {

                int finish = flow.Peek?.Finish ?? flow.Position;
                    
                throw ErrorFactory.FunExpressionIsMissed(id, arguments, 
                    new Interval(def.Start, finish));
            }

            return new UserFunctionDefenitionSyntaxNode(arguments, headNode, expression, outputType ); 
        }
        private static EquationSyntaxNode ReadEquation(TokenFlow flow, SyntaxNodeReader reader, string id, VarAttribute[] attributes)
        {
            flow.SkipNewLines();
            var start = flow.Position;
            var exNode = reader.ReadExpressionOrNull();
            if (exNode == null)
                throw ErrorFactory.VarExpressionIsMissed(start, id, flow.Current);
            return new EquationSyntaxNode(id,start, exNode, attributes);
        }
    }
}
