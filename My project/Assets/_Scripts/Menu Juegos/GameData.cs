using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Nivel de dificultad seleccionable para cualquier minijuego.
/// </summary>
public enum Difficulty
{
    Facil,
    Medio,
    Dificil
}

/// <summary>
/// Una opción de temática dentro de un juego, ej: "Animales", "Frutas", "Países".
/// </summary>
[System.Serializable]
public class TemaOpcion
{
    public string nombre;
    public Sprite icono;
}

/// <summary>
/// Datos de un juego del menú. Creá un asset por cada tarjeta
/// (click derecho en el Project > Create > Mundo de Aprendizaje > Game Data).
/// </summary>
[CreateAssetMenu(fileName = "NewGameData", menuName = "Mundo de Aprendizaje/Game Data")]
public class GameData : ScriptableObject
{
    [Header("Info general")]
    public string gameId;          // identificador único, ej: "memoria"
    public string titulo;          // ej: "Memoria"
    [TextArea] public string descripcion; // ej: "Encuentra los pares"
    public Sprite icono;
    public Color colorTema = Color.white;

    [Header("Temática (opcional, no todos los juegos la usan)")]
    public bool tieneTematica;
    public List<TemaOpcion> tematicas = new List<TemaOpcion>();

    [Header("Escena del minijuego")]
    public string sceneName;       // nombre exacto de la escena a cargar
}
