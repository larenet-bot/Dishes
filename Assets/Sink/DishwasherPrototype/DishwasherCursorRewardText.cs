using TMPro;
using UnityEngine;

/// <summary>
/// A TMP reward label over the cursor on the dishwasher menu's Canvas.
/// UI-based so it remains visible above the dishwasher panel even while open.
/// No prefab, texture, or external art is required.
/// </summary>
public class DishwasherCursorRewardText : MonoBehaviour
{
    private RectTransform rect;
    private TMP_Text label;
    private Color startingColor;
    private float age;
    private float duration;

    public static bool Show(Canvas canvas, TMP_Text style, double money, float lifetime = 1.5f)
    {
        if (canvas == null || !canvas.isActiveAndEnabled || money <= 0d) return false;
        GameObject go = new GameObject("Dishwasher_CursorReward",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI), typeof(DishwasherCursorRewardText));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 70f);
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, camera, out Vector2 local))
        {
            rect.anchoredPosition = local + Vector2.up * 38f;
        }

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Overflow;
        text.text = "+ " + BigNumberFormatter.FormatMoney(money);
        text.fontSize = style != null ? Mathf.Max(27f, style.fontSize) : 32f;
        if (style != null)
        {
            text.font = style.font;
            text.color = style.color;
        }
        else text.color = Color.white;
        go.GetComponent<DishwasherCursorRewardText>().Initialize(rect, text, lifetime);
        return true;
    }

    private void Initialize(RectTransform r, TMP_Text t, float seconds)
    {
        rect = r;
        label = t;
        startingColor = label.color;
        duration = Mathf.Max(0.1f, seconds);
    }

    private void Update()
    {
        age += Time.unscaledDeltaTime;
        if (rect != null) rect.anchoredPosition += Vector2.up * (60f * Time.unscaledDeltaTime);
        if (label != null)
        {
            Color c = startingColor;
            c.a = startingColor.a * Mathf.Clamp01(1f - age / duration);
            label.color = c;
        }
        if (age >= duration) Destroy(gameObject);
    }
}
