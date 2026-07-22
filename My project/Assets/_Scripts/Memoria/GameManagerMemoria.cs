using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MemoriaGameManager : MonoBehaviour
{

    // variable global
    public static Color SELECTED_COLOR = Color.white;
    public static Category SELECTED_CATEGORY = Category.comida;
    // referencia al componente sprite renderer 
    public SpriteRenderer fondoSpriteRenderer;
    public Text couplesTextComponent;


    // referencias para instanciar cartas
    public Transform contentDeckTransform;
    public Card prefabCard;
    public CardInfo[] allCards;

    List<CardInfo> ordererCardList;
    List<CardInfo> randomCardList;
    List<Card> selectedCards;

    int totalCouples;
    int currentCouples = 0;


    // tiempo
    public Animator gameOverAnimatorComponent;


    private void Awake()
    {
        // inicializamos las listas, nos sale un error si no lo hacemos
        ordererCardList = new List<CardInfo>();
        randomCardList = new List<CardInfo>();
        selectedCards = new List<Card>();

        allCards = Resources.LoadAll<CardInfo>("Cartas");

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // cambiamos el color
        fondoSpriteRenderer.color = SELECTED_COLOR;
        CreateGrid();
       
    }


    void CreateGrid()
    {
        foreach (CardInfo card in allCards)
        {
            if (card.cardCategory == SELECTED_CATEGORY)
            {
                totalCouples++; 
                for (int i = 0; i < 2; i++)
                {
                    ordererCardList.Add(card);

                }
            }
        }

        DisplayCouple();

        while (ordererCardList.Count > 0) // 20
        {
            int randomIndex;
            CardInfo selectedCardInfo;

            randomIndex = Random.Range(0, ordererCardList.Count); // 0 a 20
            selectedCardInfo = ordererCardList[randomIndex];

            randomCardList.Add(selectedCardInfo);
            ordererCardList.Remove(selectedCardInfo); // -1 el count, para terminar el while
        }
        foreach (CardInfo card in randomCardList)
        {
            Card instance;
            instance = Instantiate(prefabCard, contentDeckTransform);
            instance.SetCard(card);

            // asignamos el metodo a la accion
            instance.OnSelect = AddCards;
        }
    }

    void AddCards(Card selectCard)
    {
        selectedCards.Add(selectCard);
        if (Card.FLIPPED_CARD >= 2)
        {
            CompareSelectedCard();
        }
    }

    void CompareSelectedCard()
    {
        if (selectedCards[0].GetCardName() == selectedCards[1].GetCardName())
        {
            currentCouples++;
            DisplayCouple();
            selectedCards.Clear();
            Card.FLIPPED_CARD = 0;
            if (currentCouples >= totalCouples)
            {
                Win();
            }
        }
        else
        {
            Invoke("TurnWrongCard", 1.5f);
        }
        
    }
    void DisplayCouple()
    {
        couplesTextComponent.text = currentCouples.ToString("00") + " / " + totalCouples.ToString("00");
    }

    void TurnWrongCard()
    {
        foreach (Card cardSelected in selectedCards)
        {
            cardSelected.ActivateAnimator();
        }
            
        selectedCards.Clear();
        Card.FLIPPED_CARD = 0;
    }

    void Lose()
    {
        Card.GAME_OVER = true;
        gameOverAnimatorComponent.enabled = true;
    }

    void Win()
    {
    }
    // Update is called once per frame
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
