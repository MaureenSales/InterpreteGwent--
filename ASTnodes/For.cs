using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class For : ASTnode
{
    public ASTnode Element { get; private set; }
    public ASTnode Collection { get; private set; }
    public List<ASTnode> Instructions { get; private set; }
    public For(ASTnode element, ASTnode collection, List<ASTnode> instructions)
    {
        Element = element;
        Collection = collection;
        Instructions = instructions;
    }
    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);

}
