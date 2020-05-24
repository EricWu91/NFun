﻿using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NFun.Types;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.TypeInferenceAdapter
{
    public class TicSetupVisitor : ISyntaxNodeVisitor<bool>
    {
        private readonly VariableScopeAliasTable _aliasScope;
        private readonly GraphBuilder _ticTypeGraph;
        private readonly IFunctionDictionary _dictionary;
        private readonly IConstantList _constants;
        private readonly TypeInferenceResultsBuilder _resultsBuilder;

        public static bool Run(
            IEnumerable<ISyntaxNode> nodes, 
            GraphBuilder ticGraph, 
            IFunctionDictionary functions,
            IConstantList constants,
            TypeInferenceResultsBuilder results)
        {
            var visitor = new TicSetupVisitor(ticGraph, functions, constants, results);
            foreach (var syntaxNode in nodes)
            {
                if (!syntaxNode.Accept(visitor))
                    return false;
            }
            return true;
        }
        internal TicSetupVisitor(
            GraphBuilder ticTypeGraph,  
            IFunctionDictionary dictionary,
            IConstantList constants,
            TypeInferenceResultsBuilder resultsBuilder)
        {
            _aliasScope = new VariableScopeAliasTable();
            _dictionary = dictionary;
            _constants = constants;
            _resultsBuilder = resultsBuilder;
            _ticTypeGraph = ticTypeGraph;
        }

        public bool Visit(SyntaxTree node) => VisitChildren(node);
        public bool Visit(EquationSyntaxNode node)
        {
            VisitChildren(node);

            Trace(node, $"{node.Id}:{node.OutputType} = {node.Expression.OrderNumber}");

            if (node.OutputTypeSpecified)
            {
                var type = node.OutputType.ConvertToTiType();
                _ticTypeGraph.SetVarType(node.Id, type);
            }

            _ticTypeGraph.SetDef(node.Id, node.Expression.OrderNumber);
            return true;
        }
        public bool Visit(UserFunctionDefenitionSyntaxNode node)
        {
            var argNames = new string[node.Args.Count];
            int i = 0;
            foreach (var arg in node.Args)
            {
                argNames[i] = arg.Id;
                i++;
                if (arg.VarType != VarType.Empty)
                    _ticTypeGraph.SetVarType(arg.Id, arg.VarType.ConvertToTiType());
            }

            IType returnType = null;
            if (node.ReturnType != VarType.Empty)
                returnType = (IType) node.ReturnType.ConvertToTiType();

            TraceLog.WriteLine(
                $"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",", argNames)})->{node.Body.OrderNumber}:{returnType?.ToString() ?? "empty"}");
            var fun = _ticTypeGraph.SetFunDef(
                name: node.Id + "'" + node.Args.Count,
                returnId: node.Body.OrderNumber,
                returnType: returnType,
                varNames: argNames);
            _resultsBuilder.RememberUserFunctionSignature(node.Id, fun);
            
            return VisitChildren(node);
            
        }
        public bool Visit(ArraySyntaxNode node)
        {
            VisitChildren(node);

            var elementIds = node.Expressions.Select(e => e.OrderNumber).ToArray();
            Trace(node, $"[{string.Join(",", elementIds)}]");
            _ticTypeGraph.SetSoftArrayInit(
                node.OrderNumber,
                node.Expressions.Select(e => e.OrderNumber).ToArray()
            );
            return true;
        }
        public bool Visit(SuperAnonymFunctionSyntaxNode node)
        {
            _aliasScope.EnterScope(node.OrderNumber);

            var argType = _parentFunctionArgType.FunTypeSpecification;
            string[] originArgNames = null;
            string[] aliasArgNames = null;

            if (argType == null || argType.Inputs.Length==1)
                originArgNames = new[] {"it"};
            else
            {
                originArgNames = new string[argType.Inputs.Length];
                for (int i = 0; i < argType.Inputs.Length; i++)
                    originArgNames[i] = $"it{i + 1}";
            }

            aliasArgNames = new string[originArgNames.Length];

            for (var i = 0; i < originArgNames.Length; i++)
            {
                var originName = originArgNames[i];
                var aliasName = MakeAnonVariableName(node, originName);
                _aliasScope.AddVariableAlias(originName, aliasName);
                aliasArgNames[i] = aliasName;
            }

            VisitChildren(node);
            Trace(node, $"f({string.Join(" ", originArgNames)}):{node.OutputType}= {{{node.OrderNumber}}}");
            _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, aliasArgNames);

            _aliasScope.ExitScope();
            return true;
        }
        public bool Visit(ArrowAnonymFunctionSyntaxNode node)
        {
            _aliasScope.EnterScope(node.OrderNumber);
            foreach (var syntaxNode in node.ArgumentsDefenition)
            {
                string originName;
                string anonymName;
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                {
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                    if (!typed.VarType.Equals(VarType.Empty))
                    {
                        var ticType = typed.VarType.ConvertToTiType();
                        _ticTypeGraph.SetVarType(anonymName, ticType);
                    }
                }
                else if (syntaxNode is NamedIdSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                }
                else
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);

                _aliasScope.AddVariableAlias(originName, anonymName);
            }

            VisitChildren(node);
            
            var aliasNames = new string[node.ArgumentsDefenition.Length];
            for (var i = 0; i < node.ArgumentsDefenition.Length; i++)
            {
                var syntaxNode = node.ArgumentsDefenition[i];
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                    aliasNames[i] = _aliasScope.GetVariableAlias(typed.Id);
                else if (syntaxNode is NamedIdSyntaxNode varNode)
                    aliasNames[i] = _aliasScope.GetVariableAlias(varNode.Id);
            }

            Trace(node, $"f({string.Join(" ", aliasNames)}):{node.OutputType}= {{{node.OrderNumber}}}");

            if (node.OutputType == VarType.Empty)
                _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, aliasNames);
            else
            {
                var retType = (IType)node.OutputType.ConvertToTiType();
                _ticTypeGraph.CreateLambda(
                    node.Body.OrderNumber,
                    node.OrderNumber,
                    retType,
                    aliasNames);
            }

            _aliasScope.ExitScope();
            return true;
        }
        
        /// <summary>
        /// If we handle function call -
        /// it shows type of argument that currently handling
        /// if it is known
        /// </summary>
        private VarType _parentFunctionArgType = VarType.Empty;
        public bool Visit(FunCallSyntaxNode node)
        {
            var signature = _dictionary.GetOrNull(node.Id, node.Args.Length);
            if (signature is GenericMetafunction)
            {
                //If it is Metafunction - need to transform origin node to metafunction
                var firstArg = node.Args[0] as NamedIdSyntaxNode;
                if (firstArg == null)
                    throw FunParseException.ErrorStubToDo("first arg should be variable");
                node.TransformToMetafunction(firstArg);
            }

            for (int i = 0; i < node.Args.Length; i++)
            {
                if (signature != null)
                    _parentFunctionArgType = signature.ArgTypes[i];
                node.Args[i].Accept(this);
            }

            var ids = new int[node.Args.Length + 1];
            for (int i = 0; i < node.Args.Length; i++)
                ids[i] = node.Args[i].OrderNumber;
            ids[ids.Length - 1] = node.OrderNumber;

            var userFunction = _resultsBuilder.GetUserFunctionSignature(node.Id, node.Args.Length);
            if (userFunction != null)
            {
                //Call user-function if it is being built at the same time as the current expression is being built
                //for example: recursive calls, or if function relates to global variables
                Trace(node, $"Call UF{node.Id}({string.Join(",", ids)})");
                _ticTypeGraph.SetCall(userFunction, ids);
                //in the case of generic user function  - we dont know generic arg types yet 
                //we need to remember generic TIC signature to used it at the end of interpritation
                _resultsBuilder.RememberRecursiveCall(node.OrderNumber, userFunction);
                return true;
            }


            if (signature == null)
            {
                //Functional variable
                Trace(node, $"Call hi order {node.Id}({string.Join(",", ids)})");
                _ticTypeGraph.SetCall(node.Id, ids);
                return true;
            }
            //Normal function call
            Trace(node, $"Call {node.Id}({string.Join(",", ids)})");

            RefTo[] genericTypes;
            if (signature is GenericFunctionBase t)
            {
                //Optimization
                //Remember generic arguments to use it again at the built time
                genericTypes = InitializeGenericTypes(t.GenericDefenitions);
                _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, genericTypes);
            }
            else genericTypes = new RefTo[0];

            var types = new IState[signature.ArgTypes.Length + 1];
            for (int i = 0; i < signature.ArgTypes.Length; i++)
                types[i] = signature.ArgTypes[i].ConvertToTiType(genericTypes);
            types[types.Length - 1] = signature.ReturnType.ConvertToTiType(genericTypes);

            _ticTypeGraph.SetCall(types, ids);
            return true;
        }
        public bool Visit(ResultFunCallSyntaxNode node)
        {
            VisitChildren(node);

            var ids = new int[node.Args.Length + 1];
            for (int i = 0; i < node.Args.Length; i++)
                ids[i] = node.Args[i].OrderNumber;
            ids[ids.Length - 1] = node.OrderNumber;

            _ticTypeGraph.SetCall(node.ResultExpression.OrderNumber, ids);
            return true;
        }
        public bool Visit(IfThenElseSyntaxNode node)
        {
            VisitChildren(node);

            var conditions = node.Ifs.Select(i => i.Condition.OrderNumber).ToArray();
            var expressions = node.Ifs.Select(i => i.Expression.OrderNumber).Append(node.ElseExpr.OrderNumber)
                .ToArray();
            Trace(node, $"if({string.Join(",", conditions)}): {string.Join(",", expressions)}");
            _ticTypeGraph.SetIfElse(
                conditions,
                expressions,
                node.OrderNumber);
            return true;
        }
        public bool Visit(IfCaseSyntaxNode node) => VisitChildren(node);
        public bool Visit(ConstantSyntaxNode node)
        {
            Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
            var type = LangTiHelper.ConvertToTiType(node.OutputType);

            if (type is Primitive p)
                _ticTypeGraph.SetConst(node.OrderNumber, p);
            else if (type is Tic.SolvingStates.Array a && a.Element is Primitive primitiveElement)
                _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
            else
                throw new InvalidOperationException("Complex constant type is not supported");
            return true;
        }
        public bool Visit(GenericIntSyntaxNode node)
        {
            Trace(node, $"IntConst {node.Value}:{(node.IsHexOrBin ? "hex" : "int")}");

            if (node.IsHexOrBin)
            {
                //hex or bin constant
                //can be u8:< c:< i96
                ulong actualValue;
                if (node.Value is long l)
                {
                    if (l > 0) actualValue = (ulong)l;
                    else
                    {
                        //negative constant
                        if (l >= Int16.MinValue)
                            _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.I16, Primitive.I64,
                                Primitive.I32);
                        else if (l >= Int32.MinValue)
                            _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.I32, Primitive.I64,
                                Primitive.I32);
                        else _ticTypeGraph.SetConst(node.OrderNumber, Primitive.I64);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U8, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)Int16.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U12, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)UInt16.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U16, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)Int32.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U24, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)UInt32.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U32, Primitive.I96, Primitive.I64);
                else if (actualValue <= (ulong)Int64.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U48, Primitive.I96, Primitive.I64);
                else
                    _ticTypeGraph.SetConst(node.OrderNumber, Primitive.U64);
            }
            else
            {
                //1,2,3
                //Can be u8:<c:<real
                Primitive descedant;
                ulong actualValue;
                if (node.Value is long l)
                {
                    if (l > 0) actualValue = (ulong)l;
                    else
                    {
                        //negative constant
                        if (l >= Int16.MinValue) descedant = Primitive.I16;
                        else if (l >= Int32.MinValue) descedant = Primitive.I32;
                        else descedant = Primitive.I64;
                        _ticTypeGraph.SetIntConst(node.OrderNumber, descedant);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue) descedant = Primitive.U8;
                else if (actualValue <= (ulong)Int16.MaxValue) descedant = Primitive.U12;
                else if (actualValue <= (ulong)UInt16.MaxValue) descedant = Primitive.U16;
                else if (actualValue <= (ulong)Int32.MaxValue) descedant = Primitive.U24;
                else if (actualValue <= (ulong)UInt32.MaxValue) descedant = Primitive.U32;
                else if (actualValue <= (ulong)Int64.MaxValue) descedant = Primitive.U48;
                else descedant = Primitive.U64;
                _ticTypeGraph.SetIntConst(node.OrderNumber, descedant);

            }

            return true;
        }
        public bool Visit(NamedIdSyntaxNode node)
        {
            var id = node.Id;
            Trace(node, $"VAR {id} ");

            //nfun syntax allows multiple variables to have the same name depending on whether they are functions or not
            //need to know what type of argument is expected - is it variableId, or functionId?
            //if it is function - how many arguments are expected ? 
            var argType = _parentFunctionArgType;
            if (argType.BaseType == BaseVarType.Fun)// functional argument is expected
            {
                var argsCount = argType.FunTypeSpecification.Inputs.Length;
                var signature = _dictionary.GetOrNull(id, argsCount);
                if (signature == null)
                {
                    node.IdType = NamedIdNodeType.UnknownFunction;
                    return true;
                }

                if (signature is GenericFunctionBase genericFunction)
                {
                    var generics = InitializeGenericTypes(genericFunction.GenericDefenitions);
                    _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, generics);

                    _ticTypeGraph.SetVarType($"g'{argsCount}'{id}",
                        genericFunction.GetTicFunType(generics));
                    _ticTypeGraph.SetVar($"g'{argsCount}'{id}", node.OrderNumber);

                    node.IdType = NamedIdNodeType.GenericFunction;
                    node.IdContent = new FunctionalVariableCallInfo(signature, generics);
                }
                else
                {
                    _ticTypeGraph.SetVarType($"f'{argsCount}'{id}", signature.GetTicFunType());
                    _ticTypeGraph.SetVar($"f'{argsCount}'{id}", node.OrderNumber);

                    node.IdType = NamedIdNodeType.GenericFunction;
                    node.IdContent = new FunctionalVariableCallInfo(signature, null);
                }

                _resultsBuilder.RememberFunctionalVariable(node.OrderNumber, signature);
                return true;
            }
            // At this point we are sure - ID is not a function

            // ID can be constant or variable
            // if ID exists in ticTypeGraph - then ID is Variable
            // else if ID exists in constant list - then ID is constant
            // else ID is variable

            if (!_ticTypeGraph.HasNamedNode(id) && _constants.TryGetConstant(id, out var constant))
            {
                //ID is constant 
                node.IdType = NamedIdNodeType.Constant;
                node.IdContent = constant;

                var titype = constant.Type.ConvertToTiType();
                if(titype is Primitive primitive)
                    _ticTypeGraph.SetConst(node.OrderNumber, primitive);
                else if (titype is Array array && array.Element is Primitive primitiveElement)
                    _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
                else
                    throw new InvalidOperationException("Type " + constant.Type + " is not supported for constants");
            }
            //ID is variable
            var localId = _aliasScope.GetVariableAlias(node.Id);
            _ticTypeGraph.SetVar(localId, node.OrderNumber);
            
            node.IdType = NamedIdNodeType.Variable;
            return true;
        }
        public bool Visit(TypedVarDefSyntaxNode node)
        {
            VisitChildren(node);

            Trace(node, $"Tvar {node.Id}:{node.VarType}  ");
            if (node.VarType != VarType.Empty)
            {
                var type = node.VarType.ConvertToTiType();
                _ticTypeGraph.SetVarType(node.Id, type);
            }

            return true;
        }
        public bool Visit(VarDefenitionSyntaxNode node)
        {
            VisitChildren(node);

            Trace(node, $"VarDef {node.Id}:{node.VarType}  ");
            var type = node.VarType.ConvertToTiType();
            _ticTypeGraph.SetVarType(node.Id, type);
            return true;
        }
        public bool Visit(ListOfExpressionsSyntaxNode node) => VisitChildren(node);
        public bool Visit(MetaInfoSyntaxNode node) => Visit(node.NamedIdSyntaxNode);//variable node is a child of metaInfoNode;

        #region privates
        private RefTo[] InitializeGenericTypes(GenericConstrains[] constrains)
        {
            var genericTypes = new RefTo[constrains.Length];
            for (int i = 0; i < constrains.Length; i++)
            {
                var def = constrains[i];
                genericTypes[i] = _ticTypeGraph.InitializeVarNode(
                    def.Descendant,
                    def.Ancestor,
                    def.IsComparable);
            }

            return genericTypes;
        }

        private void Trace(ISyntaxNode node, string text)
        {
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"Exit:{node.OrderNumber}. {text} ");
        }
        private static string MakeAnonVariableName(ISyntaxNode node, string id)
            => LangTiHelper.GetArgAlias("anonymous_" + node.OrderNumber, id);
        private bool VisitChildren(ISyntaxNode node) 
            => node.Children.All(child => child.Accept(this));
        #endregion

    }
}