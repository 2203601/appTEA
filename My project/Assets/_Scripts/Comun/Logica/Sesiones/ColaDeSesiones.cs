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
// Guarda el JSON ya armado, no los objetos: así se puede escribir en disco y reenviar tal cual
// (mismos ids, el servidor no duplica). Si hay un almacén, cada cambio se guarda al momento.
public class ColaDeSesiones
{
    class Entrada
    {
        public Guid SesionId;
        public string Payload;
    }

    readonly List<Entrada> pendientes = new List<Entrada>();
    readonly List<Guid> rechazadas = new List<Guid>();
    readonly IAlmacenDeCola almacen;

    public ColaDeSesiones(IAlmacenDeCola almacen = null)
    {
        this.almacen = almacen;
        if (almacen == null) return;

        foreach (string linea in almacen.Cargar())
        {
            int tab = linea.IndexOf('\t');
            if (tab <= 0 || !Guid.TryParse(linea.Substring(0, tab), out Guid id)) continue;   // línea dañada: se descarta
            if (pendientes.Exists(e => e.SesionId == id)) continue;
            pendientes.Add(new Entrada { SesionId = id, Payload = linea.Substring(tab + 1) });
        }
    }

    public int Pendientes => pendientes.Count;
    public IReadOnlyList<Guid> Rechazadas => rechazadas;

    public bool Encolar(Guid perfilId, SesionDeJuego sesion)
    {
        if (ContratoSubida.Validar(perfilId, sesion).Count > 0) return false;
        if (pendientes.Exists(e => e.SesionId == sesion.Id)) return false;

        pendientes.Add(new Entrada { SesionId = sesion.Id, Payload = ContratoSubida.ConstruirPayload(perfilId, sesion) });
        Persistir();
        return true;
    }

    // Para envíos asíncronos (corrutina): se toma la primera pendiente, se envía y se informa el resultado.
    public bool TieneProxima(out string payload)
    {
        payload = pendientes.Count > 0 ? pendientes[0].Payload : null;
        return pendientes.Count > 0;
    }

    // Devuelve true si se puede seguir con la siguiente; false si hay que detenerse (sin red o sin autorización)
    public bool Resolver(ResultadoSubida resultado, ResumenVaciado resumen = null)
    {
        if (pendientes.Count == 0) return false;
        if (resultado == ResultadoSubida.Reintentar || resultado == ResultadoSubida.NoAutorizado)
        {
            if (resumen != null)
            {
                resumen.Detenida = true;
                resumen.Motivo = resultado;
                resumen.Pendientes = pendientes.Count;
            }
            return false;
        }

        Entrada actual = pendientes[0];
        pendientes.RemoveAt(0);
        Persistir();
        if (resultado == ResultadoSubida.Subida)
        {
            if (resumen != null) resumen.Subidas++;
        }
        else
        {
            rechazadas.Add(actual.SesionId);
            if (resumen != null) resumen.Rechazadas++;
        }
        if (resumen != null) resumen.Pendientes = pendientes.Count;
        return true;
    }

    public ResumenVaciado Vaciar(ClienteSubida cliente, string token)
    {
        var resumen = new ResumenVaciado();
        while (TieneProxima(out string payload))
        {
            if (!Resolver(cliente.SubirPayload(payload, token), resumen)) break;
        }
        resumen.Pendientes = pendientes.Count;
        return resumen;
    }

    void Persistir()
    {
        if (almacen == null) return;
        var lineas = new List<string>(pendientes.Count);
        foreach (Entrada e in pendientes) lineas.Add(e.SesionId + "\t" + e.Payload);
        almacen.Guardar(lineas);
    }
}
