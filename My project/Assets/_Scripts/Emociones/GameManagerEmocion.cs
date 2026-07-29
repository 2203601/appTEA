
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
public class GameManagerEmocion : MonoBehaviour
{
    [Header("Datos")]
    [Tooltip("Carpeta dentro de Assets/Resources/ de donde se cargan los EmocionInfo")]
    public string carpetaEmociones = "Emociones";

    public string carpetaSituaciones = "Situaciones";

    // Se llena solo en Awake() leyendo Assets/Resources/<carpetaEmociones>.
    // No hace falta arrastrar nada a mano en el Inspector.
    List<EmocionInfo> todasLasEmociones;
    List<SituacionInfo> todasLasSituaciones;

    [Header("Paneles")]
    [SerializeField] private GameObject panelFacilMedio;
    [SerializeField] private GameObject panelDificil;
    
    [Header("Referencias de escena")]
    public Emocion tarjetaObjetivo;         // el emoji grande a adivinar
    public Transform contenedorOpciones;    // objeto vacío con Grid Layout Group
    public OpcionEmocion opcionPrefab;      // prefab del botón de respuesta
    public Text feedbackText;               // texto tipo "¡Correcto!" / "Intentá de nuevo"
    public Text rondaText;                  // opcional: "Ronda 1/4"

    // para situacion
    public Image situacionImage;
    public Text situacionTexto;
    public Transform contenedorEmojis;
    public EmojiArrastrable emojiPrefab;




    [Header("Config")]
 
    public int totalRondas = 4;
    public float delayEntreRondas = 1.2f;
 
    int opcionesObjetivo;
 
    // cantidad de opciones según la dificultad elegida en el menú (GameSession.SelectedDifficulty)
    private static readonly Dictionary<Difficulty, int> opcionesPorDificultad = new Dictionary<Difficulty, int>
    {
        { Difficulty.Facil, 3 },
        { Difficulty.Medio, 6 },
        {Difficulty.Dificil, 4},
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
 
    [Tooltip("Usa {0} donde va el nombre de la emoción correcta")]
    public string[] frasesRevelar ={
        "La respuesta correcta era {0}",
        "Esta vez era {0}",
        "Tranquilo, la próxima la sacás. Era {0}",
        "¡Buen intento! Era {0}"
 
    };

    // situacion
    SituacionInfo situacionActual;
    EmocionInfo emocionCorrectaActual;
    List<EmojiArrastrable> emojisActuales = new List<EmojiArrastrable>();

    EmocionInfo emocionActual;
    List<OpcionEmocion> opcionesActuales = new List<OpcionEmocion>();
    int rondaActual;
    int intentosFallidos;
    bool esperandoSiguienteRonda;


    // animación del personaje
    public Animator personaje;
 
    // nombre del parámetro Trigger que armaste en el Animator Controller
    static readonly int SonreirTrigger = Animator.StringToHash("Sonreir");

    void Awake()
    {
        opcionesObjetivo = opcionesPorDificultad.TryGetValue(GameSession.SelectedDifficulty, out int opciones)
            ? opciones
            : 4; // valor de resguardo por si no hay selección

        todasLasEmociones = new List<EmocionInfo>(Resources.LoadAll<EmocionInfo>(carpetaEmociones));
        todasLasSituaciones = new List<SituacionInfo>(Resources.LoadAll<SituacionInfo>(carpetaSituaciones));


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
            NuevaRondaSituacion();
        }
        else
        {
            NuevaRondaEmociones();
        }
        
    }
 
    void NuevaRondaEmociones()
    {
        // Si ya jugamos las rondas que tocaban, cerramos el juego acá
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
 
        // 0) Limpiar las opciones de la ronda anterior
        foreach (var opcion in opcionesActuales)
        {
            Destroy(opcion.gameObject);
        }
        opcionesActuales.Clear();
 
        // 1) Elegir la emoción correcta
        emocionActual = todasLasEmociones[UnityEngine.Random.Range(0, todasLasEmociones.Count)];
        tarjetaObjetivo.SetCard(emocionActual);
 
        // 2) Armar el pool de opciones: la correcta + distractores sin repetir
        List<EmocionInfo> pool = new List<EmocionInfo>(todasLasEmociones);
        pool.Remove(emocionActual);
        Barajar(pool);
 
        List<EmocionInfo> seleccionadas = new List<EmocionInfo> { emocionActual };
        for (int i = 0; i < opcionesObjetivo - 1 && i < pool.Count; i++)
        {
            seleccionadas.Add(pool[i]);
        }
        Barajar(seleccionadas);
 
        // 3) Instanciar un botón por opción dentro del contenedor con Grid Layout Group.
        // El grid se acomoda solo (columnas/filas, spacing, tamaño de celda) según
        // cómo lo configures en el Inspector del contenedor, así que esto sirve
        // igual si mañana cambiás la dificultad y opcionesObjetivo pasa a ser 6, 8, etc.
        foreach (var info in seleccionadas)
        {
            OpcionEmocion nueva = Instantiate(opcionPrefab, contenedorOpciones);
            nueva.Configurar(info, this);
            opcionesActuales.Add(nueva);
        }
    }
    
     void NuevaRondaSituacion()
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

        // 0) Limpiar los emojis de la ronda anterior
        foreach (var emoji in emojisActuales)
        {
            Destroy(emoji.gameObject);
        }
        emojisActuales.Clear();

