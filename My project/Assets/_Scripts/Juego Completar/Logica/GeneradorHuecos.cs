using System.Collections.Generic;
using System.Text;

// Clase C# pura (no MonoBehaviour): decide qué letras se ocultan y qué distractores se ofrecen.
public class GeneradorHuecos
{
    public const string VOCALES = "AEIOU";
    public const string CONSONANTES = "CDFGLMNPRST";
    public const string NO_OCULTABLES = "BVZJHÑYKQWX";

    const int MAX_INTENTOS = 20;

    readonly HashSet<string> palabras = new HashSet<string>();
    readonly System.Random azar;

    // diccionario: una palabra por línea; las líneas vacías y las que empiezan con # se ignoran.
    public GeneradorHuecos(string diccionario, System.Random azar = null)
    {
        this.azar = azar ?? new System.Random();

        if (string.IsNullOrEmpty(diccionario)) return;

        foreach (string linea in diccionario.Split('\n'))
        {
            string limpia = linea.Trim();
            if (limpia.Length == 0 || limpia.StartsWith("#")) continue;
            palabras.Add(Normalizar(limpia));
        }
    }

    public bool EsPalabra(string palabra)
    {
        return palabras.Contains(Normalizar(palabra));
    }

    // Mayúsculas y sin tildes (la Ñ se mantiene)
    public static string Normalizar(string texto)
    {
        StringBuilder resultado = new StringBuilder(texto.Length);
        foreach (char c in texto.Trim().ToUpperInvariant())
        {
            switch (c)
            {
                case 'Á': resultado.Append('A'); break;
                case 'É': resultado.Append('E'); break;
                case 'Í': resultado.Append('I'); break;
                case 'Ó': resultado.Append('O'); break;
                case 'Ú':
                case 'Ü': resultado.Append('U'); break;
                default: resultado.Append(c); break;
            }
        }
        return resultado.ToString();
    }

    // Devuelve los huecos ordenados de izquierda a derecha, o null si no encontró una combinación válida.
    public List<Hueco> GenerarHuecosPalabra(string palabra, int cantidadHuecos, int cantidadOpciones)
    {
        if (string.IsNullOrEmpty(palabra) || cantidadHuecos <= 0 || cantidadOpciones < 1) return null;

        palabra = Normalizar(palabra);

        List<int> ocultables = new List<int>();
        for (int i = 0; i < palabra.Length; i++)
        {
            if (VOCALES.IndexOf(palabra[i]) >= 0 || CONSONANTES.IndexOf(palabra[i]) >= 0)
                ocultables.Add(i);
        }
        if (ocultables.Count < cantidadHuecos) return null;

        for (int intento = 0; intento < MAX_INTENTOS; intento++)
        {
            Barajar(ocultables);
            List<int> posiciones = ocultables.GetRange(0, cantidadHuecos);
            posiciones.Sort();

            List<Hueco> huecos = new List<Hueco>();
            foreach (int posicion in posiciones)
            {
                Hueco hueco = CrearHueco(palabra, posicion, cantidadOpciones);
                if (hueco == null) break;
                huecos.Add(hueco);
            }

            if (huecos.Count == cantidadHuecos) return huecos;
        }

        return null;
    }

    // Secuencia ascendente de 4 dígitos consecutivos (sin pasar de 9) con un hueco al azar.
    public (string secuencia, Hueco hueco) GenerarSecuenciaNumeros(int cantidadOpciones)
    {
        int inicio = azar.Next(0, 7);   // 0..6, así el último dígito es como máximo 9
        StringBuilder secuencia = new StringBuilder();
        for (int i = 0; i < 4; i++) secuencia.Append((char)('0' + inicio + i));

        return GenerarSecuenciaNumeros(cantidadOpciones, secuencia.ToString());
    }

    // Igual, pero sobre una secuencia dada (ej: las del banco): elige el hueco y los distractores.
    public (string secuencia, Hueco hueco) GenerarSecuenciaNumeros(int cantidadOpciones, string secuencia)
    {
        int indice = azar.Next(0, secuencia.Length);
        char correcta = secuencia[indice];

        List<char> candidatos = new List<char>();
        for (char digito = '0'; digito <= '9'; digito++)
        {
            if (digito != correcta) candidatos.Add(digito);
        }
        Barajar(candidatos);

        int cantidad = System.Math.Min(System.Math.Max(cantidadOpciones - 1, 0), candidatos.Count);
        string distractores = new string(candidatos.GetRange(0, cantidad).ToArray());

        return (secuencia, new Hueco { indice = indice, correcta = correcta, distractores = distractores });
    }

    Hueco CrearHueco(string palabra, int posicion, int cantidadOpciones)
    {
        char correcta = palabra[posicion];
        string grupo = VOCALES.IndexOf(correcta) >= 0 ? VOCALES : CONSONANTES;

        List<char> candidatos = new List<char>();
        foreach (char letra in grupo)
        {
            if (letra == correcta) continue;
            if (palabras.Contains(Reemplazar(palabra, posicion, letra))) continue;
            candidatos.Add(letra);
        }

        int necesarios = cantidadOpciones - 1;
        if (candidatos.Count < necesarios) return null;

        Barajar(candidatos);
        string distractores = new string(candidatos.GetRange(0, necesarios).ToArray());

        return new Hueco { indice = posicion, correcta = correcta, distractores = distractores };
    }

    static string Reemplazar(string palabra, int posicion, char letra)
    {
        char[] letras = palabra.ToCharArray();
        letras[posicion] = letra;
        return new string(letras);
    }

    void Barajar<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = azar.Next(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
