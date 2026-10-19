using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menú appTEA > Pausa: BotonMenu de cada juego abre el panel de pausa (Seguir / Salir),
// igual que en Completar y Linea. Se puede correr más de una vez sin duplicar nada.
public static class ConfiguradorPausa
{
    static readonly string[] Escenas =
    {
        "Assets/_Scenes/JuegoCompletar.unity",
        "Assets/_Scenes/JuegoLinea.unity",
        "Assets/_Scenes/JuegoEmociones.unity",
        "Assets/_Scenes/JuegoEncajarPiezas.unity",
        "Assets/_Scenes/JuegoMemoria.unity",
    };

    internal const string RutaFuente = "Assets/Font/pixelify-sans-2/static/pixelify-sans-latin-700-normal.ttf";

    [MenuItem("appTEA/Pausa/Agregar a todos los juegos")]
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
        Debug.Log("[Pausa] Listo: BotonMenu abre la pausa en todos los juegos.");
    }

    static bool AgregarEnEscena()
    {
        GameObject objCanvas = GameObject.Find("Canvas");
        if (objCanvas == null)
        {
            Debug.LogError("[Pausa] La escena no tiene un 'Canvas'.");
            return false;
        }
        Transform canvas = objCanvas.transform;

        Button botonMenu = null;
        foreach (Button b in canvas.GetComponentsInChildren<Button>(true))
            if (b.name == "BotonMenu") { botonMenu = b; break; }
        if (botonMenu == null)
        {
            Debug.LogError("[Pausa] No encontré 'BotonMenu' en " + objCanvas.scene.name);
            return false;
        }

        Font fuente = AssetDatabase.LoadAssetAtPath<Font>(RutaFuente);
        if (fuente == null)
        {
            Debug.LogWarning("[Pausa] No encontré " + RutaFuente + ", uso la fuente por defecto.");
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        Transform viejo = canvas.Find("Pausa");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        RectTransform pausa = ConfiguradorCompletar.CrearRect("Pausa", canvas, Vector2.zero, Vector2.zero);
        pausa.anchorMin = Vector2.zero;
        pausa.anchorMax = Vector2.one;
        pausa.offsetMin = pausa.offsetMax = Vector2.zero;

        RectTransform velo = ConfiguradorCompletar.CrearRect("Panel", pausa, Vector2.zero, Vector2.zero);
        velo.anchorMin = Vector2.zero;
        velo.anchorMax = Vector2.one;
        velo.offsetMin = velo.offsetMax = Vector2.zero;
        velo.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform caja = ConfiguradorCompletar.CrearRect("Caja", velo, Vector2.zero, new Vector2(520, 320));
        caja.gameObject.AddComponent<Image>().color = Color.white;
        ConfiguradorCompletar.CrearTexto("Titulo", caja, "Pausa", new Vector2(0, 100), new Vector2(480, 80), 56, fuente);
        Button seguir = ConfiguradorCompletar.CrearBoton("BotonSeguir", caja, "Seguir", new Vector2(0, 0), fuente);
        Button salir = ConfiguradorCompletar.CrearBoton("BotonSalir", caja, "Salir", new Vector2(0, -100), fuente);

        PanelPausa scriptPausa = pausa.gameObject.AddComponent<PanelPausa>();
        ConfiguradorCompletar.Asignar(scriptPausa, "panel", velo.gameObject);
        ConfiguradorCompletar.Asignar(scriptPausa, "botonSeguir", seguir);
        ConfiguradorCompletar.Asignar(scriptPausa, "botonSalir", salir);

        // BotonMenu pasa de "volver al inicio" a "pausar"
        for (int i = botonMenu.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(botonMenu.onClick, i);
        UnityEventTools.AddVoidPersistentListener(botonMenu.onClick, scriptPausa.Abrir);

        // Arriba de todo, así tapa también a BotonMenu y al Personaje
        pausa.SetAsLastSibling();
        return true;
    }
}
