using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class SuperAnonymCallSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode Body { get; }

        public SuperAnonymCallSyntaxNode(ISyntaxNode body)
        {
            Body = body;
            Interval = body.Interval;
        }

        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new[] {Body};
    }
}