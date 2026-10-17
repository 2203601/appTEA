using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SimboloSprite
{
    public char caracter;
    public Sprite sprite;
}

[Serializable]
public class PalabraConImagen
{
    public string palabra;
    public Sprite imagen;
}

// Todo el contenido del minijuego Completar en un solo asset.
// Se carga con Resources.Load("Completar/BancoCompletar").
// Lo arma el menú appTEA > Completar > 1 - Crear banco y GameData.
[CreateAssetMenu(fileName = "BancoCompletar", menuName = "Scriptable Objects/Banco Completar")]
public class BancoCompletar : ScriptableObject
{
    [Header("Símbolos (un sprite por carácter)")]
    public List<SimboloSprite> numeros = new List<SimboloSprite>();
    public List<SimboloSprite> letras = new List<SimboloSprite>();

    [Header("Nivel 1 - Números (cada dígito es un símbolo, ej: 12345)")]
    public List<string> secuenciasNumeros = new List<string>();

    [Header("Nivel 2 - Palabras con imagen de pista")]
    public List<PalabraConImagen> palabrasConImagen = new List<PalabraConImagen>();

    [Header("Nivel 3 - Palabras largas sin imagen")]
    public List<string> palabrasLargas = new List<string>();

    [NonSerialized] Dictionary<char, Sprite> cache;

    void OnEnable()
    {
        cache = null;
    }

    public Sprite BuscarSprite(char caracter)
    {
        if (cache == null)
        {
            cache = new Dictionary<char, Sprite>();
            foreach (var s in numeros) if (s.sprite != null) cache[char.ToUpperInvariant(s.caracter)] = s.sprite;
            foreach (var s in letras) if (s.sprite != null) cache[char.ToUpperInvariant(s.caracter)] = s.sprite;
        }
        cache.TryGetValue(char.ToUpperInvariant(caracter), out Sprite sprite);
        return sprite;
    }

    // Caracteres que se pueden ofrecer como opciones (solo los que tienen sprite)
    public List<char> CaracteresDisponibles(bool numerico)
    {
        List<char> resultado = new List<char>();
        foreach (var s in numerico ? numeros : letras)
        {
            if (s.sprite != null) resultado.Add(char.ToUpperInvariant(s.caracter));
        }
        return resultado;
    }
}
