
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
 
    [Header("Pista")]
    [Tooltip("Segundos sin acertar un par antes de que empiece a brillar la carta que corresponde dar vuelta")]
    public float segundosParaPista = 180f;

    TemporizadorInactividad temporizadorPista;
    List<Card> todasLasCartas = new List<Card>();
    bool pistaActiva;
    Card cartaConPista;

    int totalCouples;
    int currentCouples = 0;
    int paresObjetivo;
 
 
    // animación del personaje
    public Animator personaje;
    public PanelFinJuego panelFin;          // cartel "¡Muy bien!" al terminar
 
    // nombre del parámetro Trigger que armaste en el Animator Controller
    static readonly int SonreirTrigger = Animator.StringToHash("Sonreir");
 
 
    private void Awake()
    {
        ordererCardList = new List<CardInfo>();
        randomCardList = new List<CardInfo>();
        selectedCards = new List<Card>();
        temporizadorPista = new TemporizadorInactividad(segundosParaPista);
 
        allCards = Resources.LoadAll<CardInfo>("Cartas");
 
       
        if (Enum.TryParse(GameSession.SelectedTema, true, out Category categoriaElegida))
        {
            SELECTED_CATEGORY = categoriaElegida;
        }
 
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
            todasLasCartas.Add(instance);
 
            // asignamos el metodo a la accion
            instance.OnSelect = AddCards;
        }
    }
 
    void AddCards(Card selectCard)
    {
        selectedCards.Add(selectCard);
        if (pistaActiva) ActualizarPista();
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
            selectedCards[0].Encontrada = true;
            selectedCards[1].Encontrada = true;
            temporizadorPista.Reiniciar();
            pistaActiva = false;
            QuitarPista();
            selectedCards.Clear();
            Card.FLIPPED_CARD = 0;
 
            // Dispara la animación de sonreír. El Animator Controller se encarga
            // de volver solo a Idle cuando el clip termina (transición con Exit Time).
            if (personaje != null) personaje.SetTrigger(SonreirTrigger);
 
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

        if (pistaActiva) ActualizarPista();
    }
 
    void Update()
    {
        if (Card.GAME_OVER || currentCouples >= totalCouples) return;

        // Sin límite de tiempo: esto solo enciende una pista suave. En pausa (timeScale = 0) no avanza.
        if (temporizadorPista.Avanzar(Time.deltaTime))
        {
            pistaActiva = true;
            ActualizarPista();
        }
    }

    // La carta que "toca dar vuelta": si ya hay una dada vuelta, su par; si no, cualquiera sin par encontrado.
    void ActualizarPista()
    {
        Card objetivo = null;
        if (selectedCards.Count == 1)
        {
            Card elegida = selectedCards[0];
            objetivo = todasLasCartas.Find(c => c != elegida && !c.Encontrada && c.GetCardName() == elegida.GetCardName());
        }
        else if (selectedCards.Count == 0)
        {
            objetivo = todasLasCartas.Find(c => !c.Encontrada);
        }
        // con dos dadas vuelta se está comparando: sin pista hasta que se vuelvan a tapar

        if (objetivo == cartaConPista) return;

        QuitarPista();
        if (objetivo != null)
        {
            PistaBrillo.Activar(objetivo.gameObject);
            cartaConPista = objetivo;
        }
    }

    void QuitarPista()
    {
        if (cartaConPista != null) PistaBrillo.Desactivar(cartaConPista.gameObject);
        cartaConPista = null;
    }
 
    void Lose()
    {
        Card.GAME_OVER = true;
    }
 
    void Win()
    {
        if (panelFin != null) panelFin.Mostrar();
    }
    // Update is called once per frame
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
 


