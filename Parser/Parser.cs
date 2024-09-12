using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#nullable enable
public class Parser : MonoBehaviour
{
    // Start is called before the first frame update
    public Lexer lexer { get; private set; }
    private int position;
    public List<ASTnode> Statements { get; private set; }

    public Parser(Lexer lexer)
    {
        this.lexer = lexer;
        position = 0;
        Statements = new List<ASTnode>();
        Parsing();
    }
    private bool IsAtEnd => position >= lexer.Tokens.Count - 1;

    private Token Current => lexer.Tokens[position];

    private Token Advance() => lexer.Tokens[position++];
    private void Parsing()
    {
        while (!IsAtEnd)
        {
            Statements.Add(Declaration());
        }

    }

    private ASTnode Declaration()
    {
        if (Match(TokenType.effect, TokenType.Card))
        {
            switch (Current.Type)
            {
                case TokenType.Card:
                    return Card();
                case TokenType.effect:
                    return Effect();
            }
        }

        throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "Only cards or effects can be declared", Current.Line, Current.Col);

    }

    private ASTnode Effect()
    {
        position++;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpBraces, "'{' is expected after declaration of an effect");

        int[] fields = new int[3];
        TokenType[] fieldsType = { TokenType.Name, TokenType.Params, TokenType.Action };
        ASTnode name = null!;
        List<ASTnode> parameters = new List<ASTnode>();
        ASTnode actionFun = null!;
        int count = 0;
        while (count < 3)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == 0 && Match(fieldsType[i]))
                {
                    switch (i)
                    {
                        case 0: name = SimpleFields(); fields[0] = 1; break;
                        case 1: parameters = Params(); fields[1] = 1; break;
                        case 2: actionFun = Action(); fields[2] = 1; break;
                    }
                    break;
                }
                else if (fields[i] == 1 && Match(fieldsType[i]))
                {
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                }
            }
            count++;
        }
        for (int i = 0; i < fields.Length; i++)
        {
            if (i == 1) continue;
            if (fields[i] == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Must define the {fieldsType[i]} field in declaration of an Effect", Current.Line, Current.Col);
        }
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClBraces, "'}' is expected after body of an effect");
        return new Effect(name, parameters, actionFun);
    }

    private List<ASTnode> Params()
    {
        position++;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a Params field");
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpBraces, "'{' is expected before declaration of values of a Params field");
        List<ASTnode> parameters = new List<ASTnode>();
        position--;
        do
        {
            position++;
            ASTnode variable;
            if (Match(TokenType.Identifier)) variable = new VariableReference(Advance());
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, " An identifier is expected", Current.Line, Current.Col);
            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a parameter");
            Token type;
            if (Match(TokenType.TypeNumber, TokenType.TypeString, TokenType.Bool))
            {
                type = Advance();
            }
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "The type of the declared parameter is expected", Current.Line, Current.Col);

            parameters.Add(new AssignmentWithType(variable, type, null));
        } while (Match(TokenType.Comma));
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClBraces, "'}' is expected after declaration of values of a Params field");
        if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after declaration of the Params field");
        return parameters;
    }

    private ASTnode Action()
    {
        position++;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of an Action function");
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpParenthesis, "'(' is expected before the parameters of an Action function");
        List<ASTnode> parameters = new List<ASTnode>();
        if (!Match(TokenType.ClParenthesis))
        {
            position--;
            do
            {
                position++;
                ASTnode parameter;
                if (Match(TokenType.Identifier)) parameter = new VariableReference(Advance());
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, " An identifier is expected", Current.Line, Current.Col);
                parameters.Add(parameter);
            } while (Match(TokenType.Comma));
        }
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClParenthesis, "')' is expected after parameters");
        if (parameters.Count != 2) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la funcion Action define solo dos parametros", Current.Line, Current.Col);
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Imply, "'=>' is expected after declaration of parameter of an Action function");
        List<ASTnode> body = new List<ASTnode>();
        if (Match(TokenType.OpBraces)) body = ExprBlock();
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "'{' is expected before the body of the function", Current.Line, Current.Col);
        if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after value of a field");
        return new ActionFun(parameters, body);
    }

    private List<ASTnode> ExprBlock()
    {
        List<ASTnode> exprs = new List<ASTnode>();
        position++;
        while (!Match(TokenType.ClBraces))
        {
            exprs.Add(Statement());
            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Semicolon, "';' is expected after expresion in expressions block");
        }
        position++;
        return exprs;
    }

    private ASTnode Statement()
    {
        if (Match(TokenType.For, TokenType.While))
        {
            switch (Current.Type)
            {
                case TokenType.For:
                    return For();
                case TokenType.While:
                    return While();
            }
        }

        return Assignment();
    }

    private ASTnode While()
    {
        position++;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpParenthesis, "'(' is expected before declaration of condition of a while expression");
        ASTnode condition = Assignment();
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClParenthesis, "')' is expected after declaration of condition of a while expression");
        List<ASTnode> instructions = new List<ASTnode>();
        if (Match(TokenType.OpBraces))
        {
            instructions = ExprBlock();
        }
        else
        {
            instructions.Add(Statement());
        }
        return new While(condition, instructions);
    }

    private ASTnode For()
    {
        position++;
        ASTnode element;
        if (Match(TokenType.Identifier)) element = new VariableReference(Advance());
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "An identifier is expected", Current.Line, Current.Col);
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.In, "'In' keyword is expected");
        ASTnode collection;
        if (Match(TokenType.Identifier)) collection = new VariableReference(Advance());
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "An identifier is expected", Current.Line, Current.Col);
        List<ASTnode> instructions = new List<ASTnode>();
        if (Match(TokenType.OpBraces))
        {
            instructions = ExprBlock();
        }
        else
        {
            instructions.Add(Statement());
        }
        return new For(element, collection, instructions);
    }

    private ASTnode Card()
    {
        position++;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpBraces, "'{' is expected after declaration of a card");
        int[] fields = new int[6];
        TokenType[] fieldsType = { TokenType.Type, TokenType.Name, TokenType.Faction, TokenType.Power, TokenType.Range, TokenType.OnActivation };
        int count = 0;
        ASTnode type = null!, name = null!, faction = null!, power = null!;
        List<ASTnode> range = new List<ASTnode>();
        List<ASTnode> effects = new List<ASTnode>();
        while (count < 6)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == 0 && Match(fieldsType[i]))
                {
                    switch (i)
                    {
                        case 0: type = SimpleFields(); fields[0] = 1; break;
                        case 1: name = SimpleFields(); fields[1] = 1; break;
                        case 2: faction = SimpleFields(); fields[2] = 1; break;
                        case 3: power = SimpleFields(); fields[3] = 1; break;
                        case 4:
                            position++;
                            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                            range = ArrayDSL();
                            if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after declaration of the a field");
                            fields[4] = 1; break;
                        case 5:
                            position++;
                            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                            effects = ArrayDSL();
                            fields[5] = 1; break;
                    }
                    break;
                }
                else if (fields[i] == 1 && Match(fieldsType[i]))
                {
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                }
            }
            count++;
        }
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Must define the {fieldsType[i]} field in declaration of a Card", Current.Line, Current.Col);
        }
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClBraces, "'}' is expected after body of a card");
        return new CardDeclaration(type, name, faction, power, range, effects);

    }

    private ASTnode SimpleFields()
    {
        position++;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
        ASTnode value;
        value = Assignment();
        if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after value of a field");
        return value;
    }

    private List<ASTnode> ArrayDSL()
    {
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpCurlyBracket, "'[' is expected before declaration of an array");
        position--;
        List<ASTnode> elements = new List<ASTnode>();
        do
        {
            position++;
            elements.Add(ActivationMember(null));

        } while (Match(TokenType.Comma));
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClCurlyBracket, "']' is expected after declaration of an array");
        return elements;
    }

    private ASTnode ActivationMember(ASTnode? parent)
    {
        if (Match(TokenType.OpBraces))
        {
            position++;
            ASTnode effect = DeclarationEffect(parent);
            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClBraces, "'}' is expected after declaration of an effect in body of a card");
            return effect;
        }
        return Assignment();
    }

    private ASTnode DeclarationEffect(ASTnode? parent)
    {
        ASTnode name = null!;
        List<ASTnode> parameters = new List<ASTnode>();
        ASTnode? selector = null!;
        ASTnode? postAction = null;
        int[] fields = new int[4];
        TokenType[] fieldsType = { TokenType.Effect, TokenType.Selector, TokenType.PostAction, TokenType.Type };
        int count = 0;
        while (count < 3)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == 0 && Match(fieldsType[i]))
                {
                    switch (i)
                    {
                        case 0:
                        case 3:
                            EffectInstance(ref name!, ref parameters); fields[0] = 1; break;
                        case 1: selector = SelectorField(); fields[1] = 1; break;
                        case 2:
                            position++;
                            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpBraces, "'{' is expected before the values of a field");
                            postAction = DeclarationEffect(selector);
                            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClBraces, "'}' is expected after body of a field");
                            fields[2] = 1; break;
                    }
                    break;
                }
                else if (fields[i] == 1 && Match(fieldsType[i]))
                {
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                }
            }
            count++;
        }
        for (int i = 0; i < fields.Length - 2; i++)
        {
            if (fields[i] == 0)
            {
                if (i == 1) selector = parent;
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Must define the {fieldsType[i]} field in declaration of an Effect", Current.Line, Current.Col);
            }
        }
        return new CallEffect(name!, parameters, selector, postAction);
    }

    private void EffectInstance(ref ASTnode? name, ref List<ASTnode> parameters)
    {
        position++;
        bool sugar = false;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of an Effect field");
        if (Current.Type == TokenType.String)
        {
            name = new String(Advance());
            sugar = true;
        }
        else
        {
            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpBraces, "'{' is expected before the values of an Effect field");
            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Name, "Name field is expected");
            position--;
            name = SimpleFields();
        }

        while (Match(TokenType.Identifier) && !sugar)
        {
            var element = Arguments();
            parameters.Add(element);
        }

        if (!sugar) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClBraces, "'}' is expected after declaration of the Effect field");
        if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after declaration of the Effect field");
    }

    private ASTnode Arguments()
    {
        VariableReference variable = new VariableReference(Advance());
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
        ASTnode value;

        value = Assignment();

        if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after value of a field");
        return new Assignment(variable, value);
    }

    private ASTnode SelectorField()
    {
        position++;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a Selector field");
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpBraces, "'{' is expected before the values of a Selector field");
        ASTnode source = null!, single = null!;
        ASTnode predicate = null!;
        int[] fields = new int[3];
        TokenType[] fieldsType = { TokenType.Source, TokenType.Single, TokenType.Predicate };
        int count = 0;
        while (count < 3)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == 0 && Match(fieldsType[i]))
                {
                    switch (i)
                    {
                        case 0: source = SimpleFields(); fields[0] = 1; break;
                        case 1: single = SimpleFields(); fields[1] = 1; break;
                        case 2:
                            position++;
                            Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                            predicate = PredicateLambda(); fields[2] = 1; break;
                    }
                    break;
                }
                else if (fields[i] == 1 && Match(fieldsType[i]))
                {
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                }
            }
            count++;
        }
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, $"Must define the {fieldsType[i]} field in declaration of a Selector field", Current.Line, Current.Col);
        }
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClBraces, "'}' is expected after declaration of a field");
        if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after declaration of a field");
        return new Selector(source, single, predicate);
    }

    private PredicateLambda PredicateLambda()
    {
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpParenthesis, "'(' is expected before declaration of parameter of a predicate expression");
        ASTnode parameter;
        if (Match(TokenType.Identifier))
        {
            parameter = new VariableReference(Advance());
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "An identifier is expected", Current.Line, Current.Col);
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClParenthesis, "')' is expected after declaration of parameter of a predicate expression");
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Imply, "'=>' is expected before declaration of the body of a predicate expression");
        ASTnode body = Assignment();
        if ((Match(TokenType.Comma) && PeekNext().Type != TokenType.ClBraces) || !Match(TokenType.ClBraces, TokenType.Comma, TokenType.ClParenthesis)) Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.Comma, "',' is expected after value of a field");
        return new PredicateLambda(parameter, body);
    }

    private ASTnode Assignment()
    {
        ASTnode left = ArithmeticAssignment();
        if (Match(TokenType.Assignment))
        {
            position++;
            ASTnode right = ArithmeticAssignment();
            left = new Assignment(left, right);
        }
        return left;
    }

    private ASTnode ArithmeticAssignment()
    {
        ASTnode left = Or();
        if (Match(TokenType.SumAssignment, TokenType.MinusAssignment, TokenType.DivisionAssignment, TokenType.ProductAssignment, TokenType.ModuloAssignment))
        {
            Token op = Advance();
            ASTnode right = Or();
            left = new ArithmeticAssignment(left, op, right);
        }
        return left;
    }

    private ASTnode Or()
    {
        ASTnode left = And();
        while (Match(TokenType.Disjunction))
        {
            Token op = Advance();
            ASTnode right = And();
            left = new LogicalBinOp(left, op, right);
        }
        return left;
    }

    private ASTnode And()
    {
        ASTnode left = Equality();
        while (Match(TokenType.Conjunction))
        {
            Token op = Advance();
            ASTnode right = Equality();
            left = new LogicalBinOp(left, op, right);
        }
        return left;
    }
    private ASTnode Equality()
    {
        ASTnode left = Comparison();
        while (Match(TokenType.Equality, TokenType.NotEqual))
        {
            Token op = Advance();
            ASTnode right = Comparison();
            left = new ComparisonBinOp(left, op, right);
        }
        return left;
    }

    private ASTnode Comparison()
    {
        ASTnode left = Concat();
        while (Match(TokenType.LessThan, TokenType.LessOrEqual, TokenType.GreaterThan, TokenType.GreaterOrEqual))
        {
            Token op = Advance();
            ASTnode right = Concat();
            left = new ComparisonBinOp(left, op, right);
        }
        return left;
    }

    private ASTnode Concat()
    {
        ASTnode left = Term();
        while (Match(TokenType.Concat, TokenType.ConcatWithSpace))
        {
            Token op = Advance();
            ASTnode right = Term();
            left = new ConcatBinOp(left, op, right);
        }
        return left;
    }

    private ASTnode Term()
    {
        ASTnode left = Factor();
        while (Match(TokenType.Sum, TokenType.Subtraction))
        {
            Token op = Advance();
            ASTnode right = Factor();
            left = new ArithmeticBinOp(left, op, right);
        }
        return left;
    }

    private ASTnode Factor()
    {
        ASTnode left = Power();
        while (Match(TokenType.Product, TokenType.Division, TokenType.Modulo))
        {
            Token op = Advance();
            ASTnode right = Power();
            left = new ArithmeticBinOp(left, op, right);
        }
        return left;
    }

    private ASTnode Power()
    {
        ASTnode left = UnaryExpr();
        while (Match(TokenType.Pow))
        {
            Token op = Advance();
            ASTnode right = Power();
            left = new ArithmeticBinOp(left, op, right);
        }
        return left;
    }

    private ASTnode UnaryExpr()
    {
        if (Match(TokenType.Negation))
        {
            Token op = Advance();
            ASTnode right = UnaryExpr();
            return new UnaryOp(op, right);
        }

        if (Match(TokenType.Increment, TokenType.Decrement, TokenType.Subtraction))
        {
            Token op = Advance();
            ASTnode right = UnaryInverse();
            return new UnaryOp(op, right);
        }
        return UnaryInverse();
    }

    private ASTnode UnaryInverse()
    {
        ASTnode left = Call();
        if (Match(TokenType.Increment, TokenType.Decrement))
        {
            Token op = Advance();
            left = new UnaryInverseOp(left, op);
        }
        return left;
    }

    private ASTnode Call()
    {
        ASTnode left;
        if (Match(TokenType.Identifier, TokenType.Find))
        {
            switch (PeekNext().Type)
            {
                case TokenType.Dot:
                    left = Property(new VariableReference(Advance()));
                    break;
                case TokenType.OpParenthesis:
                    left = CallMethod(Advance());
                    break;
                case TokenType.OpCurlyBracket:
                    left = Indexer(new VariableReference(Advance()));
                    break;
                default:
                    left = new VariableReference(Advance());
                    break;
            }
            if (Match(TokenType.Dot)) left = Property(left);
            else if (Match(TokenType.OpCurlyBracket)) left = Indexer(left);
            return left;

        }
        return PrimaryExpr();
    }
    private ASTnode Indexer(ASTnode node)
    {
        ASTnode left = node;
        position++;
        ASTnode index = Assignment();
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClCurlyBracket, "']' is expected after indexer");
        left = new IndexList(left, index);
        return left;
    }

    private ASTnode CallMethod(Token id)
    {
        Token methodName = id;
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.OpParenthesis, "'(' is expected after the method name");
        List<ASTnode> arguments = new List<ASTnode>();
        if (Current.Type != TokenType.ClParenthesis && !IsAtEnd)
        {
            if (methodName.Type == TokenType.Find)
            {
                if (!Match(TokenType.OpParenthesis)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "An predicate expresion is expected in the declaration of the method Find", Current.Line, Current.Col);
                arguments.Add(PredicateLambda());
            }
            else
            {
                arguments.Add(Assignment());

                while (Current.Type == TokenType.Comma)
                {
                    position++;
                    arguments.Add(Assignment());
                }
            }
        }
        Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClParenthesis, "')' is expected after arguments");
        ASTnode method = new CallMethod(methodName, arguments);
        return method;
    }

    private ASTnode Property(ASTnode node)
    {
        ASTnode left = node;

        while (Match(TokenType.Dot))
        {
            position++;
            if (Match(TokenType.Power, TokenType.Faction, TokenType.Name, TokenType.Type))
            {
                left = new Property(left, new VariableReference(Advance()));
            }
            else if (Match(TokenType.Identifier, TokenType.Find))
            {
                switch (PeekNext().Type)
                {
                    case TokenType.Dot:
                        left = new Property(left, new VariableReference(Advance()));
                        break;
                    case TokenType.OpParenthesis:
                        left = new Property(left, CallMethod(Advance()));
                        break;
                    case TokenType.OpCurlyBracket:
                        left = Indexer(new Property(left, new VariableReference(Advance())));
                        break;
                    default:
                        left = new Property(left, new VariableReference(Advance()));
                        break;
                }
            }
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "An indentifier, method or indexer is expected", Current.Line, Current.Col);
        }

        return left;
    }

    private ASTnode PrimaryExpr()
    {
        switch (Current.Type)
        {
            case TokenType.Number:
                return new Number(Advance());
            case TokenType.String:
                return new String(Advance());
            case TokenType.True:
                return new Boolean(Advance());
            case TokenType.False:
                return new Boolean(Advance());
            case TokenType.OpParenthesis:
                position++;
                ASTnode expr = Assignment();
                Consume(ErrorExceptions.ErrorType.SYNTACTIC, TokenType.ClParenthesis, "')' is expected after expression");
                return new GroupedExpr(expr);
            case TokenType.OpBraces:
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "An expression is expected", Current.Line, Current.Col);
            case TokenType.Semicolon:
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "';' should only be used when ending an expression in an expression block", Current.Line, Current.Col);
            case TokenType.OpCurlyBracket:
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "'[' ']' should only be used for indexed on proprties that return list or specifics fields", Current.Line, Current.Col);
            case TokenType.EOF:
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "An expression is expected", Current.Line, Current.Col);

            default:
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SYNTACTIC, "Unexpected token", Current.Line, Current.Col);

        }
    }

    private Token Consume(ErrorExceptions.ErrorType typeError, TokenType type, string message)
    {
        if (Current.Type == type) return Advance();
        throw ErrorExceptions.Error(typeError, message, Current.Line, Current.Col);
    }

    private Token PeekNext()
    {
        return IsAtEnd ? lexer.Tokens.Last() : lexer.Tokens[position + 1];
    }

    private bool Match(params TokenType[] tokenTypes)
    {
        foreach (TokenType type in tokenTypes)
        {
            if (type == Current.Type) return true;
        }
        return false;
    }
}


