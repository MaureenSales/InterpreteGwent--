namespace Interprete
{
    public class VariableReference : ASTnode
    {
        public Token Variable { get; private set; }
        public string Name { get; private set; }

        public VariableReference(Token variable)
        {
            Variable = variable;
            Name = variable.Lexeme;
        }
        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}