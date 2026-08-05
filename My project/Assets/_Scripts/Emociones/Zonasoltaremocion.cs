using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Va en la imagen/panel de la situación (donde el chico suelta el emoji).
// Necesita que el Image tenga "Raycast Target" tildado para poder
// recibir el evento OnDrop.
[RequireComponent(typeof(Image))]
public class ZonaSoltarEmocion : MonoBehaviour, IDropHandler
{
    public GameManagerEmocion gameManager;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("DROP");

        if (eventData.pointerDrag == null) return;

        EmojiArrastrable emoji = eventData.pointerDrag.GetComponent<EmojiArrastrable>();
        if (emoji == null) return;

        Debug.Log(emoji.EmocionNombre);

        gameManager.ResponderSituacion(emoji);
    }
}