namespace Interprete
{
    public class While : ASTnode
    {
        public ASTnode Condition {get; private set;}

        public List<ASTnode> Instructions {get; private set;}
        public While( ASTnode condition, List<ASTnode> instructions )
        {
            Condition = condition;
            Instructions = instructions;
        }

        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}