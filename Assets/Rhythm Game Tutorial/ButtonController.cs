using UnityEngine;

public class ButtonController : MonoBehaviour
{

    private SpriteRenderer SR;
    public Sprite defaultImage;
    public Sprite pressedImage;

    public KeyCode keypress;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SR = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keypress))
        {
            SR.sprite = pressedImage;
        }

        if (Input.GetKeyUp(keypress))
        {
            SR.sprite = defaultImage;
        }
    }
}
