using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Ventana modal (popup) para elegir dificultad (siempre) y, si el juego
/// lo requiere, una temática (no todos los juegos la tienen). Ambas listas
/// se arman en runtime con el mismo prefab (OptionButtonUI). Se confirma
/// con el botón "Jugar". Se activa desde GameCardUI.
/// </summary>
public class DifficultyModalUI : MonoBehaviour
{
    [Header("Referencias generales")]
    [SerializeField] private GameObject panelRaiz;   // panel completo, incluye el fondo oscuro
    [SerializeField] private Image tituloJuego; // el logo/título del juego como sprite, no texto

    [Header("Dificultad (dinámica, siempre visible)")]
    [SerializeField] private Transform dificultadContainer;
    [SerializeField] private OptionButtonUI opcionBotonPrefab; // mismo prefab para dificultad y temática
    [SerializeField] private Sprite iconoFacil;
    [SerializeField] private Sprite iconoMedio;
    [SerializeField] private Sprite iconoDificil;
    [SerializeField] private Sprite nombreFacilSprite;   // la palabra "Fácil" dibujada, ej. cartel/etiqueta
    [SerializeField] private Sprite nombreMedioSprite;
    [SerializeField] private Sprite nombreDificilSprite;

    [Header("Temática (dinámica, solo si el juego la usa)")]
    [SerializeField] private GameObject seccionTematica;      // contenedor a activar/desactivar
    [SerializeField] private Transform tematicaContainer;

    [Header("Otros")]
    [SerializeField] private Button botonJugar;
    [SerializeField] private Button botonCerrar;

    private GameData juegoSeleccionado;
    private Difficulty dificultadSeleccionada;
    private string temaSeleccionado;

    private readonly List<OptionButtonUI> botonesDificultad = new List<OptionButtonUI>();
    private readonly List<OptionButtonUI> botonesTema = new List<OptionButtonUI>();

    private void Awake()
    {
        botonJugar.onClick.AddListener(Confirmar);
        botonCerrar.onClick.AddListener(Cerrar);

        panelRaiz.SetActive(false);
    }

    public void Abrir(GameData juego)
    {
        juegoSeleccionado = juego;

        if (tituloJuego != null) tituloJuego.sprite = juego.titulo;

        GenerarBotonesDificultad();
        dificultadSeleccionada = Difficulty.Facil; // preseleccionada por defecto
        ActualizarSeleccionDificultad();

        temaSeleccionado = null;
        bool tieneTematica = juego.tieneTematica && juego.tematicas.Count > 0;
        seccionTematica.SetActive(tieneTematica);

        if (tieneTematica)
        {
            GenerarBotonesTematica(juego);
            temaSeleccionado = juego.tematicas[0].nombre; // preseleccionada por defecto
            ActualizarSeleccionTematica();
        }

        panelRaiz.SetActive(true);
    }

    private void GenerarBotonesDificultad()
    {
        LimpiarLista(botonesDificultad);

        AgregarBotonDificultad(Difficulty.Facil, nombreFacilSprite, iconoFacil);
        AgregarBotonDificultad(Difficulty.Medio, nombreMedioSprite, iconoMedio);
        AgregarBotonDificultad(Difficulty.Dificil, nombreDificilSprite, iconoDificil);
    }

    private void AgregarBotonDificultad(Difficulty dificultad, Sprite nombreSprite, Sprite icono)
    {
        OptionButtonUI instancia = Instantiate(opcionBotonPrefab, dificultadContainer);
        instancia.Configurar(dificultad.ToString(), nombreSprite, icono, _ => OnDificultadElegida(dificultad));
        botonesDificultad.Add(instancia);
    }

    private void OnDificultadElegida(Difficulty dificultad)
    {
        dificultadSeleccionada = dificultad;
        ActualizarSeleccionDificultad();
    }

    private void ActualizarSeleccionDificultad()
    {
        foreach (OptionButtonUI boton in botonesDificultad)
        {
            boton.SetSeleccionado(boton.Id == dificultadSeleccionada.ToString());
        }
    }

    private void GenerarBotonesTematica(GameData juego)
    {
        LimpiarLista(botonesTema);

        foreach (TemaOpcion tema in juego.tematicas)
        {
            OptionButtonUI instancia = Instantiate(opcionBotonPrefab, tematicaContainer);
            instancia.Configurar(tema.nombre, tema.nombreSprite, tema.icono, OnTemaElegido);
            botonesTema.Add(instancia);
        }
    }

    private void OnTemaElegido(string nombreTema)
    {
        temaSeleccionado = nombreTema;
        ActualizarSeleccionTematica();
    }

    private void ActualizarSeleccionTematica()
    {
        foreach (OptionButtonUI boton in botonesTema)
        {
            boton.SetSeleccionado(boton.Id == temaSeleccionado);
        }
    }

    private void LimpiarLista(List<OptionButtonUI> lista)
    {
        foreach (OptionButtonUI boton in lista)
        {
            Destroy(boton.gameObject);
        }
        lista.Clear();
    }

    public void Cerrar()
    {
        panelRaiz.SetActive(false);
        juegoSeleccionado = null;
    }

    private void Confirmar()
    {
        if (juegoSeleccionado == null) return;

        GameSession.SetSelection(juegoSeleccionado.gameId, dificultadSeleccionada, temaSeleccionado);
        SceneManager.LoadScene(juegoSeleccionado.sceneName);
    }
}