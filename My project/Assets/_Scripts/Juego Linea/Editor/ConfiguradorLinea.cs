using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menú appTEA > Linea: deja listo el banco del minijuego.
// Se puede correr más de una vez sin duplicar nada.
public static class ConfiguradorLinea
{
    const string RutaSprites = "Assets/_Sprites/BOTONES/Sprite-0005PuntajeLupaEstrella.png";
    const string CarpetaBanco = "Assets/Resources/Linea";
    const string RutaBanco = CarpetaBanco + "/BancoLinea.asset";
    const string RutaGameData = "Assets/Resources/Juegos/Linea.asset";
    const string RutaEscena = "Assets/_Scenes/JuegoLinea.unity";
    const string CarpetaMateriales = "Assets/Prefabs/Linea";
    const string RutaMaterialConfeti = CarpetaMateriales + "/MaterialConfeti.mat";

    static readonly string[] PalabrasIniciales =
        { "SOL", "OSO", "PAN", "PATO", "LUNA", "MESA", "CASA", "MAMÁ", "PAPÁ" };

    [MenuItem("appTEA/Linea/1 - Crear banco y GameData")]
    public static void CrearBanco()
    {
        BancoLinea banco = ObtenerOCrearBanco();

        // El contenido solo se completa si está vacío, para no pisar lo que edites a mano
        if (banco.figuras.Count == 0) banco.figuras = CatalogoFigurasLinea.Crear();
        if (banco.palabras.Count == 0) banco.palabras = new List<string>(PalabrasIniciales);

        // Bancos armados antes: se completan las figuras que faltan (las vocales con tilde) sin tocar las demás
        foreach (FiguraLinea nueva in CatalogoFigurasLinea.Crear())
        {
            if (!banco.figuras.Any(f => f.categoria == nueva.categoria && f.nombre == nueva.nombre))
                banco.figuras.Add(nueva);
        }
        for (int i = 0; i < banco.palabras.Count; i++)
        {
            if (banco.palabras[i] == "MAMA") banco.palabras[i] = "MAMÁ";
            else if (banco.palabras[i] == "PAPA") banco.palabras[i] = "PAPÁ";
        }

        CargarSprites(banco);
        EditorUtility.SetDirty(banco);

        GameData datos = AssetDatabase.LoadAssetAtPath<GameData>(RutaGameData);
        if (datos == null)
        {
            datos = ScriptableObject.CreateInstance<GameData>();
            AssetDatabase.CreateAsset(datos, RutaGameData);
            datos.gameId = "linea";
            datos.titulo = "Escribir siguiendo la línea";
            datos.descripcion = "Seguí la línea con el dedo";
            datos.sceneName = "JuegoLinea";
            EditorUtility.SetDirty(datos);
        }
        if (datos.icono == null)
            Debug.LogWarning("[Linea] Falta asignarle un ícono al GameData (Resources/Juegos/Linea) para que se vea en el menú.");

        AssetDatabase.SaveAssets();

        Validar(banco);
        Debug.Log($"[Linea] Banco listo: {banco.figuras.Count} figuras, {banco.palabras.Count} palabras.");
    }

    [MenuItem("appTEA/Linea/3 - Regenerar figuras (pisa las del banco)")]
    public static void RegenerarFiguras()
    {
        BancoLinea banco = ObtenerOCrearBanco();
        banco.figuras = CatalogoFigurasLinea.Crear();
        EditorUtility.SetDirty(banco);
        AssetDatabase.SaveAssets();

        Validar(banco);
        Debug.Log($"[Linea] Figuras regeneradas: {banco.figuras.Count}.");
    }

