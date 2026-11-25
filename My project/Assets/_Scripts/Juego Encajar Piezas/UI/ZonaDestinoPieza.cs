using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(Image))]
public class ZonaDestinoPieza : MonoBehaviour, IDropHandler
{
    [Tooltip("Dejalo en -1 para que se autocomplete con la posición en la jerarquía " +
             "(el orden de los hijos dentro del GridLayoutGroup). Tiene que coincidir " +
             "con el índice del array Piezas en PersonajeRompecabezas.")]
    [SerializeField] private int indice = -1;

    public int Indice => indice;
    public bool Ocupada { get; private set; }

    private void Awake()
    {
        if (indice < 0) indice = transform.GetSiblingIndex();
    }

    // Se llama al arrancar cada ronda nueva, para poder reusar las mismas 9 casillas.
    public void Reiniciar()
    {
        Ocupada = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[ZonaDestinoPieza {name}] OnDrop llamado. Ocupada={Ocupada}");

        if (Ocupada) return;

        PiezaArrastrable pieza = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<PiezaArrastrable>()
            : null;
        if (pieza == null)
        {
            Debug.LogWarning($"[ZonaDestinoPieza {name}] pointerDrag no tenía PiezaArrastrable " +
                "(o era null). Revisá que el objeto que soltaste sea la pieza y no un hijo suyo.");
            return;
        }

        bool esCorrecta = pieza.IndicePieza == indice;
        Debug.Log($"[ZonaDestinoPieza {name}] pieza.IndicePieza={pieza.IndicePieza}, " +
            $"mi indice={indice}, esCorrecta={esCorrecta}");

        if (esCorrecta)
        {
            
            RectTransform piezaRect = pieza.transform as RectTransform;
            RectTransform zonaRect = GetComponent<RectTransform>();

            piezaRect.SetParent(transform, false);

            // La pieza toma exactamente el tamaño de la casilla
            piezaRect.anchorMin = new Vector2(0.5f, 0.5f);
            piezaRect.anchorMax = new Vector2(0.5f, 0.5f);
            piezaRect.pivot = new Vector2(0.5f, 0.5f);

            piezaRect.sizeDelta = zonaRect.rect.size;
            piezaRect.anchoredPosition = Vector2.zero;

            piezaRect.localScale = Vector3.one;
            piezaRect.localRotation = Quaternion.identity;

            pieza.Bloquear();
            Ocupada = true;
        }
        

        if (esCorrecta)
        {
            GameManagerEncajar.Instancia?.NotificarPiezaRompecabezasColocada();
        }
    }
}