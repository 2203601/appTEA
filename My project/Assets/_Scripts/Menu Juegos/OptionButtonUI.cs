using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class OptionButtonUI : MonoBehaviour
{
    [SerializeField] private Image icono;
    [SerializeField] private Image nombre;
    [SerializeField] private Button boton;
    [SerializeField] private GameObject marcoSeleccionado; 

    public string Id { get; private set; }

    public void Configurar(string id, Sprite nombresprite, Sprite sprite, Action<string> onElegido)
    {
        Id = id;
        if (icono != null) icono.sprite = sprite;
        if (nombre != null) nombre.sprite = nombresprite;

        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(() => onElegido(Id));

        SetSeleccionado(false);
    }

    public void SetSeleccionado(bool seleccionado)
    {
        if (marcoSeleccionado != null) marcoSeleccionado.SetActive(seleccionado);
    }
}
