using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using NFun.Interpritation.Nodes;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.ParseErrors
{
    public static class ErrorFactory
    {
        private static readonly string Nl = Environment.NewLine;
        public static Exception UnaryArgumentIsMissing(Tok operatorTok)
            => throw new FunParseException(101, $"{ToString(operatorTok)} ???{Nl} right expression is missed{Nl} Example: {ToString(operatorTok)} a",operatorTok.Start, operatorTok.FinishInString);
        public static Exception MinusDuplicates(Tok previousTok, Tok currentTok)
            => throw new FunParseException(104,$"'--' is not allowed",previousTok.Start, currentTok.FinishInString);
        public static Exception LeftBinaryArgumentIsMissing(Tok token)
            => throw new FunParseException(107,$"expression is missed before '{ToString(token)}'",token.Start, token.FinishInString);
        public static Exception RightBinaryArgumentIsMissing(LexNode leftNode, Tok token)
            => throw new FunParseException(110,$"{ToString(leftNode)} {ToString(token)} ???. Right expression is missed{Nl} Example: {ToString(leftNode)} {ToString(token)} e",token.Start, token.FinishInString);

        public static Exception OperatorIsUnknown(Tok token)
            => throw new FunParseException(113,$"operator '{ToString(token)}' is unknown",token.Start, token.FinishInString);

        public static Exception FunctionNameIsMissedAfterPipeForward(Tok token)
            => throw new FunParseException(116,$"Function name expected after '.'{Nl} Example: [1,2].myFunction()",token.Start, token.FinishInString);

        public static Exception ArrayIndexOrSliceExpected(Tok openBraket)
            => new FunParseException(119,$"Array index or slice expected after '['{Nl} Example: a[1] or a[1:3:2]",openBraket.Start,openBraket.FinishInString);

        public static Exception ArrayIndexExpected(Tok openBraket, Tok closeBracket)
            =>  new FunParseException(122,$"Array index expected inside of '[]'{Nl} Example: a[1]",openBraket.Start,closeBracket.FinishInString);

        public static Exception ArrayInitializeSecondIndexMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.FinishInString;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                
                return new FunParseException(125,
                    $"'[x,..???]. Array hi bound expected but was nothing'{Nl} Example: a[1..2] or a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(128,
                    $"'[x,..???]. Array hi bound expected but was {ToString(missedVal)}'{Nl} Example: a[1..2] or a[1..5..2]", start, finish);
        }
        public static Exception ArrayInitializeStepMissed(Tok openBracket, Tok lastToken, Tok missedVal)
        {
            int start = openBracket.Start;
            int finish = lastToken.FinishInString;
            if (string.IsNullOrWhiteSpace(missedVal?.Value))
                return new FunParseException(131,
                    $"'[x..y..???]. Array step expected but was nothing'{Nl} Example: a[1..5..2]", start,
                    finish);
            else
                return new FunParseException(134,
                    $"'[x..y..???]. Array step expected but was {ToString(missedVal)}'{Nl} Example: a[1..5..2]", start, finish);
        }
        
        public static Exception ArrayIntervalInitializeCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.FinishInString;
            return new FunParseException(137,
                $"{(hasStep?"[x..y..step ???]":"[x..y ???]")}. ']' was missed'{Nl} Example: a[1..5..2]", start,
                finish);
        }
        
        public static Exception ArrayEnumInitializeCbrMissed(Tok openBracket, IList<LexNode> arguments,Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.FinishInString;
            var argumentsStub = CreateArgumentsStub(arguments);
            return new FunParseException(140,
                $"[{argumentsStub} ??? <- ']' or ',' was missed{Nl} Example: [{argumentsStub}]", start,
                finish);
        }

     
        public static Exception ArrayIndexCbrMissed(Tok openBracket, Tok lastToken)
        {
            int start = openBracket.Start;
            int finish = lastToken.FinishInString;
            return new FunParseException(143,
                $"a[x ???. ']' was missed'{Nl} Example: a[1] or a[1:2] or a[1:5:2]", start,
                finish);        
        }
        public static Exception ArraySliceCbrMissed(Tok openBracket, Tok lastToken, bool hasStep)
        {
            int start = openBracket.Start;
            int finish = lastToken.FinishInString;
            return new FunParseException(146,
                $"a{(hasStep?"[x:y:step]":"[x:y]")} <- ']' was missed{Nl} Example: a[1:5:2]", start,
                finish);        
        }
        public static Exception ConditionIsMissing(int conditionStart, Tok flowCurrent)
            => new FunParseException(149,
                $"if ??? then{Nl} Condition expression is missing{Nl} Example: if a>b then ... ", conditionStart, flowCurrent.FinishInString);
        
        public static Exception ThanExpressionIsMissing(int conditionStart, Tok flowCurrent)
            => new FunParseException(152,$"if a then ???.  Expression is missing{Nl} Example: if a then a+1 ", conditionStart, flowCurrent.FinishInString);

        public static Exception ElseKeywordIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException(155,$"if a then b ???.  Else keyword is missing{Nl} Example: if a then b else c ", ifelseStart, flowCurrent.FinishInString);

        public static Exception ElseExpressionIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException(158,$"if a then b else ???.  Else expression is missing{Nl} Example: if a then b else c ", ifelseStart, flowCurrent.FinishInString);

        public static Exception IfKeywordIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException(161,$"if a then b (if) ...  'if' is missing{Nl} Example: if a then b if c then d else c ", ifelseStart, flowCurrent.FinishInString);

        public static Exception ThenKeywordIsMissing(int ifelseStart, Tok flowCurrent)
            => new FunParseException(164,$"if a (then) b  ...  'then' is missing{Nl} Example: if a then b  else c ", ifelseStart, flowCurrent.FinishInString);
        public static Exception FunctionCallObrMissed(int funStart, string name, Tok flowCurrent,LexNode pipedVal)
        {
            if(pipedVal==null)
                return new FunParseException(167,$"{name}( ???. Close bracket ')' is missed{Nl} Example: {name}()", funStart, flowCurrent.FinishInString);

            return new FunParseException(170,$"{ToString(pipedVal)}.{name}( ???. Close bracket ')' is missed{Nl} Example: {ToString(pipedVal)}.{name}() or {name}({ToString(pipedVal)})", funStart, flowCurrent.FinishInString);
        }

        public static Exception FunctionCallCbrOrSeparatorMissed(int funStart, string name,  IList<LexNode> arguments, Tok current,LexNode pipedVal)
        {

            if (pipedVal == null)
            {
                var argumentsStub = CreateArgumentsStub(arguments);
                return new FunParseException(173,$"{name}({argumentsStub} ???. Close bracket ')' is missed{Nl} Example: {name}({argumentsStub})", 
                    funStart,current.FinishInString);
            }

            var pipedArgsStub = CreateArgumentsStub(arguments);
            return new FunParseException(176,$"{ToString(pipedVal)}.{name}({pipedArgsStub} ??? <- ')' or ',' is missed{Nl} Example: {ToString(pipedVal)}.{name}({pipedArgsStub}) or {name}(x,{pipedArgsStub})", 
                funStart, current.FinishInString);
        }


        public static Exception BracketExprCbrOrSeparatorMissed(int start, IList<LexNode> arguments, Tok current)
        {
            var argumentsStub = CreateArgumentsStub(arguments);
            return new FunParseException(179,$"({argumentsStub})<-??? {Nl}Close bracket ')' is missed{Nl} Example: ({argumentsStub})", 
                start,current.FinishInString);
        }

        public static Exception BracketExpressionMissed(int start, IList<LexNode> arguments, Tok current)
        {
             var argumentsStub = CreateArgumentsStub(arguments);
             return new FunParseException(182,$"({argumentsStub}???) {Nl}Expression inside the brackets is missed{Nl} Example: ({argumentsStub})", 
                            start,current.FinishInString);
        }
        private static string CreateArgumentsStub(IList<LexNode> arguments)
        {
            var argumentsStub = string.Join(",", arguments.Select(ToString));
            return argumentsStub;
        }

        private static string ToString(LexNode node)
        {
            switch (node.Type)
            {
                case LexNodeType.Number: return node.Value;
                case LexNodeType.Var: return node.Value;
                case LexNodeType.Fun: return node.Value + "(...)";
                case LexNodeType.IfThen: return "if...then...";
                case LexNodeType.IfThanElse: return "if...then....else...";
                case LexNodeType.Text: return $"\"{(node.Value.Length>20?(node.Value.Substring(17)+"..."):node.Value)}\"";
                case LexNodeType.ArrayInit: return "[...]";
                case LexNodeType.AnonymFun: return "(..)=>..";
                case LexNodeType.TypedVar: return node.Value;
                case LexNodeType.ListOfExpressions: return "(,)";
                case LexNodeType.ProcArrayInit: return "[ .. ]";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Exception FunDefTokenIsMissed(string funName, List<VariableInfo> arguments, Tok actual)
        {
            return new FunParseException(201, $"{funName}({string.Join(",", arguments)}) ??? . '=' def symbol is skipped but was {ToString(actual)}{Nl}Example: {funName}({string.Join(",", arguments)}) = ...", 
                actual.Start, actual.FinishInString);
        }
        public static Exception FunExpressionIsMissed(string funName, List<VariableInfo> arguments, Tok actual) 
            => new FunParseException(204, $"{funName}({string.Join(",", arguments)}) = ??? . Function body is missed {Nl}Example: {funName}({string.Join(",", arguments)}) = #place your body here", 
                actual.Start, actual.FinishInString);

        public static Exception UnknownValueAtStartOfExpression(int exprStart, Tok flowCurrent) 
            => new FunParseException(207,$"Unexpected symbol {ToString(flowCurrent)}. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.FinishInString);
        public static Exception ExpressionBeforeTheDefenition(int exprStart, LexNode expression, Tok flowCurrent)
            => new FunParseException(210,$"Unexpected expression {ToString(expression)} before defenition. Equation, anonymous equation, function or type defenition expected.", exprStart, flowCurrent.FinishInString);

        public static Exception AnonymousExpressionHasToStartFromNewLine(int exprStart, LexNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(213,$"Anonymous equation should start from new line. {Nl}Example : y:int{Nl}y+1 #out = y:int+1", exprStart, flowCurrent.FinishInString);

        public static Exception OnlyOneAnonymousExpressionAllowed(int exprStart, LexNode lexNode, Tok flowCurrent)
            =>   throw new FunParseException(216,$"Only one anonymous equation allowed", exprStart, flowCurrent.FinishInString);
        public static Exception UnexpectedBracketsOnFunDefenition(int start, LexNode headNode, Tok flowCurrent)
            => new FunParseException(219, $"Unexpected brackets on function defenition ({headNode.Value}(...))=... {Nl}Example: {headNode.Value}(...)=...", start,  flowCurrent.StartInString);

        public static Exception WrongFunctionArgumentDefenition(int start, LexNode headNode, LexNode headNodeChild,
            Tok flowCurrent)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var child in headNode.Children)
            {
                if (child == headNodeChild)
                    sb.Append("???");
                else if (child.Is(LexNodeType.Var))
                    sb.Append(child.Value);
                if (headNode.Children.Last() != child)
                    sb.Append(",");
            }
            return new FunParseException(222,
                    $"{headNode.Value}({sb}) = ... {Nl} Function argument is invalid. Variable name (with optional type) expected", 
                 start, flowCurrent.FinishInString);
        }
        public static Exception FunctionArgumentInBracketDefenition(int start, LexNode headNode, LexNode headNodeChild,
            Tok flowCurrent)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var child in headNode.Children)
            {
                if (child == headNodeChild)
                    sb.Append("(???)");
                else if (child.Is(LexNodeType.Var))
                    sb.Append(child.Value);
                if (headNode.Children.Last() != child)
                    sb.Append(",");
            }
            return new FunParseException(225,
                $"{headNode.Value}({sb.ToString()}) = ... {Nl} Function argument is in bracket. Variable name (with optional type) without brackets expected", 
                start, flowCurrent.FinishInString);
        }

        public static Exception VarExpressionIsMissed(int start, string id, Tok flowCurrent)
            => new FunParseException(228, $"{id} = ??? . Equation body is missed {Nl}Example: {id} = {id}+1", 
                start, flowCurrent.FinishInString);
        private static string ToString(Tok tok)
        {
            if (!string.IsNullOrWhiteSpace(tok.Value))
                return tok.Value;
            switch (tok.Type)
            {
                case TokType.If: return "if";
                case TokType.Else:return "else";
                case TokType.Then:return "then";
                case TokType.Plus: return "+";
                case TokType.Minus: return "-";
                case TokType.Div: return "/";
                case TokType.Rema: return "%";
                case TokType.Mult: return "*";
                case TokType.Pow: return "**";
                case TokType.Obr: return "(";
                case TokType.Cbr: return ")";
                case TokType.ArrOBr: return "[";
                case TokType.ArrCBr: return "]";
                case TokType.ArrConcat: return "@";
                case TokType.In: return "in";
                case TokType.BitOr: return "|";
                case TokType.BitAnd: return "&";
                case TokType.BitXor: return "^";
                case TokType.BitShiftLeft: return "<<";
                case TokType.BitShiftRight: return ">>";
                case TokType.BitInverse: return "~";
                case TokType.Def: return "=";
                case TokType.Equal: return "==";
                case TokType.NotEqual: return "!=";
                case TokType.And:return "and";
                case TokType.Or:return "or";
                case TokType.Xor:return "xor";
                case TokType.Not:return "not";
                case TokType.Less:return "<";
                case TokType.More:return ">";
                case TokType.LessOrEqual:return "<=";
                case TokType.MoreOrEqual:return ">=";
                case TokType.Sep:return ",";
                case TokType.True:return "true";
                case TokType.False:return "false";
                case TokType.Colon:return ":";
                case TokType.TwoDots:return "..";
                case TokType.TextType:return "text";
                case TokType.IntType:return "int";
                case TokType.RealType:return "real";
                case TokType.BoolType:return "bool";
                case TokType.AnythingType:return "anything";
                case TokType.PipeForward:return ".";
                case TokType.AnonymFun:return "=>";
                default:
                    return tok.Type.ToString().ToLower();
            }
        }

    }
}