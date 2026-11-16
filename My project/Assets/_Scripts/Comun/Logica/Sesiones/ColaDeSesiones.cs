using System;
using System.Collections.Generic;

public class ResumenVaciado
{
    public int Subidas { get; set; }
    public int Rechazadas { get; set; }
    public int Pendientes { get; set; }
    public bool Detenida { get; set; }
    public ResultadoSubida? Motivo { get; set; }
}

// Se envían en orden. Una falla transitoria o de autorización detiene el vaciado; una sesión rechazada se aparta.
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
