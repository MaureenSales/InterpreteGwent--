using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArithmeticAssignment : ASTnode
{
    public ASTnode Variable { get; private set; }
    public Token Op { get; private set; }
    public ASTnode Value { get; private set; }

    public ArithmeticAssignment(ASTnode variable, Token op, ASTnode value)
    {
        Variable = variable;
        Value = value;
        Op = op;
    }
    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}