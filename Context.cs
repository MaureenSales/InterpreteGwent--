using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
#nullable enable
public class Context
{
    public static GameObject? gameController;
    private GameController controller;
    public string TriggerPlayer { get; private set; }
    public string OtherPlayer { get; private set; }
    public (List<GameObject>, Transform) Hand { get { return FilterOfCards(LocationCards.Hand, TriggerPlayer); } }
    public (List<GameObject>, Transform) BoosterCells { get { return FilterOfCards(LocationCards.BoosterCells, TriggerPlayer); } }
    public (List<GameObject>, Transform) Field { get { return FilterOfCards(LocationCards.Field, TriggerPlayer); } }
    public (List<GameObject>, Transform) Graveyard { get { return FilterOfCards(LocationCards.Graveyard, TriggerPlayer); } }
    public (List<GameObject>, Transform) Board { get { return FilterOfCards(LocationCards.Board, TriggerPlayer); } }
    public (List<GameObject>, Transform) WeatherZone { get { return FilterOfCards(LocationCards.WeatherZone, TriggerPlayer); } }
    public (List<Card>, Transform) Deck { get { return GetDeck(TriggerPlayer); } }

    public Context()
    {
        controller = gameController!.GetComponent<GameController>();
        TriggerPlayer = controller.currentTurn.GetComponent<Player>().Id;
        OtherPlayer = controller.notCurrentTurn.GetComponent<Player>().Id;
    }
    public (List<Card>, Transform) GetDeck(string player)
    {
        Player DemandedPlayer;
        if (TriggerPlayer == player) DemandedPlayer = controller.currentTurn.GetComponent<Player>();
        else if (OtherPlayer == player) DemandedPlayer = controller.notCurrentTurn.GetComponent<Player>();
        else return (new List<Card>(), null)!;
        return (DemandedPlayer.MyDeck.cards, DemandedPlayer.gameObject.transform);
    }

    public (List<GameObject>, Transform) FilterOfCards(LocationCards type, string player)
    {
        Player DemandedPlayer;
        if (TriggerPlayer == player) DemandedPlayer = controller.currentTurn.GetComponent<Player>();
        else if (OtherPlayer == player) DemandedPlayer = controller.notCurrentTurn.GetComponent<Player>();
        else return (new List<GameObject>(), null)!;
        List<GameObject> result = new List<GameObject>();
        Transform location = null!;
        if (type == LocationCards.Board)
        {
            result = gameController!.transform.GetComponentInChildren<Board>().AllCardsObject;
            Debug.Log(result.Count + " todas las cartas");
            location = DemandedPlayer.gameObject.transform.parent;
        }
        else if (type == LocationCards.WeatherZone)
        {
            result = gameController!.transform.GetComponentInChildren<Board>().gameObject.transform.GetComponentInChildren<WeatherZone>().GetWeathers();
            location = gameController!.transform.GetComponentInChildren<Board>().gameObject.transform.GetComponentInChildren<WeatherZone>().transform;
        }
        else
        {
            switch (type)
            {
                case LocationCards.Graveyard:
                    result = DemandedPlayer.MyGraveyard.CardsObject;
                    location = DemandedPlayer.gameObject.transform.Find("Graveyard").transform;
                    break;
                case LocationCards.Hand:
                    result = DemandedPlayer.MyHand.CardsObject;
                    location = DemandedPlayer.MyHand.gameObject.transform;
                    break;
                case LocationCards.Field:
                    result = DemandedPlayer.MyField.AllCardsObjects;
                    location = DemandedPlayer.gameObject.transform.Find(DemandedPlayer.gameObject.name + "Field").transform;
                    break;
                case LocationCards.BoosterCells:
                    result = DemandedPlayer.MyBoosts.GetBoosts();
                    location = DemandedPlayer.gameObject.transform;
                    break;
            }
        }
        Debug.Log(result.Count);
        // List<GameObject> temp = new List<GameObject>();
        // foreach (var item in result) 
        // {
        //     if (!(item.GetComponent<ThisCard>().thisCard is HeroUnit) && !(item.GetComponent<ThisCard>().thisCard is DecoyUnit)) temp.Add(item);
        // }
        Debug.Log(result.Count);
        return (result, location);
    }

