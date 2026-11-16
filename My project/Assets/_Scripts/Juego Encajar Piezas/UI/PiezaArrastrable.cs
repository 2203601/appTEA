using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Va en el prefab del emoji que el chico arrastra con el dedo/mouse.

[RequireComponent(typeof(Image))]
public class PiezaArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string PiezaNombre { get; private set; }

    Image imagen;
    CanvasGroup canvasGroup;
    Canvas canvasRaiz;

    Transform posicionOriginalParent;
    Vector2 posicionOriginalLocal;

    public int IndicePieza { get; private set; } = -1;


    void Awake()
    {
        imagen = GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasRaiz = GetComponentInParent<Canvas>().rootCanvas;

    }

    public void Configurar(PiezaInfo info)
    {
        PiezaNombre = info.piezaNombre;
        imagen.sprite = info.piezaSprite;
        AplicarAlphaHitTest();

    }

    public void ConfigurarRompecabezas(Sprite sprite, int indice)
    {
        IndicePieza = indice;
        imagen.sprite = sprite;
        AplicarAlphaHitTest();

    }


    private void AplicarAlphaHitTest()
    {
        Texture2D textura = imagen.sprite != null ? imagen.sprite.texture : null;
        if (textura != null && textura.isReadable)
        {
            imagen.alphaHitTestMinimumThreshold = 0.5f;
        }
        else if (textura != null)
        {
            Debug.LogWarning($"[{name}] \"{textura.name}\" no tiene Read/Write Enabled activado " +
                "en su Import Settings, así que el toque se va a detectar en todo el rectángulo " +
                "de la pieza (tabs transparentes incluidos) en vez de solo en la parte visible. " +
                "Activalo en el Inspector del sprite si las piezas se superponen y se \"roban\" el toque.");
        }
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Begin Drag");

        posicionOriginalParent = transform.parent;
        posicionOriginalLocal = transform.localPosition;

    
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