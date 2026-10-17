using System;
using System.Collections.Generic;
using UnityEngine;

public enum DificultadLinea
{
    Facil,
    Medio,
    Dificil
}

// Todo el contenido del minijuego Línea en un solo asset.
// Se carga con Resources.Load("Linea/BancoLinea").
// Lo arma el menú appTEA > Linea > 1 - Crear banco.
[CreateAssetMenu(fileName = "BancoLinea", menuName = "Scriptable Objects/Banco Linea")]
public class BancoLinea : ScriptableObject
{
    [Header("Figuras (trazos simples, letras y números)")]
    public List<FiguraLinea> figuras = new List<FiguraLinea>();

    [Header("Palabras (se arman con las letras del banco)")]
    public List<string> palabras = new List<string>();

    [Header("Tolerancia: fracción del tamaño de UNA figura (en palabras, de cada letra)")]
    [Range(0.01f, 0.5f)] public float toleranciaFacil = 0.20f;
    [Range(0.01f, 0.5f)] public float toleranciaMedio = 0.12f;
    [Range(0.01f, 0.5f)] public float toleranciaDificil = 0.06f;

    [Tooltip("Piso de la tolerancia en píxeles, para que las letras chicas (palabras) sigan siendo usables con el dedo")]
    public float toleranciaMinimaPx = 40f;

    [Header("Sprites")]
    public Sprite estrella;
    public Sprite flecha;                // apunta a la derecha

    [Header("Opcionales (todavía no existen)")]
    public Sprite pictograma;
    public Sprite botonRepetir;
    public Sprite botonSiguiente;
    public Sprite fondo;

    [NonSerialized] Dictionary<string, FiguraLinea> cache;

    void OnEnable()
    {
        cache = null;
    }

    public float Tolerancia(DificultadLinea dificultad)
    {
        switch (dificultad)
        {
            case DificultadLinea.Medio: return toleranciaMedio;
            case DificultadLinea.Dificil: return toleranciaDificil;
            default: return toleranciaFacil;
        }
    }

    // null si la letra no está en el banco
    public FiguraLinea FiguraDeLetra(char letra)
    {
        return Buscar(CategoriaLinea.Letra, char.ToUpperInvariant(letra).ToString());
    }

    public FiguraLinea FiguraDeNumero(char numero)
    {
        return Buscar(CategoriaLinea.Numero, numero.ToString());
    }

    public FiguraLinea FiguraSimple(string nombre)
    {
        return Buscar(CategoriaLinea.TrazoSimple, nombre);
    }

    // Letras de la palabra que no tienen figura en el banco (sin repetir)
    public List<char> LetrasFaltantes(string palabra)
    {
        List<char> faltantes = new List<char>();
        foreach (char c in palabra.ToUpperInvariant())
        {
            if (FiguraDeLetra(c) == null && !faltantes.Contains(c)) faltantes.Add(c);
        }
        return faltantes;
    }

    FiguraLinea Buscar(CategoriaLinea categoria, string nombre)
    {
        if (cache == null)
        {
            cache = new Dictionary<string, FiguraLinea>();
            foreach (var f in figuras)
                cache[f.categoria + ":" + f.nombre.ToUpperInvariant()] = f;
        }
        cache.TryGetValue(categoria + ":" + nombre.ToUpperInvariant(), out FiguraLinea figura);
        return figura;
    }
}
