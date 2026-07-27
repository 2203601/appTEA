
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
public class GameManagerEmocion : MonoBehaviour
{
    [Header("Datos")]
    [Tooltip("Carpeta dentro de Assets/Resources/ de donde se cargan los EmocionInfo")]
    public string carpetaEmociones = "Emociones";
 
    // Se llena solo en Awake() leyendo Assets/Resources/<carpetaEmociones>.
    // No hace falta arrastrar nada a mano en el Inspector.
    List<EmocionInfo> todasLasEmociones;
 
    [Header("Referencias de escena")]
    public Emocion tarjetaObjetivo;         // el emoji grande a adivinar
    public Transform contenedorOpciones;    // objeto vacío con Grid Layout Group
    public OpcionEmocion opcionPrefab;      // prefab del botón de respuesta
    public Text feedbackText;               // texto tipo "¡Correcto!" / "Intentá de nuevo"
    public Text rondaText;                  // opcional: "Ronda 1/4"
 
 
    [Header("Config")]
 
    public int totalRondas = 4;
    public float delayEntreRondas = 1.2f;
 
    int opcionesObjetivo;
 
    // cantidad de opciones según la dificultad elegida en el menú (GameSession.SelectedDifficulty)
    private static readonly Dictionary<Difficulty, int> opcionesPorDificultad = new Dictionary<Difficulty, int>
    {
        { Difficulty.Facil, 3 },
        { Difficulty.Medio, 6 },
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
 
 
    EmocionInfo emocionActual;
    List<OpcionEmocion> opcionesActuales = new List<OpcionEmocion>();
    int rondaActual;
    int intentosFallidos;
    bool esperandoSiguienteRonda;
 
    void Awake()
    {
        opcionesObjetivo = opcionesPorDificultad.TryGetValue(GameSession.SelectedDifficulty, out int opciones)
            ? opciones
            : 4; // valor de resguardo por si no hay selección
 
        CargarEmociones();
    }
 
    void Start()
    {
        rondaActual = 0;
        NuevaRonda();
    }
 
    void CargarEmociones()
    {
        todasLasEmociones = new List<EmocionInfo>(Resources.LoadAll<EmocionInfo>(carpetaEmociones));
 
        if (todasLasEmociones.Count < opcionesObjetivo)
        {
            Debug.LogWarning($"Solo se encontraron {todasLasEmociones.Count} EmocionInfo en " +
                $"Assets/Resources/{carpetaEmociones}. Necesitás al menos {opcionesObjetivo} para armar una ronda.");
        }
    }
 
    void NuevaRonda()
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
 
    public void Responder(string nombreSeleccionado, OpcionEmocion opcionElegida)
    {
        // Ya se cerró la ronda (acertó o gastó los 2 intentos): ignorar más clicks
        if (esperandoSiguienteRonda) return;
 
        bool esCorrecta = nombreSeleccionado == emocionActual.emocionNombre;
 
        if (esCorrecta)
        {
            opcionElegida.MarcarComoCorrecta();
            if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesCorrecto);
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
 
    void TerminarRonda()
    {
        esperandoSiguienteRonda = true;
        foreach (var opcion in opcionesActuales)
        {
            opcion.GetComponent<Button>().interactable = false;
        }
        Invoke(nameof(NuevaRonda), delayEntreRondas);
    }
 
 
    void TerminarJuego()
    {
        // Limpiar la última tanda de opciones y dejar la pantalla lista
        foreach (var opcion in opcionesActuales)
        {
            Destroy(opcion.gameObject);
        }
        opcionesActuales.Clear();
 
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
 


