using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Va en el prefab del emoji que el chico arrastra con el dedo/mouse.
// Necesita un Image (el sprite del emoji) y un CanvasGroup (se agrega
// solo si falta) para poder "atravesar" el raycast mientras se arrastra.
[RequireComponent(typeof(Image))]
public class EmojiArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string EmocionNombre { get; private set; }

    Image imagen;
    CanvasGroup canvasGroup;
    Canvas canvasRaiz;

    Transform posicionOriginalParent;
    Vector2 posicionOriginalLocal;

    void Awake()
    {
        imagen = GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void Configurar(EmocionInfo info)
    {
        EmocionNombre = info.emocionNombre;
        imagen.sprite = info.emocionSprite;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Begin Drag");

        posicionOriginalParent = transform.parent;
        posicionOriginalLocal = transform.localPosition;

        // Lo sacamos a la raíz del Canvas para que se dibuje por encima de todo
        // mientras se arrastra, y desactivamos el raycast para que la zona de
        // abajo (ZonaSoltarEmocion) pueda detectar el drop.
        transform.SetParent(canvasRaiz.transform, true);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRaiz.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        rectTransform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // Si ZonaSoltarEmocion.OnDrop() no lo reubicó (drop inválido, fuera de
        // la zona), lo mandamos de vuelta a su lugar original en la bandeja.
        if (transform.parent == canvasRaiz.transform)
        {
            VolverAPosicionOriginal();
        }
    }

    public void VolverAPosicionOriginal()
    {
        transform.SetParent(posicionOriginalParent, true);
        transform.localPosition = posicionOriginalLocal;
    }

    public void Bloquear()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}