using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WashBasinSoakHUD : MonoBehaviour
{
    public WashBasinSoakTechnique technique;

    [Header("UI")]
    public GameObject root;
    public Image fill;
    public TMP_Text label;

    private void Reset()
    {
        technique = FindFirstObjectByType<WashBasinSoakTechnique>();
    }

    private void Update()
    {
        if (technique == null)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        bool active = technique.HasTechnique();
        if (root != null) root.SetActive(active);
        if (!active) return;

        if (fill != null) fill.fillAmount = technique.Progress01;

        if (label != null)
        {
            if (technique.IsReady)
                label.text = "SOAK READY";
            else
                label.text = $"Soak: {(int)(technique.Progress01 * 100f)}%";
        }
    }
}
