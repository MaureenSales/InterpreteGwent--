using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComparisonBinOp : BinOp
{
    public ComparisonBinOp(ASTnode left, Token op, ASTnode right) : base(left, op, right) { }
    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}
