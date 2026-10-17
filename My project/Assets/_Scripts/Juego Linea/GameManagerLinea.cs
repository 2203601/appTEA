using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Fácil = trazos simples, Medio = letras y números, Difícil = palabras (letra por letra).
// No hay límite de tiempo: el pausa lo maneja PanelPausa, igual que en Completar.
public class GameManagerLinea : MonoBehaviour
{
    public static GameManagerLinea Instancia { get; private set; }

    [Header("Datos")]
    [SerializeField] private BancoLinea banco;
    public string rutaBanco = "Linea/BancoLinea";

    [Header("Referencias de escena")]
    public AreaTrazado area;
    public MarcadorTrazo marcador;
    public Image pictograma;
    public Button botonRepetir;
    public Button botonSiguiente;
    public ParticleSystem confeti;

    public Text feedbackText;
    public Text rondaText;
    public Animator personaje;

    [Header("Config")]
    public int rondasFacil = 4;
    public int rondasMedio = 4;
    public int rondasDificil = 3;
    public float delayEntreTrazos = 0.4f;
    [Tooltip("Tamaño de la figura como fracción del alto del área")]
    public float tamanoFigura = 0.8f;
    [Tooltip("Separación entre letras de una palabra, como fracción del lado de una letra")]
    public float separacionLetras = 0.2f;
    [Tooltip("Grosor de la línea como fracción del lado de la figura")]
    public float grosorRelativo = 0.07f;
    public float grosorMinimo = 14f;
    [Tooltip("Grados que se suman al ángulo de las flechas (el sprite apunta a la derecha: 0)")]
    public float correccionAnguloFlechas = 0f;

    [Header("Debug")]
    [Tooltip("Loguea el armado de la figura y dibuja con Gizmos (vista Scene) los puntos del trazo actual y la tolerancia")]
    public bool modoDebug;

    [Header("Colores")]
    public Color colorGuia = new Color(0.62f, 0.62f, 0.62f);
    public Color colorGuiaSuave = new Color(0.84f, 0.84f, 0.84f);
    [Tooltip("Se multiplica con el color del sprite de la flecha (que ya es verde): en blanco se ve tal cual es")]
    public Color colorFlechas = Color.white;
    public Color colorTrazo = new Color(0.15f, 0.40f, 0.85f);

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
    static readonly float[] FraccionesFlechasGuia = { 0.35f, 0.7f };

    // Una figura o palabra lista para trazar
    class Ejercicio
    {
        public string nombre;
        public bool esPalabra;
        public List<FiguraLinea> figuras = new List<FiguraLinea>();
    }

    // Una figura (o una letra de la palabra) ya ubicada en pantalla
    class LetraEnPantalla
    {
        public List<List<Vector2>> trazos = new List<List<Vector2>>();   // en coordenadas locales del área
        public List<LineaUI> guias = new List<LineaUI>();
        public List<Image> flechas = new List<Image>();
        public float lado;
        public float grosor;
    }

    DificultadLinea dificultad;
    List<Ejercicio> ejercicios = new List<Ejercicio>();
    List<int> mazo = new List<int>();

    Ejercicio ejercicioActual;
    List<LetraEnPantalla> letras = new List<LetraEnPantalla>();
    List<LineaUI> trazosNino = new List<LineaUI>();
    int letraActual;
    int trazoActual;
    SeguidorTrazo seguidor;
    LineaUI trazoNino;
    Vector2 ultimoPunto;
    bool fueraDelCamino;
    int rondaActual;
    int totalRondas;
    float toleranciaActual;

    void Awake()
    {
        Instancia = this;

        if (banco == null) banco = Resources.Load<BancoLinea>(rutaBanco);
        if (banco == null)
        {
            Debug.LogError($"No se encontró el BancoLinea en Resources/{rutaBanco}. " +
                "Corré el menú appTEA > Linea > 1 - Crear banco y GameData.");
            return;
        }

        switch (GameSession.SelectedDifficulty)
        {
            case Difficulty.Medio: dificultad = DificultadLinea.Medio; break;
            case Difficulty.Dificil: dificultad = DificultadLinea.Dificil; break;
            default: dificultad = DificultadLinea.Facil; break;
        }

        switch (dificultad)
        {
            case DificultadLinea.Medio: totalRondas = rondasMedio; break;
            case DificultadLinea.Dificil: totalRondas = rondasDificil; break;
            default: totalRondas = rondasFacil; break;
        }

        CargarEjerciciosDelNivel();

        if (area != null)
        {
            area.AlEmpezar += OnEmpezar;
            area.AlMover += OnMover;
            area.AlSoltar += OnSoltar;
            area.AlCompletar += OnCompletarTrazo;
        }
        if (botonRepetir != null) botonRepetir.onClick.AddListener(Repetir);
        if (botonSiguiente != null) botonSiguiente.onClick.AddListener(NuevaRonda);
        ConfigurarBoton(botonRepetir, banco.botonRepetir);
        ConfigurarBoton(botonSiguiente, banco.botonSiguiente);
        if (marcador != null) marcador.Configurar(banco.estrella, banco.flecha);
    }

