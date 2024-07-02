using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class String : ASTnode
{
    public Token String_ { get; private set; }
    public string Value { get; private set; }

    public String(Token string_)
    {
        String_ = string_;
        Value = string_.Lexeme.Substring(1, string_.Lexeme.Length - 2);
    }

    public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
}
