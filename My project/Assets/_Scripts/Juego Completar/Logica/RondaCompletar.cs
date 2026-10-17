using System.Collections.Generic;

// Estado de una ronda: qué huecos tiene la palabra y cuál toca completar.
// Los huecos se completan de a uno, de izquierda a derecha (así los entrega GeneradorHuecos).
public class RondaCompletar
{
    public string Texto { get; private set; }
    public IReadOnlyList<Hueco> Huecos => huecos;

    readonly List<Hueco> huecos;
    readonly System.Random azar;
    int siguiente;

    public RondaCompletar(string texto, List<Hueco> huecos, System.Random azar = null)
    {
        Texto = texto.ToUpperInvariant();
        this.huecos = huecos;
        this.azar = azar ?? new System.Random();
    }

    public bool Terminada => siguiente >= huecos.Count;
    public Hueco HuecoActual => huecos[siguiente];
    public int IndiceHuecoActual => HuecoActual.indice;
    public char CaracterEsperado => HuecoActual.correcta;

    public bool EsHueco(int indice)
    {
        foreach (Hueco hueco in huecos)
        {
            if (hueco.indice == indice) return true;
        }
        return false;
    }

    // La correcta más sus distractores, mezclados
    public List<char> OpcionesDelHuecoActual()
    {
        List<char> opciones = new List<char> { HuecoActual.correcta };
        opciones.AddRange(HuecoActual.distractores);

        for (int i = opciones.Count - 1; i > 0; i--)
        {
            int j = azar.Next(0, i + 1);
            (opciones[i], opciones[j]) = (opciones[j], opciones[i]);
        }
        return opciones;
    }

    public void Avanzar()
    {
        siguiente++;
    }
}
