using System.Collections.Generic;

public enum ResultadoSeleccion
{
    Ignorada,       // carta ya encontrada, repetida o con el tablero esperando a que se tapen
    PrimeraCarta,
    Par,
    NoPar           // la segunda no coincide: hay que llamar a TaparSeleccion()
}

// Estado del tablero de Memoria. Las cartas se identifican por su posición en el tablero.
public class TableroMemoria
{
    readonly string[] nombres;
    readonly bool[] encontrada;
    readonly List<int> seleccion = new List<int>(2);

    public int Pares => nombres.Length / 2;
    public int Encontrados { get; private set; }
    public bool Terminado => Pares > 0 && Encontrados >= Pares;

    public TableroMemoria(IList<string> nombresPorPosicion)
    {
        nombres = new string[nombresPorPosicion.Count];
        nombresPorPosicion.CopyTo(nombres, 0);
        encontrada = new bool[nombres.Length];
    }

    public bool EstaEncontrada(int posicion) => encontrada[posicion];

    // Cantidad de pares que se pueden armar: lo que pide la dificultad, o menos si no hay suficientes cartas.
    public static int CantidadPares(int paresObjetivo, int cartasDisponibles)
    {
        return System.Math.Max(0, System.Math.Min(paresObjetivo, cartasDisponibles));
    }

    public static List<T> ArmarMazo<T>(IList<T> disponibles, int pares, System.Random azar)
    {
        List<T> elegidas = new List<T>(disponibles);
        Barajar(elegidas, azar);

        int cantidad = CantidadPares(pares, elegidas.Count);
        List<T> mazo = new List<T>(cantidad * 2);
        for (int i = 0; i < cantidad; i++)
        {
            mazo.Add(elegidas[i]);
            mazo.Add(elegidas[i]);
        }
        Barajar(mazo, azar);
        return mazo;
    }

    public ResultadoSeleccion Seleccionar(int posicion)
    {
        if (posicion < 0 || posicion >= nombres.Length) return ResultadoSeleccion.Ignorada;
        if (encontrada[posicion] || seleccion.Contains(posicion) || seleccion.Count >= 2)
            return ResultadoSeleccion.Ignorada;

        seleccion.Add(posicion);
        if (seleccion.Count == 1) return ResultadoSeleccion.PrimeraCarta;

        if (nombres[seleccion[0]] == nombres[seleccion[1]])
        {
            encontrada[seleccion[0]] = true;
            encontrada[seleccion[1]] = true;
            Encontrados++;
            seleccion.Clear();
            return ResultadoSeleccion.Par;
        }
        return ResultadoSeleccion.NoPar;
    }

    public void TaparSeleccion()
    {
        seleccion.Clear();
    }

    // La carta que toca dar vuelta: si ya hay una dada vuelta, su par; si no, cualquiera sin encontrar.
    // -1 si no hay pista que dar (hay dos dadas vuelta o el tablero terminó).
    public int PosicionParaPista()
    {
        if (seleccion.Count >= 2) return -1;

        for (int i = 0; i < nombres.Length; i++)
        {
            if (encontrada[i]) continue;
            if (seleccion.Count == 0) return i;
            if (i != seleccion[0] && nombres[i] == nombres[seleccion[0]]) return i;
        }
        return -1;
    }

    static void Barajar<T>(List<T> lista, System.Random azar)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = azar.Next(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
