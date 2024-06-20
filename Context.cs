namespace Interprete
{
    public class Context
    {
        public string TriggerPlayer { get; private set; }
        public List<Card> AllCards { get; private set; }

        public Context(string triggerPlayer, List<Card> cards)
        {
            TriggerPlayer = triggerPlayer;
            AllCards = cards;
        }
        public List<Card> FilterOfCards(LocationCards type, string player)
        {
            List<Card> result = new List<Card>();
            return result;
        }

        public List<Card> Find(List<Card> cards, Func<Card, bool> predicate)
        {
            List<Card> result = new List<Card>();
            foreach (Card card in cards)
            {
                if (predicate(card)) result.Add(card);
            }
            return result;
        }

        public void Push(Card card, List<Card> cards) => cards.Insert(0, card);
        public void SendBottom(Card card, List<Card> cards) => cards.Add(card);
        public void Remove(Card card, List<Card> cards) => cards.Remove(card);

        public Card Pop(List<Card> cards)
        {
            Card result = cards[0];
            cards.RemoveAt(0);
            return result;
        }

        public void Shuffle(List<Card> cards)
        {
            Random random = new Random();
            for (int i = 0; i < cards.Count; i++)
            {
                int index1 = random.Next(0, cards.Count - 1);
                int index2 = random.Next(0, cards.Count - 1);
                (cards[index1], cards[index2]) = (cards[index2], cards[index1]);
            }
        }




    }

    public enum LocationCards
    {
        Hand,
        Graveyard,
        Field,
        Deck,
    }
}