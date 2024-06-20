namespace Interprete
{
    public class Selector : ASTnode
    {
        public ASTnode Source {get; private set;}
        public ASTnode Single {get; private set;}
        public PredicateLambda Predicate {get; private set;}

        public Selector (ASTnode source, ASTnode single, PredicateLambda predicate)
        {
            Source = source;
            Single = single;
            Predicate = predicate;
        }
        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}