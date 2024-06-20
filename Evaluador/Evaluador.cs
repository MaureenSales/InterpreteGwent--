using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace Interprete
{
    public class Evaluador : IVsitor<object?>
    {
        public Stack<Dictionary<string, object?>> VariableScopes;

        public Evaluador()
        {
            VariableScopes = new();
            EnterScope();
        }

        public void EnterScope()
        {
            VariableScopes.Push(new Dictionary<string, object?>());
        }

        public void ExitScope()
        {
            VariableScopes.Pop();
        }

        public object? evaluate(ASTnode expr)
        {
            return expr.Accept(this);
        }
        public object? Visit(UnaryOp expr)
        {
            object? right = evaluate(expr.Right);

            switch (expr.Op.Type)
            {
                case TokenType.Subtraction:
                    CheckNumberOperand(right);
                    return -(double)right!;
                case TokenType.Negation:
                    IsTruthy(right);
                    if (right is false) return true;
                    else return false;
                case TokenType.Increment:
                case TokenType.Decrement:
                    bool find = false;
                    if(expr.Right.GetType() != typeof(Property) && expr.Right.GetType() != typeof(VariableReference) || expr.Right.GetType() != typeof(IndexList))
                    {
                        ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "The operand of an increase or decrease operator must be a variable, an property or an indexer", 0, 0);
                    }

                    if (expr.Right is VariableReference variable1)
                    {
                        foreach (var item in VariableScopes)
                        {
                            if (item.ContainsKey(variable1.Name))
                            {
                                if (item[variable1.Name] is double)
                                {
                                    var value = (double)item[variable1.Name]!;
                                    if (expr.Op.Type == TokenType.Increment) item[variable1.Name] = ++value;
                                    else item[variable1.Name] = --value;
                                    find = true;
                                    break;
                                }

                            }
                        }
                        if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + ((VariableReference)expr.Right).Name + "'. ", 0, 0);
                    }
                    return null;
            }

            return right;
        }

        public object? Visit(Number number)
        {
            return number.Value;
        }

        public object? Visit(String @string)
        {
            return @string.Value;
        }

        public object? Visit(Boolean boolean)
        {
            return boolean.Value;
        }

        public object? Visit(LogicalBinOp expr)
        {
            object? left = evaluate(expr.Left);

            if (expr.Op.Type == TokenType.Disjunction)
            {
                if (IsTruthy(left)) return left;
            }
            else
            {
                if (!IsTruthy(left)) return left;
            }

            return evaluate(expr.Right);
        }

        private bool IsTruthy(object? ob)
        {
            if (ob == null) return false;
            if (ob is bool) return (bool)ob;
            throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Operand must be a boolean.", 0, 0);
        }

        private void CheckNumberOperand(params object?[] operands)
        {
            foreach (var item in operands)
            {
                if (!(item is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Operand must be a number.", 0, 0);
            }
        }

        public object? Visit(ArithmeticBinOp expr)
        {
            object? left = evaluate(expr.Left);
            object? right = evaluate(expr.Right);
            CheckNumberOperand(left, right);

            switch (expr.Op.Type)
            {
                case TokenType.Sum:
                    return (double)left! + (double)right!;
                case TokenType.Subtraction:
                    return (double)left! - (double)right!;
                case TokenType.Product:
                    return (double)left! * (double)right!;
                case TokenType.Division:
                    return (double)left! / (double)right!;
                case TokenType.Modulo:
                    return (double)left! % (double)right!;
                case TokenType.Pow:
                    return Math.Pow((double)left!, (double)right!);
            }

            return null;
        }

        public object? Visit(GroupedExpr expr)
        {
            return evaluate(expr.Group);
        }

        public object? Visit(ComparisonBinOp expr)
        {
            object? left = evaluate(expr.Left);
            object? right = evaluate(expr.Right);
            switch (expr.Op.Type)
            {
                case TokenType.LessThan:
                    CheckNumberOperand(left, right);
                    return (double)left! < (double)right!;
                case TokenType.LessOrEqual:
                    CheckNumberOperand(left, right);
                    return (double)left! <= (double)right!;
                case TokenType.GreaterThan:
                    CheckNumberOperand(left, right);
                    return (double)left! > (double)right!;
                case TokenType.GreaterOrEqual:
                    CheckNumberOperand(left, right);
                    return (double)left! >= (double)right!;
                case TokenType.Equality:
                    return Equals(left, right);
                case TokenType.NotEqual:
                    return !Equals(left, right);
            }
            return null;
        }

        public object? Visit(ConcatBinOp expr)
        {
            object? left = evaluate(expr.Left);
            object? right = evaluate(expr.Right);
            if (left == null || right == null)
            {
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Null reference operand", 0, 0);
            }
            string? a = left as string;
            string? b = right as string;
            if (a == null || b == null)
            {
                throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $" Operator {expr.Op.Lexeme} cann't be used between " + left.GetType() + " and " + right.GetType() + "types. ", 0, 0);
            }
            if (expr.Op.Type == TokenType.Concat) return a + b;
            else return a + " " + b;
        }

        public object? Visit(VariableReference expr)
        {
            foreach (var item in VariableScopes)
            {
                if (item.ContainsKey(expr.Name))
                {
                    object? x = item[expr.Name];
                    return x;
                }

            }
            return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + expr.Name + "'. ", 0, 0);
        }

        public object? Visit(Assignment expr)
        {
            object? value = evaluate(expr.Value!);
            //variable
            bool find = false;
            if (expr.Variable is VariableReference variable1)
            {
                foreach (var item in VariableScopes)
                {
                    if (item.ContainsKey(variable1.Name))
                    {
                        item[variable1.Name] = value;
                        find = true;
                        break;
                    }
                }

                if (!find)
                {
                    VariableScopes.Peek().Add(variable1.Name, value);
                }
            }
            else
            {
                //implementar
            }

            return null;
        }

        public object? Visit(Property property)
        {
            object? obj = evaluate(property.Object);
            //if(obj!.GetType() != typeof(Context)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "");
            return null;
        }

        public object? Visit(CallMethod callMethod)
        {
            throw new NotImplementedException();
        }

        public object? Visit(UnaryInverseOp expr)
        {
            object? left = evaluate(expr.Left);
            bool find = false;
            if(expr.Left.GetType() == typeof(Property)&& expr.Left.GetType() != typeof(VariableReference) || expr.Left.GetType() != typeof(IndexList))
            {
                ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "The operand of an increase or decrease operator must be a variable, an property or an indexer", 0, 0);
            }

            if (expr.Left is VariableReference variable)
            {
                foreach (var item in VariableScopes)
                {
                    if (item.ContainsKey(variable.Name))
                    {
                        if (item[variable.Name] is double)
                        {
                            var value = (double)item[variable.Name]!;
                            if (expr.Op.Type == TokenType.Increment) item[variable.Name] = value++;
                            else item[variable.Name] = value--;
                            find = true;
                            break;
                        }

                    }
                }
                if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + ((VariableReference)expr.Left).Name + "'. ", 0, 0);
            }
            return null;
        }

        public object? Visit(PredicateLambda predicateLambda)
        {
            throw new NotImplementedException();
        }

        public object? Visit(ArithmeticAssignment expr)
        {
            object? num = evaluate(expr.Value);
            if (!(num is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"Cann't {expr.Op} between {evaluate(expr.Variable)!.GetType()} and {num!.GetType()}", 0, 0);
            bool find = false;
            if (expr.Variable is VariableReference variable1)
            {
                foreach (var item in VariableScopes)
                {
                    if (item.ContainsKey(variable1.Name))
                    {
                        if (item[variable1.Name] is double)
                        {
                            var value = (double)item[variable1.Name]!;
                            switch (expr.Op.Type)
                            {
                                case TokenType.SumAssignment:
                                    item[variable1.Name] = value + (double)num; break;
                                case TokenType.MinusAssignment:
                                    item[variable1.Name] = value - (double)num; break;
                                case TokenType.ProductAssignment:
                                    item[variable1.Name] = value * (double)num; break;
                                case TokenType.DivisionAssignment:
                                    item[variable1.Name] = value / (double)num; break;
                                case TokenType.ModuloAssignment:
                                    item[variable1.Name] = value % (double)num; break;
                            }
                            find = true;
                            break;
                        }

                    }
                }
                if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + ((VariableReference)expr.Variable).Name + "'. ", 0, 0);
            }
            return null;
        }

        public object? Visit(While @while)
        {
            throw new NotImplementedException();
        }

        public object? Visit(For @for)
        {
            throw new NotImplementedException();
        }

        public object? Visit(Effect effect)
        {
            throw new NotImplementedException();
        }

        public object? Visit(ActionFun actionFun)
        {
            throw new NotImplementedException();
        }

        public object? Visit(AssignmentWithType assignmentWithType)
        {
            throw new NotImplementedException();
        }

        public object? Visit(Selector selector)
        {
            throw new NotImplementedException();
        }

        public object? Visit(CallEffect callEffect)
        {
            throw new NotImplementedException();
        }

        public object? Visit(Card card)
        {
            throw new NotImplementedException();
        }

        public object? Visit(IndexList indexList)
        {
            throw new NotImplementedException();
        }
    }
}