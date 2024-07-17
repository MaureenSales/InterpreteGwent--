using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintASTnode : IVsitor<string>
{
    public string Visit(UnaryOp unaryOp)
    {
        return $"({unaryOp.Op.Lexeme} {Print(unaryOp.Right)})";
    }

    public string Visit(Number number)
    {
        return $"({number.Value})";
    }

    public string Visit(String @string)
    {
        return $"({@string.Value})";
    }

    public string Visit(Boolean boolean)
    {
        return $"({boolean.Value})";
    }

    public string Visit(LogicalBinOp logicalBinOp)
    {
        return $"({Print(logicalBinOp.Left)} {logicalBinOp.Op.Lexeme} {Print(logicalBinOp.Right)})";
    }

    public string Visit(ArithmeticBinOp arithmeticBinOp)
    {
        return $"({Print(arithmeticBinOp.Left)} {arithmeticBinOp.Op.Lexeme} {Print(arithmeticBinOp.Right)})";
    }

    public string Visit(GroupedExpr groupedExpr)
    {
        return $"({Print(groupedExpr.Group)})";
    }

    public string Print(ASTnode node)
    {
        return node.Accept(this);
    }

    public string Visit(ComparisonBinOp comparisonBinOp)
    {
        return $"({Print(comparisonBinOp.Left)} {comparisonBinOp.Op.Lexeme} {Print(comparisonBinOp.Right)})";
    }

    public string Visit(ConcatBinOp concatBinOp)
    {
        return $"({Print(concatBinOp.Left)} {concatBinOp.Op.Lexeme} {Print(concatBinOp.Right)})";
    }

    public string Visit(VariableReference variableReference)
    {
        return $"{variableReference.Name} is VR";
    }

    public string Visit(Assignment assignment)
    {
        return $"({Print(assignment.Variable)} = {Print(assignment.Value!)})";
    }

    public string Visit(Property property)
    {
        return $"({Print(property.Object)}.{Print(property.PropertyAccess)})";
    }

    public string Visit(CallMethod callMethod)
    {
        string result = " ";
        result += callMethod.MethodName.Lexeme + "(";
        int count = 0;
        foreach (var item in callMethod.Arguments)
        {
            if (count > 0) result += ",";
            result += Print(item) + " ";
            count++;
        }
        return $"{result})";

    }

    public string Visit(UnaryInverseOp unaryInverseOp)
    {
        return $"({Print(unaryInverseOp.Left)} {unaryInverseOp.Op.Lexeme})";
    }

    public string Visit(PredicateLambda predicateLambda)
    {
        return $"({Print(predicateLambda.Parameter)} => {Print(predicateLambda.BodyPredicate)})";
    }

    public string Visit(ArithmeticAssignment arithmeticAssignment)
    {
        return $"({Print(arithmeticAssignment.Variable)} {arithmeticAssignment.Op.Lexeme} {Print(arithmeticAssignment.Value)})";
    }

    public string Visit(While @while)
    {
        string result = "While ";
        result += $"({Print(@while.Condition)}) \n";
        int count = 0;
        foreach (var item in @while.Instructions)
        {
            if (count > 0) result += "; \n";
            result += Print(item);
            count++;
        }
        return result;
    }

    public string Visit(For @for)
    {
        string result = "For ";
        result += Print(@for.Element) + " in ";
        result += Print(@for.Collection) + "\n";
        int count = 0;
        foreach (var item in @for.Instructions)
        {
            if (count > 0) result += "; \n";
            result += Print(item);
            count++;
        }
        return result;
    }

    public string Visit(Effect effect)
    {
        string result = "Effect ";
        result += $"Name:  {Print(effect.Name)},  \n";
        result += "Params: ";
        int count = 0;
        foreach (var item in effect.Params)
        {
            if (count > 0) result += ", ";
            result += Print(item);
            count++;
        }
        return $"({result} \n {Print(effect.Action)})";
    }

    public string Visit(ActionFun actionFun)
    {
        string result = "Action: (";
        int count = 0;
        foreach (var item in actionFun.Parameters)
        {
            if (count > 0) result += ", ";
            result += Print(item);
            count++;
        }
        result += ")\n";
        count = 0;
        foreach (var item in actionFun.Body)
        {
            if (count > 0) result += "; \n";
            result += Print(item);
            count++;
        }
        return $"({result}) \n";
    }

    public string Visit(AssignmentWithType assignmentWithType)
    {
        return $"({assignmentWithType.TypeVar.Lexeme}: {Print(assignmentWithType.Variable)})";
    }

    public string Visit(Selector selector)
    {
        return $"(Source: {Print(selector.Source)} \n Single: {Print(selector.Single)} \n Predicate: {Print(selector.Predicate)}) \n ";
    }

    public string Visit(CallEffect callEffect)
    {
        string result = "";
        result += Print(callEffect.Name) + "\n";
        foreach (var item in callEffect.Parameters)
        {
            result += $"{Print(item)}) \n";
        }

        if (callEffect.Selector != null) result += $"Selector: {Print(callEffect.Selector)} \n";
        if (callEffect.PostAction != null) result += $"PosAction: {Print(callEffect.PostAction)} \n";
        return $"({result}) CallE";
    }

    public string Visit(CardDeclaration card)
    {
        string result = "Card \n";
        result += $"Type: {Print(card.Type)} \n Name: {Print(card.Name)} \n Faction: {Print(card.Faction)} \n Power: {Print(card.Power)} \n Range: ";
        foreach (var item in card.Range)
        {
            result += Print(item) + ", ";
        }
        result += "\n OnActivation: ";
        foreach (var item in card.Effects)
        {
            result += $"{Print(item)} \n";
        }

        return result;
    }

    public string Visit(IndexList indexList)
    {
        return $"({Print(indexList.List)} [{Print(indexList.Index)}])";
    }
}
