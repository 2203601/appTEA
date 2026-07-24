using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Botón de opción genérico, reutilizado tanto para elegir dificultad
/// como temática dentro del modal (mismo prefab para las dos listas).
/// Se instancia dinámicamente y avisa con un callback cuándo lo tocan.
/// </summary>
public class OptionButtonUI : MonoBehaviour
{
    [SerializeField] private Image icono;
    [SerializeField] private Image nombre;
    [SerializeField] private Button boton;
    [SerializeField] private GameObject marcoSeleccionado; // borde/highlight, se prende al elegirlo

    /// <summary>Identificador de la opción (ej: "Facil", o el nombre de la temática).</summary>
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
