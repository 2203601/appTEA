using System;
using System.Collections.Generic;

// Métricas de Encajar piezas. Un ítem es cada pieza a colocar: la ronda en Fácil y Medio,
// o cada pieza del rompecabezas en Difícil.
public class MetricasEncajar
{
    // Un toque más corto que esto, que no coloca la pieza, se considera accidental
    public const double SegundosMinimos = 0.15;

    readonly RegistroDeSesion registro;
    readonly HashSet<int> intentados = new HashSet<int>();
    readonly HashSet<int> conPista = new HashSet<int>();
    int items;
    int itemsPrimerIntento;
    int itemsConPista;
    int arrastres;
    double segundosTotales;
    double segundosMaximo;

    public MetricasEncajar(RegistroDeSesion registro)
    {
        this.registro = registro;
        foreach (string clave in new[] { "items", "items_primer_intento", "items_con_pista",
                                         "llegadas", "detenciones_antes", "arrastres" })
            registro.FijarDetalle(clave, 0);
    }

    // Apareció el brillo sobre la pieza de este ítem
    public void RegistrarPista(int item)
    {
        registro.RegistrarPista();
        conPista.Add(item);
    }

    // colocada: quedó en su lugar. error: pieza equivocada o sin encajar. detenida: la soltó antes de llegar.
    public void RegistrarArrastre(int item, double segundos, bool colocada, bool error, bool detenida)
    {
        if (!colocada && segundos < SegundosMinimos) return;

        arrastres++;
        segundosTotales += segundos;
        segundosMaximo = Math.Max(segundosMaximo, segundos);
        registro.FijarDetalle("arrastres", arrastres);
        registro.FijarDetalle("arrastre_promedio_seg", Math.Round(segundosTotales / arrastres, 2));
        registro.FijarDetalle("arrastre_max_seg", Math.Round(segundosMaximo, 2));

        if (error) registro.RegistrarError();
        if (detenida) registro.SumarDetalle("detenciones_antes", 1);

        if (colocada)
        {
            items++;
            if (conPista.Contains(item)) itemsConPista++;
            else if (!intentados.Contains(item)) itemsPrimerIntento++;

            registro.RegistrarAcierto();
            registro.SumarDetalle("llegadas", 1);
            registro.FijarDetalle("items", items);
            registro.FijarDetalle("items_primer_intento", itemsPrimerIntento);
            registro.FijarDetalle("items_con_pista", itemsConPista);
        }

        intentados.Add(item);
    }
}
