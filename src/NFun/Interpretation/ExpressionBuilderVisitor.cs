using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpretation {

internal sealed class ExpressionBuilderVisitor : ISyntaxNodeVisitor<IExpressionNode> {
    private readonly IFunctionDictionary _functions;
    private readonly VariableDictionary _variables;
    private readonly TypeInferenceResults _typeInferenceResults;
    private readonly TicTypesConverter _typesConverter;
    private readonly DialectSettings _dialect;

    private static IExpressionNode BuildExpression(
        ISyntaxNode node,
        IFunctionDictionary functions,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect) =>
        node.Accept(
            new ExpressionBuilderVisitor(
                functions, variables, typeInferenceResults, typesConverter,
                dialect));

    internal static IExpressionNode BuildExpression(
        ISyntaxNode node,
        IFunctionDictionary functions,
        FunnyType outputType,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect) {
        var result = node.Accept(
            new ExpressionBuilderVisitor(functions, variables, typeInferenceResults, typesConverter, dialect));
        if (result.Type == outputType)
            return result;
        var converter = VarTypeConverter.GetConverterOrThrow(dialect.TypeBehaviour, result.Type, outputType, node.Interval);

        return new CastExpressionNode(result, outputType, converter, node.Interval);
    }

    private ExpressionBuilderVisitor(
        IFunctionDictionary functions,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect) {
        _dialect = dialect;
        _functions = functions;
        _variables = variables;
        _typeInferenceResults = typeInferenceResults;
        _typesConverter = typesConverter;
        _dialect = dialect;
    }

    public IExpressionNode Visit(SuperAnonymFunctionSyntaxNode arrowAnonymFunNode) {
        var outputTypeFunDefinition = arrowAnonymFunNode.OutputType.FunTypeSpecification;
        if (outputTypeFunDefinition == null)
            throw new NFunImpossibleException("Fun definition expected");
        string[] argNames;
        if (outputTypeFunDefinition.Inputs.Length == 1)
            argNames = new[] { "it" };
        else
        {
            argNames = new string[outputTypeFunDefinition.Inputs.Length];
            for (int i = 0; i < outputTypeFunDefinition.Inputs.Length; i++)
            {
                argNames[i] = $"it{i + 1}";
            }
        }

        //Prepare local variable scope
        //Capture all outerscope variables
        var localVariables = new VariableDictionary(_dialect.TypeBehaviour, _variables.GetAllSources());

        var arguments = new VariableSource[argNames.Length];
        for (var i = 0; i < argNames.Length; i++)
        {
            var arg = argNames[i];
            var type = outputTypeFunDefinition.Inputs[i];
            var source = VariableSource.CreateWithoutStrictTypeLabel(arg, type, FunnyVarAccess.Input, _dialect.TypeBehaviour);
            //collect argument
            arguments[i] = source;
            //add argument to local scope
            //if argument with it* name already exist - replace it
            localVariables.AddOrReplace(source);
        }

        var body = arrowAnonymFunNode.Body;
        return BuildAnonymousFunction(arrowAnonymFunNode.Interval, body, localVariables, arguments);
    }

    public IExpressionNode Visit(StructFieldAccessSyntaxNode node) {
        var structNode = ReadNode(node.Source);
        //Funtic allows default values for not specified types 
        // so call:
        //  y = {}.missingField
        // is allowed, but it semantically incorrect

        if (!structNode.Type.StructTypeSpecification.ContainsKey(node.FieldName))
            throw Errors.FieldNotExists(node.FieldName, node.Interval);
        
        return new StructFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
    }

    public IExpressionNode Visit(StructInitSyntaxNode node) {
        var types = new Dictionary<string, FunnyType>(node.Fields.Count, FunnyType.StructKeyComparer);
        var names = new string[node.Fields.Count];
        var nodes = new IExpressionNode[node.Fields.Count];

        for (int i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            nodes[i] = ReadNode(field.Node);
            names[i] = field.Name;
            types.Add(field.Name, field.Node.OutputType);
        }

        foreach (var (key, _) in node.OutputType.StructTypeSpecification)
        {
            if (!types.ContainsKey(key))
                throw Errors.FieldIsMissed(key, node.Interval);
        }

        return new StructInitExpressionNode(names, nodes, node.Interval, FunnyType.StructOf(types));
    }
    public IExpressionNode Visit(DefaultValueSyntaxNode arrowAnonymFunNode) => 
        new DefaultValueExpressionNode(
            arrowAnonymFunNode.OutputType.GetDefaultFunnyValue(), 
            arrowAnonymFunNode.OutputType, 
            arrowAnonymFunNode.Interval);

    public IExpressionNode Visit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode) {
        arrowAnonymFunNode.Definition.IfNullThrow($"{nameof(arrowAnonymFunNode.Definition)} is missing");
        arrowAnonymFunNode.Body.IfNullThrow($"{nameof(arrowAnonymFunNode.Body)} is missing");

        //Anonym fun arguments list
        var argumentLexNodes = arrowAnonymFunNode.ArgumentsDefinition;

        //Prepare local variable scope
        //Capture all outerscope variables
        var localVariables = new VariableDictionary(_dialect.TypeBehaviour, _variables.GetAllSources());

        var arguments = new VariableSource[argumentLexNodes.Length];
        var argIndex = 0;
        foreach (var arg in argumentLexNodes)
        {
            //Convert argument node
            var varNode = FunArgumentExpressionNode.CreateWith(arg);
            var source = VariableSource.CreateWithStrictTypeLabel(
                varNode.Name, varNode.Type, arg.Interval,
                FunnyVarAccess.Input,
                _dialect.TypeBehaviour);
            //collect argument
            arguments[argIndex] = source;
            argIndex++;
            //add argument to local scope
            if (!localVariables.TryAdd(source))
            {
                //Check for duplicated arg-names

                //If outer-scope contains the conflict variable name
                if (_variables.GetSourceOrNull(varNode.Name) != null)
                    throw Errors.AnonymousFunctionArgumentConflictsWithOuterScope(
                        varNode.Name,
                        arrowAnonymFunNode.Interval);
                else //else it is duplicated arg name
                    throw Errors.AnonymousFunctionArgumentDuplicates(varNode, arrowAnonymFunNode.Definition);
            }
        }

        var body = arrowAnonymFunNode.Body;
        return BuildAnonymousFunction(arrowAnonymFunNode.Interval, body, localVariables, arguments);
    }

    public IExpressionNode Visit(ArraySyntaxNode node) {
        var elements = new IExpressionNode[node.Expressions.Count];
        var expectedElementType = node.OutputType.ArrayTypeSpecification.FunnyType;
        for (int i = 0; i < node.Expressions.Count; i++)
        {
            var elementNode = ReadNode(node.Expressions[i]);
            elements[i] = CastExpressionNode.GetConvertedOrOriginOrThrow(elementNode, expectedElementType, _dialect.TypeBehaviour);
        }

        return new ArrayExpressionNode(elements, node.Interval, node.OutputType);
    }

    public IExpressionNode Visit(FunCallSyntaxNode node) {
        var id = node.Id;

        var someFunc = node.FunctionSignature ?? _functions.GetOrNull(id, node.Args.Length);

        if (someFunc is null)
        {
            //todo move to variable syntax node
            //hi order function
            var functionalVariableSource = _variables.GetSourceOrNull(id);
            if (functionalVariableSource?.Type.FunTypeSpecification == null)
                throw Errors.FunctionNotFoundForHiOrderUsage(node, _functions);
            return CreateFunctionCall(node, ConcreteHiOrderFunction.Create(functionalVariableSource));
        }

        if (someFunc is IConcreteFunction f) //concrete function
            return CreateFunctionCall(node, f);

        
        if (someFunc is IGenericFunction genericFunction) //generic function
        {
            FunnyType[] genericArgs;
            // Generic function type arguments usually stored in tic results
            var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
            if (genericTypes == null)
            {
                // Generic call arguments are unknown  in case of generic recursion function . 
                // Take them from type inference results
                var recCallSignature = _typeInferenceResults.GetRecursiveCallOrNull(node.OrderNumber);
                //if generic call arguments not exist in type inference result - it is NFUN core error
                recCallSignature.IfNullThrow($"MJ78. Function {id}`{node.Args.Length} was not found");

                var varTypeCallSignature = _typesConverter.Convert(recCallSignature);
                //Calculate generic call arguments by concrete function signature
                genericArgs = genericFunction.CalcGenericArgTypeList(varTypeCallSignature.FunTypeSpecification);
            }
            else
            {
                genericArgs = new FunnyType[genericTypes.Length];
                for (int i = 0; i < genericTypes.Length; i++)
                    genericArgs[i] = _typesConverter.Convert(genericTypes[i]);
            }

            var function = genericFunction.CreateConcrete(genericArgs, _dialect.TypeBehaviour);
            return CreateFunctionCall(node, function);
        }
        
        throw new NFunImpossibleException($"MJ101. Function {id}`{node.Args.Length} type is unknown");
    }

    public IExpressionNode Visit(ResultFunCallSyntaxNode node) {
        var functionGenerator = ReadNode(node.ResultExpression);
        var function = ConcreteHiOrderFunctionWithSyntaxNode.Create(functionGenerator);
        return CreateFunctionCall(node, function);
    }


    public IExpressionNode Visit(IfThenElseSyntaxNode node) {
        if (_dialect.IfExpressionSetup == IfExpressionSetup.Deny)
            throw Errors.IfElseExpressionIsDenied(node.Interval);

        //expressions
        //if (...) {here} 
        var expressionNodes = new IExpressionNode[node.Ifs.Length];
        //conditions
        // if ( {here} ) ...
        var conditionNodes = new IExpressionNode[node.Ifs.Length];

        if (_dialect.IfExpressionSetup == IfExpressionSetup.IfElseIf && node.Ifs.Length > 1)
            throw Errors.ElseKeywordIsMissing(node.Interval.Start, node.Ifs[1].Interval.Start);

        for (int i = 0; i < expressionNodes.Length; i++)
        {
            var ifCaseNode = node.Ifs[i];

            conditionNodes[i] = ReadNode(ifCaseNode.Condition);
            var exprNode = ReadNode(ifCaseNode.Expression);
            expressionNodes[i] = CastExpressionNode.GetConvertedOrOriginOrThrow(exprNode, node.OutputType, _dialect.TypeBehaviour);
        }

        var elseNode = CastExpressionNode.GetConvertedOrOriginOrThrow(ReadNode(node.ElseExpr), node.OutputType, _dialect.TypeBehaviour);

        return new IfElseExpressionNode(
            expressionNodes, conditionNodes, elseNode,
            node.Interval, node.OutputType);
    }

    public IExpressionNode Visit(ConstantSyntaxNode node) {
        var (enode, type) = GetConstantNodeOrNull(node.Value, node);
        return enode ?? new ConstantExpressionNode(node.Value, type, node.Interval);
    }

    public IExpressionNode Visit(GenericIntSyntaxNode node) {
        var (enode, _) = GetConstantNodeOrNull(node.Value, node);
        enode.IfNullThrow($"Generic syntax node has wrong value type: {node.Value.GetType().Name}");
        return enode;
    }

    private (IExpressionNode, FunnyType) GetConstantNodeOrNull(object value, ISyntaxNode node) {
        var type = _typesConverter.Convert(_typeInferenceResults.GetSyntaxNodeTypeOrNull(node.OrderNumber));
        return (value switch {
                    long l  => ConstantExpressionNode.CreateConcrete(type, l, _dialect.TypeBehaviour, node.Interval),
                    ulong u => ConstantExpressionNode.CreateConcrete(type, u, _dialect.TypeBehaviour, node.Interval),
                    string d => new ConstantExpressionNode(
                        _dialect.TypeBehaviour.ParseOrNull(d) ?? throw Errors.CannotParseDecimalNumber(node.Interval), type, node.Interval),
                    _ => null
                }, type);
    }

    public IExpressionNode Visit(NamedIdSyntaxNode node) {
        if (node.IdType == NamedIdNodeType.Constant)
        {
            var varVal = (ConstantValueAndType)node.IdContent;
            return new ConstantExpressionNode(varVal.FunnyValue, varVal.Type, node.Interval);
        }

        var funVariable = _typeInferenceResults.GetFunctionalVariableOrNull(node.OrderNumber);
        if (funVariable != null)
        {
            if (funVariable is IGenericFunction genericFunction)
            {
                var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
                genericTypes.IfNullThrow($"MJ79. Generic function is missed at {node.OrderNumber}:  {node.Id}`{genericFunction.Name} ");

                var genericArgs = new FunnyType[genericTypes.Length];
                for (int i = 0; i < genericTypes.Length; i++)
                    genericArgs[i] = _typesConverter.Convert(genericTypes[i]);

                var function = genericFunction.CreateConcrete(genericArgs, _dialect.TypeBehaviour);
                return new FunVariableExpressionNode(function, node.Interval);
            }
            else if (funVariable is IConcreteFunction concrete)
                return new FunVariableExpressionNode(concrete, node.Interval);
        }

        var vType = node.VariableType.BaseType == BaseFunnyType.Empty
            ? node.OutputType
            : node.VariableType;
        
        var node1 = _variables.CreateVarNode(node.Id, node.Interval, vType);
        if (node1.Source.Name != node.Id)
            throw Errors.InputNameWithDifferentCase(node.Id, node1.Source.Name, node.Interval);
        return node1;
    }


    #region not an expression

    public IExpressionNode Visit(EquationSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(IfCaseSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(ListOfExpressionsSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(SyntaxTree node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(TypedVarDefSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(UserFunctionDefinitionSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(VarDefinitionSyntaxNode node)
        => ThrowNotAnExpression(node);

    #endregion


    private IExpressionNode BuildAnonymousFunction(
        Interval interval, ISyntaxNode body,
        VariableDictionary localVariables, VariableSource[] arguments) {
        var sources = localVariables.GetAllSources().ToArray();
        var originVariables = new string[sources.Length];
        for (int i = 0; i < originVariables.Length; i++) originVariables[i] = sources[i].Name;

        var expr = BuildExpression(
            body, _functions, localVariables, _typeInferenceResults, _typesConverter,
            _dialect);

        //New variables are new closured
        var closured = localVariables.GetAllUsages()
                                     .Where(s => !originVariables.Contains(s.Source.Name))
                                     .ToList();

        var wrongItVariable = closured.FirstOrDefault(c => Helper.DoesItLooksLikeSuperAnonymousVariable(c.Source.Name));
        if (wrongItVariable!=null)
            throw Errors.CannotUseSuperAnonymousVariableHere(wrongItVariable.Usages.First().Interval);

        //Add closured vars to outer-scope dictionary
        foreach (var newVar in closured)
            _variables.TryAdd(newVar); //add full usage info to allow analyze outer errors

        var fun = ConcreteUserFunction.Create(
            isRecursive: false,
            name: "rule",
            variables: arguments,
            expression: expr);
        return new FunVariableExpressionNode(fun, interval);
    }

    private IExpressionNode CreateFunctionCall(IFunCallSyntaxNode node, IConcreteFunction function) {
        var children = node.Args.SelectToArray(ReadNode);
        var converted = function.CreateWithConvertionOrThrow(children, _dialect.TypeBehaviour, node.Interval);
        if (converted.Type != node.OutputType)
        {
            var converter = VarTypeConverter.GetConverterOrThrow(_dialect.TypeBehaviour, converted.Type, node.OutputType, node.Interval);
            return new CastExpressionNode(converted, node.OutputType, converter, node.Interval);
        }
        else
            return converted;
    }


    private static IExpressionNode ThrowNotAnExpression(ISyntaxNode node)
        =>  throw Errors.NotAnExpression(node);

    private IExpressionNode ReadNode(ISyntaxNode node)
        => node.Accept(this);
}

}