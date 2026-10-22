using System.Collections.Generic;

public enum ResultadoRespuesta
{
    Acierto,
    Fallo,
    FalloConPista
}

// Reglas de una ronda con varias opciones, compartidas por Emociones y Encajar (Fácil/Medio).
public class RondaDeOpciones
{
    public int FallosParaPista { get; }
    public int Fallos { get; private set; }
    public bool Resuelta { get; private set; }

    public RondaDeOpciones(int fallosParaPista = 2)
    {
        FallosParaPista = fallosParaPista;
    }

    // Los fallos solo se cuentan mientras la ronda no esté resuelta.
    public ResultadoRespuesta Responder(bool esCorrecta)
    {
        if (esCorrecta)
        {
            Resuelta = true;
            return ResultadoRespuesta.Acierto;
        }

        if (!Resuelta) Fallos++;
        return Fallos >= FallosParaPista ? ResultadoRespuesta.FalloConPista : ResultadoRespuesta.Fallo;
    }

    // Si no hay suficientes distractores, devuelve los que haya.
    public static List<T> ElegirOpciones<T>(IList<T> todas, T correcta, int cantidad, System.Random azar)
    {
        List<T> distractores = new List<T>(todas);
        distractores.Remove(correcta);
        Barajar(distractores, azar);

        List<T> opciones = new List<T> { correcta };
        for (int i = 0; i < cantidad - 1 && i < distractores.Count; i++)
        {
            opciones.Add(distractores[i]);
        }
        Barajar(opciones, azar);
        return opciones;
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
