using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;


public class Card : MonoBehaviour
{

    Image cardImage;
    Image backgroundImage;

    public Animator cardAnimator;
    public GameObject background;
    Button cardButton;

    CardInfo thisInfo;


    public static int FLIPPED_CARD;
    public Action<Card> OnSelect;

    public static bool GAME_OVER;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {

        cardImage = this.GetComponent<Image>();
        backgroundImage = this.transform.GetChild(0).GetComponent<Image>();

        cardAnimator = this.GetComponent<Animator>();
        background = this.transform.GetChild(0).gameObject;

        cardButton = this.GetComponent<Button>();


    }

    public void SetCard(CardInfo info)
    {
        cardImage.sprite = info.cardSprite;
        backgroundImage.sprite = info.backgroundSprite;
        thisInfo = info;
    }

    public void SetActiveBackground()
    {

        background.SetActive(!background.activeSelf);
    }

    public void DisableAnimator()
    {
        cardAnimator.enabled = false;
        cardButton.enabled = background.activeSelf;
    }

    public void ActivateAnimator()
    {
        cardAnimator.enabled = true;
    }
    public void SelectCard()
    {
        if (FLIPPED_CARD >= 2 || GAME_OVER)
        {
            return;
        }

        cardAnimator.enabled = true;
        cardButton.enabled = false;
        FLIPPED_CARD++;
        OnSelect(this);
    }

    public string GetCardName()
    {
        return thisInfo.cardName;
    }
}
