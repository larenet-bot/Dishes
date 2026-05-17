using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AchievementPopup : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public CanvasGroup canvasGroup;
    public float showDuration = 3f;
    public float fadeTime = 0.35f;

    public void Show(string title, string description)
    {
        // Auto-bind missing TMP_Text references (helpful if prefab wasn't wired).
        if (titleText == null || descriptionText == null)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts != null && texts.Length > 0)
            {
                if (titleText == null)
                    titleText = texts.Length > 0 ? texts[0] : null;
                if (descriptionText == null)
                    descriptionText = texts.Length > 1 ? texts[1] : (texts.Length > 0 ? texts[0] : null);
            }
        }

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        StopAllCoroutines();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / fadeTime);
                yield return null;
            }
        }

        yield return new WaitForSeconds(showDuration);

        if (canvasGroup != null)
        {
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeTime);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}