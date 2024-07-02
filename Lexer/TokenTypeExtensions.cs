using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenTypeExtensions : MonoBehaviour
{
    // Start is called before the first frame update
    public readonly static Dictionary<TokenType, string> TokenPatterns;
    public static readonly Dictionary<string, TokenType> KeywordsValues;
    public static readonly List<string> Properties;
    public static readonly List<string> Methods;

    static TokenTypeExtensions()
    {
        TokenPatterns = new Dictionary<TokenType, string>
            {
                {TokenType.Identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*"},
                {TokenType.Number, @"^\d+(\.\d+)?"},
                {TokenType.Increment, @"^\+\+"},
                {TokenType.SumAssignment, @"^\+\="},
                {TokenType.Sum,@"^\+"},
                {TokenType.Decrement, @"^\-\-"},
                {TokenType.MinusAssignment, @"^\-\="},
                {TokenType.Subtraction, @"^\-"},
                {TokenType.ProductAssignment, @"^\*\="},
                {TokenType.Product, @"^\*"},
                {TokenType.DivisionAssignment, @"^\/\="},
                {TokenType.Division, @"^\/"},
                {TokenType.ModuloAssignment, @"^\%\="},
                {TokenType.Modulo, @"^\%"},
                {TokenType.Pow, @"^\^"},
                {TokenType.Imply, @"^\=\>"},
                {TokenType.Equality, @"^\=\="},
                {TokenType.Assignment, @"^\="},
                {TokenType.ConcatWithSpace, @"^\@\@"},
                {TokenType.Concat, @"^@"},
                {TokenType.OpBraces, @"^\{"},
                {TokenType.ClBraces, @"^\}"},
                {TokenType.OpParenthesis, @"^\("},
                {TokenType.ClParenthesis, @"^\)"},
                {TokenType.OpCurlyBracket, @"^\["},
                {TokenType.ClCurlyBracket, @"^\]"},
                {TokenType.Comma, @"^\,"},
                {TokenType.Dot, @"^\."},
                {TokenType.Semicolon, @"^\;"},
                {TokenType.Colon, @"^\:"},
                {TokenType.String, @"^"".*?"""},
                {TokenType.LessOrEqual, @"^<="},
                {TokenType.LessThan, @"^<"},
                {TokenType.GreaterOrEqual, @"^>="},
                {TokenType.GreaterThan, @"^>"},
                {TokenType.NotEqual, @"^!="},
                {TokenType.Negation, @"^!"},
                {TokenType.Conjunction, @"^\&\&"},
                {TokenType.Disjunction, @"^\|\|"},

            };
        KeywordsValues = new Dictionary<string, TokenType>
            {
                {"Name", TokenType.Name},
                {"card", TokenType.Card},
                {"Params", TokenType.Params},
                {"Action", TokenType.Action},
                {"Bool", TokenType.Bool},
                {"Type", TokenType.Type},
                {"Faction", TokenType.Faction},
                {"Power", TokenType.Power},
                {"Range", TokenType.Range},
                {"OnActivation", TokenType.OnActivation},
                {"effect", TokenType.effect},
                {"Effect", TokenType.Effect},
                {"Selector", TokenType.Selector},
                {"Source", TokenType.Source},
                {"Single", TokenType.Single},
                {"true", TokenType.True},
                {"false", TokenType.False},
                {"Predicate", TokenType.Predicate},
                {"PostAction", TokenType.PostAction},
                {"for", TokenType.For},
                {"while", TokenType.While},
                {"in", TokenType.In},
                {"Find", TokenType.Find},
                {"Number", TokenType.TypeNumber},
                {"String", TokenType.TypeString},

            };

        Properties = new List<string>()
           {
                "Power",
                "Faction",
                "Type",
                "Name",
                "TriggerPlayer",
                "Board",
                "Hand",
                "Deck",
                "Field",
                "Graveyard",
                "Owner",

           };

        Methods = new List<string>()
           {
                "Pop",
                "Add",
                "Shuffle",
                "DeckOfPlayer",
                "HandOfPlayer",
                "FieldOfPlayer",
                "GraveyardOfPlayer",
                "Push",
                "Remove",
                "Find",
                "SendBottom",
           };
    }

}

public enum TokenType
{
    //opertors

    //booleans
    Equality, NotEqual, LessThan, GreaterThan, LessOrEqual, GreaterOrEqual, Negation, Imply,
    Conjunction, Disjunction,

    //aritmetics
    Sum, Subtraction, Product, Modulo, Division, Pow, Assignment, Concat, ConcatWithSpace, Increment, Decrement, SumAssignment,
    MinusAssignment, DivisionAssignment, ProductAssignment, ModuloAssignment,

    //keywords
    Name, Params, Action, Bool, Targets, Context, Type, Faction, Power, Range, Card, Find,
    OnActivation, Effect, effect, Selector, Source, Single, True, False, Predicate, PostAction, For, While, Amount, In, TypeNumber, TypeString,

    //constants
    PI, Euler,

    //literals
    String, Number,

    //symbols
    OpParenthesis, ClParenthesis, OpCurlyBracket, ClCurlyBracket, OpBraces, ClBraces, Comma, Semicolon, Colon, Dot, Identifier,

    EOF,
}
