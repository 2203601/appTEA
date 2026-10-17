using UnityEngine;
using System.Collections.Generic;

public enum Difficulty
{
    Facil,
    Medio,
    Dificil
}


[System.Serializable]
public class TemaOpcion
{
    public string nombre;
    public Sprite icono;
    public Sprite nombreSprite;

    
}

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
