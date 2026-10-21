using System;
using System.Collections.Generic;

public class ResumenVaciado
{
    public int Subidas { get; set; }
    public int Rechazadas { get; set; }
    public int Pendientes { get; set; }
    public bool Detenida { get; set; }              // se cortó por red caída o falta de autorización
    public ResultadoSubida? Motivo { get; set; }
}

// Cola de sesiones pendientes de subir (funciona sin conexión permanente).
// Se envían en orden. Una falla transitoria o de autorización DETIENE el vaciado y conserva todo.
// Una sesión rechazada de forma definitiva se aparta para que no trabe a las demás.
public class ColaDeSesiones
{
    class Entrada
    {
        public Guid PerfilId;
        public SesionDeJuego Sesion;
    }

    readonly List<Entrada> pendientes = new List<Entrada>();
    readonly List<SesionDeJuego> rechazadas = new List<SesionDeJuego>();

    public int Pendientes => pendientes.Count;
    public IReadOnlyList<SesionDeJuego> Rechazadas => rechazadas;

    // false si la sesión no es válida o ya estaba en la cola (mismo id)
    public bool Encolar(Guid perfilId, SesionDeJuego sesion)
    {
        if (ContratoSubida.Validar(perfilId, sesion).Count > 0) return false;
        if (pendientes.Exists(e => e.Sesion.Id == sesion.Id)) return false;

        pendientes.Add(new Entrada { PerfilId = perfilId, Sesion = sesion });
        return true;
    }

    public ResumenVaciado Vaciar(ClienteSubida cliente, string token)
    {
        var resumen = new ResumenVaciado();
        while (pendientes.Count > 0)
        {
            Entrada actual = pendientes[0];
            ResultadoSubida resultado = cliente.Subir(actual.PerfilId, actual.Sesion, token);

            if (resultado == ResultadoSubida.Reintentar || resultado == ResultadoSubida.NoAutorizado)
            {
                resumen.Detenida = true;
                resumen.Motivo = resultado;
                break;
            }

            pendientes.RemoveAt(0);
            if (resultado == ResultadoSubida.Subida) resumen.Subidas++;
            else
            {
                rechazadas.Add(actual.Sesion);
                resumen.Rechazadas++;
            }
        }
        resumen.Pendientes = pendientes.Count;
        return resumen;
    }
}
