using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
#nullable enable
public class Context
{
    public static GameObject? gameController;
    private GameController controller;
    public string TriggerPlayer { get; private set; }
    public string OtherPlayer { get; private set; }
    public List<GameObject> Hand { get { return FilterOfCards(LocationCards.Hand, TriggerPlayer); } }
    public List<GameObject> Field { get { return FilterOfCards(LocationCards.Field, TriggerPlayer); } }
    public List<GameObject> Graveyard { get { return FilterOfCards(LocationCards.Graveyard, TriggerPlayer); } }
    public List<GameObject> Board { get { return FilterOfCards(LocationCards.Board, TriggerPlayer); } }
    public List<Card> Deck { get { return GetDeck(TriggerPlayer); } }

    public Context()
    {
        controller = gameController!.GetComponent<GameController>();
        TriggerPlayer = controller.currentTurn.GetComponent<Player>().Id;
        OtherPlayer = controller.notCurrentTurn.GetComponent<Player>().Id;
    }
    public List<Card> GetDeck(string player)
    {
        Player DemandedPlayer;
        if (TriggerPlayer == player) DemandedPlayer = controller.currentTurn.GetComponent<Player>();
        else if (controller.notCurrentTurn.GetComponent<Player>().Id == player) DemandedPlayer = controller.notCurrentTurn.GetComponent<Player>();
        else return new List<Card>();
        return DemandedPlayer.MyDeck.cards;
    }

    public List<GameObject> FilterOfCards(LocationCards type, string player)
    {

        Player DemandedPlayer;
        if (TriggerPlayer == player) DemandedPlayer = controller.currentTurn.GetComponent<Player>();
        else if (controller.notCurrentTurn.GetComponent<Player>().Id == player) DemandedPlayer = controller.notCurrentTurn.GetComponent<Player>();
        else return new List<GameObject>();
        List<GameObject> result = new List<GameObject>();

        if (type == LocationCards.Board)
        {
            result = gameController!.transform.GetComponentInChildren<Board>().AllCardsObject;
            Debug.Log(result.Count + " todas las cartas");
        }
        else
        {
            switch (type)
            {
                case LocationCards.Graveyard:
                    result = DemandedPlayer.MyGraveyard.CardsObject;
                    break;
                case LocationCards.Hand:
                    result = DemandedPlayer.MyHand.CardsObject;
                    break;
                case LocationCards.Field:
                    result = DemandedPlayer.MyField.AllCardsObjects;
                    break;
            }
        }
        Debug.Log(result.Count);
        List<GameObject> temp = new List<GameObject>();
        foreach (var item in result)
        {
            if (!(item.GetComponent<ThisCard>().thisCard is HeroUnit) && !(item.GetComponent<ThisCard>().thisCard is DecoyUnit)) temp.Add(item);
        }
        return result;
    }

    public void Push(GameObject card, List<GameObject> cards, LocationCards location)
    {

    }
    public void SendBottom(GameObject card, List<GameObject> cards) => cards.Add(card);
    public void Remove(object card, IList cards)
    {
        if (card is GameObject cardUI)
        {
            string player = cardUI.GetComponent<ThisCard>().thisCard.Owner;
            Player DemandedPlayer;
            if (TriggerPlayer == player) DemandedPlayer = controller.currentTurn.GetComponent<Player>();
            else DemandedPlayer = controller.notCurrentTurn.GetComponent<Player>();
            LeanTween.move(cardUI, DemandedPlayer.MyGraveyard.gameObject.transform.position, 1f).setOnComplete(() => cardUI.transform.SetParent(DemandedPlayer.MyGraveyard.gameObject.transform));   
            DemandedPlayer.MyGraveyard.CardsObject.Add(cardUI);
            DemandedPlayer.MyGraveyard.Cards.Add(cardUI.GetComponent<ThisCard>().thisCard);
        }
        cards.Remove(card);
    }

    public object Pop(IList cards)
    {
        object result = cards[0];
        if(cards[0] is GameObject cardUI)
        {
            string player = cardUI.GetComponent<ThisCard>().thisCard.Owner;
            Player DemandedPlayer;
            if (TriggerPlayer == player) DemandedPlayer = controller.currentTurn.GetComponent<Player>();
            else DemandedPlayer = controller.notCurrentTurn.GetComponent<Player>();
            LeanTween.move(cardUI, DemandedPlayer.MyGraveyard.gameObject.transform.position, 1f).setOnComplete(() => cardUI.transform.SetParent(DemandedPlayer.MyGraveyard.gameObject.transform));   
            DemandedPlayer.MyGraveyard.CardsObject.Add(cardUI);
            DemandedPlayer.MyGraveyard.Cards.Add(cardUI.GetComponent<ThisCard>().thisCard);
        }

        cards.RemoveAt(0);
        return result;
    }

    public void Shuffle(IList cards)
    {
        System.Random random = new System.Random();
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
    Board,
}