    static BancoLinea ObtenerOCrearBanco()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(CarpetaBanco))
            AssetDatabase.CreateFolder("Assets/Resources", "Linea");

        BancoLinea banco = AssetDatabase.LoadAssetAtPath<BancoLinea>(RutaBanco);
        if (banco == null)
        {
            banco = ScriptableObject.CreateInstance<BancoLinea>();
            AssetDatabase.CreateAsset(banco, RutaBanco);
        }
        return banco;
    }

    static void CargarSprites(BancoLinea banco)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(RutaSprites).OfType<Sprite>().ToDictionary(s => s.name, s => s);

        sprites.TryGetValue("estrella", out banco.estrella);
        sprites.TryGetValue("flecha", out banco.flecha);

        if (banco.estrella == null) Debug.LogWarning("[Linea] No hay sub-sprite 'estrella' en " + RutaSprites);
        if (banco.flecha == null) Debug.LogWarning("[Linea] No hay sub-sprite 'flecha' en " + RutaSprites);
    }

    // Avisa lo que falta, sin cortar
    static void Validar(BancoLinea banco)
    {
        foreach (string palabra in banco.palabras)
        {
            List<char> faltantes = banco.LetrasFaltantes(palabra);
            if (faltantes.Count > 0)
                Debug.LogWarning($"[Linea] La palabra '{palabra}' usa letras que no están en el banco: " +
                                 string.Join(", ", faltantes));
        }

        foreach (var figura in banco.figuras)
        {
            if (figura.trazos.Count == 0 || figura.trazos.Any(t => t.puntos.Count < 2))
                Debug.LogWarning($"[Linea] La figura '{figura.nombre}' tiene trazos vacíos.");
        }

        if (banco.pictograma == null) Debug.LogWarning("[Linea] Falta el pictograma (opcional).");
        if (banco.botonRepetir == null) Debug.LogWarning("[Linea] Falta el botón Repetir (opcional).");
        if (banco.botonSiguiente == null) Debug.LogWarning("[Linea] Falta el botón Siguiente (opcional).");
        if (banco.fondo == null) Debug.LogWarning("[Linea] Falta el fondo (opcional).");
    }

    // --------------------------------------------------------------- 2) Escena

    [MenuItem("appTEA/Linea/2 - Armar escena JuegoLinea")]
    public static void ArmarEscena()
    {
        if (!System.IO.File.Exists(RutaEscena))
        {
            Debug.LogError("[Linea] No existe " + RutaEscena + ". Creá la escena (con Canvas, Feedback, Personaje, " +
                           "BotonMenu y GameManager) y volvé a correr este menú.");
            return;
        }

        BancoLinea banco = AssetDatabase.LoadAssetAtPath<BancoLinea>(RutaBanco);
        if (banco == null)
        {
            CrearBanco();
            banco = AssetDatabase.LoadAssetAtPath<BancoLinea>(RutaBanco);
        }

        EditorSceneManager.OpenScene(RutaEscena);

        GameObject objCanvas = GameObject.Find("Canvas");
        GameObject objGameManager = GameObject.Find("GameManager");
        if (objCanvas == null || objGameManager == null)
        {
            Debug.LogError("[Linea] La escena JuegoLinea necesita un 'Canvas' y un 'GameManager'.");
            return;
        }
        Transform canvas = objCanvas.transform;
        Text feedback = canvas.Find("Feedback")?.GetComponent<Text>();
        Animator personaje = canvas.Find("Personaje")?.GetComponent<Animator>();
        Button botonMenu = canvas.Find("BotonMenu")?.GetComponent<Button>();
        Font fuente = feedback != null ? feedback.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Lo que haya quedado de otra corrida
        foreach (string nombre in new[] { "PanelLinea", "Pausa" })
        {
            Transform viejo = canvas.Find(nombre);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        }
        GameObject viejoConfeti = GameObject.Find("Confeti");
        if (viejoConfeti != null) Object.DestroyImmediate(viejoConfeti);

        // El GameManager puede venir de otra escena copiada: se le saca todo lo que no sea de Linea
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(objGameManager);
        foreach (var componente in objGameManager.GetComponents<MonoBehaviour>())
        {
            if (componente is GameManagerEncajar || componente is GameManagerCompletar)
                Object.DestroyImmediate(componente);
        }

        // Panel de juego (detrás de personaje, feedback y menú, delante del fondo)
        RectTransform panel = ConfiguradorCompletar.CrearRect("PanelLinea", canvas, Vector2.zero, Vector2.zero);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        Transform fondo = canvas.Find("Fondo");
        panel.SetSiblingIndex(fondo != null ? fondo.GetSiblingIndex() + 1 : 0);

        RectTransform pictograma = ConfiguradorCompletar.CrearRect("Pictograma", panel, new Vector2(-190, 305), new Vector2(100, 100));
        Image imagenPictograma = pictograma.gameObject.AddComponent<Image>();
        imagenPictograma.preserveAspect = true;
        imagenPictograma.raycastTarget = false;
        pictograma.gameObject.SetActive(false);

        // Área de trazado: recibe el dedo; guía, trazo y marcador son hijos suyos (se crean al jugar)
        RectTransform areaRect = ConfiguradorCompletar.CrearRect("AreaTrazado", panel, new Vector2(-190, -5), new Vector2(780, 520));
        Image fondoArea = areaRect.gameObject.AddComponent<Image>();
        fondoArea.color = Color.clear;
        fondoArea.raycastTarget = true;
        AreaTrazado area = areaRect.gameObject.AddComponent<AreaTrazado>();

        RectTransform marcadorRect = ConfiguradorCompletar.CrearRect("Marcador", areaRect, Vector2.zero, Vector2.zero);
        marcadorRect.anchorMin = Vector2.zero;
        marcadorRect.anchorMax = Vector2.one;
        marcadorRect.offsetMin = marcadorRect.offsetMax = Vector2.zero;
        Image estrella = CrearIcono("Estrella", marcadorRect, banco != null ? banco.estrella : null, 100f);
        Image flecha = CrearIcono("Flecha", marcadorRect, banco != null ? banco.flecha : null, 80f);
        MarcadorTrazo marcador = marcadorRect.gameObject.AddComponent<MarcadorTrazo>();
        ConfiguradorCompletar.Asignar(marcador, "estrella", estrella);
        ConfiguradorCompletar.Asignar(marcador, "flecha", flecha);

        // Repetir y Siguiente (con texto hasta que haya sprites; el GameManager los reemplaza solo)
        Button repetir = ConfiguradorCompletar.CrearBoton("BotonRepetir", panel, "Repetir", new Vector2(-320, -315), fuente);
        Button siguiente = ConfiguradorCompletar.CrearBoton("BotonSiguiente", panel, "Siguiente", new Vector2(-60, -315), fuente);
        foreach (Button b in new[] { repetir, siguiente })
        {
            ((RectTransform)b.transform).sizeDelta = new Vector2(220, 70);
            b.gameObject.SetActive(false);
        }

        ParticleSystem confeti = CrearConfeti();

        // Pausa (igual que en Completar)
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
        caja.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        ConfiguradorCompletar.CrearTexto("Titulo", caja, "Pausa", new Vector2(0, 100), new Vector2(480, 80), 56, fuente);
        Button seguir = ConfiguradorCompletar.CrearBoton("BotonSeguir", caja, "Seguir", new Vector2(0, 0), fuente);
        Button salir = ConfiguradorCompletar.CrearBoton("BotonSalir", caja, "Salir", new Vector2(0, -100), fuente);

        PanelPausa scriptPausa = pausa.gameObject.AddComponent<PanelPausa>();
        ConfiguradorCompletar.Asignar(scriptPausa, "panel", velo.gameObject);
        ConfiguradorCompletar.Asignar(scriptPausa, "botonSeguir", seguir);
        ConfiguradorCompletar.Asignar(scriptPausa, "botonSalir", salir);

        // BotonMenu pasa de "volver al inicio" a "pausar"
        if (botonMenu != null)
        {
            for (int i = botonMenu.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(botonMenu.onClick, i);
            UnityEventTools.AddVoidPersistentListener(botonMenu.onClick, scriptPausa.Abrir);
        }

        // GameManager
        // (sin "??": en el editor un componente que falta no es null de verdad y el operador no lo detecta)
        var manager = objGameManager.GetComponent<GameManagerLinea>();
        if (manager == null) manager = objGameManager.AddComponent<GameManagerLinea>();
        // Linea no usa audio: si quedó un AudioSource de una corrida anterior, se saca
        var audioViejo = objGameManager.GetComponent<AudioSource>();
        if (audioViejo != null) Object.DestroyImmediate(audioViejo);
        manager.area = area;
        manager.marcador = marcador;
        manager.pictograma = imagenPictograma;
        manager.botonRepetir = repetir;
        manager.botonSiguiente = siguiente;
        manager.confeti = confeti;
        manager.feedbackText = feedback;
        manager.personaje = personaje;
        EditorUtility.SetDirty(manager);

        // Pausa arriba de todo, así tapa también a BotonMenu y al Personaje
        pausa.SetAsLastSibling();

        RepararCanvasRenderers(canvas);

        AgregarEscenaABuild();
        EditorSceneManager.MarkSceneDirty(objCanvas.scene);
        EditorSceneManager.SaveScene(objCanvas.scene);

        if (banco != null && (banco.estrella == null || banco.flecha == null))
            Debug.LogWarning("[Linea] El banco no tiene estrella o flecha: corré '1 - Crear banco y GameData' y volvé a armar la escena.");
        Debug.Log("[Linea] Escena armada. Revisá en la vista Game las posiciones de Feedback y Personaje.");
    }

    // Todo Graphic (Image, Text, LineaUI...) necesita un CanvasRenderer; si a alguno le falta, se agrega
    static void RepararCanvasRenderers(Transform canvas)
    {
        int reparados = 0;
        foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(true))
        {
            if (g.GetComponent<CanvasRenderer>() != null) continue;
            g.gameObject.AddComponent<CanvasRenderer>();
            reparados++;
        }
        if (reparados > 0) Debug.Log($"[Linea] Se agregó CanvasRenderer a {reparados} objeto(s) de UI que no lo tenían.");
    }

    static Image CrearIcono(string nombre, Transform padre, Sprite sprite, float lado)
    {
        RectTransform rect = ConfiguradorCompletar.CrearRect(nombre, padre, Vector2.zero, Vector2.one * lado);
        Image imagen = rect.gameObject.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        return imagen;
    }

    // Confeti lento en el mundo: cae desde arriba de la pantalla y se dibuja por encima del Canvas
    // (el Canvas es Screen Space - Camera, así que las partículas de la escena se ven con la misma cámara)
    static ParticleSystem CrearConfeti()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Linea");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialConfeti);
        if (material == null)
        {
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, RutaMaterialConfeti);
        }

        Camera camara = Camera.main;
        Vector3 posicion = camara != null
            ? camara.transform.position + camara.transform.forward * 10f + camara.transform.up * (camara.orthographicSize + 1.5f)
            : new Vector3(0f, 7f, 0f);

        var objeto = new GameObject("Confeti");
        objeto.transform.position = posicion;
        objeto.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // las partículas salen hacia abajo

        var sistema = objeto.AddComponent<ParticleSystem>();
        sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = sistema.main;
        main.duration = 4f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 9f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0f;
        main.maxParticles = 200;

        var colores = new Gradient();
        colores.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.95f, 0.35f, 0.35f), 0f),
                new GradientColorKey(new Color(0.99f, 0.80f, 0.20f), 0.25f),
                new GradientColorKey(new Color(0.30f, 0.75f, 0.40f), 0.5f),
                new GradientColorKey(new Color(0.25f, 0.55f, 0.95f), 0.75f),
                new GradientColorKey(new Color(0.70f, 0.40f, 0.85f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(colores) { mode = ParticleSystemGradientMode.RandomColor };

        var emision = sistema.emission;
        emision.rateOverTime = 40f;

        var forma = sistema.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(camara != null ? camara.orthographicSize * camara.aspect * 2f : 20f, 0.2f, 0.2f);

        var giro = sistema.rotationOverLifetime;
        giro.enabled = true;
        giro.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);   // radianes por segundo: gira despacio

        var render = sistema.GetComponent<ParticleSystemRenderer>();
        render.sharedMaterial = material;
        render.sortingOrder = 100;   // por encima del Canvas (orden 0)

        return sistema;
    }

    static void AgregarEscenaABuild()
    {
        var escenas = EditorBuildSettings.scenes.ToList();
        if (!escenas.Any(e => e.path == RutaEscena))
        {
            escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }

        AvisarSiFaltaEnElPerfilActivo();
    }

    // En Unity 6 un Build Profile puede tener su propia lista de escenas (si pisa la global).
    // No se modifica el perfil: solo se avisa.
    static void AvisarSiFaltaEnElPerfilActivo()
    {
        var perfil = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
        if (perfil == null) return;                // sin perfil activo se usa la lista global (Build Settings)
        if (!perfil.overrideGlobalScenes) return;  // el perfil usa la lista global, que ya la tiene

        if (!perfil.scenes.Any(e => e.path == RutaEscena))
            Debug.LogWarning($"[Linea] La escena JuegoLinea no está en el Build Profile activo ('{perfil.name}'), " +
                             "que usa su propia lista de escenas. Agregala en File > Build Profiles > Scene List.");
    }
}
