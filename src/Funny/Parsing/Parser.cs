using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public class Parser
    {
        private readonly TokenFlow _flow;

        public Parser(TokenFlow flow)
        {
            this._flow = flow;
        }
        public List<LexEquatation> Parse()
        {
            var res = new List<LexEquatation>();

            while (true)
            {
                _flow.SkipNewLines();
                
                if (_flow.IsDone || _flow.IsCurrent(TokType.Eof))
                    return res;

                
                if(!_flow.IsCurrent(TokType.Id))
                    throw new ParseException("Starts with no id");

                var id = _flow.Current.Value;

                _flow.MoveNext();
                _flow.SkipNewLines();

                if(!_flow.IsCurrent(TokType.Def))
                    throw new ParseException("has no =");

                 _flow.MoveNext();
                 _flow.SkipNewLines();

                var exNode = ReadExpression();
                res.Add(new LexEquatation(id, exNode));
            }

        }
        //Чтение атомарного значения (число, id или выражение в скобках)
        private LexNode ReadZeroPriorityOrNull()
        {
            _flow.SkipNewLines();
            //Если минус то читаем дальше и заворачиваем в умножение на 1
            if (_flow.IsCurrent(TokType.Minus))
            {
                if(_flow.IsPrevious(TokType.Minus))
                    throw new ParseException("minus duplicates");
                    
                _flow.MoveNext();
                var nextNode = ReadZeroPriorityOrNull();
                if(nextNode==null)
                    throw new ParseException("minus without next val");
                return LexNode.Op(LexNodeType.Mult, LexNode.Num("-1"),nextNode);
            }
            
            if (MoveIf(TokType.Number, out var val))
                return LexNode.Num(val.Value);;

            if (MoveIf(TokType.Id, out var headToken)){
                
                if (_flow.IsCurrent(TokType.Obr))
                    return ReadFunction(headToken);
                else
                    return LexNode.Var(headToken.Value);
            }
            
            if (_flow.IsCurrent(TokType.Obr))
                return  ReadBrackedExpression();
            if (_flow.IsCurrent(TokType.If))
                return ReadIfElse();
            return null;
        }

        private LexNode ReadIfElse()
        {
            var ifThenNodes = new List<LexNode>();
            do
            {
                MoveIfOrThrow(TokType.If);
                var condition = ReadExpression();
                if(condition==null)
                    throw new ParseException("condition expression is missing");
                MoveIfOrThrow(TokType.Then);
                var thenResult = ReadExpression();
                if(thenResult==null)
                    throw new ParseException("then expression is missing");
                ifThenNodes.Add(LexNode.IfThen(condition, thenResult));
            } while (!_flow.IsCurrent(TokType.Else));
            
            MoveIfOrThrow(TokType.Else);
            var elseResult = ReadExpression();
            if(elseResult == null)
                throw new ParseException("else expression is missing");
            return LexNode.IfElse(ifThenNodes, elseResult);
        }

        private LexNode ReadFunction(Tok id)
        {
            _flow.MoveNext();//skip Obr
            var arguments = new List<LexNode>();
            while (true)
            {
                if (MoveIf(TokType.Cbr, out _))
                    return LexNode.Fun(id.Value, arguments.ToArray());

                if (arguments.Any())
                    MoveIfOrThrow(TokType.Sep,"\",\" or \")\" expected");

                var arg = ReadExpression();
                arguments.Add(arg);
            }
        }

        //Чтение высокоуровневой операции (атомарное значение или их умножение, степень, деление)
        LexNode ReadHiPriorityVal()
        {
            var node = ReadZeroPriorityOrNull();
            if (node == null)
                return null;
            
            _flow.SkipNewLines();
            if (   _flow.IsCurrent(TokType.Mult)
                || _flow.IsCurrent(TokType.Div)
                || _flow.IsCurrent(TokType.Pow)
                || _flow.IsCurrent(TokType.Rema)
                || _flow.IsCurrent(TokType.Equal)
                || _flow.IsCurrent(TokType.NotEqual)
                || _flow.IsCurrent(TokType.More)
                || _flow.IsCurrent(TokType.Less)
                || _flow.IsCurrent(TokType.MoreOrEqual)
                || _flow.IsCurrent(TokType.LessOrEqual))
            {
                var op = _flow.Current;
                _flow.MoveNext();
                var nextVal = ReadHiPriorityVal();
                if (nextVal == null)
                    throw new ParseException($"{_flow.Current.Type} without \'b\' arg");
                return LexNode.Op(op.Type, node, nextVal);
            }
            
            return node;
        }
        
        LexNode ReadExpression()
        {
            var node = ReadHiPriorityVal();
            
            if (  _flow.IsDone 
                  || _flow.IsCurrent(TokType.Eof) 
                  || _flow.IsCurrent(TokType.Cbr))
                return node;

            //Check for next equatation
            var hasNewLine = _flow.SkipNewLines();
            if (hasNewLine && _flow.IsCurrent(TokType.Abc))
                    return node;
            
            
            if (  _flow.IsCurrent(TokType.Plus)
                ||_flow.IsCurrent(TokType.Minus)
                ||_flow.IsCurrent(TokType.And)
                ||_flow.IsCurrent(TokType.Or)
                ||_flow.IsCurrent(TokType.Xor))
            {
                var op = _flow.Current;
                if (node == null)
                    throw new ParseException($"{op.Type} without \'a\' arg");
                _flow.MoveNext();
                var nextVal = ReadExpression();
                if (nextVal == null)
                    throw new ParseException($"{op.Type} without \'b\' arg");
                node = LexNode.Op(op.Type, node, nextVal);
            }
            return node;
        }

        
        private LexNode ReadBrackedExpression()
        {
            _flow.MoveNext();
            
            var node = ReadExpression();

            if (node == null)
                throw new ParseException("No expr. \"" + _flow.Current + "\" instead");
          
            MoveIfOrThrow(TokType.Cbr);
            return node;
        }

        private bool MoveIf(TokType tokType, out Tok tok)
        {
            if (_flow.IsCurrent(tokType))
            {
                tok = _flow.Current;
                _flow.MoveNext();
                return true;
            }
            tok = null;
            return false;
        }
        private void MoveIfOrThrow(TokType tokType)
        {
            if(!_flow.IsCurrent(tokType))
                throw new ParseException($"\"{tokType}\" is missing but was {_flow.Current?.ToString()??"nothing"}");
            _flow.MoveNext();

        }
        private void MoveIfOrThrow(TokType tokType, string error)
        {
            if(!_flow.IsCurrent(tokType))
                throw new ParseException(error);
            _flow.MoveNext();

        }
    }
}