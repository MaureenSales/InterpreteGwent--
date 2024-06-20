using System.Data;

namespace Interprete
{
    public class Parser
    {
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
                Statements.Add(Assignment());
            }

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
            ASTnode left = Indexer();
            if (Match(TokenType.Increment, TokenType.Decrement))
            {
                Token op = Advance();
                left = new UnaryInverseOp(left, op);
            }
            return left;
        }

        private ASTnode Indexer()
        {
            ASTnode left = Call();
            if(Match(TokenType.OpCurlyBracket))
            {
                position++;
                ASTnode index = Assignment();
                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClCurlyBracket, "']' is expected after indexer");
                left = new IndexList(left, index);
            }

            return left;
        }

        private ASTnode Call()
        {
            if(Match(TokenType.Identifier))
            {
                return PropertyOrCallMethod();
            }
            return PrimaryExpr();
        }
        
        private ASTnode PropertyOrCallMethod()
        {
            ASTnode object_;

            if (PeekNext().Type == TokenType.OpParenthesis)
            {
                object_ = CallMethod(Advance());
            }
            else object_ = new VariableReference(Advance());

            while (Match(TokenType.Dot))
            {
                position++;
                if (Match(TokenType.Identifier, TokenType.Power, TokenType.Faction, TokenType.Name))
                {
                    ASTnode property = PropertyOrCallMethod();
                    object_ = new Property(object_, property);
                }
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "An indentifier or method is expected", Current.Line, Current.Col);
            }
            return object_;
        }
        private ASTnode CallMethod(Token id)
        {
            Token methodName = id;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpParenthesis, "'(' is expected after the method name");
            List<ASTnode> arguments = new List<ASTnode>();
            if (Current.Type != TokenType.ClParenthesis && !IsAtEnd)
            {
                arguments.Add(Assignment());

                while (Current.Type == TokenType.Comma)
                {
                    position++;
                    //puede ser predicate
                    arguments.Add(Assignment());
                }
            }

            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClParenthesis, "')' is expected after arguments");
            return new CallMethod(methodName, arguments);
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
                    Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClParenthesis, "')' is expected after expression");
                    return new GroupedExpr(expr);
                case TokenType.While:
                    return While();
                case TokenType.For:
                    return For();
                case TokenType.effect:
                    return Effect();
                case TokenType.Card:
                    return Card();
                case TokenType.OpBraces:
                    if (PeekNext().Type == TokenType.Effect)
                    {
                        return CallToEffect(null);
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "An expression is expected", Current.Line, Current.Col);
                case TokenType.Semicolon:
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "';' should only be used when ending an expression in an expression block", Current.Line, Current.Col);
                case TokenType.OpCurlyBracket:
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "'[' ']' should only be used for indexed on proprties that return list or specifics fields", Current.Line, Current.Col);
                case TokenType.EOF:
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "An expression is expected", Current.Line, Current.Col);

                default:
                    throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "Unexpected characer", Current.Line, Current.Col);

            }
        }

        private PredicateLambda PredicateLambda()
        {
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpParenthesis, "'(' is expected before declaration of parameter of a predicate expression");
            Token parameter;
            if (Match(TokenType.Identifier))
            {
                parameter = Advance();
            }
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "An identifier is expected", Current.Line, Current.Col);
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClParenthesis, "')' is expected after declaration of parameter of a predicate expression");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Imply, "'=>' is expected before declaration of the body of a predicate expression");
            ASTnode body = Assignment();

            return new PredicateLambda(parameter, body);
        }

        private ASTnode While()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpParenthesis, "'(' is expected before declaration of condition of a while expression");
            ASTnode condition = Assignment();
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClParenthesis, "')' is expected after declaration of condition of a while expression");
            List<ASTnode> instructions = new List<ASTnode>();
            if (Match(TokenType.OpBraces))
            {
                instructions = ExprBlock();
            }
            else
            {
                instructions.Add(Assignment());
            }
            return new While(condition, instructions);
        }

        private ASTnode For()
        {
            position++;
            ASTnode element = PrimaryExpr();
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.In, "'In' keyword is expected");
            ASTnode collection = PrimaryExpr();
            List<ASTnode> instructions = new List<ASTnode>();
            if (Match(TokenType.OpBraces))
            {
                instructions = ExprBlock();
            }
            else
            {
                instructions.Add(Assignment());
            }
            return new For(element, collection, instructions);
        }

        private List<ASTnode> ExprBlock()
        {
            List<ASTnode> exprs = new List<ASTnode>();
            position++;
            do
            {
                exprs.Add(Assignment());
                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Semicolon, "';' is expected after expresion in expressions block");
            } while (!Match(TokenType.ClBraces));
            position++;
            return exprs;
        }

        private ASTnode Effect()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpBraces, "'{' is expected after declaration of an effect");
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
                            case 0: name = FieldsStrings(); fields[0] = 1; break;
                            case 1: parameters = Params(); fields[1] = 1; break;
                            case 2: actionFun = Action(); fields[2] = 1; break;
                        }
                    }
                    else if (fields[i] == 1 && Match(fieldsType[i]))
                    {
                        throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                    }
                }
                count++;
            }
            for (int i = 0; i < fields.Length; i++)
            {
                if (i == 1) continue;
                if (fields[i] == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Must define the {fieldsType[i]} field in declaration of an Effect", Current.Line, Current.Col);
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClBraces, "'}' is expected after body of an effect");
            return new Effect(name, parameters, actionFun);
        }

        private ASTnode FieldsStrings()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
            ASTnode value;
            if (Match(TokenType.String))
            {
                value = new String(Advance());
            }
            else
            {
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "A string is expected as value of the field", Current.Line, Current.Col);
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after value of a field");
            return value;
        }

        private ASTnode FieldsBoolean()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
            ASTnode value;
            if (Match(TokenType.True, TokenType.False))
            {
                value = new Boolean(Advance());
            }
            else
            {
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "A boolean is expected as value of the field", Current.Line, Current.Col);
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after value of a field");
            return value;
        }

        private ASTnode FieldsNumbers()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
            ASTnode value;
            if (Match(TokenType.Number))
            {
                value = new Number(Advance());
            }
            else
            {
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "A number is expected as value of the field", Current.Line, Current.Col);
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after value of a field");
            return value;
        }

        private List<ASTnode> ArrayDSL()
        {
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpCurlyBracket, "'[' is expected before declaration of an array");
            position--;
            List<ASTnode> elements = new List<ASTnode>();
            do
            {
                position++;
                elements.Add(Assignment());

            } while (Match(TokenType.Comma));
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClCurlyBracket, "']' is expected after declaration of an array");
            return elements;
        }

        private (VariableReference, ASTnode) Arguments()
        {
            VariableReference variable = new VariableReference(Advance());
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
            ASTnode value;
            if (Match(TokenType.Number))
            {
                value = new Number(Advance());
            }
            else
            {
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "A number is expected as value of the field", Current.Line, Current.Col);
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after value of a field");
            return (variable, value);
        }

        private List<ASTnode> Params()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a Params field");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpBraces, "'{' is expected before declaration of values of a Params field");
            List<ASTnode> parameters = new List<ASTnode>();
            position--;
            do
            {
                position++;
                ASTnode variable;
                if (Match(TokenType.Identifier)) variable = new VariableReference(Advance());
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, " An identifier is expected", Current.Line, Current.Col);
                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a parameter");
                Token type;
                if (Match(TokenType.TypeNumber, TokenType.TypeString, TokenType.Bool))
                {
                    type = Advance();
                }
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "The type of the declared parameter is expected", Current.Line, Current.Col);

                parameters.Add(new AssignmentWithType(variable, type, null));
            } while (Match(TokenType.Comma));
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClBraces, "'}' is expected after declaration of values of a Params field");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after declaration of the Params field");
            return parameters;
        }

        private ASTnode Action()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of an Action function");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpParenthesis, "'(' is expected before the parameters of an Action function");
            List<Token> parameters = new List<Token>();
            if (!Match(TokenType.ClParenthesis))
            {
                position--;
                do
                {
                    position++;
                    Token parameter;
                    if (Match(TokenType.Identifier)) parameter = Advance();
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, " An identifier is expected", Current.Line, Current.Col);
                    parameters.Add(parameter);
                } while (Match(TokenType.Comma));
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClParenthesis, "')' is expected after parameters");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Imply, "'=>' is expected after declaration of parameter of an Action function");
            List<ASTnode> body = new List<ASTnode>();
            if (Match(TokenType.OpBraces)) body = ExprBlock();
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, "'{' is expected before the body of the function", Current.Line, Current.Col);
            return new ActionFun(parameters, body);
        }

        private ASTnode Card()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpBraces, "'{' is expected after declaration of a card");
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
                            case 0: type = FieldsStrings(); fields[0] = 1; break;
                            case 1: name = FieldsStrings(); fields[1] = 1; break;
                            case 2: faction = FieldsStrings(); fields[2] = 1; break;
                            case 3: power = FieldsNumbers(); fields[3] = 1; break;
                            case 4:
                                position++;
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                                range = ArrayDSL();
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after declaration of the a field");
                                fields[4] = 1; break;
                            case 5:
                                position++;
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                                effects = ArrayDSL();
                                fields[5] = 1; break;
                        }
                    }
                    else if (fields[i] == 1 && Match(fieldsType[i]))
                    {
                        throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                    }
                }
                count++;
            }
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Must define the {fieldsType[i]} field in declaration of a Card", Current.Line, Current.Col);
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClBraces, "'}' is expected after body of a card");
            return new Card(type, name, faction, power, range, effects);

        }

        private CallEffect CallToEffect(Selector? parent)
        {
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpBraces, "'{' is expected before declaration of an effect in body of a card");
            ASTnode name = null!;
            Dictionary<VariableReference, ASTnode> parameters = new Dictionary<VariableReference, ASTnode>();
            Selector? selector = null!;
            CallEffect? postAction = null;
            int[] fields = new int[3];
            TokenType[] fieldsType = { TokenType.Effect, TokenType.Selector, TokenType.PostAction };
            int count = 0;
            while (count < 3)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] == 0 && Match(fieldsType[i]))
                    {
                        switch (i)
                        {
                            case 0: EffectField(ref name!, ref parameters); fields[0] = 1; break;
                            case 1: selector = SelectorField(); fields[1] = 1; break;
                            case 2:
                                position++;
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpBraces, "'{' is expected before the values of a field");
                                postAction = CallToEffect(selector);
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClBraces, "'}' is expected after body of a field");
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after declaration of the a field");
                                fields[2] = 1; break;
                        }
                    }
                    else if (fields[i] == 1 && Match(fieldsType[i]))
                    {
                        throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                    }
                }
                count++;
            }
            for (int i = 0; i < fields.Length - 1; i++)
            {
                if (fields[i] == 0)
                {
                    if (i == 1 && parent != null) selector = parent;
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Must define the {fieldsType[i]} field in declaration of an OnActivation field", Current.Line, Current.Col);
                }
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClBraces, "'}' is expected after declaration of an effect in body of a card");
            return new CallEffect(name!, parameters, selector, postAction);
        }

        private void EffectField(ref ASTnode? name, ref Dictionary<VariableReference, ASTnode> parameters)
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of an Effect field");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpBraces, "'{' is expected before the values of an Effect field");
            if (PeekNext().Type == TokenType.String)
            {
                position++;
                name = new String(Advance());
            }
            else
            {
                name = FieldsStrings();
            }

            while (!Match(TokenType.ClBraces))
            {
                var element = Arguments();
                parameters.Add(element.Item1, element.Item2);
            }
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after declaration of the Effect field");
        }

        private Selector SelectorField()
        {
            position++;
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a Selector field");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.OpBraces, "'{' is expected before the values of a Selector field");
            ASTnode source = null!, single = null!;
            PredicateLambda predicate = null!;
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
                            case 0: source = FieldsStrings(); fields[0] = 1; break;
                            case 1: single = FieldsBoolean(); fields[1] = 1; break;
                            case 2:
                                position++;
                                Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Colon, "':' is expected after declaration of a field");
                                predicate = PredicateLambda(); fields[2] = 1; break;
                        }
                    }
                    else if (fields[i] == 1 && Match(fieldsType[i]))
                    {
                        throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Field {Current.Lexeme} already defined", Current.Line, Current.Col);
                    }
                }
                count++;
            }
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SINTACTIC, $"Must define the {fieldsType[i]} field in declaration of a Selector field", Current.Line, Current.Col);
            }
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.ClBraces, "'}' is expected after declaration of a field");
            Consume(ErrorExceptions.ErrorType.SINTACTIC, TokenType.Comma, "',' is expected after declaration of a field");
            return new Selector(source, single, predicate);
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
}