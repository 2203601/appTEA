using System.Collections.Generic;

// Dónde se guarda la cola entre ejecuciones. En Unity es un archivo en el dispositivo.
public interface IAlmacenDeCola
{
    List<string> Cargar();                 // una línea por sesión pendiente; vacío si no hay nada
    void Guardar(IList<string> lineas);    // reemplaza todo el contenido
}
