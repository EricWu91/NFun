using System.Collections.Generic;
using System.IO;
using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class FunctionWithTwoArgs : IConcreteFunction
    {
        protected FunctionWithTwoArgs(string name,  VarType returnType, VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
        }
        public string Name { get; }
        public VarType[] ArgTypes { get; }
        public VarType ReturnType { get; }
        public abstract object Calc(object a, object b);
        
        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children,  Interval interval)
        {
            var castedChildren = new IExpressionNode[children.Count];

            var i = 0;
            foreach (var argNode in children)
            {
                var toType = ArgTypes[i];
                var fromType = argNode.Type;
                var castedNode = argNode;
                
                if (fromType != toType && !(argNode is MetaInfoExpressionNode))
                {
                    var converter = VarTypeConverter.GetConverterOrThrow(fromType, toType, argNode.Interval);
                    castedNode = new CastExpressionNode(argNode, toType, converter,argNode.Interval);
                }

                castedChildren[i] = castedNode;
                i++;
            }

            return new FunOf2ArgsExpressionNode(this, castedChildren, interval);
        }

        

    }
    public abstract class FunctionBase: IConcreteFunction
    {
        public string Name { get; }
        public VarType[] ArgTypes { get; }
        protected FunctionBase(string name,  VarType returnType, params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
        }
        public VarType ReturnType { get; }
        public abstract object Calc(object[] args);

        public IExpressionNode CreateWithConvertionOrThrow(IList<IExpressionNode> children,  Interval interval)
        {
            var castedChildren = new IExpressionNode[children.Count];

            var i = 0;
            foreach (var argNode in children)
            {
                var toType = ArgTypes[i];
                var fromType = argNode.Type;
                var castedNode = argNode;
                
                if (fromType != toType && !(argNode is MetaInfoExpressionNode))
                {
                    var converter = VarTypeConverter.GetConverterOrThrow(fromType, toType, argNode.Interval);
                    castedNode = new CastExpressionNode(argNode, toType, converter,argNode.Interval);
                }

                castedChildren[i] = castedNode;
                i++;
            }

            return new FunExpressionNode(this, castedChildren, interval);
        }

        public override string ToString() 
            => $"fun {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
    }
        
   
}