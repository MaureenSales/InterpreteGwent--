namespace Interprete
{
    public class Assignment : ASTnode
    {
        public ASTnode Variable { get; private set; }
        public ASTnode? Value { get; private set; }

        public Assignment(ASTnode variable, ASTnode? value)
        {
            Variable = variable;
            Value = value;
        }
        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}