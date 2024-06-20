namespace Interprete
{
    public class IndexList : ASTnode
    {
        public ASTnode List { get; private set; }
        public ASTnode Index { get; private set; }

        public IndexList(ASTnode list, ASTnode index)
        {
            List = list;
            Index = index;
        }
        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}