using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Dibuja una línea gruesa (o punteada) con la malla del Graphic, sin sprites.
// Los puntos están en coordenadas locales del RectTransform (relativas a su pivot).
// Se usa para la guía (punteada) y para el trazo del niño (continua).
// Con un color semitransparente las uniones se ven más oscuras: usá color opaco y bajá la
// transparencia con un CanvasGroup.
[RequireComponent(typeof(CanvasRenderer))]
public class LineaUI : Graphic
{
    [SerializeField] private float grosor = 24f;
    [SerializeField] private bool punteada;
    [Tooltip("Distancia entre el centro de un punto y el siguiente, en la guía punteada")]
    [SerializeField] private float separacionPuntos = 44f;
    [Range(16, 24)]
    [SerializeField] private int segmentosCirculo = 20;

    // Giro mínimo (en coseno) para poner un círculo de unión; con menos giro alcanza con los cuadrados
    const float CosenoUnion = 0.998f;

    readonly List<Vector2> puntos = new List<Vector2>();

    public float Grosor
    {
        get => grosor;
        set { grosor = value; SetVerticesDirty(); }
    }

    public bool Punteada
    {
        get => punteada;
        set { punteada = value; SetVerticesDirty(); }
    }

    public float SeparacionPuntos
    {
        get => separacionPuntos;
        set { separacionPuntos = Mathf.Max(1f, value); SetVerticesDirty(); }
    }

    public int CantidadPuntos => puntos.Count;

    // Cuántos vértices generó la última vez que se armó la malla (para diagnosticar si no se ve)
    public int VerticesGenerados { get; private set; }

    protected override void OnEnable()
    {
        base.OnEnable();
        canvasRenderer.cullTransparentMesh = false;   // que nunca se descarte la malla por el color
    }

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();
        raycastTarget = false;   // la línea no tiene que tapar el toque del AreaTrazado
    }
#endif

    public void SetPuntos(IReadOnlyList<Vector2> nuevos)
    {
        puntos.Clear();
        puntos.AddRange(nuevos);
        SetVerticesDirty();
    }

    public void AgregarPunto(Vector2 punto)
    {
        if (puntos.Count > 0 && (puntos[puntos.Count - 1] - punto).sqrMagnitude < 0.25f) return;
        puntos.Add(punto);
        SetVerticesDirty();
    }

    public void Limpiar()
    {
        puntos.Clear();
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        grosor = Mathf.Max(1f, grosor);
        separacionPuntos = Mathf.Max(1f, separacionPuntos);
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        VerticesGenerados = 0;
        if (puntos.Count == 0) return;

        float radio = grosor * 0.5f;

        if (puntos.Count == 1) AgregarCirculo(vh, puntos[0], radio);
        else if (punteada) DibujarPunteada(vh, radio);
        else DibujarContinua(vh, radio);

        VerticesGenerados = vh.currentVertCount;
    }

    void DibujarContinua(VertexHelper vh, float radio)
    {
        for (int i = 0; i < puntos.Count - 1; i++)
        {
            AgregarSegmento(vh, puntos[i], puntos[i + 1], radio);
        }

        // Puntas redondeadas
        AgregarCirculo(vh, puntos[0], radio);
        AgregarCirculo(vh, puntos[puntos.Count - 1], radio);

        // Uniones redondeadas solo donde la línea gira
        for (int i = 1; i < puntos.Count - 1; i++)
        {
            Vector2 entrada = (puntos[i] - puntos[i - 1]).normalized;
            Vector2 salida = (puntos[i + 1] - puntos[i]).normalized;
            if (Vector2.Dot(entrada, salida) < CosenoUnion) AgregarCirculo(vh, puntos[i], radio);
        }
    }

    // Un círculo cada "separacionPuntos" a lo largo del camino, empezando en el primer punto
    void DibujarPunteada(VertexHelper vh, float radio)
    {
        float recorrido = 0f;
        float proximo = 0f;
        Vector2 ultimo = puntos[0];

        for (int i = 0; i < puntos.Count - 1; i++)
        {
            Vector2 a = puntos[i], b = puntos[i + 1];
            float largo = Vector2.Distance(a, b);
            if (largo <= 0f) continue;

            while (proximo <= recorrido + largo)
            {
                ultimo = Vector2.Lerp(a, b, (proximo - recorrido) / largo);
                AgregarCirculo(vh, ultimo, radio);
                proximo += separacionPuntos;
            }
            recorrido += largo;
        }

        // Que el final también tenga su punto
        Vector2 fin = puntos[puntos.Count - 1];
        if (Vector2.Distance(ultimo, fin) > separacionPuntos * 0.5f) AgregarCirculo(vh, fin, radio);
    }

    void AgregarSegmento(VertexHelper vh, Vector2 a, Vector2 b, float radio)
    {
        Vector2 direccion = (b - a).normalized;
        if (direccion == Vector2.zero) return;
        Vector2 lateral = new Vector2(-direccion.y, direccion.x) * radio;

        int baseIndice = vh.currentVertCount;
        vh.AddVert(Vertice(a - lateral));
        vh.AddVert(Vertice(a + lateral));
        vh.AddVert(Vertice(b + lateral));
        vh.AddVert(Vertice(b - lateral));
        vh.AddTriangle(baseIndice, baseIndice + 1, baseIndice + 2);
        vh.AddTriangle(baseIndice + 2, baseIndice + 3, baseIndice);
    }

    void AgregarCirculo(VertexHelper vh, Vector2 centro, float radio)
    {
        int baseIndice = vh.currentVertCount;
        vh.AddVert(Vertice(centro));
        for (int i = 0; i < segmentosCirculo; i++)
        {
            float angulo = i * Mathf.PI * 2f / segmentosCirculo;
            vh.AddVert(Vertice(centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio));
        }
        for (int i = 0; i < segmentosCirculo; i++)
        {
            vh.AddTriangle(baseIndice, baseIndice + 1 + i, baseIndice + 1 + (i + 1) % segmentosCirculo);
        }
    }

    UIVertex Vertice(Vector2 posicion)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = posicion;
        v.color = color;
        v.uv0 = Vector2.zero;
        return v;
    }
}
