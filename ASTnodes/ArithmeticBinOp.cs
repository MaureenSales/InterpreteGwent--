namespace Interprete
{
    public class ArithmeticBinOp : BinOp
    {
        public ArithmeticBinOp(ASTnode left, Token op, ASTnode right): base(left, op, right){}
        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}