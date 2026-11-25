using UnityEngine;
using UnityEngine.UI;
using System;
public class Pieza : MonoBehaviour
{
    Image piezaImage;

    PiezaInfo thisInfo;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        piezaImage = this.GetComponent<Image>();
    }

    // Update is called once per frame
    public void SetPieza(PiezaInfo info)
    {
        piezaImage.sprite = info.piezaSprite;
        thisInfo = info;
    }

    public string GetPiezaName()
    {
        return thisInfo.piezaNombre;
    }
}
