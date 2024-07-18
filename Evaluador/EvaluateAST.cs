using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
#nullable enable

public class Evaluador : IVsitor<object?>
{
    public Stack<Dictionary<string, object?>> VariableScopes;
    public Dictionary<string, (List<(string, object)>, ASTnode)> Selectors;
    public List<object>? SelectorsList;

    public Evaluador()
    {
        VariableScopes = new();
        Selectors = new();
        SelectorsList = null;
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
                                find = true;
                                break;
                            }
                            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{expr.Op.Lexeme} operator cannot be applied to {item[variable1.Name]!.GetType()}", 0, 0);

                        }
                    }
                    if (!find) return ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Undeclared varible '" + ((VariableReference)expr.Right).Name + "'. ", 0, 0);
                }
                else if (expr.Right is Property)
                {
                    if (right is double value)
                    {
                        if (expr.Op.Type == TokenType.Increment) ++value;
                        else --value;
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{expr.Op.Lexeme} operator cannot be applied to {right!.GetType()}", 0, 0);
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
        string access = "";
        if (property.PropertyAccess is VariableReference variable)
        {
            access = variable.Name;
            if (!TokenTypeExtensions.Properties.Contains(access)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene una definicion para {access}", 0, 0);
            access = variable.Name;
        }
        else if (property.PropertyAccess is CallMethod method)
        {
            access = method.MethodName.Lexeme;
            if (!TokenTypeExtensions.Methods.Contains(access)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene una definicion para {access}", 0, 0);
        }

        if (obj is Card card)
        {
            PropertyInfo? propertyInfo = card.GetType().GetProperty(access);
            if (propertyInfo is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
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
            else if(access == "Power") return (double)propertyInfo.GetValue(card);
            return propertyInfo.GetValue(card);
        }
        else if (obj is Context context)
        {
            PropertyInfo? propertyInfo = context.GetType().GetProperty(access);
            if (propertyInfo is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
            return propertyInfo.GetValue(context);
        }
        else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"{obj!.GetType()} no contiene un definicion para {access}", 0, 0);
    }

    public object? Visit(CallMethod callMethod)
    {
        throw new System.NotImplementedException();
    }

    public object? Visit(UnaryInverseOp expr)
    {
        object? left = evaluate(expr.Left);
        bool find = false;
        if (expr.Left.GetType() == typeof(Property) && expr.Left.GetType() != typeof(VariableReference) || expr.Left.GetType() != typeof(IndexList))
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
        bool[] parameters = new bool[2];
        IList list;
        Context context;
        foreach (var param in actionFun.Parameters)
        {
            var parameter = evaluate(param);
            if (parameter is IList)
            {
                list = (IList)parameter;
                if (parameters[0] == false) parameters[0] = true;
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Action no posee ninguna sobrecarga con dos parametros IList", 0, 0);
            }
            else if (parameter is Context)
            {
                context = (Context)parameter;
                if (parameters[1] == false) parameters[1] = true;
                else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "Action no posee ninguna sobrecarga con dos parametros Context", 0, 0);
            }
            else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"Action no acepta argumentos de tipo {parameter!.GetType()}", 0, 0);
        }

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
                    VariableScopes.Peek()[name] = card.GetComponent<ThisCard>().thisCard;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "hand":
                Debug.Log("source: hand");
                sources = context.FilterOfCards(LocationCards.Hand, context.TriggerPlayer);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card.GetComponent<ThisCard>().thisCard;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "otherHand":
                Debug.Log("source: otherHand");
                sources = context.FilterOfCards(LocationCards.Hand, context.OtherPlayer);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card.GetComponent<ThisCard>().thisCard;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "deck":
                Debug.Log("source: deck");
                cards = context.Deck(context.TriggerPlayer);
                foreach (var card in cards)
                {
                    VariableScopes.Peek()[name] = card;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(target);
                }
                break;
            case "otherDeck":
                Debug.Log("source: otherDeck");
                cards = context.Deck(context.OtherPlayer);
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
                    VariableScopes.Peek()[name] = card.GetComponent<ThisCard>().thisCard;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "otherField":
                Debug.Log("source: otherField");
                sources = context.FilterOfCards(LocationCards.Field, context.OtherPlayer);
                foreach (var card in sources)
                {
                    VariableScopes.Peek()[name] = card.GetComponent<ThisCard>().thisCard;
                    var target = evaluate(predicate);
                    if (target != null) targets.Add(card);
                }
                break;
            case "parent":
                Debug.Log("source: parent");
                if (SelectorsList is null) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "No existe una fuente previamente definida", 0, 0);
                else targets = SelectorsList;
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
        Effect effect = Global.EffectsCreated[name];

        List<(string, object)> arguments = new List<(string, object)>();
        if (effect.Params.Count != callEffect.Parameters.Count) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"El efecto {name} recibe {effect.Params.Count} argumentos", 0, 0);
        foreach (var assignment in callEffect.Parameters)
        {
            evaluate(assignment);
            Assignment? argument = assignment as Assignment;
            VariableReference? variable = argument!.Variable as VariableReference;
            arguments.Add((variable!.Name, evaluate(variable)!));
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
            Selectors[name] = (arguments, callEffect.Selector);

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
            Debug.Log(skill.Key);
            skills.Add(new Skill(skill.Key, skill.Value.Item1, skill.Value.Item2));
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
        throw new System.NotImplementedException();
    }
}

