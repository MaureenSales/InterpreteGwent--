namespace Interprete
{
    public class GroupedExpr: ASTnode
    {
        public ASTnode Group { get; private set; }

        public GroupedExpr(ASTnode group)
        {
            Group = group;
        }

        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }

    
}