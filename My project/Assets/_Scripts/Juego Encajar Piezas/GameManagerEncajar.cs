using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class GameManagerEncajar : MonoBehaviour
{
    public static GameManagerEncajar Instancia { get; private set; }

    [Header("Datos")]
    public string carpetaPiezasAEncajar1 = "Piezas A Encajar 1";
    public string carpetaPiezasDondeEncajar1 = "Piezas Donde Encajar 1";
    public string carpetaPiezasAEncajar2 = "Piezas A Encajar 2";
    public string carpetaPiezasDondeEncajar2 = "Piezas Donde Encajar 2";

    List<PiezaInfo> todasLasPiezasAEncajar;
    List<PiezaInfo> todasLasPiezasDondeEncajar;


    [Header("Paneles")]
    [SerializeField] private GameObject panelFacilMedio;
    [SerializeField] private GameObject panelDificil;

    [Header("Referencias de escena")]
    public Image piezaAEncajar;
    public Transform contenedorPiezas;    // objeto vacío con Grid Layout Group
    public PiezaArrastrable piezaPrefab;

    [Header("Rompecabezas (Difícil)")]
    [Tooltip("La zona donde arrancan desparramadas las piezas del rompecabezas")]
    public RectTransform contenedorPiezasRompecabezas;

    public Text feedbackText;               // texto tipo "¡Correcto!" / "Intentá de nuevo"
    public Text rondaText;                  // opcional: "Ronda 1/4"

    [Header("Config")]
    public int totalRondas = 4;
    public float delayEntreRondas = 1.2f;
    int opcionesObjetivo;

    [Header("Pista")]
    [Tooltip("Segundos sin que el chico toque la pantalla antes de que una pieza empiece a brillar")]
    public float segundosParaPista = 20f;

    TemporizadorInactividad inactividad;
    GameObject piezaConPista;

    private static readonly Dictionary<Difficulty, int> opcionesPorDificultad = new Dictionary<Difficulty, int>
    {
        { Difficulty.Facil, 3 },
        { Difficulty.Medio, 4 },
    };

    // frases que se eligen al azar

    public string[] frasesCorrecto ={
        "Correcto ! Muy bien!",
        "Excelente! Sigue así!",
        "Así es! ",
        "Genial!"

    };
    public string[] frasesReintentar ={
            "Intentá de nuevo, vos podes!",
            "Casi, proba otra vez!",
            "No es esa, cuál será?",
            "Ups, elegí otra opción!"

    };

    [Tooltip("Usa {0} donde va el nombre de la pieza correcta")]
    public string[] frasesRevelar ={
        "La respuesta correcta era {0}",
        "Esta vez era {0}",
        "Tranquilo, la próxima la sacás. Era {0}",
        "¡Buen intento! Era {0}"

    };


    // nivel uno y dos
    PiezaInfo piezaActual;
    PiezaInfo piezaCorrecta;

    // nivel tres
    PiezaRompecabezas[] piezasRompecabezas;
    PiezaRompecabezas piezaAgarrada;
    int piezasColocadasCorrectas;

    List<PiezaArrastrable> piezasActuales = new List<PiezaArrastrable>();

    int rondaActual;
    int intentosFallidos;
    bool esperandoSiguienteRonda;

    // animación del personaje
    public Animator personaje;
    public PanelFinJuego panelFin;          // cartel "¡Muy bien!" al terminar

    // nombre del parámetro Trigger que armaste en el Animator Controller
    static readonly int SonreirTrigger = Animator.StringToHash("Sonreir");
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        Instancia = this;
        inactividad = new TemporizadorInactividad(segundosParaPista);

        opcionesObjetivo = opcionesPorDificultad.TryGetValue(GameSession.SelectedDifficulty, out int opciones)
            ? opciones
            : 4; // valor de resguardo por si no hay selección

        if (GameSession.SelectedDifficulty == Difficulty.Facil)
        {
            todasLasPiezasAEncajar = new List<PiezaInfo>(Resources.LoadAll<PiezaInfo>(carpetaPiezasAEncajar1));
            todasLasPiezasDondeEncajar = new List<PiezaInfo>(Resources.LoadAll<PiezaInfo>(carpetaPiezasDondeEncajar1));
        }
        else if (GameSession.SelectedDifficulty == Difficulty.Medio)
        {
            todasLasPiezasAEncajar = new List<PiezaInfo>(Resources.LoadAll<PiezaInfo>(carpetaPiezasAEncajar2));
            todasLasPiezasDondeEncajar = new List<PiezaInfo>(Resources.LoadAll<PiezaInfo>(carpetaPiezasDondeEncajar2));
        }


    }
    private void ConfigurarVista()
    {
        bool esDificil = GameSession.SelectedDifficulty == Difficulty.Dificil;

        panelFacilMedio.SetActive(!esDificil);
        panelDificil.SetActive(esDificil);
    }
    void Start()
    {
        ConfigurarVista();
        rondaActual = 0;
        NuevaRonda();
    }

    void NuevaRonda()
    {
        if (GameSession.SelectedDifficulty == Difficulty.Dificil)
        {
            NuevaRondaRompecabezas();
        }
        else
        {
            NuevaRondaPiezas();
        }

    }

    void NuevaRondaPiezas()
    {
        if (rondaActual >= totalRondas)
        {
            TerminarJuego();
            return;
        }
        rondaActual++;
        if (rondaText != null) rondaText.text = $"Ronda {rondaActual}/{totalRondas}";

        esperandoSiguienteRonda = false;
        intentosFallidos = 0;
        if (feedbackText != null) feedbackText.text = "";
        QuitarPista();
        inactividad.Reiniciar();

        // 0) Limpiar los emojis de la ronda anterior
        foreach (var pieza in piezasActuales)
        {
            Destroy(pieza.gameObject);
        }
        piezasActuales.Clear();

        // 1) Elegir la situación y su emoción correcta
        piezaActual = todasLasPiezasDondeEncajar[Random.Range(0, todasLasPiezasDondeEncajar.Count)];
        piezaAEncajar.sprite = piezaActual.piezaSprite;

        piezaCorrecta = todasLasPiezasAEncajar.Find(e => e.piezaNombre == piezaActual.piezaNombre);
        if (piezaCorrecta == null)
        {
            Debug.LogError($"La pieza '{piezaActual.name}' tiene piezaCorrecta = " +
                $"'{piezaActual.piezaNombre}', que no matchea con ningún PiezaInfo cargado.");
            return;
        }

        // 2) Armar el pool de emojis: el correcto + distractores sin repetir
        List<PiezaInfo> pool = new List<PiezaInfo>(todasLasPiezasAEncajar);
        pool.Remove(piezaCorrecta);
        Barajar(pool);


        List<PiezaInfo> seleccionadas = new List<PiezaInfo> { piezaCorrecta };
        for (int i = 0; i < opcionesObjetivo - 1 && i < pool.Count; i++)
        {
            seleccionadas.Add(pool[i]);
        }
        Barajar(seleccionadas);

        // 3) Instanciar un emoji arrastrable por opción
        foreach (var info in seleccionadas)
        {
            PiezaArrastrable nuevo = Instantiate(piezaPrefab, contenedorPiezas);
            nuevo.Configurar(info);
            piezasActuales.Add(nuevo);
        }

    }

    void NuevaRondaRompecabezas()
    {

        if (rondaText != null) rondaText.text = "";

        esperandoSiguienteRonda = false;
        intentosFallidos = 0;
        if (feedbackText != null) feedbackText.text = "";

        piezasColocadasCorrectas = 0;
        piezaAgarrada = null;
        QuitarPista();
        inactividad.Reiniciar();
        PiezaRompecabezas.ReiniciarOrden();

        // Las piezas ya están armadas en la escena, dentro del panel difícil
        piezasRompecabezas = panelDificil.GetComponentsInChildren<PiezaRompecabezas>();
        if (piezasRompecabezas.Length == 0)
        {
            Debug.LogError("No hay ninguna PiezaRompecabezas dentro de " + panelDificil.name +
                ". Agregale el componente a cada pieza del rompecabezas.");
            return;
        }

        // Repartirlas en una grilla prolija dentro de la bandeja, en orden mezclado
        Rect zona = contenedorPiezasRompecabezas.rect;
        int total = piezasRompecabezas.Length;
        int columnas = Mathf.CeilToInt(Mathf.Sqrt(total));
        int filas = Mathf.CeilToInt(total / (float)columnas);
        Vector2 celda = new Vector2(zona.width / columnas, zona.height / filas);

        List<int> casillas = new List<int>();
        for (int i = 0; i < total; i++) casillas.Add(i);
        Barajar(casillas);

        for (int i = 0; i < total; i++)
        {
            int columna = casillas[i] % columnas;
            int fila = casillas[i] / columnas;
            Vector2 punto = new Vector2(
                zona.xMin + (columna + 0.5f) * celda.x,
                zona.yMax - (fila + 0.5f) * celda.y);

            piezasRompecabezas[i].Desparramar(contenedorPiezasRompecabezas.TransformPoint(punto));
        }

    }


    public void ResponderPiezas(PiezaArrastrable piezaSoltada)
    {
        Debug.Log("Entró a Responder");
        Debug.Log("Pieza: " + piezaSoltada.PiezaNombre);
        Debug.Log("Correcta: " + piezaActual.piezaNombre);
        if (esperandoSiguienteRonda)
        {
            piezaSoltada.VolverAPosicionOriginal();
            return;
        }

        bool esCorrecta = piezaSoltada.PiezaNombre == piezaActual.piezaNombre;

        if (esCorrecta)
        {
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesCorrecto);
            piezaSoltada.transform.SetParent(piezaAEncajar.transform, true);
            if (personaje != null) personaje.SetTrigger(SonreirTrigger);

            TerminarRonda();
            return;
        }

        // Respuesta incorrecta: el emoji vuelve a la bandeja
        piezaSoltada.VolverAPosicionOriginal();
        intentosFallidos++;

        if (intentosFallidos < 2)
        {
            // Primer error: puede volver a intentar con los emojis que quedan
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesReintentar);
            return;
        }

        // Segundo error: se revela cuál era la correcta y se cierra la ronda
        if (feedbackText != null)
            feedbackText.text = string.Format(FraseAlAzar(frasesRevelar), piezaActual.piezaNombre);
        TerminarRonda();
    }


    void Update()
    {
        // En pausa (timeScale = 0) no avanza nada: ni la pista ni el arrastre del rompecabezas
        if (Time.timeScale == 0f) return;

        ActualizarPista();

        if (piezasRompecabezas == null || esperandoSiguienteRonda) return;

        Pointer puntero = Pointer.current;
        Camera camara = Camera.main;
        if (puntero == null || camara == null) return;

        Vector3 punto = camara.ScreenToWorldPoint(puntero.position.ReadValue());

        if (puntero.press.wasPressedThisFrame)
        {
            // De las piezas sueltas que están bajo el dedo, agarrar la de más arriba
            piezaAgarrada = null;
            foreach (var pieza in piezasRompecabezas)
            {
                if (pieza.Colocada || !pieza.Contiene(punto)) continue;
                if (piezaAgarrada == null || pieza.Orden > piezaAgarrada.Orden) piezaAgarrada = pieza;
            }
            if (piezaAgarrada != null) piezaAgarrada.Agarrar(punto);
        }
        else if (piezaAgarrada != null && puntero.press.isPressed)
        {
            piezaAgarrada.Arrastrar(punto);
        }
        else if (piezaAgarrada != null)
        {
            bool encajo = piezaAgarrada.Soltar();
            piezaAgarrada = null;
            if (encajo) NotificarPiezaRompecabezasColocada();
        }
    }

    public void NotificarPiezaRompecabezasColocada()
    {
        if (esperandoSiguienteRonda) return;

        piezasColocadasCorrectas++;

        if (piezasRompecabezas != null && piezasColocadasCorrectas >= piezasRompecabezas.Length)
        {
            esperandoSiguienteRonda = true;
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesCorrecto);
            if (personaje != null) personaje.SetTrigger(SonreirTrigger);
            Invoke(nameof(TerminarJuego), delayEntreRondas);
        }
    }

    // Pista suave: tras segundosParaPista sin tocar la pantalla, brilla la pieza que corresponde.
    // Cualquier toque la apaga y vuelve a empezar la cuenta.
    void ActualizarPista()
    {
        Pointer puntero = Pointer.current;
        if (puntero != null && puntero.press.wasPressedThisFrame)
        {
            QuitarPista();
            inactividad.Reiniciar();
        }

        if (esperandoSiguienteRonda) return;

        if (inactividad.Avanzar(Time.deltaTime)) MostrarPista();
    }

    void MostrarPista()
    {
        GameObject objetivo = null;

        if (GameSession.SelectedDifficulty == Difficulty.Dificil)
        {
            // La pieza suelta que está más arriba (la que se ve completa)
            PiezaRompecabezas mejor = null;
            if (piezasRompecabezas != null)
            {
                foreach (var pieza in piezasRompecabezas)
                {
                    if (pieza.Colocada) continue;
                    if (mejor == null || pieza.Orden > mejor.Orden) mejor = pieza;
                }
            }
            if (mejor != null) objetivo = mejor.gameObject;
        }
        else if (piezaActual != null)
        {
            // La opción que encaja en la silueta de esta ronda
            foreach (var pieza in piezasActuales)
            {
                if (pieza != null && pieza.PiezaNombre == piezaActual.piezaNombre)
                {
                    objetivo = pieza.gameObject;
                    break;
                }
            }
        }

        if (objetivo == null) return;
        PistaBrillo.Activar(objetivo);
        piezaConPista = objetivo;
    }

    void QuitarPista()
    {
        if (piezaConPista != null) PistaBrillo.Desactivar(piezaConPista);
        piezaConPista = null;
    }

    void TerminarRonda()
    {
        esperandoSiguienteRonda = true;
        QuitarPista();


        foreach (var pieza in piezasActuales)
        {
            pieza.Bloquear();
        }


        Invoke(nameof(NuevaRonda), delayEntreRondas);
    }
    void TerminarJuego()
    {

        foreach (var pieza in piezasActuales)
        {
            Destroy(pieza.gameObject);
        }
        piezasActuales.Clear();

        if (feedbackText != null) feedbackText.text = "";
        if (panelFin != null) panelFin.Mostrar();

    }
    string FraseAlAzar(string[] frases)
    {
        if (frases == null || frases.Length == 0) return "";
        return frases[UnityEngine.Random.Range(0, frases.Length)];
    }
    void Barajar(List<PiezaInfo> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
    
    void Barajar(List<int> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
