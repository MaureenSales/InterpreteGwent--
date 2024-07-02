using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable
public class CallEffect : ASTnode
{
    public ASTnode Name { get; private set; }
    public Dictionary<VariableReference, ASTnode> Parameters { get; private set; }
    public ASTnode? Selector { get; private set; }
    public ASTnode? PostAction { get; private set; }

    public CallEffect(ASTnode name, Dictionary<VariableReference, ASTnode> parameters, ASTnode? selector, ASTnode? postAction)
    {
        Name = name;
        Parameters = parameters;
        Selector = selector;
        PostAction = postAction;
    }

    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}