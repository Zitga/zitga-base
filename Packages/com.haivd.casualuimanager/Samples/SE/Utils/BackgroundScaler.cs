using deVoid.UIFramework;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundScaler : MonoBehaviour
{
    private UIFrame uiFrame;
    private Image background;

    private void Awake()
    {
        uiFrame = UIFrame.Instance;
        background = GetComponent<Image>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(ScreenUtils.IsLandscape)
        {
            FitHorizontalCanvasScaler();
        }
        else
        {
            FitVerticalCanvasScaler();
        }
       
    }

    /// <summary>
    /// try to fit width or height of the screen. It dependens on size of screen and sprite background
    /// </summary>
    void FitHorizontalCanvasScaler()
    {
        var scalerRect = uiFrame.MainCanvasScaler.referenceResolution;
        var spriteRect = background.sprite.rect;
        var spriteRatio = spriteRect.width / spriteRect.height;
        var screenRatio = (float)Screen.width / Screen.height;
        float width, height;
        if (screenRatio > spriteRatio)
        {
            width = scalerRect.x;
            height = scalerRect.x / spriteRatio;
        }
        else
        {
            height = Mathf.CeilToInt(scalerRect.x * screenRatio);
            width = Mathf.CeilToInt(height * spriteRatio);
        }
        background.rectTransform.sizeDelta = new Vector2(width, height);
    }

    /// <summary>
    /// try to fit width or height of the screen. It dependens on size of screen and sprite background
    /// </summary>
    void FitVerticalCanvasScaler()
    {
        var scalerRect = uiFrame.MainCanvasScaler.referenceResolution;
        var spriteRect = background.sprite.rect;
        var spriteRatio = spriteRect.height / spriteRect.width;
        var screenRatio = (float)Screen.height / Screen.width;
        float width, height;
        if (screenRatio > spriteRatio)
        {
            width = scalerRect.x * spriteRatio;
            height = scalerRect.y;
        }
        else
        {
            width = Mathf.CeilToInt(scalerRect.y * screenRatio);
            height = Mathf.CeilToInt(width * spriteRatio);
        }
        background.rectTransform.sizeDelta = new Vector2(width, height);
    }
}
