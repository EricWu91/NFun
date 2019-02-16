using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public static class Parser
    {
        public static LexTree Parse(TokenFlow flow)
        {
            var reader = new LexNodeReader(flow);
            var equations = new List<LexEquation>();
            var funs = new List<LexFunction>();
            while (true)
            {
                flow.SkipNewLines();

                if (flow.IsDone || flow.IsCurrent(TokType.Eof))
                    break;

                var id = reader.MoveIfOrThrow(TokType.Id).Value;
                flow.SkipNewLines();
                
                if (flow.IsCurrent(TokType.Def))
                {
                    flow.MoveNext();
                    equations.Add(ReadEquation(flow, reader, id));
                }
                else if (flow.IsCurrent(TokType.Obr))
                {
                    flow.MoveNext();
                    funs.Add(ReadUserFunction(flow, reader, id));
                }
                else
                    throw new ParseException("has no =");
            }

            return new LexTree
            {
                UserFuns = funs.ToArray(),
                Equations = equations.ToArray()
            };
        }

        private static LexFunction ReadUserFunction(TokenFlow flow, LexNodeReader reader, string id)
        {
            var arguments = new List<string>();
            while (true)
            {
                if (reader.MoveIf(TokType.Cbr, out _))
                    break;
                if (arguments.Any())
                    reader.MoveIfOrThrow(TokType.Sep, "\",\" or \")\" expected");
                var argId = reader.MoveIfOrThrow(TokType.Id, "Argument name expected");
                arguments.Add(argId.Value);
            }
            flow.SkipNewLines();
            reader.MoveIfOrThrow(TokType.Def, "\'=\' expected");
            var expression =reader.ReadExpressionOrNull();
            if(expression==null)
                throw new ParseException("Function contains no body");
            return new LexFunction{Args = arguments.ToArray(), Id= id, Node = expression};
        }


        private static LexEquation ReadEquation(TokenFlow flow, LexNodeReader reader, string id)
        {
            flow.SkipNewLines();

            var exNode = reader.ReadExpressionOrNull();
            return new LexEquation(id, exNode);
        }
    }
}
