using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Guarda las sesiones terminadas en la cola local y las sube cuando se puede.
// Si no hay configuración, token o conexión, las sesiones quedan en el dispositivo y se reintenta más tarde.
public class SubidorDeSesiones : MonoBehaviour
{
    public ConfigSupabase config;
    [Tooltip("Cada cuántos segundos reintenta si quedaron sesiones pendientes")]
    public float segundosEntreReintentos = 60f;

    static SubidorDeSesiones instancia;
    ColaDeSesiones cola;
    bool subiendo;
    float proximoIntento;

    // El inicio de sesión (todavía no implementado) debe completar estos dos datos
    public string TokenUsuario { get; set; }
    public System.Guid PerfilActivo { get; set; }

    public static SubidorDeSesiones Instancia => instancia;
    public int Pendientes => cola != null ? cola.Pendientes : 0;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);
        cola = new ColaDeSesiones(new AlmacenArchivoCola());
    }

    void Update()
    {
        if (cola.Pendientes > 0 && !subiendo && Time.unscaledTime >= proximoIntento) IntentarSubir();
    }

    // Lo llama cada juego al terminar la partida
    public void Registrar(SesionDeJuego sesion)
    {
        if (sesion == null) return;
        if (PerfilActivo == System.Guid.Empty)
        {
            Debug.LogWarning("Sesión no registrada: no hay un perfil activo.");
            return;
        }
        cola.Encolar(PerfilActivo, sesion);
        IntentarSubir();
    }

    public void IntentarSubir()
    {
        proximoIntento = Time.unscaledTime + segundosEntreReintentos;
        if (subiendo || cola.Pendientes == 0) return;
        if (config == null || !config.EstaCompleta || string.IsNullOrEmpty(TokenUsuario)) return;
        StartCoroutine(Vaciar());
    }

    IEnumerator Vaciar()
    {
        subiendo = true;
        string url = config.urlProyecto.TrimEnd('/') + ContratoSubida.Ruta;

        while (cola.TieneProxima(out string payload))
        {
            using (var peticion = new UnityWebRequest(url, "POST"))
            {
                peticion.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                peticion.downloadHandler = new DownloadHandlerBuffer();
                peticion.timeout = 20;
                peticion.SetRequestHeader("Content-Type", "application/json");
                peticion.SetRequestHeader("apikey", config.anonKey);
                peticion.SetRequestHeader("Authorization", "Bearer " + TokenUsuario);

                yield return peticion.SendWebRequest();

                // Sin respuesta del servidor (sin red, tiempo agotado): se reintenta más tarde
                ResultadoSubida resultado = peticion.result == UnityWebRequest.Result.ConnectionError
                    ? ResultadoSubida.Reintentar
                    : ClienteSubida.Clasificar((int)peticion.responseCode);

                if (resultado == ResultadoSubida.Rechazada)
                    Debug.LogWarning("Sesión rechazada por el servidor (" + peticion.responseCode + "): " + peticion.downloadHandler.text);

                if (!cola.Resolver(resultado)) break;
            }
        }
        subiendo = false;
    }
}