    async private void AggCard(object card, IList cards, Dictionary<IList, Transform> listingLocation, int index)
    {
        Transform location = listingLocation[cards];
        if (!CanStoreCard(cards, card))
        {
            if (card is GameObject cardUi)
            {
                Card newCard = cardUi.GetComponent<ThisCard>().thisCard;
                if (newCard.Owner == TriggerPlayer) controller.currentTurn.GetComponent<Player>().MyDeck.cards.Insert(index, newCard);
                else controller.notCurrentTurn.GetComponent<Player>().MyDeck.cards.Insert(index, newCard);
                return;
            }
            else
            {
                GameObject newCard = GameObject.Instantiate(controller.CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                newCard.GetComponent<ThisCard>().PrintCard((Card)card);
                Debug.Log(card.GetType());
                card = newCard;
            }
        }

        if (card is GameObject cardUI)
        {
            if (cards.Contains(card)) return;
            GameObject newCardUI;
            switch (location.name)
            {
                case "WeatherZone":
                    if (!(cardUI.GetComponent<ThisCard>().thisCard is Weather)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en la zona de climas");
                    else
                    {
                        newCardUI = CreateCard(new Vector3(0.8f, 0.8f, 0f), cardUI, location); 
                        switch (((Weather)cardUI.GetComponent<ThisCard>().thisCard).Skills[0].Name)
                        {
                            case "WeatherMelee":
                                if (location.GetComponent<WeatherController>().weather[0])
                                {
                                    Debug.Log("Clima M activo");
                                    return;
                                }
                                newCardUI.transform.SetParent(location.GetChild(0));
                                location.GetComponent<WeatherController>().weather[0] = true;
                                location.GetComponent<WeatherController>().ApplyWeather("Frost"); break;
                            case "WeatherRanged":
                                if (location.GetComponent<WeatherController>().weather[1])
                                {
                                    Debug.Log("Clima R activo");
                                    return;
                                }
                                newCardUI.transform.SetParent(location.GetChild(1));
                                location.GetComponent<WeatherController>().weather[1] = true;
                                location.GetComponent<WeatherController>().ApplyWeather("Fog"); break;
                            case "WeatherSiege":
                                if (location.GetComponent<WeatherController>().weather[2])
                                {
                                    Debug.Log("Clima S activo");
                                    return;
                                }
                                newCardUI.transform.SetParent(location.GetChild(2));
                                location.GetComponent<WeatherController>().weather[2] = true;
                                location.GetComponent<WeatherController>().ApplyWeather("Rain"); break;
                        }
                        newCardUI.GetComponent<Drag>().enabled = false;
                    }
                    await Task.Delay(1000);
                    break;
                case "Hand":
                    newCardUI = CreateCard(new Vector3(1f, 1f, 0f), cardUI, location);
                    location.GetComponent<Hand>().CardsObject.Insert(index, cardUI);
                    location.GetComponent<Hand>().Cards.Insert(index, newCardUI.GetComponent<ThisCard>().thisCard);
                    await Task.Delay(1000);
                    break;
                case "Graveyard":
                    newCardUI = CreateCard(new Vector3(0.9f, 0.9f, 0f), cardUI, location);
                    location.GetComponent<Graveyard>().CardsObject.Insert(index, cardUI);
                    location.GetComponent<Graveyard>().Cards.Insert(index, newCardUI.GetComponent<ThisCard>().thisCard);
                    newCardUI.GetComponent<Drag>().enabled = false;
                    await Task.Delay(1000); break;
                case "Enemy":
                case "Player":
                    if (cardUI.GetComponent<ThisCard>().thisCard.Owner != location.GetComponent<Player>().Id) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "no se puede colocar una carta propia en el campo rival o viceversa");
                    if (!(cardUI.GetComponent<ThisCard>().thisCard is Boost)) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en una celda de aumento");
                    location = location.gameObject.transform.Find(location.name + "Field").transform;
                    for (int i = 1; i < location.childCount; i++)
                    {
                        if (location.GetChild(i).GetChild(2).childCount == 0)
                        {
                            location = location.GetChild(i).GetChild(2).transform;
                            newCardUI = CreateCard(new Vector3(0.9f, 0.9f, 0), cardUI, location);
                            newCardUI.GetComponent<Drag>().enabled = false;
                            break;
                        }
                    }
                    await Task.Delay(1000); break;
                case "EnemyField":
                case "PlayerField":
                    Debug.Log("correcto");
                    if (cardUI.GetComponent<ThisCard>().thisCard.Owner != location.parent.GetComponent<Player>().Id) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "no se puede colocar una carta propia en el campo rival o viceversa");
                    if (cardUI.GetComponent<ThisCard>().thisCard is UnitCard unitCard && !(cardUI.GetComponent<ThisCard>().thisCard is DecoyUnit))
                    {
                        Debug.Log("here");
                        newCardUI = CreateCard(new Vector3(0.9f, 0.9f, 0), cardUI, location);
                        if (unitCard.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            newCardUI.transform.SetParent(location.GetChild(1).GetChild(0));
                            location.GetChild(1).GetComponentInChildren<Row>().InsertInRow(index, newCardUI);
                            newCardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(newCardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                controller.Improve(newCardUI, "Melee");
                            }
                        }
                        else if (unitCard.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            newCardUI.transform.SetParent(location.GetChild(2).GetChild(0));
                            location.GetChild(2).GetComponentInChildren<Row>().InsertInRow(index, newCardUI);
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(newCardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                controller.Improve(newCardUI, "Ranged");
                            }
                        }
                        else
                        {
                            newCardUI.transform.SetParent(location.GetChild(3).GetChild(0));
                            location.GetChild(3).GetComponentInChildren<Row>().InsertInRow(index, newCardUI);
                            newCardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(newCardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                controller.Improve(newCardUI, "Siege");
                            }
                        }
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en una fila");
                    await Task.Delay(1000); break;
                case "Canvas":
                    if (cardUI.GetComponent<ThisCard>().thisCard is UnitCard unit1 && !(cardUI.GetComponent<ThisCard>().thisCard is DecoyUnit))
                    {
                        Transform newLocation;
                        if (unit1.Owner == TriggerPlayer) newLocation = controller.currentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                        else newLocation = controller.notCurrentTurn.transform.Find(controller.notCurrentTurn.name + "Field").transform;
                        newCardUI = CreateCard(new Vector3(0.9f, 0.9f, 0), cardUI, newLocation);
                        if (unit1.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            newCardUI.transform.SetParent(newLocation.GetChild(1).GetChild(0));
                            newLocation.GetChild(1).GetComponentInChildren<Row>().InsertInRow(index, newCardUI);
                            newCardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(newCardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                controller.Improve(newCardUI, "Melee");
                            }
                        }
                        else if (unit1.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            newCardUI.transform.SetParent(newLocation.GetChild(2).GetChild(0));
                            newLocation.GetChild(2).GetComponentInChildren<Row>().InsertInRow(index, newCardUI);
                            newCardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(newCardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                controller.Improve(newCardUI, "Ranged");
                            }
                        }
                        else
                        {
                            newCardUI.transform.SetParent(newLocation.GetChild(3).GetChild(0));
                            newLocation.GetChild(3).GetComponentInChildren<Row>().InsertInRow(index, newCardUI);
                            newCardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(newCardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                controller.Improve(newCardUI, "Siege");
                            }
                        }
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en una fila");
                    await Task.Delay(1000); break;
            }
        }
        else
        {
            if (cards.Contains(card)) return;
            location.GetComponent<Player>().MyDeck.cards.Insert(index, (Card)card);
        }
        await Task.Delay(1000);
    }

    public void Push(object card, IList cards, Dictionary<IList, Transform> listingLocation) => AggCard(card, cards, listingLocation, 0);
    public void SendBottom(object card, IList cards, Dictionary<IList, Transform> listingLocation)
    {
        if (cards.Count == 0) AggCard(card, cards, listingLocation, cards.Count);
        else AggCard(card, cards, listingLocation, cards.Count - 1);
    }
    async public void Remove(object card, IList cards, Dictionary<IList, Transform> listingLocation)
    {
        Transform location = listingLocation[cards];
        if (!CanStoreCard(cards, card))
        {
            return;
        }
        else
        {

            if (card is GameObject cardUI)
            {
                if (cardUI.GetComponent<ThisCard>().thisCard is Unit unit)
                {
                    cardUI.GetComponent<ThisCard>().power = unit.Power.ToString();
                    cardUI.GetComponent<ThisCard>().powerText.text = unit.Power.ToString();
                }
                switch (location.name)
                {
                    case "WeatherZone":
                        if (!(cardUI.GetComponent<ThisCard>().thisCard is Weather)) return;
                        switch (((Weather)cardUI.GetComponent<ThisCard>().thisCard).Skills[0].Name)
                        {
                            case "WeatherMelee":
                                controller.ClearWeatherZone(0, 1); break;
                            case "WeatherRanged":
                                controller.ClearWeatherZone(1, 2); break;
                            case "WeatherSiege":
                                controller.ClearWeatherZone(2, 3); break;
                        }
                        await Task.Delay(1000); break;
                    case "Graveyard":
                        location.GetComponent<Graveyard>().CardsObject.Remove(cardUI);
                        location.GetComponent<Graveyard>().Cards.Remove(cardUI.GetComponent<ThisCard>().thisCard);
                        await Task.Delay(1000); break;
                    case "Hand":
                        location.GetComponent<Hand>().CardsObject.Remove(cardUI);
                        location.GetComponent<Hand>().Cards.Remove(cardUI.GetComponent<ThisCard>().thisCard);
                        await Task.Delay(1000); break;
                    case "Enemy":
                    case "Field":
                        break;
                    case "EnemyField":
                    case "PlayerField":
                        if (!(cardUI.GetComponent<ThisCard>().thisCard is Unit unit1)) return;
                        if (unit1.Owner == TriggerPlayer) location = controller.currentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                        else location = controller.notCurrentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                        if (unit1.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            location.GetChild(1).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                        }
                        else if (unit1.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            location.GetChild(2).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                        }
                        else
                        {
                            location.GetChild(3).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                        }
                        await Task.Delay(1000); break;
                    case "Canvas":
                        Debug.Log("enterBoard");
                        if (cardUI.GetComponent<ThisCard>().thisCard is UnitCard unit2)
                        {
                            Transform newLocation1;
                            if (unit2.Owner == TriggerPlayer) newLocation1 = controller.currentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                            else newLocation1 = controller.notCurrentTurn.transform.Find(controller.notCurrentTurn.name + "Field").transform;
                            if (unit2.AttackTypes.Contains(Global.AttackModes.Melee))
                            {
                                Debug.Log(cardUI.transform.parent.name);
                                Debug.Log(newLocation1.GetChild(1).GetChild(0).transform.name);
                                newLocation1.GetChild(1).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                            }
                            else if (unit2.AttackTypes.Contains(Global.AttackModes.Ranged))
                            {
                                newLocation1.GetChild(2).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                            }
                            else
                            {
                                newLocation1.GetChild(3).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                            }
                        }
                        await Task.Delay(1000); break;
                }
                GameObject.Destroy(cardUI);
            }
            else
            {
                Debug.Log("removeDelDeck");
                location.GetComponent<Player>().MyDeck.RemoveCard((Card)card);
            }
        }
    }

    private void MoveGraveyard(GameObject cardUI)
    {
        string player = cardUI.GetComponent<ThisCard>().thisCard.Owner;
        Player DemandedPlayer;
        if (TriggerPlayer == player) DemandedPlayer = controller.currentTurn.GetComponent<Player>();
        else DemandedPlayer = controller.notCurrentTurn.GetComponent<Player>();
        LeanTween.move(cardUI, DemandedPlayer.MyGraveyard.gameObject.transform.position, 1f).setOnComplete(() => cardUI.transform.SetParent(DemandedPlayer.MyGraveyard.gameObject.transform));
        DemandedPlayer.MyGraveyard.CardsObject.Add(cardUI);
        DemandedPlayer.MyGraveyard.Cards.Add(cardUI.GetComponent<ThisCard>().thisCard);
        cardUI.GetComponent<Drag>().enabled = false;
    }

    public object Pop(IList cards, Dictionary<IList, Transform> listingLocation)
    {
        if (cards.Count == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "lista vacia no se puede obtener elemento");
        Transform location = listingLocation[cards];
        object result = cards[0];
        Remove(result, cards, listingLocation);
        Debug.Log(result);
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

    private bool CanStoreCard(IList cards, object card)
    {
        Type listType = cards.GetType();
        if (listType.IsGenericType)
        {
            Type itemType = listType.GetGenericArguments()[0];
            return itemType.IsAssignableFrom(card.GetType());
        }
        return false;
    }

    private GameObject CreateCard(Vector3 scale, GameObject cardUI, Transform location)
    {
        GameObject newCardUI;
        newCardUI = GameObject.Instantiate(cardUI, location.position, Quaternion.identity);
        newCardUI.GetComponent<ThisCard>().PrintCard((Card)cardUI.GetComponent<ThisCard>().thisCard);
        newCardUI.transform.SetParent(location);
        newCardUI.transform.localScale = scale;
        return newCardUI;
    }

}

public enum LocationCards
{
    Hand,
    Graveyard,
    Field,
    Deck,
    Board,
    WeatherZone,
    BoosterCells,
}
