// Stress y esfuerzo sostenido de la lógica de los juegos.
//
//  - STRESS: muchas partidas completas de cada juego, en paralelo en todos los núcleos.
//    Mide partidas por segundo y latencia por partida (p50, p95, p99, máximo).
//  - ESFUERZO SOSTENIDO: los cinco juegos en ronda durante N segundos.
//    Mide si el rendimiento se degrada (partidas por segundo, inicio contra final) y si la memoria crece.
//
// Cada partida verifica sus invariantes (ej.: el tablero de Memoria termina, el trazo se completa):
// una partida que rompe una regla cuenta como error. Los umbrales se pueden ajustar con variables de entorno.
//
// Uso: dotnet run -c Release --project quality/Juegos.Stress -- <carpeta de salida> [segundos de esfuerzo]
using System.Diagnostics;
using System.Text.Json;

var salida = args.Length > 0 ? args[0] : "out/stress";
int segundosSostenido = args.Length > 1 ? int.Parse(args[1]) : Config.Entero("STRESS_SEGUNDOS", 60);
Directory.CreateDirectory(salida);

var u = new Umbrales
{
    PartidasPorEscenario = Config.Entero("STRESS_PARTIDAS", 100_000),
    MinPartidasPorSeg = Config.Real("STRESS_MIN_PARTIDAS_SEG", 2000),
    MaxP99Micros = Config.Real("STRESS_MAX_P99_US", 5000),
    MinRatioDegradacion = Config.Real("STRESS_MIN_RATIO", 0.6),
    MaxCrecimientoMemoriaMB = Config.Real("STRESS_MAX_MEM_MB", 20),
};

Console.WriteLine($"Núcleos: {Environment.ProcessorCount} · partidas por escenario: {u.PartidasPorEscenario} · esfuerzo: {segundosSostenido} s");

var escenarios = Escenarios.Todos;

// ---------- STRESS ----------
var resultadosStress = new List<ResultadoStress>();
foreach (var (nombre, fabrica) in escenarios)
{
    resultadosStress.Add(Stress.Correr(nombre, fabrica, u.PartidasPorEscenario));
    var r = resultadosStress[^1];
    Console.WriteLine($"[stress] {r.Nombre,-28} {r.PartidasPorSeg,10:N0} partidas/s · p99 {r.P99Micros,8:N1} µs · errores {r.Errores}");
}

// ---------- ESFUERZO SOSTENIDO ----------
Console.WriteLine($"[sostenido] corriendo {segundosSostenido} s…");
var sostenido = Sostenido.Correr(escenarios, segundosSostenido);
Console.WriteLine($"[sostenido] {sostenido.PartidasPorSegInicial:N0} → {sostenido.PartidasPorSegFinal:N0} partidas/s (ratio {sostenido.Ratio:F2}) · memoria {sostenido.MemoriaInicialMB:F1} → {sostenido.MemoriaFinalMB:F1} MB (+{sostenido.CrecimientoMB:F1}) · errores {sostenido.Errores}");

// ---------- VEREDICTO ----------
var fallas = new List<string>();
foreach (var r in resultadosStress)
{
    if (r.Errores > 0) fallas.Add($"{r.Nombre}: {r.Errores} partidas rompieron una regla");
    if (r.PartidasPorSeg < u.MinPartidasPorSeg) fallas.Add($"{r.Nombre}: {r.PartidasPorSeg:N0} partidas/s < {u.MinPartidasPorSeg:N0}");
    if (r.P99Micros > u.MaxP99Micros) fallas.Add($"{r.Nombre}: p99 {r.P99Micros:N0} µs > {u.MaxP99Micros:N0} µs");
}
if (sostenido.Errores > 0) fallas.Add($"sostenido: {sostenido.Errores} partidas rompieron una regla");
if (sostenido.Ratio < u.MinRatioDegradacion) fallas.Add($"sostenido: el rendimiento cayó a {sostenido.Ratio:P0} del inicial (mínimo {u.MinRatioDegradacion:P0})");
if (sostenido.CrecimientoMB > u.MaxCrecimientoMemoriaMB) fallas.Add($"sostenido: la memoria creció {sostenido.CrecimientoMB:F1} MB (máximo {u.MaxCrecimientoMemoriaMB:F0} MB)");

