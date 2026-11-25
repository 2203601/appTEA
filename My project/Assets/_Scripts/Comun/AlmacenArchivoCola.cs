using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Guarda la cola de sesiones pendientes en un archivo del dispositivo (una sesión por línea).
// Escribe primero a un archivo temporal y después lo reemplaza, para que un cierre brusco no deje el archivo a medias.
public class AlmacenArchivoCola : IAlmacenDeCola
{
    readonly string ruta;

    public AlmacenArchivoCola(string nombreArchivo = "sesiones_pendientes.txt")
    {
        ruta = Path.Combine(Application.persistentDataPath, nombreArchivo);
    }

    public List<string> Cargar()
    {
        try
        {
            if (File.Exists(ruta)) return new List<string>(File.ReadAllLines(ruta));
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo leer la cola de sesiones: " + e.Message);
        }
        return new List<string>();
    }

    public void Guardar(IList<string> lineas)
    {
        try
        {
            string temporal = ruta + ".tmp";
            File.WriteAllLines(temporal, lineas);
            if (File.Exists(ruta)) File.Delete(ruta);
            File.Move(temporal, ruta);
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo guardar la cola de sesiones: " + e.Message);
        }
    }
}
