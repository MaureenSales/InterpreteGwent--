using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnaryInverseOp : ASTnode
{
    public ASTnode Left { get; private set; }
    public Token Op { get; private set; }
    public UnaryInverseOp(ASTnode left, Token op)
    {
        Left = left;
        Op = op;
    }

    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}