var resultado = new
{
    fecha = DateTime.UtcNow.ToString("o"),
    nucleos = Environment.ProcessorCount,
    umbrales = u,
    stress = resultadosStress,
    sostenido,
    fallas,
    ok = fallas.Count == 0,
};
File.WriteAllText(Path.Combine(salida, "resultados.json"),
    JsonSerializer.Serialize(resultado, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

Console.WriteLine(fallas.Count == 0 ? "STRESS: OK" : "STRESS: FALLÓ\n - " + string.Join("\n - ", fallas));
return fallas.Count == 0 ? 0 : 1;

// =====================================================================

static class Config
{
    public static int Entero(string nombre, int porDefecto) =>
        int.TryParse(Environment.GetEnvironmentVariable(nombre), out var v) ? v : porDefecto;

    public static double Real(string nombre, double porDefecto) =>
        double.TryParse(Environment.GetEnvironmentVariable(nombre), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : porDefecto;
}

class Umbrales
{
    public int PartidasPorEscenario { get; set; }
    public double MinPartidasPorSeg { get; set; }
    public double MaxP99Micros { get; set; }
    public double MinRatioDegradacion { get; set; }
    public double MaxCrecimientoMemoriaMB { get; set; }
}

record ResultadoStress(string Nombre, int Partidas, int Errores, double Segundos, double PartidasPorSeg,
                       double P50Micros, double P95Micros, double P99Micros, double MaxMicros);

record ResultadoSostenido(int Segundos, long Partidas, long Errores, double PartidasPorSegInicial, double PartidasPorSegFinal,
                          double Ratio, double MemoriaInicialMB, double MemoriaFinalMB, double CrecimientoMB);

static class Stress
{
    public static ResultadoStress Correr(string nombre, Func<Random, Func<bool>> fabrica, int partidas)
    {
        int hilos = Environment.ProcessorCount;
        int porHilo = partidas / hilos;
        var tiempos = new double[porHilo * hilos];
        int errores = 0;
        double tickAMicros = 1_000_000.0 / Stopwatch.Frequency;

        // calentamiento: que el JIT compile antes de medir
        var calentar = fabrica(new Random(1));
        for (int i = 0; i < 2000; i++) calentar();

        var reloj = Stopwatch.StartNew();
        Parallel.For(0, hilos, new ParallelOptions { MaxDegreeOfParallelism = hilos }, h =>
        {
            var jugar = fabrica(new Random(1000 + h));
            for (int i = 0; i < porHilo; i++)
            {
                long t0 = Stopwatch.GetTimestamp();
                bool ok = jugar();
                tiempos[h * porHilo + i] = (Stopwatch.GetTimestamp() - t0) * tickAMicros;
                if (!ok) Interlocked.Increment(ref errores);
            }
        });
        reloj.Stop();

        Array.Sort(tiempos);
        double Pct(double p) => tiempos[Math.Min(tiempos.Length - 1, (int)(p * tiempos.Length))];
        return new ResultadoStress(nombre, tiempos.Length, errores, reloj.Elapsed.TotalSeconds,
            tiempos.Length / reloj.Elapsed.TotalSeconds, Pct(0.50), Pct(0.95), Pct(0.99), tiempos[^1]);
    }
}

static class Sostenido
{
    public static ResultadoSostenido Correr(List<(string, Func<Random, Func<bool>>)> escenarios, int segundos)
    {
        var azar = new Random(42);
        var jugar = escenarios.Select(e => e.Item2(azar)).ToArray();

        // calentamiento
        for (int i = 0; i < 2000; i++) foreach (var j in jugar) j();

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double memInicial = GC.GetTotalMemory(true) / 1048576.0;

        var porSegundo = new List<long>();
        long total = 0, errores = 0, enVentana = 0;
        var reloj = Stopwatch.StartNew();
        long ventanaHasta = 1000;
        while (reloj.Elapsed.TotalSeconds < segundos)
        {
            foreach (var j in jugar)
            {
                if (!j()) errores++;
                total++; enVentana++;
            }
            if (reloj.ElapsedMilliseconds >= ventanaHasta)
            {
                porSegundo.Add(enVentana);
                enVentana = 0;
                ventanaHasta += 1000;
            }
        }

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double memFinal = GC.GetTotalMemory(true) / 1048576.0;

        // inicio y final = primer y último 10 % de las ventanas de un segundo
        int n = Math.Max(1, porSegundo.Count / 10);
        double inicial = porSegundo.Count == 0 ? 0 : porSegundo.Take(n).Average();
        double final = porSegundo.Count == 0 ? 0 : porSegundo.Skip(porSegundo.Count - n).Average();
        double ratio = inicial == 0 ? 1 : final / inicial;
        return new ResultadoSostenido(segundos, total, errores, inicial, final, ratio, memInicial, memFinal, memFinal - memInicial);
    }
}

static class Escenarios
{
    const string Diccionario = "MASA\nMISA\nPESA\nTESA\nMOSA\nCASA\nCOSA\nGATO\nPATO\nMESA\nPERA\nLUNA\nSOPA\nNOTA\nRATA\n";
    static readonly string[] Animales = { "gato", "perro", "pato", "oveja", "jirafa", "tortuga", "conejo", "chancho", "carpincho" };
    static readonly string[] Emociones = { "alegria", "tristeza", "enojo", "miedo", "sorpresa", "calma" };

    // (nombre, fábrica): la fábrica recibe un Random propio de cada hilo y devuelve una "partida" completa.
    // La partida devuelve true si todas las reglas del juego se cumplieron.
    public static List<(string, Func<Random, Func<bool>>)> Todos => new()
    {
        ("Memoria (8 pares)", Memoria),
        ("Emociones/Encajar (4 opc.)", Opciones),
        ("Completar palabras", Completar),
        ("Línea (trazo recto)", Linea),
        ("Pausa + pista", PausaYPista),
    };

    static Func<bool> Memoria(Random r) => () =>
    {
        var mazo = TableroMemoria.ArmarMazo(Animales, 8, r);
        var tablero = new TableroMemoria(mazo);
        for (int pasos = 0; !tablero.Terminado && pasos < 100_000; pasos++)
        {
            int pos = r.Next(-1, mazo.Count + 1);                 // incluye posiciones fuera del tablero
            if (r.Next(3) == 0) pos = tablero.PosicionParaPista(); // a veces sigue la pista
            var res = tablero.Seleccionar(pos);
            if (res == ResultadoSeleccion.NoPar) tablero.TaparSeleccion();
        }
        return tablero.Terminado && tablero.Encontrados == tablero.Pares;
    };

    static Func<bool> Opciones(Random r) => () =>
    {
        var ronda = new RondaDeOpciones(2);
        string correcta = Emociones[r.Next(Emociones.Length)];
        var opciones = RondaDeOpciones.ElegirOpciones(Emociones, correcta, 4, r);
        bool ok = opciones.Count == 4 && opciones.Contains(correcta) && opciones.Distinct().Count() == 4;
        for (int i = 0; i < 10 && !ronda.Resuelta; i++)
        {
            var res = ronda.Responder(opciones[r.Next(opciones.Count)] == correcta);
            if (res == ResultadoRespuesta.FalloConPista && ronda.Fallos < 2) ok = false;
        }
        if (!ronda.Resuelta) ronda.Responder(true);
        return ok && ronda.Resuelta;
    };

    static Func<bool> Completar(Random r)
    {
        var generador = new GeneradorHuecos(Diccionario, r);
        return () =>
        {
            var huecos = generador.GenerarHuecosPalabra("ELEFANTE", 2, 3);
            if (huecos == null) return false;
            var ronda = new RondaCompletar("elefante", huecos, r);
            int completados = 0;
            while (!ronda.Terminada)
            {
                var opciones = ronda.OpcionesDelHuecoActual();
                if (!opciones.Contains(ronda.CaracterEsperado) || !ronda.EsHueco(ronda.IndiceHuecoActual)) return false;
                ronda.Avanzar();
                completados++;
            }
            return completados == huecos.Count;
        };
    }

    static readonly List<Punto2> Recta = Enumerable.Range(0, 21).Select(i => new Punto2(i * 10f, 0f)).ToList();

    static Func<bool> Linea(Random r) => () =>
    {
        var seguidor = new SeguidorTrazo(Recta, 10f);
        seguidor.Actualizar(Recta[0]);
        for (int i = 1; i < Recta.Count; i++)
        {
            Punto2 a = Recta[i - 1], b = Recta[i];
            for (int k = 1; k <= 4; k++)
                seguidor.Actualizar(new Punto2(a.x + (b.x - a.x) * k / 4f, a.y + (b.y - a.y) * k / 4f + (float)(r.NextDouble() * 2 - 1)));
        }
        return seguidor.Completo;
    };

    static Func<bool> PausaYPista(Random r) => () =>
    {
        var temporizador = new TemporizadorInactividad(20f);
        int disparos = 0;
        for (int cuadro = 0; cuadro < 2000; cuadro++)
        {
            bool pausado = cuadro % 100 < 30;                       // 30 cuadros de cada 100 en pausa
            float dt = ControlPausa.EnPausa(pausado ? 0f : 1f) ? 0f : 0.1f;
            if (temporizador.Avanzar(dt)) disparos++;
            if (cuadro % 700 == 699) temporizador.Reiniciar();
        }
        return disparos == 3;
    };
}
