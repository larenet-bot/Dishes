using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Reel : MonoBehaviour
{
    public Sprite[] symbolSprites;
    public Image reelImage;

    public int ResultIndex { get; private set; }
    public bool IsSpinning { get; private set; }

    private void Awake()
    {
        if (reelImage == null)
        {
            reelImage = GetComponent<Image>();
        }
    }

    public IEnumerator SpinReel(float runTime)
    {
        IsSpinning = true;
        float timer = 0;
        while (timer < runTime)
        {
            reelImage.sprite = symbolSprites[Random.Range(0, symbolSprites.Length)];
            timer += 0.05f;
            yield return new WaitForSeconds(0.05f);

        }
        ResultIndex = Random.Range(0, symbolSprites.Length);
        reelImage.sprite = symbolSprites[ResultIndex];
        IsSpinning = false;
    }
}