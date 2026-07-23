using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla una tarjeta individual del grid (ej: "Memoria", "Encajar Formas").
/// La tarjeta muestra solo el sprite (ícono) y el nombre del juego.
/// Este componente va en el mismo GameObject que tiene el Button
/// (el GameObject ES el botón; el script no referencia un botón aparte).
/// Al tocarla, abre el modal de dificultad para ese juego.
/// </summary>
/// /// 
[RequireComponent(typeof(Button))]
public class GameCardUI : MonoBehaviour
{
    [Header("Referencias UI (arrastrar desde el prefab)")]
    [SerializeField] private Image fondo;
    [SerializeField] private Text tituloTexto;

    private Button boton;
    private GameData data;
    private DifficultyModalUI modal;

    private void Awake()
    {
        boton = GetComponent<Button>();
    }

    public void Configurar(GameData gameData, DifficultyModalUI modalRef)
    {
        data = gameData;
        modal = modalRef;

        if (tituloTexto != null) tituloTexto.text = data.titulo;
        if (fondo != null) fondo.color = data.colorTema;

        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        modal.Abrir(data);
    }
}
