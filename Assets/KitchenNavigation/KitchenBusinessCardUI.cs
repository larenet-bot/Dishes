using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls one card in the Other Businesses menu.
/// Root object should be the Button/card color.
/// Child image should be the kitchen preview picture.
/// </summary>
public class KitchenBusinessCardUI : MonoBehaviour
{
    [Header("Root Button")]
    [SerializeField] private Button button;

    [Tooltip("Image on the root Button/card. This is only used for card color.")]
    [SerializeField] private Image buttonColorImage;

    [Header("Button State Alpha")]
    [Range(0f, 1f)]
    [SerializeField] private float buttonAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float disabledButtonAlpha = 1f;

    [Header("Kitchen Picture")]
    [Tooltip("Child Image that displays the kitchen/background preview.")]
    [SerializeField] private Image kitchenPictureImage;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text moneyPerSecondText;

    [Header("Locked / Mystery State")]
    [Tooltip("Optional object containing question marks or mystery overlay visuals.")]
    [SerializeField] private GameObject questionMarkRoot;

    private void Reset()
    {
        button = GetComponent<Button>();
        buttonColorImage = GetComponent<Image>();
    }

    public void Initialize(
        string kitchenName,
        Sprite kitchenPicture,
        Color cardColor,
        bool mysteryMode,
        bool statsAvailable,
        float money,
        float moneyPerSecond,
        Action onClick)
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (buttonColorImage == null)
        {
            buttonColorImage = GetComponent<Image>();
        }

        ApplyButtonColors(cardColor);

        if (kitchenPictureImage != null)
        {
            kitchenPictureImage.sprite = kitchenPicture;
            kitchenPictureImage.enabled = kitchenPicture != null;
        }

        if (nameText != null)
        {
            nameText.text = kitchenName;
        }

        if (questionMarkRoot != null)
        {
            questionMarkRoot.SetActive(mysteryMode);
        }

        if (mysteryMode)
        {
            if (moneyText != null)
            {
                moneyText.text = "Money: ???";
            }

            if (moneyPerSecondText != null)
            {
                moneyPerSecondText.text = "Per Second: ???";
            }
        }
        else if (statsAvailable)
        {
            if (moneyText != null)
            {
                moneyText.text = "Money: " + BigNumberFormatter.FormatMoney((double)money);
            }

            if (moneyPerSecondText != null)
            {
                moneyPerSecondText.text = "Per Second: " + BigNumberFormatter.FormatMoney((double)moneyPerSecond);
            }
        }
        else
        {
            if (moneyText != null)
            {
                moneyText.text = "Money: ???";
            }

            if (moneyPerSecondText != null)
            {
                moneyPerSecondText.text = "Per Second: ???";
            }
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick.Invoke());
            }

            button.interactable = true;
        }
    }

    private void ApplyButtonColors(Color baseColor)
    {
        Color fullColor = baseColor;
        fullColor.a = buttonAlpha;

        if (buttonColorImage != null)
        {
            buttonColorImage.color = fullColor;
        }

        if (button == null)
        {
            return;
        }

        Color highlightedColor = fullColor * 1.08f;
        highlightedColor.a = buttonAlpha;

        Color pressedColor = fullColor * 0.88f;
        pressedColor.a = buttonAlpha;

        Color selectedColor = fullColor * 1.04f;
        selectedColor.a = buttonAlpha;

        Color disabledColor = fullColor;
        disabledColor.a = disabledButtonAlpha;

        ColorBlock colors = button.colors;
        colors.normalColor = fullColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = selectedColor;
        colors.disabledColor = disabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;

        button.colors = colors;
    }
}