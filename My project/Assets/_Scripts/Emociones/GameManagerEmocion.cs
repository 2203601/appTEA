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

    [Header("Config")]
    public int cantidadOpciones = 4;
    public float delayEntreRondas = 1.2f;

    EmocionInfo emocionActual;
    List<OpcionEmocion> opcionesActuales = new List<OpcionEmocion>();
    int puntaje;
    bool esperandoSiguienteRonda;

    void Awake()
    {
        CargarEmociones();
    }

    void Start()
    {
        NuevaRonda();
    }

    void CargarEmociones()
    {
        todasLasEmociones = new List<EmocionInfo>(Resources.LoadAll<EmocionInfo>(carpetaEmociones));

        if (todasLasEmociones.Count < cantidadOpciones)
        {
            Debug.LogWarning($"Solo se encontraron {todasLasEmociones.Count} EmocionInfo en " +
                $"Assets/Resources/{carpetaEmociones}. Necesitás al menos {cantidadOpciones} para armar una ronda.");
        }
    }

    void NuevaRonda()
    {
        esperandoSiguienteRonda = false;
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
        for (int i = 0; i < cantidadOpciones - 1 && i < pool.Count; i++)
        {
            seleccionadas.Add(pool[i]);
        }
        Barajar(seleccionadas);

        // 3) Instanciar un botón por opción dentro del contenedor con Grid Layout Group.
        // El grid se acomoda solo (columnas/filas, spacing, tamaño de celda) según
        // cómo lo configures en el Inspector del contenedor, así que esto sirve
        // igual si mañana cambiás cantidadOpciones a 6, 8, etc.
        foreach (var info in seleccionadas)
        {
            OpcionEmocion nueva = Instantiate(opcionPrefab, contenedorOpciones);
            nueva.Configurar(info, this);
            opcionesActuales.Add(nueva);
        }
    }

    public void Responder(string nombreSeleccionado, OpcionEmocion opcionElegida)
    {
        if (esperandoSiguienteRonda) return;
        esperandoSiguienteRonda = true;

        foreach (var opcion in opcionesActuales)
        {
            opcion.GetComponent<Button>().interactable = false;
        }

        bool esCorrecta = nombreSeleccionado == emocionActual.emocionNombre;

        if (esCorrecta)
        {
            opcionElegida.MarcarComoCorrecta();
            if (feedbackText != null) feedbackText.text = "¡Correcto!";
        }
        else
        {
            opcionElegida.MarcarComoIncorrecta();
            if (feedbackText != null) feedbackText.text = "No, era " + emocionActual.emocionNombre;

            // marcar cuál era la correcta para que el chico la vea
            foreach (var opcion in opcionesActuales)
            {
                if (opcion.label.text == emocionActual.emocionNombre)
                    opcion.MarcarComoCorrecta();
            }
        }

        Invoke(nameof(NuevaRonda), delayEntreRondas);
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