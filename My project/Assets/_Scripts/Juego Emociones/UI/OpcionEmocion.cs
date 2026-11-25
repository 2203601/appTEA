using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpcionEmocion : MonoBehaviour
{

    public Text label;

    string nombreEmocion;
    GameManagerEmocion gameManager;
    Button boton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(OnClick);

    }

    // Update is called once per frame
    public void Configurar(EmocionInfo info, GameManagerEmocion manager)
    {
        nombreEmocion = info.emocionNombre;
        gameManager = manager;
        label.text = info.emocionNombre;

        boton.image.color = Color.white;
        boton.interactable = true;
    }

    void OnClick()
    {
        gameManager.ResponderEmocion(nombreEmocion, this);
    }

    public void MarcarComoCorrecta()
    {
        boton.image.color = Color.green;
    }
 
    public void MarcarComoIncorrecta()
    {
        // Sin rojo: la opción equivocada solo se apaga (como en Completar), sin señal negativa
        boton.image.color = new Color(1f, 1f, 1f, 0.35f);
    }




}
