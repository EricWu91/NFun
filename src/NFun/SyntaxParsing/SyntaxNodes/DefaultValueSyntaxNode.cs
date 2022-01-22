using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes {

public class DefaultValueSyntaxNode : ISyntaxNode {
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public bool IsInBrackets { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public override string ToString() => "default";
}

}