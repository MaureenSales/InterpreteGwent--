using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredicateLambda : ASTnode
{
    public Token Parameter { get; private set; }
    public ASTnode BodyPredicate { get; private set; }
    public PredicateLambda(Token variableReference, ASTnode bodyPredicate)
    {
        Parameter = variableReference;
        BodyPredicate = bodyPredicate;
    }

    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}