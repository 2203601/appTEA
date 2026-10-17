using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(Image))]
public class ZonaSoltarPieza : MonoBehaviour, IDropHandler
{
    public GameManagerEncajar gameManager;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("DROP");

        if (eventData.pointerDrag == null) return;

        PiezaArrastrable pieza = eventData.pointerDrag.GetComponent<PiezaArrastrable>();
        if (pieza == null) return;

        gameManager.ResponderPiezas(pieza);
    }
}