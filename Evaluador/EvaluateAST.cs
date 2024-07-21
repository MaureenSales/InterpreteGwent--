using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
using Unity.VisualScripting;
#nullable enable

public class Evaluador : IVsitor<object?>
{
    public Stack<Dictionary<string, object?>> VariableScopes;
    public List<(string, (Dictionary<string, object>, ASTnode))> Selectors;
    public List<object>? SelectorsList;
    private IList? listForMethod;
    private LocationCards location;

    public Evaluador()
    {
        VariableScopes = new();
        Selectors = new();
        SelectorsList = null;
        listForMethod = null;
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

    public object? Evaluate(Effect effect, Skill skill, IList targets)
    {
        EnterScope();
        foreach (var param in skill.Arguments!)
        {
            VariableScopes.Peek()[param.Key] = param.Value;
        }

        ActionFun? action = effect.Action as ActionFun;
        Debug.Log(action is null);
        VariableReference? param1 = action!.Parameters[0] as VariableReference;
        Debug.Log(param1 is null);
        Debug.Log(param1!.Name);
        VariableScopes.Peek()[param1!.Name] = targets;
        VariableReference? param2 = action!.Parameters[1] as VariableReference;
        VariableScopes.Peek()[param2!.Name] = new Context();

        evaluate(effect.Action);
        ExitScope();
        return null;
    }

    public object? Visit(UnaryOp expr)
    {
        object? right = evaluate(expr.Right);
        object? result = null;
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
                if (expr.Right.GetType() != typeof(Property) && expr.Right.GetType() != typeof(VariableReference))
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
                                result = item[variable1.Name];
                                find = true;
                                break;
                            }
                            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{expr.Op.Lexeme} operator cannot be applied to {item[variable1.Name]!.GetType()}", 0, 0);

                        }
                    }
                    if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + ((VariableReference)expr.Right).Name + "'. ", 0, 0);
                }
                if (expr.Right is Property property)
                {
                    object? obj = evaluate(property.Object);
                    string access = "";
                    PropertyIsValid(ref access, property, obj);

                    if (obj is Card card)
                    {
                        SetterPropertyIsPublic(obj, access);
                        Unit unit = (Unit)card;
                        if (expr.Op.Type == TokenType.Increment) ++unit.Power;
                        else --unit.Power;
                        result = unit.Power;
                    }
                    else if (obj is GameObject cardUI)
                    {
                        SetterPropertyIsPublic(cardUI.GetComponent<ThisCard>().thisCard, access);
                        if (expr.Op.Type == TokenType.Increment) cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) + 1).ToString();
                        else cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) - 1).ToString(); ;
                        result = int.Parse(cardUI.GetComponent<ThisCard>().powerText.text);
                    }
                    else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible", 0, 0);
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
                }
                else if (expr.Right is IndexList indexList)
                {
                    var i = evaluate(indexList.Index)!;
                    int index = (int)(double)i;
                    if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero", 0, 0);
                    var List = evaluate(indexList.List);
                    if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}", 0, 0);
                    else
                    {
                        SetterIndexerIsValid(list, index, i);
                        var value = (double)list[index]!;
                        if (expr.Op.Type == TokenType.Increment) list[index] = ++value;
                        else list[index] = --value;
                        result = value;
                    }
                }
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el operador {expr.Op.Lexeme} solo se puede aplicar a una variable, una propiedad o un indizador", 0, 0);

                return null;
        }

        return result;
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
                return System.Math.Pow((double)left!, (double)right!);
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
        return FindVariable(expr); ;
    }

    private object? FindVariable(VariableReference expr)
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
        if (value is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la expresion derecha de una asignacion debe ser directamente un valor o una variable de referencia", 0, 0);

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
        else if (expr.Variable is Property property)
        {
            object? obj = evaluate(property.Object);
            string access = "";
            PropertyIsValid(ref access, property, obj);
            if (!(value is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir implicitamente el tipo {obj!.GetType()} en int", 0, 0);

            if (obj is Card card)
            {
                SetterPropertyIsPublic(obj, access);
                Unit unit = (Unit)card;
                unit.Power = (int)(double)value;
            }
            else if (obj is GameObject cardUI)
            {
                SetterPropertyIsPublic(cardUI.GetComponent<ThisCard>().thisCard, access);
                cardUI.GetComponent<ThisCard>().powerText.text = value.ToString();
            }
            else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible", 0, 0);
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
        }
        else if (expr.Variable is IndexList indexList)
        {
            var i = evaluate(indexList.Index)!;
            int index = (int)(double)i;
            if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero", 0, 0);
            var List = evaluate(indexList.List);
            if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}", 0, 0);
            else
            {
                SetterIndexerIsValid(list, index, value);
                list[index] = value;
            }
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la parte izquierda de una asignacion debe ser una variable, una propiedad o un indizador", 0, 0);
        return value;
    }

    public object? Visit(Property property)
    {
        object? obj = evaluate(property.Object);
        string access = "";
        PropertyIsValid(ref access, property, obj);
        if (obj is GameObject cardUI)
        {
            if (access != "Power") obj = cardUI.GetComponent<ThisCard>().thisCard;
            else
            {
                PropertyIsValid(cardUI.GetComponent<ThisCard>().thisCard, access);
                return int.Parse(cardUI.GetComponent<ThisCard>().powerText.text);
            }
        }
        if (obj is Card card)
        {
            PropertyInfo propertyInfo = PropertyIsValid(obj, access);
            if (access == "Faction") return propertyInfo.GetValue(card).ToString();
            else if (access == "Type")
            {
                string type = propertyInfo.GetValue(card).ToString();
                Debug.Log(type);
                switch (type)
                {
                    case "HeroUnit": return "Oro";
                    case "DecoyUnit": return "Senuelo";
                    case "Boost": return "Aumento";
                    case "Clear": return "Despeje";
                    case "Leader": return "Lider";
                    case "Unit": return "Plata";
                    case "Weather": return "Clima";
                }
            }
            else if (access == "Power")
            {
                Debug.Log(propertyInfo.GetValue(card).GetType().ToString());
                double result = (double)(int)propertyInfo.GetValue(card);
                return result;
            }
            return propertyInfo.GetValue(card);
        }
        else if (obj is Context context)
        {
            if (property.PropertyAccess is CallMethod method) return evaluate(method);
            return PropertyIsValid(obj, access).GetValue(context);
        }
        else if (obj is IList list)
        {
            listForMethod = list;
            if (property.PropertyAccess is CallMethod method) return evaluate(property.PropertyAccess);
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {evaluate(property.PropertyAccess)}", 0, 0);
        }
        return null;
    }

    public object? Visit(CallMethod callMethod)
    {
        if (!TokenTypeExtensions.Methods.ContainsKey(callMethod.MethodName.Lexeme)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el metodo {callMethod.MethodName.Lexeme} no esta definido", 0, 0);
        if (callMethod.Arguments.Count != TokenTypeExtensions.Methods[callMethod.MethodName.Lexeme]) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no existe una sobrecarga de {callMethod.MethodName.Lexeme} con {callMethod.Arguments.Count} parametros", 0, 0);
        Context context = new Context();
        List<object> result = new List<object>();
        string player = "";
        switch (callMethod.MethodName.Lexeme)
        {
            case "Pop":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Pop no esta definido", 0, 0);
                context.Pop(listForMethod);
                break;
            case "Add":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Add no esta definido", 0, 0);
                break;
            case "Shuffle":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Shuffle no esta definido", 0, 0);
                context.Shuffle(listForMethod);
                break;
            case "DeckOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                return context.FilterOfCards(LocationCards.Deck, player);
            case "HandOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                return context.FilterOfCards(LocationCards.Hand, player);
            case "FieldOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                return context.FilterOfCards(LocationCards.Field, player);
            case "GraveyardOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                return context.FilterOfCards(LocationCards.Graveyard, player);
            case "Push":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Push no esta definido", 0, 0);
                break;
            case "Remove":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Remove no esta definido", 0, 0);
                context.Remove(evaluate(callMethod.Arguments[0])!, listForMethod);
                break;
            case "Find":
                EnterScope();
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Find no esta definido", 0, 0);
                PredicateLambda? predicate = callMethod.Arguments[0] as PredicateLambda;
                VariableReference? card = predicate!.Parameter as VariableReference;
                foreach (var element in listForMethod)
                {
                    VariableScopes.Peek()[card!.Name] = element;
                    var target = evaluate(predicate);
                    if (target != null) result.Add(card);
                }
                ExitScope();
                return result;
            case "SendBottom":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo SendBottom no esta definido", 0, 0);

                break;
        }
        return null;
    }

    public object? Visit(UnaryInverseOp expr)
    {
        Debug.Log(expr.Left.GetType());
        object? left = evaluate(expr.Left);
        Debug.Log(left);
        bool find = false;
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
                    else if (item[variable.Name] is null) continue;
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir implicitamente del tipo {item[variable.Name]!.GetType()} a int", 0, 0);

                }
            }
            if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + ((VariableReference)expr.Left).Name + "'. ", 0, 0);
        }
        else if (expr.Left is Property property)
        {
            object? obj = evaluate(property.Object);
            string access = "";
            PropertyIsValid(ref access, property, obj);

            if (obj is Card card)
            {
                SetterPropertyIsPublic(obj, access);
                Unit unit = (Unit)card;
                if (expr.Op.Type == TokenType.Increment) unit.Power++;
                else unit.Power--;

            }
            else if (obj is GameObject cardUI)
            {
                SetterPropertyIsPublic(cardUI.GetComponent<ThisCard>().thisCard, access);
                if (expr.Op.Type == TokenType.Increment) cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) + 1).ToString();
                else cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) - 1).ToString();
            }
            else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible", 0, 0);
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
        }
        else if (expr.Left is IndexList indexList)
        {
            var i = evaluate(indexList.Index)!;
            int index = (int)(double)i;
            if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero", 0, 0);
            var List = evaluate(indexList.List);
            if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}", 0, 0);
            else
            {
                SetterIndexerIsValid(list, index, i);
                var value = (double)list[index]!;
                if (expr.Op.Type == TokenType.Increment) list[index] = value++;
                else list[index] = value--;
            }
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el operador {expr.Op.Lexeme} solo se puede aplicar a una variable, una propiedad o un indizador", 0, 0);
        return left;
    }

    public object? Visit(PredicateLambda predicateLambda)
    {
        var target = evaluate(predicateLambda.Parameter);
        if (!(evaluate(predicateLambda.BodyPredicate) is bool condition)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Un predicado debe devolver un booleano", 0, 0);
        else if (condition) return target;
        else return null;
    }

    public object? Visit(ArithmeticAssignment expr)
    {
        object? num = evaluate(expr.Value);
        if (!(num is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"Cann't {expr.Op} between {evaluate(expr.Variable)!.GetType()} and {num!.GetType()}", 0, 0);
        bool find = false;
        double result = 0;
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
                                item[variable1.Name] = value + (double)num; ; break;
                            case TokenType.MinusAssignment:
                                item[variable1.Name] = value - (double)num; break;
                            case TokenType.ProductAssignment:
                                item[variable1.Name] = value * (double)num; break;
                            case TokenType.DivisionAssignment:
                                item[variable1.Name] = value / (double)num; break;
                            case TokenType.ModuloAssignment:
                                item[variable1.Name] = value % (double)num; break;
                        }
                        result = (double)item[variable1.Name]!;
                        find = true;
                        break;
                    }

                }
            }
            if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + ((VariableReference)expr.Variable).Name + "'. ", 0, 0);
        }
        if (expr.Variable is Property property)
        {
            object? obj = evaluate(property.Object);
            string access = "";
            PropertyIsValid(ref access, property, obj);

            if (obj is Card card)
            {
                SetterPropertyIsPublic(obj, access);
                Unit unit = (Unit)card;
                switch (expr.Op.Type)
                {
                    case TokenType.SumAssignment:
                        unit.Power += (int)(double)num; ; break;
                    case TokenType.MinusAssignment:
                        unit.Power -= (int)(double)num; break;
                    case TokenType.ProductAssignment:
                        unit.Power *= (int)(double)num; break;
                    case TokenType.DivisionAssignment:
                        unit.Power /= (int)(double)num; break;
                    case TokenType.ModuloAssignment:
                        unit.Power %= (int)(double)num; break;
                }
                result = unit.Power;

            }
            else if (obj is GameObject cardUI)
            {
                SetterPropertyIsPublic(cardUI.GetComponent<ThisCard>().thisCard, access);
                switch (expr.Op.Type)
                {
                    case TokenType.SumAssignment:
                        cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) + (int)(double)num).ToString(); break;
                    case TokenType.MinusAssignment:
                        cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) - (int)(double)num).ToString(); break;
                    case TokenType.ProductAssignment:
                        cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) * (int)(double)num).ToString(); break;
                    case TokenType.DivisionAssignment:
                        cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) / (int)(double)num).ToString(); break;
                    case TokenType.ModuloAssignment:
                        cardUI.GetComponent<ThisCard>().powerText.text = (int.Parse(cardUI.GetComponent<ThisCard>().powerText.text) % (int)(double)num).ToString(); break;
                }
                result = int.Parse(cardUI.GetComponent<ThisCard>().powerText.text);
            }
            else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible", 0, 0);
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
        }
        else if (expr.Variable is IndexList indexList)
        {
            var i = evaluate(indexList.Index)!;
            int index = (int)(double)i;
            if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero", 0, 0);
            var List = evaluate(indexList.List);
            if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}", 0, 0);
            else
            {
                SetterIndexerIsValid(list, index, num);
                var value = (double)list[index]!;
                switch (expr.Op.Type)
                {
                    case TokenType.SumAssignment:
                        list[index] = value + (double)num; ; break;
                    case TokenType.MinusAssignment:
                        list[index] = value - (double)num; break;
                    case TokenType.ProductAssignment:
                        list[index] = value * (double)num; break;
                    case TokenType.DivisionAssignment:
                        list[index] = value / (double)num; break;
                    case TokenType.ModuloAssignment:
                        list[index] = value % (double)num; break;
                }
                result = (double)list[index];
            }
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la parte izquierda de una asignacion debe ser una variable, una propiedad o un indizador", 0, 0);
        return result;
    }

    public object? Visit(While @while)
    {
        EnterScope();
        while (IsTruthy(evaluate(@while.Condition)))
        {
            foreach (var item in @while.Instructions)
            {
                evaluate(item);
            }
        }
        ExitScope();
        return null;
    }

    public object? Visit(For @for)
    {
        EnterScope();
        var element = @for.Element as VariableReference;
        VariableScopes.Peek().Add(element!.Name, null);
        var list = evaluate(@for.Collection);
        if (list is IList collection)
        {
            foreach (var item in collection)
            {
                VariableScopes.Peek()[element!.Name] = item;
                foreach (var instrctruction in @for.Instructions)
                {
                    evaluate(instrctruction);
                }
            }
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "El ciclo 'for' solo puede ser usado en listas", 0, 0);
        ExitScope();
        return null;
    }

    public object? Visit(Effect effect)
    {
        string? name = evaluate(effect.Name) as string;
        if (name is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el capo Name de un effecto es de tipo string", 0, 0);
        if (!Global.EffectsCreated.ContainsKey(name!))
        {
            Global.EffectsCreated.Add(name!, effect);
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el efecto {name!} ya esta definido", 0, 0);
        return null;
    }

    public object? Visit(ActionFun actionFun)
    {
        EnterScope();
        foreach (var instruction in actionFun.Body)
        {
            evaluate(instruction);
        }
        ExitScope();
        return null;
    }

    public object? Visit(AssignmentWithType assignmentWithType)
    {
        var variable = evaluate(assignmentWithType.Variable);
        switch (assignmentWithType.TypeVar.Type)
        {
            case TokenType.TypeString:
                if (!(variable is string)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir de tipo {variable!.GetType()} a string", 0, 0);
                break;
            case TokenType.TypeNumber:
                if (!(variable is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir de tipo {variable!.GetType()} a number", 0, 0);
                break;
            case TokenType.Bool:
                if (!(variable is bool)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir de tipo {variable!.GetType()} a bool", 0, 0);
                break;
        }
        return null;
    }

    public object? Visit(Selector selector)
    {
        string? source = evaluate(selector.Source) as string;
        bool firstValue = (bool)evaluate(selector.Single)!;
        Context context = new Context();
        List<object> targets = new List<object>();
        EnterScope();
        PredicateLambda? predicate = selector.Predicate as PredicateLambda;
        VariableReference? variable = predicate!.Parameter as VariableReference;
        string? name = variable!.Name;
        List<GameObject> sources = new List<GameObject>();
        List<Card> cards = new List<Card>();

        switch (source)
        {
            case "board":
                Debug.Log("source: board");
                sources = context.FilterOfCards(LocationCards.Board, context.TriggerPlayer);
                Debug.Log(sources.Count);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "hand":
                Debug.Log("source: hand");
                sources = context.FilterOfCards(LocationCards.Hand, context.TriggerPlayer);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "otherHand":
                Debug.Log("source: otherHand");
                sources = context.FilterOfCards(LocationCards.Hand, context.OtherPlayer);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "deck":
                Debug.Log("source: deck");
                cards = context.GetDeck(context.TriggerPlayer);
                foreach (var card in cards)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(target);
                }
                break;
            case "otherDeck":
                Debug.Log("source: otherDeck");
                cards = context.GetDeck(context.OtherPlayer);
                foreach (var card in cards)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(target);
                }
                break;
            case "field":
                Debug.Log("source: field");
                sources = context.FilterOfCards(LocationCards.Field, context.TriggerPlayer);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "otherField":
                Debug.Log("source: otherField");
                Debug.Log(context.OtherPlayer);
                sources = context.FilterOfCards(LocationCards.Field, context.OtherPlayer);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "parent":
                Debug.Log("source: parent");
                Debug.Log(SelectorsList is null);
                if (SelectorsList is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "No existe una fuente previamente definida", 0, 0);
                else
                {
                    foreach (var card in SelectorsList)
                    {
                        VariableScopes.Peek()[name] = card;
                        var target = evaluate(predicate);
                        if (target != null) targets.Add(card);
                    }
                }
                Debug.Log(targets.Count);
                break;
        }

        if (firstValue)
        {
            var target = targets[0];
            targets = new List<object> { target };
        }

        SelectorsList = targets;
        Debug.Log(targets.Count);
        ExitScope();
        return null;

    }

    public object? Visit(CallEffect callEffect)
    {
        EnterScope();
        string? name = evaluate(callEffect.Name) as string;
        if (name == null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Name de un efecto es de tipo string", 0, 0);
        if (!Global.EffectsCreated.ContainsKey(name)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El efecto {name} no esta definido", 0, 0);
        Effect effect = (Effect)Global.EffectsCreated[name];

        Dictionary<string, object> arguments = new Dictionary<string, object>();
        if (effect.Params.Count != callEffect.Parameters.Count) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El efecto {name} recibe {effect.Params.Count} argumentos", 0, 0);
        foreach (var assignment in callEffect.Parameters)
        {
            evaluate(assignment);
            Assignment? argument = assignment as Assignment;
            VariableReference? variable = argument!.Variable as VariableReference;
            arguments[variable!.Name] = evaluate(variable)!;
        }

        foreach (var declaration in effect.Params)
        {
            evaluate(declaration);
        }

        if (!(callEffect.Selector is null))
        {
            Selector? selector = callEffect.Selector as Selector;
            string? source = evaluate(selector!.Source) as string;
            if (source == null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Source de un selector es de tipo string", 0, 0);
            if (!(evaluate(selector.Single) is bool)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Single de un Selector es de tipo booleano", 0, 0);
            if (!Global.Sources.Contains(source)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"la fuente {source} no esta definida", 0, 0);
            Selectors.Add((name, (arguments, callEffect.Selector)));
        }
        else SelectorsList = null;
        if (!(callEffect.PostAction is null))
        {
            evaluate(callEffect.PostAction);
        }


        ExitScope();
        return null;
    }

    public object? Visit(CardDeclaration card)
    {
        string? type = evaluate(card.Type) as string;
        if (type is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Type de una carta es de tipo string", 0, 0);
        if (!Global.TypeCards.Contains(type)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El tipo {type} no esta definido", 0, 0);

        string? name = evaluate(card.Name) as string;
        if (name is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Name de una carta es de tipo string", 0, 0);

        string? faction = evaluate(card.Faction) as string;
        if (faction is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Faction de una carta es de tipo string", 0, 0);
        bool tryParse = Enum.TryParse(faction, out Global.Factions fact);
        if (!tryParse) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"La faccion {faction} no esta definida", 0, 0);
        Global.Factions Faction = fact;
        double power;
        if (!(evaluate(card.Power) is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Power de una carta es de tipo number", 0, 0);
        else power = (double)evaluate(card.Power)!;

        List<Global.AttackModes> attackModes = new List<Global.AttackModes>();
        if (card.Range.Count > 3) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "solo exiten tres posibles rangos", 0, 0);
        foreach (var element in card.Range)
        {
            string? range = evaluate(element) as string;
            if (range is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Range de una carta esta compuesto por elementos de tipo string", 0, 0);
            tryParse = Enum.TryParse(range, out Global.AttackModes mode);
            if (!tryParse) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El rango {range} no esta definido", 0, 0);
            else attackModes.Add(mode);
        }

        foreach (var effect in card.Effects)
        {
            evaluate(effect);
            SelectorsList = null;
        }
        List<Skill> skills = new List<Skill>();
        foreach (var skill in Selectors)
        {
            Debug.Log(skill.Item1);
            skills.Add(new Skill(skill.Item1, skill.Item2.Item1, skill.Item2.Item2));
        }
        Card newCard = null!;
        //lider
        switch (type)
        {
            case "Oro":
                newCard = new HeroUnit(name, Faction, skills, "", (int)power, attackModes, Resources.Load<Sprite>("image"));
                break;
            case "Plata":
                newCard = new Unit(name, Faction, skills, "", (int)power, attackModes, Resources.Load<Sprite>("image"));
                break;
            case "Lider":
                break;
            case "Aumento":
                newCard = new Boost(name, skills, "", Resources.Load<Sprite>("image"));
                break;
            case "Clima":
                newCard = new Weather(name, skills, "", Resources.Load<Sprite>("image"), Resources.Load<Sprite>("Frost"));
                break;
            case "Despeje":
                newCard = new Clear(name, skills, "", Resources.Load<Sprite>("image"));
                break;
        }
        Debug.Log(CardDataBase.Decks.Count);
        foreach (var deck in CardDataBase.Decks.Values)
        {
            deck.AddCard(newCard);
        }
        return null;
    }

    public object? Visit(IndexList indexList)
    {
        var i = evaluate(indexList.Index)!;
        int index = (int)(double)i;
        if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero", 0, 0);
        var List = evaluate(indexList.List);
        if (!(List is IList)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}", 0, 0);
        else
        {
            return ((IList)List)[index];
        }
    }

    private void PropertyIsValid(ref string access, Property property, object? obj)
    {
        if (property.PropertyAccess is VariableReference variable)
        {
            access = variable.Name;
            if (!TokenTypeExtensions.Properties.Contains(access)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene una definicion para {access}", 0, 0);
            access = variable.Name;
        }
        else if (property.PropertyAccess is CallMethod method)
        {
            access = method.MethodName.Lexeme;
            if (!TokenTypeExtensions.Methods.ContainsKey(access)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene una definicion para {access}", 0, 0);
        }
    }
    private PropertyInfo PropertyIsValid(object obj, string access)
    {
        PropertyInfo? propertyInfo = obj.GetType().GetProperty(access);
        if (propertyInfo is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
        return propertyInfo;
    }

    private void SetterIndexerIsValid(IList list, int index, object value)
    {
        PropertyInfo IsReadOnly = list.GetType().GetProperty("IsReadOnly");
        if (IsReadOnly != null) if ((bool)IsReadOnly.GetValue(list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la lista es de solo lectura", 0, 0);
        if (list[index].GetType() != value.GetType()) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir implicitamente del tipo {value.GetType()} a {list[index].GetType()}", 0, 0);
    }
    private void SetterPropertyIsPublic(object obj, string access)
    {
        PropertyInfo propertyInfo = PropertyIsValid(obj, access);
        if (propertyInfo.CanWrite)
        {
            MethodInfo setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod.IsPublic)
            {
                return;
            }
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible", 0, 0);
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"la propiedad {access} es de solo lectura", 0, 0);
    }

}

