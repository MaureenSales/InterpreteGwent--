namespace Interprete
{
    public class Boolean : ASTnode
    {
        public Token Bool { get; private set; }
        public bool Value { get; private set; }

        public Boolean(Token bool_)
        {
            Bool = bool_;
            if (Bool.Type == TokenType.True) Value = true;
            else Value = false;
        }

        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}