using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public class Lexer : MonoBehaviour
{
    // Start is called before the first frame update
    public readonly string input;
    private int position;
    private int line;
    private int errorPosition = 0;
    public List<Token> Tokens { get; private set; }

    public Lexer(string input)
    {
        this.input = input;
        position = 0;
        line = 1;
        Tokens = new List<Token>();
        ScanTokens();
    }

    private void ScanTokens()
    {
        while (!IsAtEnd())
        {
            if (input[position] == ' ' || input[position] == '\t') { position++; errorPosition++; }
            else if (input[position] == '\n') { line++; position++; errorPosition = 0; }
            else GetToken();
        }

        AddToken(TokenType.EOF, "EOF", line, errorPosition);
    }

    private void GetToken()
    {
        foreach (var key in TokenTypeExtensions.TokenPatterns.Keys)
        {
            Regex pattern = new Regex(TokenTypeExtensions.TokenPatterns[key]);
            Match match = pattern.Match(input.Substring(position));
            if (match.Success)
            {
                if (key == TokenType.Identifier && TokenTypeExtensions.KeywordsValues.ContainsKey(match.Value))
                {

                    AddToken(TokenTypeExtensions.KeywordsValues[match.Value], match.Value, line, errorPosition);

                }
                else AddToken(key, match.Value, line, errorPosition);

                position += match.Value.Length;
                errorPosition += match.Value.Length;
                return;
            }
        }
        throw ErrorExceptions.Error(ErrorExceptions.ErrorType.LEXICAL, $"Unexpected Character", line, errorPosition);
    }

    private void AddToken(TokenType type, string value, int line, int col)
    {
        Tokens.Add(new Token(type, value, line, col));
    }

    private bool IsAtEnd()
    {
        return position >= input.Length;
    }
}