    void OnDestroy()
    {
        if (area != null)
        {
            area.AlEmpezar -= OnEmpezar;
            area.AlMover -= OnMover;
            area.AlSoltar -= OnSoltar;
            area.AlCompletar -= OnCompletarTrazo;
        }
    }

    void Start()
    {
        if (banco == null) return;
        if (area == null || marcador == null)
        {
            Debug.LogError("Faltan referencias en GameManagerLinea (area o marcador). " +
                "Corré el menú appTEA > Linea > 2 - Armar escena JuegoLinea.");
            return;
        }
        if (ejercicios.Count == 0)
        {
            Debug.LogError("No hay ejercicios para este nivel en el BancoLinea.");
            return;
        }

        rondaActual = 0;
        NuevaRonda();
    }

    void CargarEjerciciosDelNivel()
    {
        switch (dificultad)
        {
            case DificultadLinea.Dificil:
                foreach (string palabra in banco.palabras)
                {
                    List<char> faltantes = banco.LetrasFaltantes(palabra);
                    if (faltantes.Count > 0)
                    {
                        Debug.LogWarning($"La palabra '{palabra}' usa letras que no están en el banco: " +
                            string.Join(", ", faltantes) + ". Se saltea.");
                        continue;
                    }
                    var ejercicio = new Ejercicio { nombre = palabra, esPalabra = true };
                    foreach (char c in palabra) ejercicio.figuras.Add(banco.FiguraDeLetra(c));
                    ejercicios.Add(ejercicio);
                }
                break;

            default:
                bool simples = dificultad == DificultadLinea.Facil;
                foreach (var figura in banco.figuras)
                {
                    bool esSimple = figura.categoria == CategoriaLinea.TrazoSimple;
                    if (esSimple != simples) continue;
                    var ejercicio = new Ejercicio { nombre = figura.nombre };
                    ejercicio.figuras.Add(figura);
                    ejercicios.Add(ejercicio);
                }
                break;
        }
    }

    // ------------------------------------------------------------------ Rondas

    void NuevaRonda()
    {
        if (rondaActual >= totalRondas)
        {
            TerminarJuego();
            return;
        }
        rondaActual++;
        if (rondaText != null) rondaText.text = $"Ronda {rondaActual}/{totalRondas}";

        IniciarEjercicio(SiguienteEjercicio());
    }

    void Repetir()
    {
        IniciarEjercicio(ejercicioActual);
    }

    Ejercicio SiguienteEjercicio()
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

    void IniciarEjercicio(Ejercicio ejercicio)
    {
        LimpiarEjercicio();
        ejercicioActual = ejercicio;
        if (feedbackText != null) feedbackText.text = "";
        MostrarBotones(false);

        MostrarPictograma(ejercicio);

        ArmarLetras(ejercicio);
        ActivarLetra(0);
        // METRICA: inicio de la figura o palabra (ejercicio.nombre, dificultad, repetición o no)
    }

    void MostrarPictograma(Ejercicio ejercicio)
    {
        if (pictograma == null) return;

        Sprite sprite = banco.pictograma;
        if (!ejercicio.esPalabra && ejercicio.figuras[0].pictograma != null) sprite = ejercicio.figuras[0].pictograma;

        pictograma.gameObject.SetActive(sprite != null);
        pictograma.sprite = sprite;
        pictograma.preserveAspect = true;
    }

