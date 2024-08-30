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
    public List<(string, (Dictionary<string, object>?, ASTnode?))> Selectors;
    public List<object>? SelectorsList;
    private IList? listForMethod;
    private Dictionary<IList, Transform> ListingLocation;
    private bool IsPostAction = false;

    public Evaluador()
    {
        ListingLocation = new();
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
        ActionFun? action = effect.Action as ActionFun;

        if (!(skill.Arguments is null))
        {
            foreach (var param in skill.Arguments!)
            {
                VariableScopes.Peek()[param.Key] = param.Value;
            }
        }
        Debug.Log(action is null);
        VariableReference? param1 = action!.Parameters[0] as VariableReference;
        Debug.Log(param1 is null);
        Debug.Log(param1!.Name);
        VariableReference? param2 = action!.Parameters[1] as VariableReference;
        if (targets is null) VariableScopes.Peek()[param1!.Name] = new List<object>();
        else VariableScopes.Peek()[param1!.Name] = targets;
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
                    ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "El operando de un operador de incremento o decremento debe ser una variable, propiedad o indexador");
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
                            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El operador {expr.Op.Lexeme} no puede ser aplicado a {item[variable1.Name]!.GetType()}");

                        }
                    }
                    if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"Variable {((VariableReference)expr.Right).Name} no declarada");
                }
                else if (expr.Right is Property property)
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
                        //cardUI.transform.parent.parent.GetComponentInChildren<SumPower>().UpdatePower();
                        result = int.Parse(cardUI.GetComponent<ThisCard>().powerText.text);
                    }
                    else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible");
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}");
                }
                else if (expr.Right is IndexList indexList)
                {
                    var i = evaluate(indexList.Index)!;
                    int index = (int)(double)i;
                    if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero");
                    var List = evaluate(indexList.List);
                    if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}");
                    else
                    {
                        SetterIndexerIsValid(list, index, i);
                        var value = (double)list[index]!;
                        if (expr.Op.Type == TokenType.Increment) list[index] = ++value;
                        else list[index] = --value;
                        result = value;
                    }
                }
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el operador {expr.Op.Lexeme} solo se puede aplicar a una variable, una propiedad o un indexador");
                Debug.Log(result + "result");
                return result;
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
        throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "El operando debe ser un booleano.");
    }

    private void CheckNumberOperand(params object?[] operands)
    {
        foreach (var item in operands)
        {
            if (!(item is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "El operando debe ser un entero.");
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
        Debug.Log(left + " compareTo " + right);
        if (left is int || left is float) left = Convert.ToDouble(left);
        if (right is int || right is float) right = Convert.ToDouble(right);
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
        if (left == null || right == null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Operando nulo");
        string? a = left as string;
        string? b = right as string;
        if (a == null || b == null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El operador {expr.Op.Lexeme} no puede ser usando entre los tipos" + left.GetType() + " y " + right.GetType() + ". ");
        if (expr.Op.Type == TokenType.Concat) return a + b;
        else return a + " " + b;
    }

    public object? Visit(VariableReference expr)
    {
        Debug.Log(expr.Name);
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
        return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Variable '" + expr.Name + "' no declarada. ");
    }

    public object? Visit(Assignment expr)
    {
        object? value = evaluate(expr.Value!);
        Debug.Log(value);
        if (value is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la expresion derecha de una asignacion debe ser directamente un valor o una variable de referencia");

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
                Debug.Log(variable1.Name + "=" + value);

                VariableScopes.Peek().Add(variable1.Name, value);
            }
        }
        else if (expr.Variable is Property property)
        {
            object? obj = evaluate(property.Object);
            string access = "";
            PropertyIsValid(ref access, property, obj);
            if (!(value is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir implicitamente el tipo {obj!.GetType()} en int");

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
            else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible");
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}");
        }
        else if (expr.Variable is IndexList indexList)
        {
            var i = evaluate(indexList.Index)!;
            int index = (int)(double)i;
            if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero");
            var List = evaluate(indexList.List);
            if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}");
            else
            {
                SetterIndexerIsValid(list, index, value);
                list[index] = value;
            }
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la parte izquierda de una asignacion debe ser una variable, una propiedad o un indexador");
        return value;
    }

    public object? Visit(Property property)
    {
        Debug.Log("enterProperty");
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
            Debug.Log("ahoraescard");
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
            if (property.PropertyAccess is CallMethod method) { Debug.Log("aquiiiiii"); var result1 = evaluate(method); Debug.Log(result1); return result1; };
            object result = PropertyIsValid(obj, access).GetValue(context);
            if (result is string) return result;
            else
            {
                if (result is (IList list, Transform transform))
                {
                    ListingLocation[list] = transform;
                    return list;
                }
            }
        }
        else if (obj is IList list)
        {
            listForMethod = list;
            if (property.PropertyAccess is CallMethod method) return evaluate(property.PropertyAccess);
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {evaluate(property.PropertyAccess)}");
        }
        return null;
    }

    public object? Visit(CallMethod callMethod)
    {
        if (!TokenTypeExtensions.Methods.ContainsKey(callMethod.MethodName.Lexeme)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el metodo {callMethod.MethodName.Lexeme} no esta definido");
        if (callMethod.Arguments.Count != TokenTypeExtensions.Methods[callMethod.MethodName.Lexeme]) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no existe una sobrecarga de {callMethod.MethodName.Lexeme} con {callMethod.Arguments.Count} parametros");
        Context context = new Context();
        List<object> result = new List<object>();
        string player = "";
        (List<GameObject>, Transform) location = default;
        switch (callMethod.MethodName.Lexeme)
        {
            case "Pop":
                Debug.Log("enterPop");
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Pop no esta definido");
                return context.Pop(listForMethod, ListingLocation);
            case "Add":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Add no esta definido");
                object? card = evaluate(callMethod.Arguments[0]);
                if (!(card is GameObject) && !(card is Card)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"ninguna sobrecarga de Add recibe un argumento de tipo {card!.GetType()}");
                context.SendBottom(card, listForMethod, ListingLocation);
                break;
            case "Shuffle":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Shuffle no esta definido");
                context.Shuffle(listForMethod);
                break;
            case "DeckOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                var locat = context.GetDeck(player);
                ListingLocation[locat.Item1] = locat.Item2;
                return locat.Item1;
            case "HandOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                location = context.FilterOfCards(LocationCards.Hand, player);
                ListingLocation[location.Item1] = location.Item2;
                return location.Item1;
            case "FieldOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                location = context.FilterOfCards(LocationCards.Field, player);
                ListingLocation[location.Item1] = location.Item2;
                return location.Item1;
            case "GraveyardOfPlayer":
                player = (string)evaluate(callMethod.Arguments[0])!;
                location = context.FilterOfCards(LocationCards.Graveyard, player);
                ListingLocation[location.Item1] = location.Item2;
                return location.Item1;
            case "Push":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Push no esta definido");
                Debug.Log(callMethod.Arguments[0].GetType());
                Debug.Log(evaluate(callMethod.Arguments[0]));
                object? card1 = evaluate(callMethod.Arguments[0]);
                if (!(card1 is GameObject) && !(card1 is Card)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"ninguna sobrecarga de Push recibe un argumento de tipo {card1!.GetType()}");
                context.Push(card1, listForMethod, ListingLocation);
                break;
            case "Remove":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Remove no esta definido");
                object? card2 = evaluate(callMethod.Arguments[0]);
                if (!(card2 is GameObject) && !(card2 is Card)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"ninguna sobrecarga de Remove recibe un argumento de tipo {card2!.GetType()}");
                context.Remove(card2, listForMethod, ListingLocation);
                break;
            case "Find":
                EnterScope();
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo Find no esta definido");
                PredicateLambda? predicate = callMethod.Arguments[0] as PredicateLambda;
                VariableReference? unit = predicate!.Parameter as VariableReference;
                foreach (var element in listForMethod)
                {
                    VariableScopes.Peek()[unit!.Name] = element;
                    var target = evaluate(predicate);
                    if (target != null) result.Add(target);
                }
                ListingLocation[result] = ListingLocation[listForMethod];
                ExitScope();
                return result;
            case "SendBottom":
                if (listForMethod is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el metodo SendBottom no esta definido");
                object? card3 = evaluate(callMethod.Arguments[0]);
                if (!(card3 is GameObject) && !(card3 is Card)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"ninguna sobrecarga de SendBottom recibe un argumento de tipo {card3!.GetType()}");
                context.SendBottom(card3, listForMethod, ListingLocation);
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
                        if (expr.Op.Type == TokenType.Increment) item[variable.Name] = ++value;
                        else item[variable.Name] = --value;
                        find = true;
                        break;
                    }
                    else if (item[variable.Name] is null) continue;
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir implicitamente del tipo {item[variable.Name]!.GetType()} a int");
                }
            }
            if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Varible '" + ((VariableReference)expr.Left).Name + "' no declarada. ");
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
                //cardUI.transform.parent.parent.GetComponentInChildren<SumPower>().UpdatePower();
            }
            else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible");
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}");
        }
        else if (expr.Left is IndexList indexList)
        {
            var i = evaluate(indexList.Index)!;
            int index = (int)(double)i;
            if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero");
            var List = evaluate(indexList.List);
            if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}");
            else
            {
                SetterIndexerIsValid(list, index, i);
                var value = (double)list[index]!;
                if (expr.Op.Type == TokenType.Increment) list[index] = value++;
                else list[index] = value--;
            }
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el operador {expr.Op.Lexeme} solo se puede aplicar a una variable, una propiedad o un indexador");
        return left;
    }

    public object? Visit(PredicateLambda predicateLambda)
    {
        var target = evaluate(predicateLambda.Parameter);
        if (!(evaluate(predicateLambda.BodyPredicate) is bool condition)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Un predicado debe devolver un booleano");
        else if (condition) { Debug.Log("isTRue"); return target; }
        else return null;
    }

    public object? Visit(ArithmeticAssignment expr)
    {
        object? num = evaluate(expr.Value);
        if (!(num is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El operador {expr.Op} no puede ser usado entre {evaluate(expr.Variable)!.GetType()} y {num!.GetType()}");
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
            if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Varible '" + ((VariableReference)expr.Variable).Name + "'no declarada. ");
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
                //cardUI.transform.parent.parent.GetComponentInChildren<SumPower>().UpdatePower();
                result = int.Parse(cardUI.GetComponent<ThisCard>().powerText.text);
            }
            else if (obj is Context) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible");
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}");
        }
        else if (expr.Variable is IndexList indexList)
        {
            var i = evaluate(indexList.Index)!;
            int index = (int)(double)i;
            if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero");
            var List = evaluate(indexList.List);
            if (!(List is IList list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}");
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
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la parte izquierda de una asignacion debe ser una variable, una propiedad o un indexador");
        return result;
    }

    public object? Visit(While @while)
    {
        EnterScope();
        while (IsTruthy(evaluate(@while.Condition)))
        {
            Debug.Log("enterWhile");
            Debug.Log(@while.Instructions.Count);
            foreach (var item in @while.Instructions)
            {
                evaluate(item);
            }
        }
        Debug.Log("salio del ciclo W");
        ExitScope();
        return null;
    }

    public object? Visit(For @for)
    {
        EnterScope();
        var element = @for.Element as VariableReference;
        VariableScopes.Peek().Add(element!.Name, null);
        Debug.Log(element!.Name);
        var list = evaluate(@for.Collection);
        Debug.Log(@for.Collection.GetType());
        if (list is IList collection)
        {
            foreach (var item in collection)
            {
                Debug.Log(item);
                VariableScopes.Peek()[element!.Name] = item;
                foreach (var instrctruction in @for.Instructions)
                {
                    evaluate(instrctruction);
                }
            }
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "El ciclo 'for' solo puede ser usado en listas");
        ExitScope();
        return null;
    }

    public object? Visit(Effect effect)
    {
        string? name = evaluate(effect.Name) as string;
        if (name is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el capo Name de un effecto es de tipo string");
        if (!Global.EffectsCreated.ContainsKey(name!))
        {
            Global.EffectsCreated.Add(name!, effect);
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el efecto {name!} ya esta definido");
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
                if (!(variable is string)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir de tipo {variable!.GetType()} a string");
                break;
            case TokenType.TypeNumber:
                if (!(variable is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir de tipo {variable!.GetType()} a number");
                break;
            case TokenType.Bool:
                if (!(variable is bool)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir de tipo {variable!.GetType()} a bool");
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
        (List<GameObject>, Transform) list;
        switch (source)
        {
            case "board":
                Debug.Log("source: board");
                list = context.FilterOfCards(LocationCards.Board, context.TriggerPlayer);
                sources = list.Item1;
                Debug.Log(sources.Count);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                ListingLocation[targets] = list.Item2;
                break;
            case "hand":
                Debug.Log("source: hand");
                list = context.FilterOfCards(LocationCards.Hand, context.TriggerPlayer);
                sources = list.Item1;
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                ListingLocation[targets] = list.Item2;
                break;
            case "otherHand":
                Debug.Log("source: otherHand");
                list = context.FilterOfCards(LocationCards.Hand, context.OtherPlayer);
                sources = list.Item1;
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                Debug.Log(targets.Count);
                ListingLocation[targets] = list.Item2;
                break;
            case "deck":
                Debug.Log("source: deck");
                var list1 = context.GetDeck(context.TriggerPlayer);
                cards = list1.Item1;
                foreach (var card in cards)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(target);
                }
                ListingLocation[targets] = list1.Item2;
                break;
            case "otherDeck":
                Debug.Log("source: otherDeck");
                var list2 = context.GetDeck(context.OtherPlayer);
                cards = list2.Item1;
                foreach (var card in cards)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(target);
                }
                ListingLocation[targets] = list2.Item2;
                break;
            case "field":
                Debug.Log("source: field");
                list = context.FilterOfCards(LocationCards.Field, context.TriggerPlayer);
                sources = list.Item1;
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                ListingLocation[targets] = list.Item2;
                break;
            case "otherField":
                Debug.Log("source: otherField");
                Debug.Log(context.OtherPlayer);
                list = context.FilterOfCards(LocationCards.Field, context.OtherPlayer);
                sources = list.Item1;
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                ListingLocation[targets] = list.Item2;
                break;
            case "parent":
                Debug.Log("source: parent");
                Debug.Log(SelectorsList is null);
                if (SelectorsList is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "No existe una fuente previamente definida");
                else
                {
                    foreach (var card in SelectorsList)
                    {
                        VariableScopes.Peek()[name] = card;
                        var target = evaluate(predicate);
                        if (target != null) targets.Add(card);
                    }
                }
                ListingLocation[targets] = ListingLocation[SelectorsList];
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
        if (name == null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Name de un efecto es de tipo string");
        if (!Global.EffectsCreated.ContainsKey(name)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El efecto {name} no esta definido");
        Effect effect = (Effect)Global.EffectsCreated[name];

        Dictionary<string, object> arguments = new Dictionary<string, object>();
        if (effect.Params.Count != callEffect.Parameters.Count) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El efecto {name} recibe {effect.Params.Count} argumentos");
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
            if (source == null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Source de un selector es de tipo string");
            else if (source == "parent" && !IsPostAction) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la fuente parent solo puede ser declarada en un PostAction");
            if (!(evaluate(selector.Single) is bool)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Single de un Selector es de tipo booleano");
            if (!Global.Sources.Contains(source)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"la fuente {source} no esta definida");
            Selectors.Add((name, (arguments, callEffect.Selector)));
        }
        else
        {
            SelectorsList = null;
            Selectors.Add((name, (null, null)));
        }

        if (!(callEffect.PostAction is null))
        {
            IsPostAction = true;
            evaluate(callEffect.PostAction);
            IsPostAction = false;
        }


        ExitScope();
        return null;
    }

    public object? Visit(CardDeclaration card)
    {
        string? type = evaluate(card.Type) as string;
        if (type is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Type de una carta es de tipo string");
        if (!Global.TypeCards.Contains(type)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El tipo {type} no esta definido");

        string? name = evaluate(card.Name) as string;
        if (name is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Name de una carta es de tipo string");

        string? faction = evaluate(card.Faction) as string;
        if (faction is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Faction de una carta es de tipo string");
        bool tryParse = Enum.TryParse(faction, out Global.Factions fact);
        if (!tryParse) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"La faccion {faction} no esta definida");
        Global.Factions Faction = fact;
        double power;
        if (!(evaluate(card.Power) is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Power de una carta es de tipo number");
        else power = (double)evaluate(card.Power)!;

        List<Global.AttackModes> attackModes = new List<Global.AttackModes>();
        if (card.Range.Count > 3) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "solo exiten tres posibles rangos");
        foreach (var element in card.Range)
        {
            string? range = evaluate(element) as string;
            if (range is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "el campo Range de una carta esta compuesto por elementos de tipo string");
            tryParse = Enum.TryParse(range, out Global.AttackModes mode);
            if (!tryParse) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El rango {range} no esta definido");
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
                Debug.Log(skills.Count);
                newCard = new Unit(name, Faction, skills, "", (int)power, attackModes, Resources.Load<Sprite>("image"));
                break;
            case "Lider":
                newCard = new Leader(name, Faction, skills, "", Resources.Load<Sprite>("image"));
                CardDataBase.Leaders[Faction] = (Leader)newCard;
                switch (Faction)
                {
                    case Global.Factions.Gryffindor:
                        CardDataBase.Gryffindor.Leader = (Leader)newCard;
                        break;
                    case Global.Factions.Slytherin:
                        CardDataBase.Slytherin.Leader = (Leader)newCard;
                        break;
                    case Global.Factions.Ravenclaw:
                        CardDataBase.Ravenclaw = new Deck((Leader)newCard);
                        foreach (var item in CardDataBase.Neutral) CardDataBase.Ravenclaw.AddCard(item);
                        foreach (var item in CardDataBase.Specials) CardDataBase.Ravenclaw.AddCard(item);
                        CardDataBase.Decks.Add(Global.Factions.Ravenclaw, CardDataBase.Ravenclaw);
                        break;
                    case Global.Factions.Hufflepuff:
                        CardDataBase.Hufflepuff = new Deck((Leader)newCard);
                        foreach (var item in CardDataBase.Neutral) CardDataBase.Hufflepuff.AddCard(item);
                        foreach (var item in CardDataBase.Specials) CardDataBase.Hufflepuff.AddCard(item);
                        CardDataBase.Decks.Add(Global.Factions.Hufflepuff, CardDataBase.Hufflepuff);
                        break;
                    default: throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "un lider representa una faccion");
                }
                break;
            case "Aumento":
                newCard = new Boost(name, new List<Skill>() { new Skill("Boost", null, null) }, "", Resources.Load<Sprite>("image"));
                break;
            case "Clima":
                switch (attackModes[0])
                {
                    case Global.AttackModes.Melee:
                        newCard = new Weather(name, new List<Skill>() { new Skill("WeatherMelee", null, null) }, "", Resources.Load<Sprite>("image"), Resources.Load<Sprite>("Frost"));
                        break;
                    case Global.AttackModes.Ranged:
                        newCard = new Weather(name, new List<Skill>() { new Skill("WeatherRanged", null, null) }, "", Resources.Load<Sprite>("image"), Resources.Load<Sprite>("Fog"));
                        break;
                    case Global.AttackModes.Siege:
                        newCard = new Weather(name, new List<Skill>() { new Skill("WeatherSiege", null, null) }, "", Resources.Load<Sprite>("image"), Resources.Load<Sprite>("Rain"));
                        break;
                }
                break;
            case "Despeje":
                newCard = new Clear(name, new List<Skill>() { new Skill("ClearWeather", null, null) }, "", Resources.Load<Sprite>("image"));
                break;
        }
        Debug.Log(CardDataBase.Decks.Count);
        if (Faction == Global.Factions.Neutral && newCard is UnitCard unitCard) CardDataBase.Neutral.Add(unitCard);
        else if (Faction == Global.Factions.Neutral) CardDataBase.Specials.Add((SpecialCard)newCard);

        foreach (var deck in CardDataBase.Decks.Values) deck.AddCard(newCard);
        return null;
    }

    public object? Visit(IndexList indexList)
    {
        var i = evaluate(indexList.Index)!;
        int index = (int)(double)i;
        if (!(i is double)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "una lista se debe indexar mediante un valor entero");
        var List = evaluate(indexList.List);
        if (!(List is IList)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede aplicar la indizacion a un tipo {List!.GetType()}");
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
            if (!TokenTypeExtensions.Properties.Contains(access)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene una definicion para {access}");
            access = variable.Name;
        }
        else if (property.PropertyAccess is CallMethod method)
        {
            access = method.MethodName.Lexeme;
            if (!TokenTypeExtensions.Methods.ContainsKey(access)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene una definicion para {access}");
        }
    }
    private PropertyInfo PropertyIsValid(object obj, string access)
    {
        PropertyInfo? propertyInfo = obj.GetType().GetProperty(access);
        if (propertyInfo is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}");
        return propertyInfo;
    }

    private void SetterIndexerIsValid(IList list, int index, object value)
    {
        PropertyInfo IsReadOnly = list.GetType().GetProperty("IsReadOnly");
        if (IsReadOnly != null) if ((bool)IsReadOnly.GetValue(list)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "la lista es de solo lectura");
        if(list.Count < index + 1) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "indexacion fuera de rango");
        if (list[index].GetType() != value.GetType()) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"no se puede convertir implicitamente del tipo {value.GetType()} a {list[index].GetType()}");
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
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"el descriptor de acceso de la propiedad {access} es inaccesible");
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"la propiedad {access} es de solo lectura");
    }

}

