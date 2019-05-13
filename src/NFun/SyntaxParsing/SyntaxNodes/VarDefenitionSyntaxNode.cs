using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class VarDefenitionSyntaxNode : ISyntaxNode
    {
        public int NodeNumber { get; set; }

        public string Id { get; }
        public VarType VarType { get; }
        public VarAttribute[] Attributes { get; }

        public VarDefenitionSyntaxNode(TypedVarDefSyntaxNode node, VarAttribute[] attributes = null)
        {
            Id = node.Id;
            VarType = node.VarType;
            Attributes = attributes??new VarAttribute[0];
            Interval = node.Interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.GlobalVarDefenition;
        public Interval Interval { get; set; }
        public override string ToString() => Id + ":" + Type;
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];

    }
}