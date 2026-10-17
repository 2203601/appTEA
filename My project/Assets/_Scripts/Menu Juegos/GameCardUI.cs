using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class GameCardUI : MonoBehaviour
{
    [Header("Referencias UI (arrastrar desde el prefab)")]
    [SerializeField] private Image fondo;
    [SerializeField] private Text titulo;
    [SerializeField] private Image icono;


    private Button boton;
    private GameData data;
    private DifficultyModalUI modal;


    private Button botonHome;

    private void Awake()
    {
        boton = GetComponent<Button>();
    }

    public void Configurar(GameData gameData, DifficultyModalUI modalRef)
    {
        data = gameData;
        modal = modalRef;

        if (titulo != null) titulo.text = data.titulo;
        if (fondo != null) fondo.color = data.colorTema;
        if (icono != null) icono.sprite = data.icono;


        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
    

        modal.Abrir(data);
    }
    
}
