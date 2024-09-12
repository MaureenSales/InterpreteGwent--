using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Token : MonoBehaviour
{
    public TokenType Type { get; private set; }
    public string Lexeme { get; private set; }
    public int Line { get; private set; }
    public int Col { get; private set; }

    public Token(TokenType type, string lexeme, int line, int col)
    {
        Type = type;
        Lexeme = lexeme;
        Line = line;
        Col = col;
    }

    public new void ToString()
    {
        System.Console.Write(this.Type);
        System.Console.Write(": ");
        System.Console.Write(this.Lexeme);
    }
}
