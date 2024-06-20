namespace Interprete
{
    public abstract class ASTnode
    {
        public abstract T Accept<T>(IVsitor<T> visitor);
    }
}