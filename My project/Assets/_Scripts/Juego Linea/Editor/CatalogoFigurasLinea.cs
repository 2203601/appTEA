using System.Collections.Generic;
using UnityEngine;

// Las figuras del banco, con su orden de trazo escolar (mayúsculas imprenta).
// Coordenadas 0-1 con (0,0) abajo a la izquierda. Los ángulos son en grados: crecen en sentido
// antihorario (0 = derecha, 90 = arriba), así que de mayor a menor el arco se recorre en sentido horario.
public static class CatalogoFigurasLinea
{
    // Cada punto queda a ~5 % del largo de su trazo del siguiente; las esquinas se conservan.
    const float PasoRelativo = 0.05f;

    // Un tramo: una recta, un arco o una curva suave. Un trazo se arma con varios tramos seguidos.
    class Tramo
    {
        public List<Vector2> puntos = new List<Vector2>();   // muestreo fino, solo para medir y remuestrear

        public float Largo()
        {
            float largo = 0f;
            for (int i = 1; i < puntos.Count; i++) largo += Vector2.Distance(puntos[i - 1], puntos[i]);
            return largo;
        }

        // Punto a una distancia dada desde el inicio del tramo
        public Vector2 En(float distancia)
        {
            for (int i = 1; i < puntos.Count; i++)
            {
                float segmento = Vector2.Distance(puntos[i - 1], puntos[i]);
                if (distancia <= segmento || i == puntos.Count - 1)
                    return Vector2.Lerp(puntos[i - 1], puntos[i], segmento > 0f ? Mathf.Clamp01(distancia / segmento) : 0f);
                distancia -= segmento;
            }
            return puntos[puntos.Count - 1];
        }
    }

    static Vector2 V(float x, float y) { return new Vector2(x, y); }

    static Vector2 Punto(float cx, float cy, float rx, float ry, float grados)
    {
        float r = grados * Mathf.Deg2Rad;
        return V(cx + rx * Mathf.Cos(r), cy + ry * Mathf.Sin(r));
    }

    static Tramo Recta(Vector2 a, Vector2 b)
    {
        var t = new Tramo();
        t.puntos.Add(a);
        t.puntos.Add(b);
        return t;
    }

