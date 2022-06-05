using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.SyntaxParsing; 

public static class SyntaxNodeFactory {

    public static  ISyntaxNode DefaultValue(Interval interval) => new DefaultValueSyntaxNode(interval);

    public static ISyntaxNode AnonymFunction(ISyntaxNode definition, FunnyType type, ISyntaxNode body)
        => new AnonymFunctionSyntaxNode(
            definition, body, type,
            new Interval(definition.Interval.Start, body.Interval.Finish));

    public static ISyntaxNode SuperAnonymFunction(ISyntaxNode body) => new SuperAnonymFunctionSyntaxNode(body);

    public static ISyntaxNode IfElse(IfCaseSyntaxNode[] ifThenNodes, ISyntaxNode elseResult, int start, int end)
        => new IfThenElseSyntaxNode(ifThenNodes, elseResult, new Interval(start, end));

    public static IfCaseSyntaxNode IfThen(ISyntaxNode condition, ISyntaxNode expression, int start, int end)
        => new(condition, expression, new Interval(start, end));

    public static ISyntaxNode Var(Tok token)
        => new NamedIdSyntaxNode(token.Value, token.Interval);

    public static ISyntaxNode Constant(object value, FunnyType type, Interval interval)
        => new ConstantSyntaxNode(value, type, interval);

    public static ISyntaxNode IntGenericConstant(ulong value, Interval interval)
        => new GenericIntSyntaxNode(value, false, interval);

    public static ISyntaxNode IntGenericConstant(long value, bool isHexOrBin, Interval interval)
        => new GenericIntSyntaxNode(value, isHexOrBin, interval);

    public static ISyntaxNode HexOrBinIntConstant(ulong value, Interval interval)
        => new GenericIntSyntaxNode(value, true, interval);

    public static ISyntaxNode Array(IList<ISyntaxNode> elements, int start, int end)
        => new ArraySyntaxNode(elements, new Interval(start, end));

    public static ISyntaxNode ListOf(IList<ISyntaxNode> elements, Interval interval, bool hasBrackets)
        => new ListOfExpressionsSyntaxNode(elements, hasBrackets, interval);

    public static TypedVarDefSyntaxNode TypedVar(string name, FunnyType type, int start, int end)
        => new(name, type, new Interval(start, end));

    public static ISyntaxNode FunCall(string name, ISyntaxNode[] children, int start, int end)
        => new FunCallSyntaxNode(name, children, new Interval(start, end), false, false);

    public static ISyntaxNode PipedFunCall(string name, ISyntaxNode[] children, int start, int end)
        => new FunCallSyntaxNode(name, children, new Interval(start, end), true, false);

    public static ISyntaxNode OperatorFun(string name, ISyntaxNode[] children, int start, int end)
        => new FunCallSyntaxNode(name, children, new Interval(start, end), false, true);

    public static ISyntaxNode Struct(List<EquationSyntaxNode> equations, Interval interval)
        => new StructInitSyntaxNode(equations, interval);

    public static ISyntaxNode FieldAccess(ISyntaxNode leftNode, Tok memberId) =>
        new StructFieldAccessSyntaxNode(
            leftNode, memberId.Value,
            new Interval(leftNode.Interval.Start, memberId.Finish));

    public static EquationSyntaxNode Equation(Tok idToken, ISyntaxNode body) => new(
        idToken.Value, idToken.Start, body, System.Array.Empty<FunnyAttribute>());
    public static EquationSyntaxNode Equation(string id, ISyntaxNode body, int start, FunnyAttribute[] attributes) => new(
        id, start, body, attributes);

    public static ISyntaxNode ResultFunCall(
        ISyntaxNode functionResultNode, List<ISyntaxNode> arguments, int tokFinish) => new ResultFunCallSyntaxNode(
        functionResultNode, arguments.ToArray(),
        new Interval(functionResultNode.Interval.Start, tokFinish));

    public static ISyntaxNode VarDefinition(TypedVarDefSyntaxNode typed, FunnyAttribute[] attributes) =>
        new VarDefinitionSyntaxNode(typed, attributes);

    public static ISyntaxNode UserFunctionDef(
        List<TypedVarDefSyntaxNode> arguments, FunCallSyntaxNode fun, ISyntaxNode expression, FunnyType outputType) =>
        new UserFunctionDefinitionSyntaxNode(arguments, fun, expression, outputType);
}