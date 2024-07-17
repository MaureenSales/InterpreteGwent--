using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
#nullable enable
public class Context
{
    public static GameObject gameController = null!;
    private GameController controller;
    public string TriggerPlayer { get; private set; }
    public string OtherPlayer { get; private set; }
    public Context()
    {
        controller = gameController.GetComponent<GameController>();
        TriggerPlayer = controller!.currentTurn.GetComponent<Player>().Id;
        OtherPlayer = controller.notCurrentTurn.GetComponent<Player>().Id;
    }
    public List<Card> Deck(string player)
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
            Debug.Log(controller is null);
            //arreglar da eeror la linea de abajo
            result = gameController!.transform.parent.GetComponentInChildren<Board>().AllCardsObject;
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
        return result;
    }

    public List<GameObject> Find(List<GameObject> cards, System.Func<GameObject, bool> predicate)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject card in cards)
        {
            if (predicate(card)) result.Add(card);
        }
        return result;
    }

    public void Push(GameObject card, List<GameObject> cards, LocationCards location)
    {
        
    }
    public void SendBottom(GameObject card, List<GameObject> cards) => cards.Add(card);
    public void Remove(GameObject card, List<GameObject> cards) => cards.Remove(card);

    public GameObject Pop(List<GameObject> cards)
    {
        GameObject result = cards[0];
        cards.RemoveAt(0);
        return result;
    }

    public void Shuffle(List<GameObject> cards)
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