    // Ubica las letras una al lado de la otra, centradas y ajustadas para que entren en el área
    void ArmarLetras(Ejercicio ejercicio)
    {
        Rect r = ((RectTransform)area.transform).rect;
        int n = ejercicio.figuras.Count;

        float anchoEnLados = n + separacionLetras * (n - 1);
        float lado = Mathf.Min(r.height * tamanoFigura, r.width * 0.92f / anchoEnLados);
        float grosor = Mathf.Max(grosorMinimo, lado * grosorRelativo);
        float x = r.center.x - lado * anchoEnLados / 2f + lado / 2f;

        foreach (FiguraLinea figura in ejercicio.figuras)
        {
            Vector2 centro = new Vector2(x, r.center.y);
            var letra = new LetraEnPantalla { lado = lado, grosor = grosor };

            foreach (Trazo trazo in figura.trazos)
            {
                var puntos = new List<Vector2>();
                foreach (Vector2 p in trazo.puntos) puntos.Add(centro + (p - Vector2.one * 0.5f) * lado);
                letra.trazos.Add(puntos);

                // La guía: punteada, gruesa y gris; la de las letras que todavía no tocan es más suave
                LineaUI guia = CrearLinea("Guia", colorGuiaSuave, grosor, true);
                guia.SeparacionPuntos = grosor * 2f;
                guia.SetPuntos(puntos);
                letra.guias.Add(guia);
            }

            letras.Add(letra);
            x += lado * (1f + separacionLetras);
        }

        marcador.AjustarTamanos(Mathf.Max(grosor * 3.2f, 60f), Mathf.Max(grosor * 2f, 40f));

        if (modoDebug) DiagnosticarArmado(r, lado, grosor);
    }

    // ------------------------------------------------------------------- Trazos

    // Solo la letra actual tiene guía fuerte, flechas y estrella
    void ActivarLetra(int indice)
    {
        letraActual = indice;
        trazoActual = 0;
        LetraEnPantalla letra = letras[indice];

        foreach (LineaUI guia in letra.guias) guia.color = colorGuia;
        PonerFlechasGuia(letra);
        IniciarTrazo();
    }

    void PonerFlechasGuia(LetraEnPantalla letra)
    {
        if (banco.flecha == null) return;

        foreach (List<Vector2> puntos in letra.trazos)
        {
            foreach (float fraccion in FraccionesFlechasGuia)
            {
                Vector2 posicion = PuntoEn(puntos, fraccion, out Vector2 direccion);
                Image flecha = CrearImagen("FlechaGuia", banco.flecha, Mathf.Max(letra.grosor * 1.8f, 30f), colorFlechas);
                flecha.transform.localPosition = posicion;
                flecha.transform.localRotation = RotacionFlecha(direccion);
                if (modoDebug)
                    Debug.Log($"[Linea] flecha en {posicion} dirección {direccion} -> {flecha.transform.localEulerAngles.z:F0}°");
                letra.flechas.Add(flecha);
            }
        }
        marcador.transform.SetAsLastSibling();
    }

    void IniciarTrazo()
    {
        LetraEnPantalla letra = letras[letraActual];
        List<Vector2> puntos = letra.trazos[trazoActual];

        // El seguidor trabaja en unidades del canvas, con el origen abajo a la izquierda del área
        Rect r = ((RectTransform)area.transform).rect;
        var puntosSeguidor = new List<Punto2>();
        foreach (Vector2 p in puntos) puntosSeguidor.Add(new Punto2(p.x - r.xMin, p.y - r.yMin));

        toleranciaActual = CalcularTolerancia(letra.lado);
        seguidor = new SeguidorTrazo(puntosSeguidor, toleranciaActual);
        area.Seguidor = seguidor;

        trazoNino = CrearLinea("Trazo", colorTrazo, letra.grosor, false);
        trazosNino.Add(trazoNino);
        marcador.transform.SetAsLastSibling();

        fueraDelCamino = false;
        ultimoPunto = puntos[0];
        marcador.MostrarInicio(puntos[0]);
    }

    // El MAYOR entre la fracción del tamaño de la figura en pantalla y un mínimo en píxeles
    // (así en las letras chicas de una palabra sigue siendo usable con el dedo)
    float CalcularTolerancia(float ladoFigura)
    {
        Canvas canvas = area.GetComponentInParent<Canvas>();
        float escala = canvas != null ? Mathf.Max(canvas.scaleFactor, 0.01f) : 1f;
        float minimaEnUnidades = banco.toleranciaMinimaPx / escala;   // píxeles de pantalla -> unidades del canvas
        return Mathf.Max(banco.Tolerancia(dificultad) * ladoFigura, minimaEnUnidades);
    }

    void OnEmpezar(Vector2 normalizado)
    {
        marcador.OcultarEstrella();
        trazoNino.AgregarPunto(letras[letraActual].trazos[trazoActual][0]);
        trazoNino.AgregarPunto(area.ALocal(normalizado));
        // METRICA: inicio del trazo (letraActual, trazoActual)
    }

