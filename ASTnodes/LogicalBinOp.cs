namespace Interprete
{
    public class LogicalBinOp : BinOp
    {
        public LogicalBinOp(ASTnode left, Token op, ASTnode right) : base(left, op, right) {}
        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}