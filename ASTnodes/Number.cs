namespace Interprete
{
    public class Number: ASTnode
    {
        public Token Num { get; private set; }
        public double Value { get; private set; }

        public Number(Token num)
        {
            Num = num;
            Value = double.Parse(Num.Lexeme);
        }

        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}