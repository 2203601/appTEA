using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Botón de una temática individual (ej: "Animales"), instanciado
/// dinámicamente dentro del modal de dificultad cuando el juego
/// tiene tieneTematica = true.
/// </summary>
public class TemaButtonUI : MonoBehaviour
{
    [SerializeField] private Image icono;
    [SerializeField] private TMP_Text nombreTexto;
    [SerializeField] private Button boton;
    [SerializeField] private GameObject marcoSeleccionado; // borde/highlight, se prende al elegirlo

    public string Nombre { get; private set; }

    public void Configurar(TemaOpcion tema, Action<string> onElegido)
    {
        Nombre = tema.nombre;
        if (icono != null) icono.sprite = tema.icono;
        if (nombreTexto != null) nombreTexto.text = tema.nombre;

        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(() => onElegido(Nombre));

        SetSeleccionado(false);
    }

    public void SetSeleccionado(bool seleccionado)
    {
        if (marcoSeleccionado != null) marcoSeleccionado.SetActive(seleccionado);
    }
}
