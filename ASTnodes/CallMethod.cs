using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallMethod : ASTnode
{
    public Token MethodName { get; private set; }
    public List<ASTnode> Arguments { get; private set; }
    public CallMethod(Token methodName, List<ASTnode> arguments)
    {
        MethodName = methodName;
        Arguments = arguments;
    }

    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}
