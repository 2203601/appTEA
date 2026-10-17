using UnityEngine;
using UnityEngine.UI;

// La estrella que marca dónde empezar (con un latido suave, como en Completar) y la flecha
// que muestra hacia dónde sigue el camino cuando el dedo se sale. Las posiciones son coordenadas
// locales del contenedor, las mismas que usa LineaUI (AreaTrazado.ALocal las convierte).
// Tiene que ser un RectTransform que ocupe toda el área, con el mismo pivot.
public class MarcadorTrazo : MonoBehaviour
{
    [SerializeField] private Image estrella;
    [SerializeField] private Image flecha;    // el sprite apunta a la derecha
    [Tooltip("Qué tan adelantada del punto va la flecha, en unidades del canvas")]
    [SerializeField] private float distanciaFlecha = 70f;

    Coroutine latido;

    void Awake()
    {
        // No tapan el toque del AreaTrazado
        if (estrella != null) estrella.raycastTarget = false;
        if (flecha != null) flecha.raycastTarget = false;
        Ocultar();
    }

    public void Configurar(Sprite spriteEstrella, Sprite spriteFlecha)
    {
        if (estrella != null && spriteEstrella != null) estrella.sprite = spriteEstrella;
        if (flecha != null && spriteFlecha != null) flecha.sprite = spriteFlecha;
    }

    public void AjustarTamanos(float ladoEstrella, float ladoFlecha)
    {
        if (estrella != null) ((RectTransform)estrella.transform).sizeDelta = Vector2.one * ladoEstrella;
        if (flecha != null) ((RectTransform)flecha.transform).sizeDelta = Vector2.one * ladoFlecha;
        distanciaFlecha = ladoEstrella * 0.9f + ladoFlecha;
    }

    // Antes de empezar un trazo: la estrella late sobre el inicio
    public void MostrarInicio(Vector2 posicion)
    {
        OcultarFlecha();
        if (estrella == null) return;

        estrella.gameObject.SetActive(true);
        estrella.transform.localPosition = posicion;
        DetenerLatido();
        latido = StartCoroutine(AnimacionSuave.Latido(estrella.transform));
    }

    public void OcultarEstrella()
    {
        DetenerLatido();
        if (estrella != null) estrella.gameObject.SetActive(false);
    }

    // Cuando el dedo se sale: la flecha muestra hacia dónde seguir
    public void MostrarFlecha(Vector2 posicion, Punto2 direccion)
    {
        if (flecha == null) return;

        Vector2 sentido = new Vector2(direccion.x, direccion.y);
        flecha.gameObject.SetActive(true);
        flecha.transform.localPosition = posicion + sentido * distanciaFlecha;
        flecha.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(sentido.y, sentido.x) * Mathf.Rad2Deg);
    }

    public void OcultarFlecha()
    {
        if (flecha != null) flecha.gameObject.SetActive(false);
    }

    public void Ocultar()
    {
        OcultarEstrella();
        OcultarFlecha();
    }

    void DetenerLatido()
    {
        if (latido != null) StopCoroutine(latido);
        latido = null;
        if (estrella != null) estrella.transform.localScale = Vector3.one;
    }
}
