using UnityEngine;

[CreateAssetMenu(fileName = "New Situacion Info", menuName = "Scriptable Objects/Situacion Info")]
public class SituacionInfo : ScriptableObject
{
    [TextArea]
    public string descripcion;

    public Sprite situacionSprite;

    [Tooltip("Tiene que matchear con EmocionInfo.emocionNombre")]
    public string emocionCorrecta;
}
