using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Menú appTEA > Linea > Ver figuras: dibuja las figuras del banco para revisar el orden y el sentido de cada trazo.
// Cada trazo con su color, un círculo numerado en el inicio y flechas que muestran la dirección.
public class VentanaFigurasLinea : EditorWindow
{
    static readonly Color[] Colores =
    {
        new Color(0.85f, 0.20f, 0.20f), new Color(0.15f, 0.40f, 0.85f), new Color(0.15f, 0.65f, 0.30f),
        new Color(0.90f, 0.55f, 0.10f), new Color(0.60f, 0.25f, 0.75f), new Color(0.10f, 0.65f, 0.65f)
    };
    static readonly float[] PosicionesFlecha = { 0.25f, 0.5f, 0.75f, 1f };

    const float Margen = 24f;
    const float RadioInicio = 11f;
    const float TamanoFlecha = 9f;

    [SerializeField] BancoLinea banco;
    int modo;                 // 0 = figura, 1 = palabra
    int indiceFigura;
    int indicePalabra;
    GUIStyle estiloNumero;

    [MenuItem("appTEA/Linea/Ver figuras")]
    public static void Abrir()
    {
        GetWindow<VentanaFigurasLinea>("Figuras Línea").minSize = new Vector2(320, 360);
    }

    void OnEnable()
    {
        if (banco == null) banco = Resources.Load<BancoLinea>("Linea/BancoLinea");
    }

    void OnGUI()
    {
        banco = (BancoLinea)EditorGUILayout.ObjectField("Banco", banco, typeof(BancoLinea), false);
        if (banco == null)
        {
            EditorGUILayout.HelpBox("No hay banco. Corré appTEA > Linea > 1 - Crear banco.", MessageType.Info);
            return;
        }

        modo = GUILayout.Toolbar(modo, new[] { "Figura", "Palabra" });

        if (modo == 0) SelectorFigura();
        else SelectorPalabra();

        Rect area = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint) return;

        EditorGUI.DrawRect(area, new Color(0.95f, 0.95f, 0.95f));
        if (modo == 0) DibujarFiguraSeleccionada(area);
        else DibujarPalabra(area);
    }

    void DibujarFiguraSeleccionada(Rect area)
    {
        if (indiceFigura >= banco.figuras.Count) return;
        float lado = Mathf.Min(area.width, area.height) - 2f * Margen;
        Rect caja = new Rect(area.center.x - lado / 2f, area.center.y - lado / 2f, lado, lado);
        Dibujar(banco.figuras[indiceFigura], caja);
    }

    void SelectorFigura()
    {
        if (banco.figuras.Count == 0) return;
        indiceFigura = Mathf.Clamp(indiceFigura, 0, banco.figuras.Count - 1);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Width(28))) indiceFigura = (indiceFigura - 1 + banco.figuras.Count) % banco.figuras.Count;
        indiceFigura = EditorGUILayout.Popup(indiceFigura,
            banco.figuras.Select(f => f.categoria + " / " + f.nombre).ToArray());
        if (GUILayout.Button(">", GUILayout.Width(28))) indiceFigura = (indiceFigura + 1) % banco.figuras.Count;
        EditorGUILayout.EndHorizontal();
    }

    void SelectorPalabra()
    {
        if (banco.palabras.Count == 0) return;
        indicePalabra = Mathf.Clamp(indicePalabra, 0, banco.palabras.Count - 1);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Width(28))) indicePalabra = (indicePalabra - 1 + banco.palabras.Count) % banco.palabras.Count;
        indicePalabra = EditorGUILayout.Popup(indicePalabra, banco.palabras.ToArray());
        if (GUILayout.Button(">", GUILayout.Width(28))) indicePalabra = (indicePalabra + 1) % banco.palabras.Count;
        EditorGUILayout.EndHorizontal();
    }

    // Las letras una al lado de la otra, cada una en su cuadrado
    void DibujarPalabra(Rect area)
    {
        if (indicePalabra >= banco.palabras.Count) return;
        string palabra = banco.palabras[indicePalabra];
        if (palabra.Length == 0) return;

        float separacion = 0.15f;   // en fracción del lado de una letra
        float lado = Mathf.Min(area.height - 2f * Margen,
            (area.width - 2f * Margen) / (palabra.Length + separacion * (palabra.Length - 1)));
        float ancho = lado * (palabra.Length + separacion * (palabra.Length - 1));
        float x = area.center.x - ancho / 2f;

        foreach (char letra in palabra)
        {
            Rect caja = new Rect(x, area.center.y - lado / 2f, lado, lado);
            FiguraLinea figura = banco.FiguraDeLetra(letra);
            if (figura != null) Dibujar(figura, caja);
            else GUI.Label(caja, "?", EditorStyles.centeredGreyMiniLabel);
            x += lado * (1f + separacion);
        }
    }

    void Dibujar(FiguraLinea figura, Rect caja)
    {
        Handles.color = new Color(0.7f, 0.7f, 0.7f);
        Handles.DrawSolidRectangleWithOutline(caja, Color.clear, new Color(0.7f, 0.7f, 0.7f));

        for (int i = 0; i < figura.trazos.Count; i++)
        {
            Vector3[] puntos = figura.trazos[i].puntos.Select(p => (Vector3)ALaPantalla(p, caja)).ToArray();
            if (puntos.Length < 2) continue;

            Color color = Colores[i % Colores.Length];
            Handles.color = color;
            Handles.DrawAAPolyLine(Mathf.Max(2f, caja.width * 0.025f), puntos);

            foreach (float fraccion in PosicionesFlecha) DibujarFlecha(puntos, fraccion);

            Handles.DrawSolidDisc(puntos[0], Vector3.forward, RadioInicio);
            GUI.Label(new Rect(puntos[0].x - RadioInicio, puntos[0].y - RadioInicio, RadioInicio * 2f, RadioInicio * 2f),
                (i + 1).ToString(), EstiloNumero());
        }
    }

    // Coordenadas normalizadas (y hacia arriba) a la pantalla (y hacia abajo)
    static Vector2 ALaPantalla(Vector2 p, Rect caja)
    {
        return new Vector2(caja.x + p.x * caja.width, caja.yMax - p.y * caja.height);
    }

    // Punta de flecha a una fracción del largo del trazo, orientada según el recorrido
    static void DibujarFlecha(Vector3[] puntos, float fraccion)
    {
        float total = 0f;
        for (int i = 1; i < puntos.Length; i++) total += Vector3.Distance(puntos[i - 1], puntos[i]);

        float objetivo = total * fraccion;
        for (int i = 1; i < puntos.Length; i++)
        {
            float segmento = Vector3.Distance(puntos[i - 1], puntos[i]);
            if (objetivo > segmento && i < puntos.Length - 1)
            {
                objetivo -= segmento;
                continue;
            }
            if (segmento <= 0f) return;

            Vector3 direccion = (puntos[i] - puntos[i - 1]) / segmento;
            Vector3 centro = puntos[i - 1] + direccion * Mathf.Min(objetivo, segmento);
            Vector3 lateral = new Vector3(-direccion.y, direccion.x, 0f);

            Handles.DrawAAConvexPolygon(
                centro + direccion * TamanoFlecha,
                centro - direccion * TamanoFlecha * 0.6f + lateral * TamanoFlecha * 0.7f,
                centro - direccion * TamanoFlecha * 0.6f - lateral * TamanoFlecha * 0.7f);
            return;
        }
    }

    GUIStyle EstiloNumero()
    {
        if (estiloNumero == null)
        {
            estiloNumero = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            estiloNumero.normal.textColor = Color.white;
        }
        return estiloNumero;
    }
}
