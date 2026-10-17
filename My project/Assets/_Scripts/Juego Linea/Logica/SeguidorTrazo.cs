using System;
using System.Collections.Generic;

// Clase C# pura (no MonoBehaviour): sigue el dedo sobre UN trazo y dice cuánto avanzó.
// Los puntos y la tolerancia tienen que venir en las mismas unidades (por ejemplo píxeles de pantalla).
// Solo avanza hacia adelante y solo mira una ventana cerca del avance actual, así no se puede
// saltar al final cortando camino (importante en el círculo, donde el final queda sobre el inicio).
public class SeguidorTrazo
{
    public const float ProgresoCompleto = 0.97f;
    const float FactorVentana = 4f;   // cuánto se mira por delante, en tolerancias

    readonly Punto2[] puntos;
    readonly float[] acumulado;   // largo recorrido hasta cada punto
    readonly float largoTotal;
    readonly float tolerancia;
    readonly float ventana;

    float avance;                 // largo recorrido por el dedo sobre el trazo
    int segmento;                 // segmento donde está el avance

    public bool FueraDelCamino { get; private set; }
    public float Distancia { get; private set; }
    public bool Completo => Progreso >= ProgresoCompleto;
    public float Progreso => avance / largoTotal;

    public SeguidorTrazo(IReadOnlyList<Punto2> puntos, float tolerancia)
    {
        if (puntos == null || puntos.Count < 2)
            throw new ArgumentException("Un trazo necesita al menos 2 puntos.", nameof(puntos));
        if (tolerancia <= 0f)
            throw new ArgumentException("La tolerancia tiene que ser mayor que 0.", nameof(tolerancia));

        this.puntos = new Punto2[puntos.Count];
        acumulado = new float[puntos.Count];
        for (int i = 0; i < puntos.Count; i++)
        {
            this.puntos[i] = puntos[i];
            if (i > 0) acumulado[i] = acumulado[i - 1] + Distancia2(puntos[i - 1], puntos[i]);
        }

        largoTotal = acumulado[puntos.Count - 1];
        if (largoTotal <= 0f) throw new ArgumentException("El trazo no tiene largo.", nameof(puntos));

        this.tolerancia = tolerancia;
        ventana = FactorVentana * tolerancia;
    }

    public void Reiniciar()
    {
        avance = 0f;
        segmento = 0;
        FueraDelCamino = false;
        Distancia = 0f;
    }

    // true si el dedo toca cerca del inicio
    public bool PuedeEmpezar(Punto2 punto)
    {
        return Distancia2(punto, puntos[0]) <= tolerancia;
    }

    public void Actualizar(Punto2 punto)
    {
        float limite = Math.Min(avance + ventana, largoTotal);

        float mejorDistancia = float.MaxValue;
        float mejorPosicion = avance;
        int mejorSegmento = segmento;

        for (int i = segmento; i < puntos.Length - 1 && acumulado[i] <= limite; i++)
        {
            float largo = acumulado[i + 1] - acumulado[i];
            if (largo <= 0f) continue;

            Punto2 a = puntos[i], b = puntos[i + 1];
            float t = ((punto.x - a.x) * (b.x - a.x) + (punto.y - a.y) * (b.y - a.y)) / (largo * largo);

            // t limitado al tramo de este segmento que cae dentro de la ventana
            float tMin = Math.Max(0f, (avance - acumulado[i]) / largo);
            float tMax = Math.Min(1f, (limite - acumulado[i]) / largo);
            t = Math.Max(tMin, Math.Min(tMax, t));

            Punto2 cercano = new Punto2(a.x + t * (b.x - a.x), a.y + t * (b.y - a.y));
            float distancia = Distancia2(punto, cercano);
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorPosicion = acumulado[i] + t * largo;
                mejorSegmento = i;
            }
        }

        Distancia = mejorDistancia;
        FueraDelCamino = mejorDistancia > tolerancia;
        if (FueraDelCamino) return;

        if (mejorPosicion > avance)
        {
            avance = mejorPosicion;
            segmento = mejorSegmento;
        }
    }

    // Dirección (unitaria) del trazo en el punto donde está el avance; sirve para orientar la flecha
    public Punto2 DireccionActual
    {
        get
        {
            int i = segmento;
            // Si el avance está justo en el final de un segmento, se usa el siguiente
            while (i < puntos.Length - 2 && avance >= acumulado[i + 1]) i++;

            float dx = puntos[i + 1].x - puntos[i].x;
            float dy = puntos[i + 1].y - puntos[i].y;
            float largo = (float)Math.Sqrt(dx * dx + dy * dy);
            return largo > 0f ? new Punto2(dx / largo, dy / largo) : new Punto2(1f, 0f);
        }
    }

    static float Distancia2(Punto2 a, Punto2 b)
    {
        float dx = a.x - b.x, dy = a.y - b.y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
}
