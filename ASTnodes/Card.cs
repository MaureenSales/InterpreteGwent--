namespace Interprete
{
    public class Card : ASTnode
    {
        public ASTnode Type { get; private set; }
        public ASTnode Name { get; private set; }
        public ASTnode Faction { get; private set; }
        public ASTnode Power { get; private set; }
        public List<ASTnode> Range { get; private set; }
        public List<ASTnode> Effects { get; private set; }

        public Card(ASTnode type, ASTnode name, ASTnode faction, ASTnode power, List<ASTnode> range, List<ASTnode> effects)
        {
            Type = type;
            Name = name;
            Faction = faction;
            Power = power;
            Range = range;
            Effects = effects;
        }
        public override T Accept<T>(IVsitor<T> visitor) => visitor.Visit(this);
    }
}