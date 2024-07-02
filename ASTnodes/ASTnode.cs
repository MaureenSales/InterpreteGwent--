using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ASTnode
{
    public abstract T Accept<T>(IVsitor<T> visitor);
}
