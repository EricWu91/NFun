using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class ArraySyntaxNode : ISyntaxNode {
    public ArraySyntaxNode(IList<ISyntaxNode> expressions, Interval interval) {
        Expressions = expressions;
        Interval = interval;
    }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public IList<ISyntaxNode> Expressions { get; }
    public bool IsInBrackets { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Expressions;
}