using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVsitor<T>
{
    T Visit(UnaryOp unaryOp);
    T Visit(Number number);
    T Visit(String @string);
    T Visit(Boolean boolean);
    T Visit(LogicalBinOp logicalBinOp);
    T Visit(ArithmeticBinOp arithmeticBinOp);
    T Visit(GroupedExpr groupedExpr);
    T Visit(ComparisonBinOp comparisonBinOp);
    T Visit(ConcatBinOp concatBinOp);
    T Visit(VariableReference variableReference);
    T Visit(Assignment assignment);
    T Visit(Property property);
    T Visit(CallMethod callMethod);
    T Visit(UnaryInverseOp unaryInverseOp);
    T Visit(PredicateLambda predicateLambda);
    T Visit(ArithmeticAssignment arithmeticAssignment);
    T Visit(While @while);
    T Visit(For @for);
    T Visit(Effect effect);
    T Visit(ActionFun actionFun);
    T Visit(AssignmentWithType assignmentWithType);
    T Visit(Selector selector);
    T Visit(CallEffect callEffect);
    T Visit(CardDeclaration card);
    T Visit(IndexList indexList);

}
