using UnityEngine;
using UnityEngine.UI;
using System;
public class Emocion : MonoBehaviour
{

    Image emocionImage;


    EmocionInfo thisInfo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        emocionImage = this.GetComponent<Image>();

    }

    // Update is called once per frame
    public void SetCard(EmocionInfo info)
    {
        emocionImage.sprite = info.emocionSprite;
        thisInfo = info;
    }


    public string GetEmocionName()
    {
        return thisInfo.emocionNombre;
    }


}