    static Tramo Arco(float cx, float cy, float rx, float ry, float desde, float hasta)
    {
        var t = new Tramo();
        int pasos = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(hasta - desde) / 2f));
        for (int i = 0; i <= pasos; i++)
            t.puntos.Add(Punto(cx, cy, rx, ry, Mathf.Lerp(desde, hasta, i / (float)pasos)));
        return t;
    }

    // Curva suave (Catmull-Rom) que pasa por todos los puntos de control
    static Tramo Curva(params Vector2[] c)
    {
        var t = new Tramo();
        for (int i = 0; i < c.Length - 1; i++)
        {
            Vector2 p0 = c[Mathf.Max(i - 1, 0)], p1 = c[i], p2 = c[i + 1], p3 = c[Mathf.Min(i + 2, c.Length - 1)];
            for (int k = 0; k < 16; k++)
            {
                float u = k / 16f, u2 = u * u, u3 = u2 * u;
                t.puntos.Add(0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 +
                                     (-p0 + 3f * p1 - 3f * p2 + p3) * u3));
            }
        }
        t.puntos.Add(c[c.Length - 1]);
        return t;
    }

    static Trazo Armar(params Tramo[] tramos)
    {
        float total = 0f;
        foreach (var t in tramos) total += t.Largo();
        float paso = total * PasoRelativo;

        var trazo = new Trazo();
        for (int i = 0; i < tramos.Length; i++)
        {
            float largo = tramos[i].Largo();
            int divisiones = Mathf.Max(1, Mathf.CeilToInt(largo / paso - 0.01f));
            for (int k = (i == 0 ? 0 : 1); k <= divisiones; k++)
                trazo.puntos.Add(tramos[i].En(largo * k / divisiones));
        }
        return trazo;
    }

    static FiguraLinea Figura(string nombre, CategoriaLinea categoria, params Trazo[] trazos)
    {
        return new FiguraLinea { nombre = nombre, categoria = categoria, trazos = new List<Trazo>(trazos) };
    }

    public static List<FiguraLinea> Crear()
    {
        var s = CategoriaLinea.TrazoSimple;
        var l = CategoriaLinea.Letra;
        var n = CategoriaLinea.Numero;

        var figuras = new List<FiguraLinea>
        {
            // ----------------------------------------------------------- Trazos simples
            Figura("Línea vertical", s, Armar(Recta(V(0.5f, 1), V(0.5f, 0)))),            // de arriba a abajo
            Figura("Línea horizontal", s, Armar(Recta(V(0, 0.5f), V(1, 0.5f)))),          // de izquierda a derecha
            Figura("Línea diagonal", s, Armar(Recta(V(0, 1), V(1, 0)))),                  // de arriba-izquierda a abajo-derecha
            Figura("Curva", s, Armar(Arco(0.5f, 0.1f, 0.5f, 0.8f, 180, 0))),              // arco: sube por la izquierda y baja por la derecha
            Figura("Círculo", s, Armar(Arco(0.5f, 0.5f, 0.5f, 0.5f, 90, 450))),           // desde arriba, hacia la izquierda
            Figura("Zigzag", s, Armar(
                Recta(V(0, 0.1f), V(0.25f, 0.9f)), Recta(V(0.25f, 0.9f), V(0.5f, 0.1f)),
                Recta(V(0.5f, 0.1f), V(0.75f, 0.9f)), Recta(V(0.75f, 0.9f), V(1, 0.1f)))),

            // ----------------------------------------------------------------- Letras
            Figura("A", l,
                Armar(Recta(V(0.5f, 1), V(0, 0))),                    // 1) diagonal izquierda, de la punta hacia abajo
                Armar(Recta(V(0.5f, 1), V(1, 0))),                    // 2) diagonal derecha, de la punta hacia abajo
                Armar(Recta(V(0.2f, 0.4f), V(0.8f, 0.4f)))),          // 3) travesaño
            Figura("C", l, Armar(Arco(0.5f, 0.5f, 0.5f, 0.5f, 45, 315))),   // un arco, de arriba a la derecha a abajo a la derecha
            Figura("E", l,
                Armar(Recta(V(0, 1), V(0, 0))),                       // 1) palo vertical
                Armar(Recta(V(0, 1), V(1, 1))),                       // 2) barra de arriba
                Armar(Recta(V(0, 0.5f), V(0.8f, 0.5f))),              // 3) barra del medio
                Armar(Recta(V(0, 0), V(1, 0)))),                      // 4) barra de abajo
            Figura("L", l, Armar(Recta(V(0, 1), V(0, 0)), Recta(V(0, 0), V(1, 0)))),   // un trazo: baja y gira a la derecha
            Figura("M", l,
                Armar(Recta(V(0, 1), V(0, 0))),                                        // 1) palo izquierdo
                Armar(Recta(V(0, 1), V(0.5f, 0.35f)), Recta(V(0.5f, 0.35f), V(1, 1))), // 2) la V del medio
                Armar(Recta(V(1, 1), V(1, 0)))),                                       // 3) palo derecho
            Figura("N", l,
                Armar(Recta(V(0, 1), V(0, 0))),                       // 1) palo izquierdo
                Armar(Recta(V(0, 1), V(1, 0))),                       // 2) diagonal
                Armar(Recta(V(1, 1), V(1, 0)))),                      // 3) palo derecho
            Figura("O", l, Armar(Arco(0.5f, 0.5f, 0.45f, 0.5f, 90, 450))),            // un óvalo, desde arriba hacia la izquierda
            Figura("P", l,
                Armar(Recta(V(0, 1), V(0, 0))),                       // 1) palo vertical
                Armar(Recta(V(0, 1), V(0.45f, 1)),                    // 2) panza: va a la derecha, curva en sentido horario y vuelve
                      Arco(0.45f, 0.725f, 0.5f, 0.275f, 90, -90),
                      Recta(V(0.45f, 0.45f), V(0, 0.45f)))),
            Figura("S", l, Armar(
                Arco(0.5f, 0.75f, 0.45f, 0.25f, 40, 270),             // curva de arriba (hacia la izquierda)
                Arco(0.5f, 0.25f, 0.45f, 0.25f, 90, -140))),          // curva de abajo (hacia la derecha)
            Figura("T", l,
                Armar(Recta(V(0, 1), V(1, 1))),                       // 1) barra de arriba
                Armar(Recta(V(0.5f, 1), V(0.5f, 0)))),                // 2) palo vertical
            Figura("U", l, Armar(
                Recta(V(0, 1), V(0, 0.35f)),                          // un trazo: baja, curva abajo y sube
                Arco(0.5f, 0.35f, 0.5f, 0.35f, 180, 360),
                Recta(V(1, 0.35f), V(1, 1)))),

            // ---------------------------------------------------------------- Números
            Figura("0", n, Armar(Arco(0.5f, 0.5f, 0.4f, 0.5f, 90, 450))),             // óvalo, desde arriba hacia la izquierda
            Figura("1", n, Armar(Recta(V(0.2f, 0.75f), V(0.5f, 1)), Recta(V(0.5f, 1), V(0.5f, 0)))),   // banderita y palo
            Figura("2", n, Armar(
                Arco(0.5f, 0.7f, 0.4f, 0.3f, 160, -40),               // curva de arriba
                Recta(Punto(0.5f, 0.7f, 0.4f, 0.3f, -40), V(0, 0)),   // diagonal hasta abajo a la izquierda
                Recta(V(0, 0), V(1, 0)))),                            // base
            Figura("3", n, Armar(
                Arco(0.45f, 0.75f, 0.4f, 0.25f, 150, -90),            // panza de arriba
                Arco(0.45f, 0.25f, 0.45f, 0.25f, 90, -150))),         // panza de abajo
            Figura("4", n,
                Armar(Recta(V(0.7f, 1), V(0, 0.3f)), Recta(V(0, 0.3f), V(1, 0.3f))),   // 1) diagonal y barra
                Armar(Recta(V(0.7f, 1), V(0.7f, 0)))),                                 // 2) palo
            Figura("5", n,
                Armar(Recta(V(0.15f, 1), Punto(0.45f, 0.28f, 0.45f, 0.28f, 140)),      // 1) palo corto y panza
                      Arco(0.45f, 0.28f, 0.45f, 0.28f, 140, -140)),
                Armar(Recta(V(0.15f, 1), V(0.9f, 1)))),                                // 2) barra de arriba
            Figura("6", n, Armar(
                Curva(V(0.8f, 1), V(0.45f, 0.88f), V(0.2f, 0.6f), V(0.22f, 0.28f)),    // baja curvando por la izquierda
                Arco(0.5f, 0.28f, 0.28f, 0.28f, 180, 540))),                           // y cierra la vuelta de abajo
            Figura("7", n, Armar(Recta(V(0, 1), V(1, 1)), Recta(V(1, 1), V(0.35f, 0)))),   // barra y diagonal
            Figura("8", n, Armar(
                Arco(0.5f, 0.76f, 0.3f, 0.24f, 90, 270),              // medio círculo de arriba (izquierda)
                Arco(0.5f, 0.26f, 0.35f, 0.26f, 90, -270),            // círculo de abajo completo (horario)
                Arco(0.5f, 0.76f, 0.3f, 0.24f, 270, 450))),           // termina el de arriba (derecha)
            Figura("9", n,
                Armar(Arco(0.5f, 0.72f, 0.3f, 0.28f, 90, 450)),                                       // 1) vuelta de arriba
                Armar(Curva(V(0.8f, 0.72f), V(0.8f, 0.4f), V(0.6f, 0.1f), V(0.2f, 0.05f)))),          // 2) palo curvo
        };

        // Vocales con tilde: la letra completa y después la tilde, que sobresale por arriba del cuadro (y > 1)
        foreach (var (sinTilde, conTilde) in new[] { ('A', 'Á'), ('E', 'É'), ('O', 'Ó'), ('U', 'Ú') })
        {
            FiguraLinea basica = figuras.Find(f => f.categoria == l && f.nombre == sinTilde.ToString());
            var figura = new FiguraLinea { nombre = conTilde.ToString(), categoria = l, trazos = new List<Trazo>(basica.trazos) };
            // La tilde: un trazo corto en diagonal, de abajo a la izquierda hacia arriba a la derecha
            figura.trazos.Add(Armar(Recta(V(0.35f, 1.05f), V(0.65f, 1.25f))));
            figuras.Add(figura);
        }

        return figuras;
    }
}