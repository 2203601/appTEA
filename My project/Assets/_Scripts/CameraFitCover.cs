using UnityEngine;

public class CameraFitCover : MonoBehaviour
{
    public SpriteRenderer background;

    Camera cam;
    int lastWidth, lastHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        ApplyFit();
    }

    void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
            ApplyFit();
    }

    void ApplyFit()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float spriteHeight = background.bounds.size.y;
        float spriteWidth = background.bounds.size.x;

        float screenRatio = (float)Screen.width / Screen.height;
        float spriteRatio = spriteWidth / spriteHeight;

        if (screenRatio >= spriteRatio)
            cam.orthographicSize = spriteWidth / screenRatio / 2f;
        else
            cam.orthographicSize = spriteHeight / 2f;
    }
}