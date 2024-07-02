using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnaryOp : ASTnode
{
    public Token Op { get; private set; }
    public ASTnode Right { get; private set; }

    public UnaryOp(Token op, ASTnode right)
    {
        Op = op;
        Right = right;
    }

    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}