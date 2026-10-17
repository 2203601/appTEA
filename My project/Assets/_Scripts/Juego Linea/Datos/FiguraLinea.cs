using System;
using System.Collections.Generic;
using UnityEngine;

public enum CategoriaLinea
{
    TrazoSimple,
    Letra,
    Numero
}

// Un trazo es lo que se dibuja sin levantar el dedo.
// Puntos normalizados 0-1 dentro de la figura: (0,0) abajo a la izquierda, (1,1) arriba a la derecha,
// en el orden en que se dibujan.
[Serializable]
public class Trazo
{
    public List<Vector2> puntos = new List<Vector2>();
}

[Serializable]
public class FiguraLinea
{
    public string nombre;               // "A", "7", "Círculo"...
    public CategoriaLinea categoria;
    public List<Trazo> trazos = new List<Trazo>();   // en el orden en que se dibujan
    public Sprite pictograma;                        // opcional; si falta se usa el del banco
}
