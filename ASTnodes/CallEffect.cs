using System.Runtime.InteropServices;

namespace Interprete
{
    public class CallEffect : ASTnode
    {
        public ASTnode Name {get; private set;}
        public Dictionary<VariableReference, ASTnode> Parameters {get; private set;}
        public Selector? Selector {get; private set;}
        public CallEffect? PostAction {get; private set;}

        public CallEffect( ASTnode name, Dictionary<VariableReference,ASTnode> parameters, Selector? selector, CallEffect? postAction)
        {
            Name = name;
            Parameters = parameters;
            Selector = selector;
            PostAction = postAction;
        }

        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}