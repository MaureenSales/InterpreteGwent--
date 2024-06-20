namespace Interprete
{
    public class Property : ASTnode
    {
        public ASTnode Object { get; private set; }
        public ASTnode PropertyAccess { get; private set; }
        public Property(ASTnode object_, ASTnode property)
        {
            Object = object_;
            PropertyAccess = property;
        }

        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}