        // 1) Elegir la situación y su emoción correcta
        situacionActual = todasLasSituaciones[Random.Range(0, todasLasSituaciones.Count)];
        situacionImage.sprite = situacionActual.situacionSprite;
        if (situacionTexto != null) situacionTexto.text = situacionActual.descripcion;

        emocionCorrectaActual = todasLasEmociones.Find(e => e.emocionNombre == situacionActual.emocionCorrecta);
        if (emocionCorrectaActual == null)
        {
            Debug.LogError($"La situación '{situacionActual.name}' tiene emocionCorrecta = " +
                $"'{situacionActual.emocionCorrecta}', que no matchea con ningún EmocionInfo cargado.");
            return;
        }

        // 2) Armar el pool de emojis: el correcto + distractores sin repetir
        List<EmocionInfo> pool = new List<EmocionInfo>(todasLasEmociones);
        pool.Remove(emocionCorrectaActual);
        Barajar(pool);

        List<EmocionInfo> seleccionadas = new List<EmocionInfo> { emocionCorrectaActual };
        for (int i = 0; i < opcionesObjetivo - 1 && i < pool.Count; i++)
        {
            seleccionadas.Add(pool[i]);
        }
        Barajar(seleccionadas);

        // 3) Instanciar un emoji arrastrable por opción
        foreach (var info in seleccionadas)
        {
            EmojiArrastrable nuevo = Instantiate(emojiPrefab, contenedorEmojis);
            nuevo.Configurar(info);
            emojisActuales.Add(nuevo);
        }
    }
 
    public void ResponderEmocion(string nombreSeleccionado, OpcionEmocion opcionElegida)
    {
        // Ya se cerró la ronda (acertó o gastó los 2 intentos): ignorar más clicks
        if (esperandoSiguienteRonda) return;

        bool esCorrecta = nombreSeleccionado == emocionActual.emocionNombre;

        if (esCorrecta)
        {
            opcionElegida.MarcarComoCorrecta();
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesCorrecto);

            // Dispara la animación de sonreír. El Animator Controller se encarga
            // de volver solo a Idle cuando el clip termina (transición con Exit Time).
            if (personaje != null) personaje.SetTrigger(SonreirTrigger);
            TerminarRonda();
            return;
        }

        // Respuesta incorrecta
        opcionElegida.MarcarComoIncorrecta();
        opcionElegida.GetComponent<Button>().interactable = false; // esa opción ya no se puede volver a elegir
        intentosFallidos++;

        if (intentosFallidos < 2)
        {
            // Primer error: puede volver a intentar con las opciones que quedan
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesReintentar);
            return;
        }

        // Segundo error: se revela cuál era la correcta y se cierra la ronda
        if (feedbackText != null) feedbackText.text = string.Format(FraseAlAzar(frasesRevelar), emocionActual.emocionNombre);
        foreach (var opcion in opcionesActuales)
        {
            if (opcion.label.text == emocionActual.emocionNombre)
                opcion.MarcarComoCorrecta();
        }
        TerminarRonda();
    }
 
    public void ResponderSituacion(EmojiArrastrable emojiSoltado)
    {
        Debug.Log("Entró a Responder");
        Debug.Log("Emoji: " + emojiSoltado.EmocionNombre);
        Debug.Log("Correcta: " + situacionActual.emocionCorrecta);
        if (esperandoSiguienteRonda)
        {
            emojiSoltado.VolverAPosicionOriginal();
            return;
        }

        bool esCorrecta = emojiSoltado.EmocionNombre == situacionActual.emocionCorrecta;

        if (esCorrecta)
        {
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesCorrecto);
            emojiSoltado.transform.SetParent(situacionImage.transform, true);
            if (personaje != null) personaje.SetTrigger(SonreirTrigger);

            TerminarRonda();
            return;
        }

        // Respuesta incorrecta: el emoji vuelve a la bandeja
        emojiSoltado.VolverAPosicionOriginal();
        intentosFallidos++;

        if (intentosFallidos < 2)
        {
            // Primer error: puede volver a intentar con los emojis que quedan
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesReintentar);
            return;
        }

        // Segundo error: se revela cuál era la correcta y se cierra la ronda
        if (feedbackText != null)
            feedbackText.text = string.Format(FraseAlAzar(frasesRevelar), situacionActual.emocionCorrecta);
        TerminarRonda();
    }
    void TerminarRonda()
    {
        esperandoSiguienteRonda = true;

        if (GameSession.SelectedDifficulty == Difficulty.Dificil)
        {
            foreach (var emoji in emojisActuales)
            {
                emoji.Bloquear();
            }
        }
        else
        {
            foreach (var opcion in opcionesActuales)
            {
                opcion.GetComponent<Button>().interactable = false;
            }
        }


        Invoke(nameof(NuevaRonda), delayEntreRondas);
    }
 
 
    void TerminarJuego()
    {
        if (GameSession.SelectedDifficulty == Difficulty.Dificil)
        {
            foreach (var emoji in emojisActuales)
            {
                Destroy(emoji.gameObject);
            }
            emojisActuales.Clear();
        }
        else
        {// Limpiar la última tanda de opciones y dejar la pantalla lista
            foreach (var opcion in opcionesActuales)
            {
                Destroy(opcion.gameObject);
            }
            opcionesActuales.Clear();
 
        }
        
    
        if (feedbackText != null) feedbackText.text = "";
 
    }
    string FraseAlAzar(string[] frases)
    {
        if (frases == null || frases.Length == 0) return "";
        return frases[UnityEngine.Random.Range(0, frases.Length)];
    }
    void Barajar(List<EmocionInfo> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
 
}
 


