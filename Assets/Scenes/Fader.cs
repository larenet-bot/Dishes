using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public Image fadeImage;

    public IEnumerator FadeOut(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(0f, 1f, t / duration);
            fadeImage.color = c;

            yield return null;
        }
    }

    public IEnumerator FadeIn(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(1f, 0f, t / duration);
            fadeImage.color = c;

            yield return null;
        }
    }
}