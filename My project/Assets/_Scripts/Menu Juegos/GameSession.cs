/// <summary>
/// Guarda qué juego y qué dificultad (y temática, si aplica) se eligieron
/// en el menú, para que la escena del minijuego los lea al arrancar.
/// No se destruye entre escenas porque es una clase estática (no un MonoBehaviour).
/// </summary>
public static class GameSession
{
    public static string GameId { get; private set; }
    public static Difficulty SelectedDifficulty { get; private set; }

    /// <summary>
    /// Nombre de la temática elegida (ej: "Animales"). Queda en null
    /// para los juegos que no usan temática.
    /// </summary>
    public static string SelectedTema { get; private set; }

    public static void SetSelection(string gameId, Difficulty difficulty, string tema = null)
    {
        GameId = gameId;
        SelectedDifficulty = difficulty;
        SelectedTema = tema;
    }
}
