using UnityEngine;

[CreateAssetMenu(fileName = "New Card Info", menuName = "Scriptable Objects/Card Info")]
public class CardInfo : ScriptableObject
{
    public string cardName;
    public Sprite cardSprite;
    public Sprite backgroundSprite;
    public Category cardCategory;
}

public enum Category
{
    comida,
    paises,
    animales
}