using UnityEngine;

// Una palabra o secuencia lista para jugar, con su imagen de pista (puede ser null).
public class EjercicioCompletar
{
    public string texto;
    public Sprite pista;
    public bool esNumerico;

    public EjercicioCompletar(string texto, Sprite pista, bool esNumerico)
    {
        this.texto = texto.ToUpperInvariant();
        this.pista = pista;
        this.esNumerico = esNumerico;
    }
}
