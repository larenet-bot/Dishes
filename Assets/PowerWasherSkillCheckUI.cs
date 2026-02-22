using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerWasherSkillCheckUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    [Tooltip("The bar RectTransform. Needle and success zone positions are computed from this.")]
    public RectTransform bar;
    [Tooltip("The moving needle/marker RectTransform.")]
    public RectTransform needle;
    [Tooltip("A RectTransform representing the success window.")]
    public RectTransform successZone;
    public TMP_Text promptText;

    [Header("Tuning")]
    [Tooltip("How long the player has to respond.")]
    public float durationSeconds = 2.0f;
    [Tooltip("Needle speed in bar-widths per second.")]
    public float needleSpeed = 1.6f;
    [Tooltip("Success window width as a fraction of the bar (0-1).")]
    [Range(0.05f, 0.5f)]
    public float successWindowWidth01 = 0.18f;
    [Tooltip("Randomize the success window each time.")]
    public bool randomizeWindow = true;

    [Header("Input")]
    public KeyCode inputKey = KeyCode.Space;

    private Action<bool> onComplete;
    private bool isActive;
    private float timer;

    // Needle movement state
    private float needle01;
    private float dir = 1f;

    // Success window
    private float winStart01;
    private float winEnd01;

    public bool IsActive => isActive;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Begin(Action<bool> onCompleteCallback)
    {
        onComplete = onCompleteCallback;
        isActive = true;
        timer = 0f;
        needle01 = 0f;
        dir = 1f;

        if (promptText != null)
            promptText.text = $"Turbo Jet Skill Check  (Press {inputKey})";

        SetupSuccessWindow();
        ApplyVisuals();

        if (root != null)
            root.SetActive(true);
    }

    public void Cancel()
    {
        if (!isActive) return;
        isActive = false;
        onComplete = null;

        if (root != null)
            root.SetActive(false);
    }

    private void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;
        if (timer >= durationSeconds)
        {
            Resolve(false);
            return;
        }

        // Move needle with bounce.
        needle01 += dir * (needleSpeed * Time.deltaTime);
        if (needle01 >= 1f)
        {
            needle01 = 1f;
            dir = -1f;
        }
        else if (needle01 <= 0f)
        {
            needle01 = 0f;
            dir = 1f;
        }

        ApplyNeedlePosition();

        if (Input.GetKeyDown(inputKey))
        {
            bool success = (needle01 >= winStart01 && needle01 <= winEnd01);
            Resolve(success);
        }
    }

    private void Resolve(bool success)
    {
        isActive = false;

        if (root != null)
            root.SetActive(false);

        var cb = onComplete;
        onComplete = null;
        cb?.Invoke(success);
    }

    private void SetupSuccessWindow()
    {
        float w = Mathf.Clamp01(successWindowWidth01);

        if (!randomizeWindow)
        {
            winStart01 = 0.55f - (w * 0.5f);
            winEnd01 = 0.55f + (w * 0.5f);
        }
        else
        {
            float start = UnityEngine.Random.Range(0.1f, 0.9f - w);
            winStart01 = start;
            winEnd01 = start + w;
        }

        winStart01 = Mathf.Clamp01(winStart01);
        winEnd01 = Mathf.Clamp01(winEnd01);

        ApplySuccessWindowPosition();
    }

    private void ApplyVisuals()
    {
        ApplySuccessWindowPosition();
        ApplyNeedlePosition();
    }

    private void ApplyNeedlePosition()
    {
        if (bar == null || needle == null) return;

        float barWidth = bar.rect.width;
        float x = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, needle01);

        Vector2 p = needle.anchoredPosition;
        p.x = x;
        needle.anchoredPosition = p;
    }

    private void ApplySuccessWindowPosition()
    {
        if (bar == null || successZone == null) return;

        float barWidth = bar.rect.width;
        float xStart = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, winStart01);
        float xEnd = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, winEnd01);

        float mid = (xStart + xEnd) * 0.5f;
        float width = Mathf.Abs(xEnd - xStart);

        Vector2 p = successZone.anchoredPosition;
        p.x = mid;
        successZone.anchoredPosition = p;

        Vector2 size = successZone.sizeDelta;
        size.x = width;
        successZone.sizeDelta = size;
    }
}