    void OnMover(Vector2 normalizado)
    {
        if (seguidor.FueraDelCamino)
        {
            // La línea deja de crecer y la flecha muestra por dónde seguir; sin sonido ni texto
            if (!fueraDelCamino)
            {
                fueraDelCamino = true;
                // METRICA: salida del camino (letraActual, trazoActual, progreso)
            }
            marcador.MostrarFlecha(ultimoPunto, seguidor.DireccionActual);
            return;
        }

        if (fueraDelCamino)
        {
            fueraDelCamino = false;
            marcador.OcultarFlecha();
        }

        ultimoPunto = area.ALocal(normalizado);
        trazoNino.AgregarPunto(ultimoPunto);
    }

    // Soltó antes de terminar: ese trazo vuelve a empezar, sin texto de error
    void OnSoltar(Vector2 normalizado)
    {
        trazoNino.Limpiar();
        seguidor.Reiniciar();

        fueraDelCamino = false;
        ultimoPunto = letras[letraActual].trazos[trazoActual][0];
        marcador.MostrarInicio(ultimoPunto);
    }

    void OnCompletarTrazo()
    {
        area.Seguidor = null;
        marcador.Ocultar();
        fueraDelCamino = false;

        // El trazo queda prolijo, sobre el camino exacto
        trazoNino.SetPuntos(letras[letraActual].trazos[trazoActual]);
        // METRICA: fin del trazo (letraActual, trazoActual)

        StartCoroutine(AvanzarTrasTrazo());
    }

    IEnumerator AvanzarTrasTrazo()
    {
        yield return new WaitForSeconds(delayEntreTrazos);

        LetraEnPantalla letra = letras[letraActual];
        trazoActual++;

        if (trazoActual < letra.trazos.Count)
        {
            IniciarTrazo();
            yield break;
        }

        // Letra completa: las flechas ya no hacen falta; la letra queda dibujada
        foreach (Image flecha in letra.flechas) Destroy(flecha.gameObject);
        letra.flechas.Clear();

        if (letraActual + 1 < letras.Count) ActivarLetra(letraActual + 1);
        else CompletarEjercicio();
    }

    void CompletarEjercicio()
    {
        // METRICA: figura o palabra completa (ejercicio.nombre)
        if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesCorrecto);
        if (personaje != null) personaje.SetTrigger(SonreirTrigger);
        if (confeti != null)
        {
            confeti.Clear();
            confeti.Play();
        }

