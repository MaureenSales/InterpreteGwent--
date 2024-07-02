using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionFun : ASTnode
{
    public List<Token> Parameters { get; private set; }
    public List<ASTnode> Body { get; private set; }

    public ActionFun(List<Token> parameters, List<ASTnode> body)
    {
        Parameters = parameters;
        Body = body;
    }
    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}
