using UnityEngine;

// Va en cada pieza del rompecabezas (el objeto con el Sprite Mask, ej. "Pieces_0 (4)").
// La pieza se deja armada en la escena en su lugar correcto: al arrancar guarda esa
// posición como destino, el GameManager la desparrama, y al soltarla cerca encaja sola.
// El arrastre lo maneja el GameManager (las piezas son sprites, no UI).

[RequireComponent(typeof(SpriteRenderer), typeof(SpriteMask))]
public class PiezaRompecabezas : MonoBehaviour
{
    [Tooltip("Qué tan cerca de su lugar hay que soltarla para que encaje (en unidades del canvas)")]
    [SerializeField] private float distanciaEncaje = 45f;

    // Por encima del canvas, que está en orden 0
    const int OrdenBase = 10;
    static int ultimoTurno;

    public bool Colocada { get; private set; }
    public int Orden => forma.sortingOrder;

    SpriteRenderer forma;
    SpriteMask mascara;
    SpriteRenderer[] imagenes;
    Vector3 posicionCorrectaLocal;
    Vector3 offsetAgarre;

    void Awake()
    {
        forma = GetComponent<SpriteRenderer>();
        mascara = GetComponent<SpriteMask>();
        imagenes = GetComponentsInChildren<SpriteRenderer>(true);
        posicionCorrectaLocal = transform.localPosition;
    }

    public static void ReiniciarOrden()
    {
        ultimoTurno = 0;
    }

    // La centra en un punto de la bandeja (posición en mundo) al arrancar la ronda
    public void Desparramar(Vector3 posicionMundo)
    {
        Colocada = false;
        // Centrar por lo que se ve de la pieza, no por el pivot del sprite
        MoverA(posicionMundo + transform.position - forma.bounds.center);
        TraerAlFrente();
    }

    public bool Contiene(Vector3 puntoMundo)
    {
        Bounds limites = forma.bounds;
        return puntoMundo.x >= limites.min.x && puntoMundo.x <= limites.max.x
            && puntoMundo.y >= limites.min.y && puntoMundo.y <= limites.max.y;
    }

    public void Agarrar(Vector3 puntoMundo)
    {
        TraerAlFrente();
        offsetAgarre = transform.position - puntoMundo;
    }

    public void Arrastrar(Vector3 puntoMundo)
    {
        MoverA(puntoMundo + offsetAgarre);
    }

    // Devuelve true si quedó encajada en su lugar
    public bool Soltar()
    {
        if (Vector2.Distance(transform.localPosition, posicionCorrectaLocal) > distanciaEncaje) return false;

        Colocada = true;
        transform.localPosition = posicionCorrectaLocal;
        // Las ya encajadas quedan atrás, para que no tapen a las que todavía se mueven
        AplicarTurno(0);
        return true;
    }

    void MoverA(Vector3 posicionMundo)
    {
        transform.position = new Vector3(posicionMundo.x, posicionMundo.y, transform.position.z);
    }

    void TraerAlFrente()
    {
        AplicarTurno(++ultimoTurno);
    }

    // Cada pieza suelta usa su propio rango de orden, así su máscara solo recorta
    // su propia imagen y no deja ver la de las piezas que tiene encima o debajo.
    void AplicarTurno(int turno)
    {
        // 3 órdenes por pieza: forma y fondo de la máscara, imagen, frente de la máscara.
        // La imagen tiene que quedar estrictamente entre el fondo y el frente.
        int fondo = OrdenBase + turno * 3;

        mascara.isCustomRangeActive = true;
        mascara.backSortingLayerID = forma.sortingLayerID;
        mascara.frontSortingLayerID = forma.sortingLayerID;
        mascara.backSortingOrder = fondo;
        mascara.frontSortingOrder = fondo + 2;

        foreach (SpriteRenderer imagen in imagenes)
        {
            imagen.sortingOrder = imagen == forma ? fondo : fondo + 1;
        }
    }
}
