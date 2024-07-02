using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect : ASTnode
{
    public ASTnode Name { get; private set; }
    public List<ASTnode> Params { get; private set; }
    public ASTnode Action { get; private set; }

    public Effect(ASTnode name, List<ASTnode> parameters, ASTnode action)
    {
        Name = name;
        Params = parameters;
        Action = action;
    }
    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}
