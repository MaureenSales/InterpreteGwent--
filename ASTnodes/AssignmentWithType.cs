namespace Interprete
{
    public class AssignmentWithType : Assignment
    {
        public Token TypeVar {get; private set;} 
        public AssignmentWithType(ASTnode variable, Token typeVar, ASTnode? value) : base(variable, value)
        {
            TypeVar = typeVar;
        }
        public override T Accept<T>( IVsitor<T> visitor) => visitor.Visit(this);
    }
}