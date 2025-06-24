using UnityEngine;
using UnityEngine.UI;

public class DishTestCycle : MonoBehaviour
{
    public Image dishImage;        // Assign this to your UI Image
    public Sprite[] dishStages;    // Drop in Dirty, Soapy, Clean
    private int currentIndex = 0;

    public void CycleDish()
    {
        if (dishStages.Length == 0 || dishImage == null)
        {
            Debug.LogWarning("Missing sprites or image!");
            return;
        }

        currentIndex = (currentIndex + 1) % dishStages.Length;
        dishImage.sprite = dishStages[currentIndex];
        Debug.Log($"Changed to sprite: {dishStages[currentIndex]?.name}");
    }
}
