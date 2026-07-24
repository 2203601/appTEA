using System;
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

    // cantidad de pares según la dificultad elegida en el menú (GameSession.SelectedDifficulty)
    private static readonly Dictionary<Difficulty, int> PARES_POR_DIFICULTAD = new Dictionary<Difficulty, int>
    {
        { Difficulty.Facil, 4 },
        { Difficulty.Medio, 6 },
        { Difficulty.Dificil, 8 },
    };

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
    int paresObjetivo;


    // tiempo
    public Animator gameOverAnimatorComponent;


    private void Awake()
    {
        // inicializamos las listas, nos sale un error si no lo hacemos
        ordererCardList = new List<CardInfo>();
        randomCardList = new List<CardInfo>();
        selectedCards = new List<Card>();

        allCards = Resources.LoadAll<CardInfo>("Cartas");

        // --- Leemos lo elegido en el menú (seteado por DifficultyModalUI.Confirmar) ---

        // Temática: el nombre que viene de GameSession.SelectedTema ("Comida", "Animales", "Paises")
        // matchea directo con los valores del enum Category (comida, animales, paises), sin
        // distinguir mayúsculas/minúsculas. Si no matchea nada (o vino null), se mantiene
        // el valor por defecto ya asignado arriba (Category.comida).
        if (Enum.TryParse(GameSession.SelectedTema, true, out Category categoriaElegida))
        {
            SELECTED_CATEGORY = categoriaElegida;
        }

        // Dificultad: cuántos pares se van a jugar.
        paresObjetivo = PARES_POR_DIFICULTAD.TryGetValue(GameSession.SelectedDifficulty, out int pares)
            ? pares
            : 6; // valor de resguardo por si no hay selección
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
        // filtramos solo las cartas de la temática elegida
        List<CardInfo> disponibles = new List<CardInfo>();
        foreach (CardInfo card in allCards)
        {
            if (card.cardCategory == SELECTED_CATEGORY)
            {
                disponibles.Add(card);
            }
        }

        // barajamos esas cartas (Fisher-Yates)
        for (int i = disponibles.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            CardInfo temp = disponibles[i];
            disponibles[i] = disponibles[j];
            disponibles[j] = temp;
        }

        // nos quedamos con tantos pares como pida la dificultad (o menos, si no hay suficientes cartas de esa temática)
        totalCouples = Mathf.Min(paresObjetivo, disponibles.Count);

        for (int i = 0; i < totalCouples; i++)
        {
            ordererCardList.Add(disponibles[i]);
            ordererCardList.Add(disponibles[i]);
        }

        DisplayCouple();

        while (ordererCardList.Count > 0) // 20
        {
            int randomIndex;
            CardInfo selectedCardInfo;

            randomIndex = UnityEngine.Random.Range(0, ordererCardList.Count); // 0 a 20
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