        MostrarBotones(true);
    }

    void TerminarJuego()
    {
        LimpiarEjercicio();
        MostrarBotones(false);
        if (pictograma != null) pictograma.gameObject.SetActive(false);
        if (rondaText != null) rondaText.text = "";
        if (feedbackText != null) feedbackText.text = FraseAlAzar(frasesFin);
        if (personaje != null) personaje.SetTrigger(SonreirTrigger);
        if (confeti != null)
        {
            confeti.Clear();
            confeti.Play();
        }
    }

    // ----------------------------------------------------------------- Soporte

    void LimpiarEjercicio()
    {
        StopAllCoroutines();
        if (area != null) area.Seguidor = null;
        if (marcador != null) marcador.Ocultar();
        if (confeti != null)
        {
            confeti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        foreach (LetraEnPantalla letra in letras)
        {
            foreach (LineaUI guia in letra.guias) Destroy(guia.gameObject);
            foreach (Image flecha in letra.flechas) Destroy(flecha.gameObject);
        }
        letras.Clear();

        foreach (LineaUI trazo in trazosNino) Destroy(trazo.gameObject);
        trazosNino.Clear();

        seguidor = null;
        trazoNino = null;
        fueraDelCamino = false;
    }

    void MostrarBotones(bool visible)
    {
        if (botonRepetir != null) botonRepetir.gameObject.SetActive(visible);
        if (botonSiguiente != null) botonSiguiente.gameObject.SetActive(visible);
    }

    // Si ya hay sprite para el botón, reemplaza al texto provisional
    void ConfigurarBoton(Button boton, Sprite sprite)
    {
        if (boton == null || sprite == null) return;

        Image imagen = boton.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = Color.white;
        imagen.preserveAspect = true;

        Text texto = boton.GetComponentInChildren<Text>(true);
        if (texto != null) texto.gameObject.SetActive(false);
    }

    // Hijo del área que ocupa todo su rectángulo, para que sus coordenadas locales sean las del área
    LineaUI CrearLinea(string nombre, Color color, float grosor, bool punteada)
    {
        RectTransform areaRect = (RectTransform)area.transform;

        // El CanvasRenderer va desde el principio: un Graphic sin él no puede dibujar
        var objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer));
        objeto.layer = area.gameObject.layer;
        var rect = (RectTransform)objeto.transform;
        rect.SetParent(areaRect, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = areaRect.pivot;

        LineaUI linea = objeto.AddComponent<LineaUI>();
        linea.raycastTarget = false;
        linea.color = color;
        linea.Grosor = grosor;
        linea.Punteada = punteada;
        return linea;
    }

    Image CrearImagen(string nombre, Sprite sprite, float lado, Color color)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer));
        objeto.layer = area.gameObject.layer;
        var rect = (RectTransform)objeto.transform;
        rect.SetParent(area.transform, false);
        rect.sizeDelta = Vector2.one * lado;

        Image imagen = objeto.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        imagen.color = color;
        return imagen;
    }

    // El sprite apunta a la derecha (0°): el ángulo es Atan2(y, x) de la dirección del recorrido
    Quaternion RotacionFlecha(Vector2 direccion)
    {
        float grados = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg + correccionAnguloFlechas;
        return Quaternion.Euler(0f, 0f, grados);
    }

    // Loguea lo que hace falta para saber por qué algo no se ve
    void DiagnosticarArmado(Rect rectArea, float lado, float grosor)
    {
        Canvas canvas = area.GetComponentInParent<Canvas>();
        Canvas.ForceUpdateCanvases();   // arma las mallas ahora para poder contar los vértices
        Debug.Log($"[Linea] área rect={rectArea} pivot={((RectTransform)area.transform).pivot} " +
                  $"lado={lado:F1} grosor={grosor:F1} escalaCanvas={(canvas != null ? canvas.scaleFactor : 0f):F2} " +
                  $"letras={letras.Count}");

        foreach (LetraEnPantalla letra in letras)
        {
            foreach (LineaUI guia in letra.guias)
            {
                RectTransform rt = (RectTransform)guia.transform;
                Debug.Log($"[Linea] guía '{guia.name}': vértices={guia.VerticesGenerados} puntos={guia.CantidadPuntos} " +
                          $"color={guia.color} rect={rt.rect} activa={guia.gameObject.activeInHierarchy} " +
                          $"sibling={rt.GetSiblingIndex()}/{area.transform.childCount}");
            }
        }
    }

    // Vista Scene: los puntos del trazo actual y el radio de tolerancia
    void OnDrawGizmos()
    {
        if (!modoDebug || !Application.isPlaying || area == null) return;
        if (letraActual < 0 || letraActual >= letras.Count) return;

        LetraEnPantalla letra = letras[letraActual];
        if (trazoActual < 0 || trazoActual >= letra.trazos.Count) return;

        Transform raiz = area.transform;
        float escala = raiz.lossyScale.x;   // unidades del mundo por unidad del canvas
        List<Vector2> puntos = letra.trazos[trazoActual];

        Gizmos.color = Color.red;
        for (int i = 0; i < puntos.Count; i++)
        {
            Vector3 p = raiz.TransformPoint(puntos[i]);
            Gizmos.DrawSphere(p, 2f * escala);
            if (i > 0) Gizmos.DrawLine(raiz.TransformPoint(puntos[i - 1]), p);
        }

        // Radio de tolerancia en el inicio y en la última posición válida del dedo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(raiz.TransformPoint(puntos[0]), toleranciaActual * escala);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(raiz.TransformPoint(ultimoPunto), toleranciaActual * escala);
    }

    // Punto de la polilínea a cierta fracción de su largo, y hacia dónde sigue
    static Vector2 PuntoEn(List<Vector2> puntos, float fraccion, out Vector2 direccion)
    {
        float total = 0f;
        for (int i = 1; i < puntos.Count; i++) total += Vector2.Distance(puntos[i - 1], puntos[i]);

        float objetivo = total * fraccion;
        for (int i = 1; i < puntos.Count; i++)
        {
            float segmento = Vector2.Distance(puntos[i - 1], puntos[i]);
            if (objetivo <= segmento || i == puntos.Count - 1)
            {
                direccion = segmento > 0f ? (puntos[i] - puntos[i - 1]) / segmento : Vector2.right;
                return puntos[i - 1] + direccion * Mathf.Min(objetivo, segmento);
            }
            objetivo -= segmento;
        }

        direccion = Vector2.right;
        return puntos[0];
    }

    string FraseAlAzar(string[] frases)
    {
        if (frases == null || frases.Length == 0) return "";
        return frases[Random.Range(0, frases.Length)];
    }

    void Barajar(List<int> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
