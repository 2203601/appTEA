using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menú appTEA > Completar: deja listo el minijuego sin cablear nada a mano.
// Se puede correr más de una vez sin duplicar nada.
public static class ConfiguradorCompletar
{
    const string RutaSprites = "Assets/_Sprites/COMPLETAR/";
    const string CarpetaBanco = "Assets/Resources/Completar";
    const string RutaBanco = CarpetaBanco + "/BancoCompletar.asset";
    const string RutaGameData = "Assets/Resources/Juegos/Completar.asset";
    const string RutaEscena = "Assets/_Scenes/JuegoCompletar.unity";
    const string RutaPrefabCasillero = "Assets/Prefabs/Completar/CasilleroLetra.prefab";
    const string RutaPrefabBoton = "Assets/Prefabs/Completar/BotonOpcion.prefab";

    // ---------------------------------------------------------------- 1) Datos

    [MenuItem("appTEA/Completar/1 - Crear banco y GameData")]
    public static void CrearBancoYGameData()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaBanco))
            AssetDatabase.CreateFolder("Assets/Resources", "Completar");

        BancoCompletar banco = AssetDatabase.LoadAssetAtPath<BancoCompletar>(RutaBanco);
        if (banco == null)
        {
            banco = ScriptableObject.CreateInstance<BancoCompletar>();
            AssetDatabase.CreateAsset(banco, RutaBanco);
        }

        // Los sprites se releen siempre de los sheets (cada sub-sprite se llama como su carácter)
        banco.numeros = LeerSimbolos("numeros.png");
        banco.letras = LeerSimbolos("letras.png");

        // El contenido solo se completa si está vacío, para no pisar lo que edites a mano
        if (banco.secuenciasNumeros.Count == 0)
            banco.secuenciasNumeros = new List<string>
                { "01234", "12345", "23456", "34567", "45678", "56789" };

        var imagenes = Sprites("imagenes.png").ToDictionary(s => s.name, s => s);
        if (banco.palabrasConImagen.Count == 0)
        {
            foreach (string nombre in new[] { "abrazo", "cerdo", "conejo", "dona", "dormir", "empanada",
                                              "leer", "oveja", "pato", "pizza", "tacos" })
            {
                imagenes.TryGetValue(nombre, out Sprite imagen);
                if (imagen == null) Debug.LogWarning($"[Completar] No hay sub-sprite '{nombre}' en imagenes.png");
                banco.palabrasConImagen.Add(new PalabraConImagen { palabra = nombre.ToUpperInvariant(), imagen = imagen });
            }
        }
        else
        {
            foreach (var p in banco.palabrasConImagen)
                if (imagenes.TryGetValue(p.palabra.ToLowerInvariant(), out Sprite imagen)) p.imagen = imagen;
        }

        if (banco.palabrasLargas.Count == 0)
            banco.palabrasLargas = new List<string>
                { "PELOTA", "ZAPATO", "TOMATE", "TORTUGA", "MARIPOSA", "ELEFANTE" };

        EditorUtility.SetDirty(banco);

        GameData datos = AssetDatabase.LoadAssetAtPath<GameData>(RutaGameData);
        if (datos == null)
        {
            datos = ScriptableObject.CreateInstance<GameData>();
            AssetDatabase.CreateAsset(datos, RutaGameData);
            datos.gameId = "completar";
            datos.titulo = "Completar";
            datos.descripcion = "Completa las palabras y los números";
            datos.sceneName = "JuegoCompletar";
            EditorUtility.SetDirty(datos);
        }

        AssetDatabase.SaveAssets();
        if (Resources.Load<TextAsset>("Completar/palabras") == null)
            Debug.LogWarning("[Completar] Falta el diccionario " + CarpetaBanco + "/palabras.txt " +
                             "(una palabra por línea). Sin él los distractores no se validan.");
        Debug.Log("[Completar] Banco y GameData listos. Falta asignarle un ícono al GameData " +
                  "(Resources/Juegos/Completar) para que se vea en el menú.");
    }

    static IEnumerable<Sprite> Sprites(string archivo)
    {
        return AssetDatabase.LoadAllAssetsAtPath(RutaSprites + archivo).OfType<Sprite>();
    }

    static List<SimboloSprite> LeerSimbolos(string archivo)
    {
        return Sprites(archivo)
            .Where(s => s.name.Length == 1)
            .Select(s => new SimboloSprite { caracter = char.ToUpperInvariant(s.name[0]), sprite = s })
            .OrderBy(s => s.caracter)
            .ToList();
    }

    // --------------------------------------------------------------- 2) Escena

    [MenuItem("appTEA/Completar/2 - Armar escena JuegoCompletar")]
    public static void ArmarEscena()
    {
        CasilleroCompletar casillero = PrepararPrefab<CasilleroCompletar>(RutaPrefabCasillero);
        BotonOpcionCompletar botonOpcion = PrepararPrefab<BotonOpcionCompletar>(RutaPrefabBoton);
        if (casillero == null || botonOpcion == null) return;

        EditorSceneManager.OpenScene(RutaEscena);

        GameObject objCanvas = GameObject.Find("Canvas");
        GameObject objGameManager = GameObject.Find("GameManager");
        if (objCanvas == null || objGameManager == null)
        {
            Debug.LogError("[Completar] La escena JuegoCompletar necesita un 'Canvas' y un 'GameManager'.");
            return;
        }
        Transform canvas = objCanvas.transform;
        Text feedback = canvas.Find("Feedback")?.GetComponent<Text>();
        Animator personaje = canvas.Find("Personaje")?.GetComponent<Animator>();
        Button botonMenu = canvas.Find("BotonMenu")?.GetComponent<Button>();
        Font fuente = feedback != null ? feedback.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Lo que quedó de Encajar y lo que armó una corrida anterior
        foreach (string nombre in new[] { "NivelUnoDos", "NivelTres", "PanelCompletar", "Pausa" })
        {
            Transform viejo = canvas.Find(nombre);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        }
        var managerEncajar = objGameManager.GetComponent<GameManagerEncajar>();
        if (managerEncajar != null) Object.DestroyImmediate(managerEncajar);

        // Panel de juego: pista, palabra y opciones
        RectTransform panel = CrearRect("PanelCompletar", canvas, Vector2.zero, Vector2.zero);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.SetAsFirstSibling();

        RectTransform pista = CrearRect("Pista", panel, new Vector2(-200, 190), new Vector2(200, 200));
        Image imagenPista = pista.gameObject.AddComponent<Image>();
        imagenPista.preserveAspect = true;
        imagenPista.raycastTarget = false;

        RectTransform palabra = CrearFila("Palabra", panel, new Vector2(-200, 0), new Vector2(850, 150), 12f);
        RectTransform opciones = CrearFila("Opciones", panel, new Vector2(-200, -190), new Vector2(850, 150), 24f);

        // Pausa
        RectTransform pausa = CrearRect("Pausa", canvas, Vector2.zero, Vector2.zero);
        pausa.anchorMin = Vector2.zero;
        pausa.anchorMax = Vector2.one;
        pausa.offsetMin = pausa.offsetMax = Vector2.zero;

        RectTransform velo = CrearRect("Panel", pausa, Vector2.zero, Vector2.zero);
        velo.anchorMin = Vector2.zero;
        velo.anchorMax = Vector2.one;
        velo.offsetMin = velo.offsetMax = Vector2.zero;
        velo.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform caja = CrearRect("Caja", velo, Vector2.zero, new Vector2(520, 320));
        caja.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        CrearTexto("Titulo", caja, "Pausa", new Vector2(0, 100), new Vector2(480, 80), 56, fuente);
        Button seguir = CrearBoton("BotonSeguir", caja, "Seguir", new Vector2(0, 0), fuente);
        Button salir = CrearBoton("BotonSalir", caja, "Salir", new Vector2(0, -100), fuente);

        PanelPausa scriptPausa = pausa.gameObject.AddComponent<PanelPausa>();
        Asignar(scriptPausa, "panel", velo.gameObject);
        Asignar(scriptPausa, "botonSeguir", seguir);
        Asignar(scriptPausa, "botonSalir", salir);

        // BotonMenu pasa de "volver al inicio" a "pausar"
        if (botonMenu != null)
        {
            for (int i = botonMenu.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(botonMenu.onClick, i);
            UnityEventTools.AddVoidPersistentListener(botonMenu.onClick, scriptPausa.Abrir);
        }

        // GameManager
        var manager = objGameManager.GetComponent<GameManagerCompletar>()
                      ?? objGameManager.AddComponent<GameManagerCompletar>();
        manager.imagenPista = imagenPista;
        manager.contenedorPalabra = palabra;
        manager.contenedorOpciones = opciones;
        manager.casilleroPrefab = casillero;
        manager.botonPrefab = botonOpcion;
        manager.feedbackText = feedback;
        manager.personaje = personaje;
        EditorUtility.SetDirty(manager);

        // Pausa arriba de todo, así tapa también a BotonMenu y al Personaje
        pausa.SetAsLastSibling();

        AgregarEscenaABuild();
        EditorSceneManager.MarkSceneDirty(objCanvas.scene);
        EditorSceneManager.SaveScene(objCanvas.scene);
        Debug.Log("[Completar] Escena armada. Revisá posiciones de Feedback y Personaje en la vista Game.");
    }

    static T PrepararPrefab<T>(string ruta) where T : Component
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
        if (raiz == null)
        {
            Debug.LogError("[Completar] No se encontró el prefab " + ruta);
            return null;
        }
        // Saca los componentes viejos (CasilleroLetra / BotonOpcion) que ya no existen
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        if (raiz.GetComponent<T>() == null) raiz.AddComponent<T>();
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        PrefabUtility.UnloadPrefabContents(raiz);
        return AssetDatabase.LoadAssetAtPath<GameObject>(ruta).GetComponent<T>();
    }

    internal static RectTransform CrearRect(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        var obj = new GameObject(nombre, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)obj.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        return rect;
    }

    static RectTransform CrearFila(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, float separacion)
    {
        RectTransform fila = CrearRect(nombre, padre, posicion, tamano);
        var layout = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = separacion;
        return fila;
    }

    internal static Text CrearTexto(string nombre, Transform padre, string contenido, Vector2 posicion, Vector2 tamano,
        int tamanoFuente, Font fuente)
    {
        RectTransform rect = CrearRect(nombre, padre, posicion, tamano);
        var texto = rect.gameObject.AddComponent<Text>();
        texto.text = contenido;
        texto.font = fuente;
        texto.fontSize = tamanoFuente;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.black;
        texto.raycastTarget = false;
        return texto;
    }

    internal static Button CrearBoton(string nombre, Transform padre, string contenido, Vector2 posicion, Font fuente)
    {
        RectTransform rect = CrearRect(nombre, padre, posicion, new Vector2(300, 80));
        rect.gameObject.AddComponent<Image>().color = new Color(0.8f, 0.9f, 1f, 1f);
        var boton = rect.gameObject.AddComponent<Button>();
        CrearTexto("Texto", rect, contenido, Vector2.zero, new Vector2(280, 70), 44, fuente);
        return boton;
    }

    internal static void Asignar(Object objetivo, string campo, Object valor)
    {
        var so = new SerializedObject(objetivo);
        so.FindProperty(campo).objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AgregarEscenaABuild()
    {
        var escenas = EditorBuildSettings.scenes.ToList();
        if (escenas.Any(e => e.path == RutaEscena)) return;
        escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));
        EditorBuildSettings.scenes = escenas.ToArray();
    }
}
