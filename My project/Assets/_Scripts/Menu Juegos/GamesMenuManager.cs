using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera el grid de tarjetas de juego a partir de una lista de GameData,
/// mostrando solo "juegosPorPagina" tarjetas a la vez. Los botones
/// Anterior/Siguiente recorren el resto de la lista.
/// Colocar en un GameObject vacío dentro del Canvas, junto al contenedor
/// que tiene el Grid/Horizontal Layout Group.
/// </summary>
public class GamesMenuManager : MonoBehaviour
{
    [Header("Datos de los juegos (arrastrar los assets GameData)")]
    [SerializeField] private List<GameData> juegos = new List<GameData>();

    [Header("Paginación")]
    [SerializeField] private int juegosPorPagina = 3;
    [SerializeField] private Button botonAnterior;
    [SerializeField] private Button botonSiguiente;

    [Header("Referencias")]
    [SerializeField] private Transform gridContainer;      // objeto con Grid/Horizontal Layout Group
    [SerializeField] private GameCardUI tarjetaPrefab;      // prefab de la tarjeta
    [SerializeField] private DifficultyModalUI modalDificultad;

    private int paginaActual;

    private int TotalPaginas => Mathf.Max(1, Mathf.CeilToInt((float)juegos.Count / juegosPorPagina));

    private void Awake()
    {
        botonAnterior.onClick.AddListener(PaginaAnterior);
        botonSiguiente.onClick.AddListener(PaginaSiguiente);
    }

    private void Start()
    {
        paginaActual = 0;
        MostrarPagina();
    }

    private void PaginaAnterior()
    {
        if (paginaActual <= 0) return;
        paginaActual--;
        MostrarPagina();
    }

    private void PaginaSiguiente()
    {
        if (paginaActual >= TotalPaginas - 1) return;
        paginaActual++;
        MostrarPagina();
    }

    private void MostrarPagina()
    {
        foreach (Transform hijo in gridContainer)
        {
            Destroy(hijo.gameObject);
        }

        int inicio = paginaActual * juegosPorPagina;
        int fin = Mathf.Min(inicio + juegosPorPagina, juegos.Count);

        for (int i = inicio; i < fin; i++)
        {
            GameCardUI tarjeta = Instantiate(tarjetaPrefab, gridContainer);
            tarjeta.Configurar(juegos[i], modalDificultad);
        }

        // ocultamos los botones cuando no hay más páginas para ese lado
        botonAnterior.gameObject.SetActive(paginaActual > 0);
        botonSiguiente.gameObject.SetActive(paginaActual < TotalPaginas - 1);
    }
}
