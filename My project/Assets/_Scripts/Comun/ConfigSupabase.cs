using UnityEngine;

// Datos del proyecto de Supabase. Se crea desde Assets > Create > Amancay > Config Supabase y se completa a mano.
// La anon key es pública por diseño (la protección está en las políticas de seguridad de la base),
// pero NUNCA se pone acá la service_role key.
[CreateAssetMenu(menuName = "Amancay/Config Supabase", fileName = "ConfigSupabase")]
public class ConfigSupabase : ScriptableObject
{
    [Tooltip("Ej.: https://xxxx.supabase.co (sin barra al final)")]
    public string urlProyecto;

    [Tooltip("Clave anon / publishable del proyecto. No usar la service_role.")]
    public string anonKey;

    public bool EstaCompleta => !string.IsNullOrWhiteSpace(urlProyecto) && !string.IsNullOrWhiteSpace(anonKey);
}
