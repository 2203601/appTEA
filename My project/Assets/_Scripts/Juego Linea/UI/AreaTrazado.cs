using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Recibe el arrastre (dedo o mouse) sobre el área de escritura y se lo pasa al SeguidorTrazo.
// Va en un RectTransform que ocupa toda la zona; la guía, el trazo del niño y los marcadores
// pueden ser hijos suyos, así comparten sus coordenadas locales.
//
// Hacia afuera (eventos) la posición va normalizada 0-1 dentro del área, con (0,0) abajo a la izquierda.
// Al SeguidorTrazo se le pasa en píxeles del área, con el mismo origen: así un círculo no se
// deforma en un área que no es cuadrada, y la tolerancia en píxeles tiene el mismo valor en x e y.
public class AreaTrazado : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public SeguidorTrazo Seguidor { get; set; }
    public bool Arrastrando { get; private set; }

    public event Action<Vector2> AlEmpezar;
    public event Action<Vector2> AlMover;
    public event Action<Vector2> AlSoltar;
    public event Action AlCompletar;

    RectTransform rect;
    int punteroActivo = int.MinValue;

    public Vector2 Tamano => RectArea.rect.size;

    RectTransform RectArea
    {
        get
        {
            if (rect == null) rect = (RectTransform)transform;
            return rect;
        }
    }

    // Un Graphic transparente que reciba los toques, si el objeto no tiene ninguno
    void Reset()
    {
        if (GetComponent<Graphic>() == null)
        {
            Image imagen = gameObject.AddComponent<Image>();
            imagen.color = Color.clear;
            imagen.raycastTarget = true;
        }
    }

    // Normalizado 0-1 -> coordenadas locales del área (las mismas que usa LineaUI)
    public Vector2 ALocal(Vector2 normalizado)
    {
        Rect r = RectArea.rect;
        return new Vector2(r.xMin + normalizado.x * r.width, r.yMin + normalizado.y * r.height);
    }

    // Normalizado 0-1 -> píxeles del área, para armar los puntos del SeguidorTrazo
    public Punto2 AUnidadesDelSeguidor(Vector2 normalizado)
    {
        Rect r = RectArea.rect;
        return new Punto2(normalizado.x * r.width, normalizado.y * r.height);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Seguidor == null || Arrastrando) return;

        Vector2 normalizado = Normalizar(eventData);
        if (!Seguidor.PuedeEmpezar(AUnidadesDelSeguidor(normalizado))) return;

        Arrastrando = true;
        punteroActivo = eventData.pointerId;
        Seguidor.Actualizar(AUnidadesDelSeguidor(normalizado));
        AlEmpezar?.Invoke(normalizado);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!Arrastrando || Seguidor == null || eventData.pointerId != punteroActivo) return;

        Vector2 normalizado = Normalizar(eventData);
        Seguidor.Actualizar(AUnidadesDelSeguidor(normalizado));
        AlMover?.Invoke(normalizado);

        if (Seguidor.Completo)
        {
            Arrastrando = false;
            AlCompletar?.Invoke();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!Arrastrando || eventData.pointerId != punteroActivo) return;

        Arrastrando = false;
        AlSoltar?.Invoke(Normalizar(eventData));
    }

    // Si el área se desactiva en pleno trazo no queda un arrastre colgado
    void OnDisable()
    {
        Arrastrando = false;
    }

    Vector2 Normalizar(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            RectArea, eventData.position, eventData.pressEventCamera, out Vector2 local);
        return Rect.PointToNormalized(RectArea.rect, local);
    }
}
