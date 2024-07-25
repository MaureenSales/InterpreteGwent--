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
    public (List<GameObject>, Transform) Field { get { return FilterOfCards(LocationCards.Field, TriggerPlayer); } }
    public (List<GameObject>, Transform) Graveyard { get { return FilterOfCards(LocationCards.Graveyard, TriggerPlayer); } }
    public (List<GameObject>, Transform) Board { get { return FilterOfCards(LocationCards.Board, TriggerPlayer); } }
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
            }
        }
        Debug.Log(result.Count);
        List<GameObject> temp = new List<GameObject>();
        foreach (var item in result)
        {
            if (!(item.GetComponent<ThisCard>().thisCard is HeroUnit) && !(item.GetComponent<ThisCard>().thisCard is DecoyUnit)) temp.Add(item);
        }
        Debug.Log(result.Count);
        return (result, location);
    }

    async public void Push(object card, IList cards, Dictionary<IList, Transform> listingLocation)
    {
        Transform location = listingLocation[cards];
        if (!CanStoreCard(cards, card))
        {
            if (card is GameObject cardUi)
            {
                Card newCard = cardUi.GetComponent<ThisCard>().thisCard;
                if (newCard.Owner == TriggerPlayer) controller.currentTurn.GetComponent<Player>().MyDeck.cards.Insert(0, newCard);
                else controller.notCurrentTurn.GetComponent<Player>().MyDeck.cards.Insert(0, newCard);
                cards.Insert(0, newCard);
                return;
            }
            else
            {
                GameObject newCard = GameObject.Instantiate(controller.CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                newCard.GetComponent<ThisCard>().PrintCard((Card)card);
                card = newCard;
            }
        }

        if (card is GameObject cardUI)
        {
            if (cardUI.GetComponent<ThisCard>().thisCard is Unit unit)
            {
                cardUI.GetComponent<ThisCard>().power = unit.Power.ToString();
                cardUI.GetComponent<ThisCard>().powerText.text = unit.Power.ToString();
            }
            if (cards.Contains(card)) return;
            switch (location.name)
            {
                case "Hand":
                    Debug.Log("enterHand");
                    Debug.Log(location);
                    cardUI.transform.SetParent(location);
                    location.GetComponent<Hand>().CardsObject.Insert(0, cardUI);
                    location.GetComponent<Hand>().Cards.Insert(0, cardUI.GetComponent<ThisCard>().thisCard);
                    //LeanTween.move(cardUI, location.position, 1f).setOnComplete(() => cardUI.transform.SetParent(location));
                    cardUI.GetComponent<Drag>().enabled = true;
                    if (cardUI.GetComponent<ClickOnCard>() != null) cardUI.GetComponent<ClickOnCard>().enabled = true;
                    Debug.Log(cardUI.transform.parent.name);
                    await Task.Delay(1000);
                    break;
                case "Graveyard":
                    cardUI.transform.SetParent(location);
                    location.GetComponent<Graveyard>().CardsObject.Insert(0, cardUI);
                    location.GetComponent<Graveyard>().Cards.Insert(0, cardUI.GetComponent<ThisCard>().thisCard);
                    cardUI.GetComponent<Drag>().enabled = false;
                    //LeanTween.move(cardUI, location.position, 1f).setOnComplete(() => cardUI.transform.SetParent(location));
                    await Task.Delay(1000); break;
                case "EnemyField":
                case "PlayerField":
                    cardUI.transform.localScale = new Vector3(0.55f, 0.55f, 0);
                    if (cardUI.GetComponent<ThisCard>().thisCard.Owner != location.parent.GetComponent<Player>().Id) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "no se puede colocar una carta propia en el campo rival o viceversa", 0, 0);
                    if (cardUI.GetComponent<ThisCard>().thisCard is UnitCard unitCard)
                    {
                        if (unitCard.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            cardUI.transform.SetParent(location.GetChild(1).GetChild(0));
                            //LeanTween.move(cardUI, location.GetChild(1).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(location.GetChild(1).GetChild(0)));
                            location.GetChild(1).GetComponentInChildren<Row>().InsertInRow(0, cardUI);
                            location.GetChild(1).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Melee");
                            }
                        }
                        else if (unitCard.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            cardUI.transform.SetParent(location.GetChild(2).GetChild(0));
                            //LeanTween.move(cardUI, location.GetChild(2).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(location.GetChild(2).GetChild(0)));
                            location.GetChild(2).GetComponentInChildren<Row>().InsertInRow(0, cardUI);
                            location.GetChild(2).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Ranged");
                            }
                        }
                        else
                        {
                            cardUI.transform.SetParent(location.GetChild(3).GetChild(0));
                            //LeanTween.move(cardUI, location.GetChild(3).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(location.GetChild(3).GetChild(0)));
                            location.GetChild(3).GetComponentInChildren<Row>().InsertInRow(0, cardUI);
                            location.GetChild(3).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Siege");
                            }
                        }
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en una fila", 0, 0);
                    await Task.Delay(1000); break;
                case "Canvas":
                    cardUI.transform.localScale = new Vector3(0.55f, 0.55f, 0);
                    if (cardUI.GetComponent<ThisCard>().thisCard is UnitCard unit1)
                    {
                        Transform newLocation;
                        if (unit1.Owner == TriggerPlayer) newLocation = controller.currentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                        else newLocation = controller.notCurrentTurn.transform.Find(controller.notCurrentTurn.name + "Field").transform;
                        if (unit1.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            cardUI.transform.SetParent(newLocation.GetChild(1).GetChild(0));
                            //LeanTween.move(cardUI, newLocation.GetChild(1).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(newLocation.GetChild(1).GetChild(0)));
                            newLocation.GetChild(1).GetComponentInChildren<Row>().InsertInRow(0, cardUI);
                            newLocation.GetChild(1).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Melee");
                            }
                        }
                        else if (unit1.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            cardUI.transform.SetParent(newLocation.GetChild(2).GetChild(0));
                            //LeanTween.move(cardUI, newLocation.GetChild(2).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(newLocation.GetChild(2).GetChild(0)));
                            newLocation.GetChild(2).GetComponentInChildren<Row>().InsertInRow(0, cardUI);
                            newLocation.GetChild(2).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Ranged");
                            }
                        }
                        else
                        {
                            cardUI.transform.SetParent(newLocation.GetChild(3).GetChild(0));
                            //LeanTween.move(cardUI, newLocation.GetChild(3).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(newLocation.GetChild(3).GetChild(0)));
                            newLocation.GetChild(3).GetComponentInChildren<Row>().InsertInRow(0, cardUI);
                            newLocation.GetChild(3).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Siege");
                            }
                        }
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en una fila", 0, 0);
                    await Task.Delay(1000); break;
            }
            cards.Insert(0, card);
        }
        else
        {
            if (cards.Contains(card)) return;
            location.GetComponent<Player>().MyDeck.cards.Insert(0, (Card)card);
            cards.Insert(0, card);
        }
        await Task.Delay(1000);
    }
    async public void SendBottom(object card, IList cards, Dictionary<IList, Transform> listingLocation)
    {
        Transform location = listingLocation[cards];
        if (!CanStoreCard(cards, card))
        {
            if (card is GameObject cardUi)
            {
                Card newCard = cardUi.GetComponent<ThisCard>().thisCard;
                if (newCard.Owner == TriggerPlayer) controller.currentTurn.GetComponent<Player>().MyDeck.cards.Insert(0, newCard);
                else controller.notCurrentTurn.GetComponent<Player>().MyDeck.cards.Insert(0, newCard);
                cards.Add(newCard);
                return;
            }
            else
            {
                Debug.Log("deberia entar aqui");
                GameObject newCard = GameObject.Instantiate(controller.CardPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                newCard.GetComponent<ThisCard>().PrintCard((Card)card);
                card = newCard;
            }
        }

        if (card is GameObject cardUI)
        {
            Debug.Log("luego aqui");
            if (cardUI.GetComponent<ThisCard>().thisCard is Unit unit)
            {
                cardUI.GetComponent<ThisCard>().power = unit.Power.ToString();
                cardUI.GetComponent<ThisCard>().powerText.text = unit.Power.ToString();
            }
            if (cards.Contains(card)) return;
            switch (location.name)
            {
                case "Hand":
                    cardUI.transform.SetParent(location);
                    location.GetComponent<Hand>().CardsObject.Add(cardUI);
                    location.GetComponent<Hand>().Cards.Add(cardUI.GetComponent<ThisCard>().thisCard);
                    //LeanTween.move(cardUI, location.position, 1f).setOnComplete(() => cardUI.transform.SetParent(location));
                    cardUI.GetComponent<Drag>().enabled = true;
                    if (cardUI.GetComponent<ClickOnCard>() != null) cardUI.GetComponent<ClickOnCard>().enabled = true;
                    await Task.Delay(1000); break;
                case "Graveyard":
                    cardUI.transform.SetParent(location);
                    location.GetComponent<Graveyard>().CardsObject.Add(cardUI);
                    location.GetComponent<Graveyard>().Cards.Add(cardUI.GetComponent<ThisCard>().thisCard);
                    cardUI.GetComponent<Drag>().enabled = false;
                    //LeanTween.move(cardUI, location.position, 1f).setOnComplete(() => cardUI.transform.SetParent(location));
                    await Task.Delay(1000); break;
                case "EnemyField":
                case "PlayerField":
                    if (cardUI.GetComponent<ThisCard>().thisCard.Owner != location.parent.GetComponent<Player>().Id) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "no se puede colocar una carta propia en el campo rival o viceversa", 0, 0);
                    cardUI.transform.localScale = new Vector3(0.55f, 0.55f, 0);
                    if (cardUI.GetComponent<ThisCard>().thisCard is UnitCard unitCard)
                    {
                        if (unitCard.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            cardUI.transform.SetParent(location.GetChild(1).GetChild(0));
                            //LeanTween.move(cardUI, location.GetChild(1).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(location.GetChild(1).GetChild(0)));
                            location.GetChild(1).GetComponentInChildren<Row>().AddToRow(cardUI);
                            location.GetChild(1).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                Debug.Log("putUnitPlata");
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Melee");
                            }
                        }
                        else if (unitCard.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            cardUI.transform.SetParent(location.GetChild(2).GetChild(0));
                            //LeanTween.move(cardUI, location.GetChild(2).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(location.GetChild(2).GetChild(0)));
                            location.GetChild(2).GetComponentInChildren<Row>().AddToRow(cardUI);
                            location.GetChild(2).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Ranged");
                            }
                        }
                        else
                        {
                            cardUI.transform.SetParent(location.GetChild(3).GetChild(0));
                            //LeanTween.move(cardUI, location.GetChild(3).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(location.GetChild(3).GetChild(0)));
                            location.GetChild(3).GetComponentInChildren<Row>().AddToRow(cardUI);
                            location.GetChild(3).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unitCard is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Siege");
                            }
                        }
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en una fila", 0, 0);
                    await Task.Delay(1000); break;
                case "Canvas":
                    cardUI.transform.localScale = new Vector3(0.55f, 0.55f, 0);
                    if (cardUI.GetComponent<ThisCard>().thisCard is UnitCard unit1)
                    {
                        Transform newLocation;
                        if (unit1.Owner == TriggerPlayer) newLocation = controller.currentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                        else newLocation = controller.notCurrentTurn.transform.Find(controller.notCurrentTurn.name + "Field").transform;
                        if (unit1.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            cardUI.transform.SetParent(newLocation.GetChild(1).GetChild(0));
                            //LeanTween.move(cardUI, newLocation.GetChild(1).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(newLocation.GetChild(1).GetChild(0)));
                            newLocation.GetChild(1).GetComponentInChildren<Row>().AddToRow(cardUI);
                            newLocation.GetChild(1).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Melee");
                            }
                        }
                        else if (unit1.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            cardUI.transform.SetParent(newLocation.GetChild(2).GetChild(0));
                            //LeanTween.move(cardUI, newLocation.GetChild(2).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(newLocation.GetChild(2).GetChild(0)));
                            newLocation.GetChild(2).GetComponentInChildren<Row>().AddToRow(cardUI);
                            newLocation.GetChild(2).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Ranged");
                            }
                        }
                        else
                        {
                            cardUI.transform.SetParent(newLocation.GetChild(3).GetChild(0));
                            //LeanTween.move(cardUI, newLocation.GetChild(3).GetChild(0).position, 1f).setOnComplete(() => cardUI.transform.SetParent(newLocation.GetChild(3).GetChild(0)));
                            newLocation.GetChild(3).GetComponentInChildren<Row>().AddToRow(cardUI);
                            newLocation.GetChild(3).GetComponentInChildren<SumPower>().UpdatePower();
                            cardUI.GetComponent<Drag>().enabled = false;
                            controller.Effects(cardUI);
                            if (!(unit1 is HeroUnit))
                            {
                                GameObject.Find("WeatherZone").GetComponent<WeatherController>().WeatherEffect(cardUI, cardUI.transform.parent);
                                controller.Improve(cardUI, "Siege");
                            }
                        }
                    }
                    else throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, $"una carta de tipo {cardUI.GetComponent<ThisCard>().thisCard.Type} no puede colocarse en una fila", 0, 0);
                    await Task.Delay(1000); break;
            }
            cards.Add(card);

        }
        else
        {
            if (cards.Contains(card)) return;
            location.GetComponent<Player>().MyDeck.cards.Add((Card)card);
            cards.Add(card);
        }
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
                    case "Graveyard":
                        location.GetComponent<Graveyard>().CardsObject.Remove(cardUI);
                        location.GetComponent<Graveyard>().Cards.Remove(cardUI.GetComponent<ThisCard>().thisCard);
                        await Task.Delay(1000); break;
                    case "Hand":
                        if (cardUI.transform.parent == location) MoveGraveyard(cardUI);
                        location.GetComponent<Hand>().CardsObject.Remove(cardUI);
                        location.GetComponent<Hand>().Cards.Remove(cardUI.GetComponent<ThisCard>().thisCard);
                        await Task.Delay(1000); break;
                    case "EnemyField":
                    case "PlayerField":
                        if (!(cardUI.GetComponent<ThisCard>().thisCard is Unit unit1)) return;
                        if (unit1.Owner == TriggerPlayer) location = controller.currentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                        else location = controller.notCurrentTurn.transform.Find(controller.currentTurn.name + "Field").transform;
                        if (unit1.AttackTypes.Contains(Global.AttackModes.Melee))
                        {
                            if (cardUI.transform.parent == location.GetChild(1).GetChild(0).transform) MoveGraveyard(cardUI);
                            location.GetChild(1).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                            location.GetChild(1).GetComponentInChildren<SumPower>().UpdatePower();
                        }
                        else if (unit1.AttackTypes.Contains(Global.AttackModes.Ranged))
                        {
                            if (cardUI.transform.parent == location.GetChild(2).GetChild(0).transform) MoveGraveyard(cardUI);
                            location.GetChild(2).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                            location.GetChild(2).GetComponentInChildren<SumPower>().UpdatePower();
                        }
                        else
                        {
                            if (cardUI.transform.parent == location.GetChild(3).GetChild(0).transform) MoveGraveyard(cardUI);
                            location.GetChild(3).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                            location.GetChild(3).GetComponentInChildren<SumPower>().UpdatePower();
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
                                if (cardUI.transform.parent == newLocation1.GetChild(1).GetChild(0).transform) MoveGraveyard(cardUI);
                                newLocation1.GetChild(1).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                                newLocation1.GetChild(1).GetComponentInChildren<SumPower>().UpdatePower();
                            }
                            else if (unit2.AttackTypes.Contains(Global.AttackModes.Ranged))
                            {
                                if (cardUI.transform.parent == newLocation1.GetChild(2).GetChild(0).transform) MoveGraveyard(cardUI);
                                newLocation1.GetChild(2).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                                newLocation1.GetChild(2).GetComponentInChildren<SumPower>().UpdatePower();
                            }
                            else
                            {
                                if (cardUI.transform.parent == newLocation1.GetChild(3).GetChild(0).transform) MoveGraveyard(cardUI);
                                newLocation1.GetChild(3).GetComponentInChildren<Row>().RemoveFromRow(cardUI);
                                newLocation1.GetChild(3).GetComponentInChildren<SumPower>().UpdatePower();
                            }
                        }
                        await Task.Delay(1000); break;
                }
            }
            else
            {
                Debug.Log("removeDelDeck");
                location.GetComponent<Player>().MyDeck.cards.Remove((Card)card);
            }
        }
        cards.Remove(card);
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
        if (cards.Count == 0) throw ErrorExceptions.Error(ErrorExceptions.ErrorType.SEMANTIC, "lista vacia no se puede obtener elemento", 0, 0);
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

}

public enum LocationCards
{
    Hand,
    Graveyard,
    Field,
    Deck,
    Board,
}
