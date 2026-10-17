using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerCompletar : MonoBehaviour
{
    public static GameManagerCompletar Instancia { get; private set; }

    [Header("Datos")]
    [SerializeField] private BancoCompletar banco;
    public string rutaBanco = "Completar/BancoCompletar";
    public string rutaDiccionario = "Completar/palabras";

    [Header("Referencias de escena")]
    public Image imagenPista;
    public RectTransform contenedorPalabra;   // con Horizontal Layout Group
    public RectTransform contenedorOpciones;  // con Horizontal Layout Group
    public CasilleroCompletar casilleroPrefab;
    public BotonOpcionCompletar botonPrefab;

    public Text feedbackText;
    public Text rondaText;
    public Animator personaje;

    [Header("Config")]
    public int totalRondas = 5;
    public float delayEntreRondas = 1.5f;
    public float delayEntreHuecos = 0.9f;
    [Tooltip("Fallos en un mismo hueco antes de que la opción correcta haga el latido como pista")]
    public int fallosParaPista = 2;
    [Tooltip("Ancho máximo de la palabra; si es más larga, se achica entera para que entre")]
    public float anchoDisponible = 850f;

    // frases que se eligen al azar (siempre positivas, nunca punitivas)
    public string[] frasesCorrecto ={
        "Correcto! Muy bien!",
        "Excelente! Sigue así!",
        "Así es!",
        "Genial!"
    };
    public string[] frasesFin ={
        "Terminaste! Qué bien lo hiciste!",
        "Lo lograste! Muy buen trabajo!"
    };

    static readonly int SonreirTrigger = Animator.StringToHash("Sonreir");

    List<EjercicioCompletar> ejercicios = new List<EjercicioCompletar>();
    List<int> mazo = new List<int>();
    GeneradorHuecos generador;
    int huecosPorEjercicio, opcionesPorHueco;

    RondaCompletar ronda;
    List<CasilleroCompletar> casilleros = new List<CasilleroCompletar>();
    List<BotonOpcionCompletar> botones = new List<BotonOpcionCompletar>();

    int rondaActual;
    int intentosFallidos;
    bool esperandoSiguiente;

    void Awake()
    {
        Instancia = this;

        if (banco == null) banco = Resources.Load<BancoCompletar>(rutaBanco);
        if (banco == null)
        {
            Debug.LogError($"No se encontró el BancoCompletar en Resources/{rutaBanco}. " +
                "Corré el menú appTEA > Completar > 1 - Crear banco y GameData.");
            return;
        }

        TextAsset diccionario = Resources.Load<TextAsset>(rutaDiccionario);
        if (diccionario == null)
            Debug.LogWarning($"No se encontró el diccionario en Resources/{rutaDiccionario}.txt: " +
                "los distractores no se validan contra palabras reales.");
        generador = new GeneradorHuecos(diccionario != null ? diccionario.text : "");

        CargarEjerciciosDelNivel();
    }

    // Fácil = números, Medio = palabras con imagen, Difícil = palabras largas
    void CargarEjerciciosDelNivel()
    {
        switch (GameSession.SelectedDifficulty)
        {
            case Difficulty.Medio:
                foreach (var p in banco.palabrasConImagen)
                    ejercicios.Add(new EjercicioCompletar(p.palabra, p.imagen, false));
                huecosPorEjercicio = 2; opcionesPorHueco = 4;
                break;
            case Difficulty.Dificil:
                foreach (var p in banco.palabrasLargas)
                    ejercicios.Add(new EjercicioCompletar(p, null, false));
                huecosPorEjercicio = 3; opcionesPorHueco = 5;
                break;
            default:
                foreach (var s in banco.secuenciasNumeros)
                    ejercicios.Add(new EjercicioCompletar(s, null, true));
                huecosPorEjercicio = 1; opcionesPorHueco = 3;
                break;
        }
    }

    void Start()
    {
        if (ejercicios.Count == 0)
        {
            Debug.LogError("No hay ejercicios para este nivel en el BancoCompletar.");
            return;
        }
        rondaActual = 0;
        NuevaRonda();
    }

    void NuevaRonda()
    {
        if (rondaActual >= totalRondas)
        {
            TerminarJuego();
            return;
        }

        // Si el generador no puede con un ejercicio, se pasa al siguiente
        EjercicioCompletar ejercicio = null;
        for (int intento = 0; intento < ejercicios.Count && ejercicio == null; intento++)
            ejercicio = ArmarRonda();
        if (ejercicio == null)
        {
            Debug.LogError("No se pudo generar ningún ejercicio válido para este nivel.");
            return;
        }

        rondaActual++;
        if (rondaText != null) rondaText.text = $"Ronda {rondaActual}/{totalRondas}";
        if (feedbackText != null) feedbackText.text = "";

        if (imagenPista != null)
        {
            imagenPista.gameObject.SetActive(ejercicio.pista != null);
            imagenPista.sprite = ejercicio.pista;
            imagenPista.preserveAspect = true;
        }

        ConstruirPalabra();
        ActivarHuecoActual();
    }

    // Arma la ronda con el próximo ejercicio; devuelve null si el generador no pudo
    EjercicioCompletar ArmarRonda()
    {
        EjercicioCompletar ejercicio = SiguienteEjercicio();

        string texto = ejercicio.texto;
        List<Hueco> huecos;

        if (ejercicio.esNumerico)
        {
            var (secuencia, hueco) = generador.GenerarSecuenciaNumeros(opcionesPorHueco, texto);
            texto = secuencia;
            huecos = new List<Hueco> { hueco };
        }
        else
        {
            texto = GeneradorHuecos.Normalizar(texto);
            huecos = generador.GenerarHuecosPalabra(texto, huecosPorEjercicio, opcionesPorHueco);
            if (huecos == null)
            {
                Debug.LogWarning($"No se pudieron generar huecos para '{ejercicio.texto}'. Se pasa al siguiente ejercicio.");
                return null;
            }
        }

        LimpiarRonda();
        ronda = new RondaCompletar(texto, huecos);
        return ejercicio;
    }

    EjercicioCompletar SiguienteEjercicio()
    {
        // Mazo mezclado: no se repite hasta usar todos
        if (mazo.Count == 0)
        {
            for (int i = 0; i < ejercicios.Count; i++) mazo.Add(i);
            Barajar(mazo);
        }
        int indice = mazo[mazo.Count - 1];
        mazo.RemoveAt(mazo.Count - 1);
        return ejercicios[indice];
    }

    void LimpiarRonda()
    {
        foreach (var c in casilleros) if (c != null) Destroy(c.gameObject);
        casilleros.Clear();
        LimpiarOpciones();
    }

    void LimpiarOpciones()
    {
        foreach (var b in botones) if (b != null) Destroy(b.gameObject);
        botones.Clear();
    }

    void ConstruirPalabra()
    {
        string texto = ronda.Texto;
        for (int i = 0; i < texto.Length; i++)
        {
            CasilleroCompletar casillero = Instantiate(casilleroPrefab, contenedorPalabra);
            casillero.Configurar(banco.BuscarSprite(texto[i]), ronda.EsHueco(i));
            casilleros.Add(casillero);
        }

        // Si la palabra es larga, se achica entera para que entre en pantalla
        float anchoCasillero = ((RectTransform)casilleroPrefab.transform).sizeDelta.x;
        var layout = contenedorPalabra.GetComponent<HorizontalLayoutGroup>();
        float separacion = layout != null ? layout.spacing : 0f;
        float anchoTotal = texto.Length * anchoCasillero + (texto.Length - 1) * separacion;
        contenedorPalabra.localScale = Vector3.one * Mathf.Min(1f, anchoDisponible / anchoTotal);
    }

    void ActivarHuecoActual()
    {
        esperandoSiguiente = false;
        intentosFallidos = 0;

        casilleros[ronda.IndiceHuecoActual].Resaltar(true);

        LimpiarOpciones();
        foreach (char c in ronda.OpcionesDelHuecoActual())
        {
            BotonOpcionCompletar boton = Instantiate(botonPrefab, contenedorOpciones);
            boton.Configurar(c, banco.BuscarSprite(c), Responder);
            botones.Add(boton);
        }
    }

    public void Responder(BotonOpcionCompletar boton)
    {
        if (esperandoSiguiente) return;

        if (boton.Caracter == ronda.CaracterEsperado)
        {
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesCorrecto);
            if (personaje != null) personaje.SetTrigger(SonreirTrigger);
            CompletarHueco();
            return;
        }

        // Error solo visual: la opción se atenúa, sin texto ni sonido
        boton.Atenuar();
        intentosFallidos++;

        // Desde el segundo error la correcta late como pista; el chico la tiene que tocar
        if (intentosFallidos >= fallosParaPista)
        {
            foreach (var b in botones)
                if (b.Caracter == ronda.CaracterEsperado) b.Resaltar();
        }
    }

    void CompletarHueco()
    {
        esperandoSiguiente = true;
        foreach (var b in botones) b.Bloquear();
        StartCoroutine(CompletarHuecoTras());
    }

    IEnumerator CompletarHuecoTras()
    {
        casilleros[ronda.IndiceHuecoActual].Rellenar(banco.BuscarSprite(ronda.CaracterEsperado));
        ronda.Avanzar();

        if (ronda.Terminada)
        {
            LimpiarOpciones();
            yield return new WaitForSeconds(delayEntreRondas);
            NuevaRonda();
        }
        else
        {
            yield return new WaitForSeconds(delayEntreHuecos);
            if (feedbackText != null) feedbackText.text = "";
            ActivarHuecoActual();
        }
    }

    void TerminarJuego()
    {
        LimpiarRonda();
        if (imagenPista != null) imagenPista.gameObject.SetActive(false);
        if (rondaText != null) rondaText.text = "";
        if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesFin);
        if (personaje != null) personaje.SetTrigger(SonreirTrigger);
    }

    string FraseAlAzar(string[] frases)
    {
        if (frases == null || frases.Length == 0) return "";
        return frases[Random.Range(0, frases.Length)];
    }

    void Barajar<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
