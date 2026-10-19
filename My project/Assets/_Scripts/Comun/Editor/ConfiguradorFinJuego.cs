using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menú appTEA > Fin de juego: arma el cartel "¡Muy bien!" con el botón de volver al menú
// en cada juego y lo conecta al GameManager. Se puede correr más de una vez sin duplicar nada.
public static class ConfiguradorFinJuego
{
    static readonly string[] Escenas =
    {
        "Assets/_Scenes/JuegoCompletar.unity",
        "Assets/_Scenes/JuegoLinea.unity",
        "Assets/_Scenes/JuegoEmociones.unity",
        "Assets/_Scenes/JuegoEncajarPiezas.unity",
        "Assets/_Scenes/JuegoMemoria.unity",
    };

    [MenuItem("appTEA/Fin de juego/Agregar a todos los juegos")]
    public static void AgregarATodos()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (string ruta in Escenas)
        {
            var escena = EditorSceneManager.OpenScene(ruta);
            if (AgregarEnEscena())
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
        }
        Debug.Log("[FinJuego] Listo: cartel de fin de juego en todos los juegos.");
    }

    static bool AgregarEnEscena()
    {
        GameObject objCanvas = GameObject.Find("Canvas");
        GameObject objManager = GameObject.Find("GameManager");
        if (objCanvas == null || objManager == null)
        {
            Debug.LogError("[FinJuego] La escena necesita un 'Canvas' y un 'GameManager'.");
            return false;
        }
        Transform canvas = objCanvas.transform;

        Font fuente = AssetDatabase.LoadAssetAtPath<Font>(ConfiguradorPausa.RutaFuente);
        if (fuente == null)
        {
            Debug.LogWarning("[FinJuego] No encontré " + ConfiguradorPausa.RutaFuente + ", uso la fuente por defecto.");
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        Transform viejo = canvas.Find("PanelFin");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        RectTransform raiz = ConfiguradorCompletar.CrearRect("PanelFin", canvas, Vector2.zero, Vector2.zero);
        raiz.anchorMin = Vector2.zero;
        raiz.anchorMax = Vector2.one;
        raiz.offsetMin = raiz.offsetMax = Vector2.zero;

        RectTransform velo = ConfiguradorCompletar.CrearRect("Panel", raiz, Vector2.zero, Vector2.zero);
        velo.anchorMin = Vector2.zero;
        velo.anchorMax = Vector2.one;
        velo.offsetMin = velo.offsetMax = Vector2.zero;
        velo.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform caja = ConfiguradorCompletar.CrearRect("Caja", velo, Vector2.zero, new Vector2(640, 380));
        caja.gameObject.AddComponent<Image>().color = Color.white;
        ConfiguradorCompletar.CrearTexto("Titulo", caja, "¡Muy bien!", new Vector2(0, 110), new Vector2(600, 90), 64, fuente);
        ConfiguradorCompletar.CrearTexto("Mensaje", caja, "Terminaste el juego", new Vector2(0, 30), new Vector2(600, 70), 40, fuente);
        Button volver = ConfiguradorCompletar.CrearBoton("BotonVolver", caja, "Volver al menú", new Vector2(0, -100), fuente);

        PanelFinJuego script = raiz.gameObject.AddComponent<PanelFinJuego>();
        ConfiguradorCompletar.Asignar(script, "panel", velo.gameObject);
        ConfiguradorCompletar.Asignar(script, "botonMenu", volver);

        // Conecta el cartel al manager del juego (el que tenga el campo panelFin)
        bool conectado = false;
        foreach (MonoBehaviour mb in objManager.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            var so = new SerializedObject(mb);
            SerializedProperty prop = so.FindProperty("panelFin");
            if (prop == null) continue;
            prop.objectReferenceValue = script;
            so.ApplyModifiedProperties();
            conectado = true;
        }
        if (!conectado)
            Debug.LogError("[FinJuego] El GameManager de " + objCanvas.scene.name + " no tiene el campo 'panelFin'.");

        // Arriba de todo, pero por debajo de la pausa
        raiz.SetAsLastSibling();
        Transform pausa = canvas.Find("Pausa");
        if (pausa != null) pausa.SetAsLastSibling();
        return conectado;
    }
}
