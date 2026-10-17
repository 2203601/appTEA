using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Va en el prefab BotonOpcion: un botón grande con la letra/número que el chico puede tocar.

[RequireComponent(typeof(Button))]
public class BotonOpcionCompletar : MonoBehaviour
{
    public char Caracter { get; private set; }

    Button boton;
    Image icono;
    CanvasGroup canvasGroup;
    Coroutine latido;

    void Awake()
    {
        boton = GetComponent<Button>();
        icono = transform.Find("Image").GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Sin cambios bruscos de color al tocar: el feedback lo damos nosotros, suave
        boton.transition = Selectable.Transition.None;
    }

    public void Configurar(char caracter, Sprite sprite, Action<BotonOpcionCompletar> alElegir)
    {
        Caracter = caracter;
        icono.sprite = sprite;
        icono.preserveAspect = true;
        boton.onClick.AddListener(() => alElegir(this));
    }

    // Opción equivocada: se apaga despacio, sin ruido ni sacudidas
    public void Atenuar()
    {
        Bloquear();
        StartCoroutine(AnimacionSuave.Alpha(canvasGroup, 0.35f, 0.6f));
    }

    public void Resaltar()
    {
        if (latido == null) latido = StartCoroutine(AnimacionSuave.Latido(transform));
    }

    public void Bloquear()
    {
        canvasGroup.blocksRaycasts = false;
    }
}
