using deVoid.UIFramework;
using deVoid.Utils;
using UnityEngine;

public class SafeAreaSignal : ASignal { }

[RequireComponent(typeof(RectTransform))]
public class UISafeArea : MonoBehaviour
{
    /// <summary>
    /// Current orientation of the window. Always set popup orientation at the first time showing popup
    /// </summary>
    private ScreenOrientation orientation;
    /// <summary>
    /// Will reset offset value of RectTransform
    /// </summary>
    private RectTransform rectTransform;

    private UIFrame uiFrame;

    // Start is called before the first frame update
    void Awake()
    {
        uiFrame = UIFrame.Instance;
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (orientation != Screen.orientation)
        {
            SetSafeArea();
        }

        Signals.Get<SafeAreaSignal>().AddListener(SetSafeArea);
    }

    private void OnDisable()
    {
        Signals.Get<SafeAreaSignal>().RemoveListener(SetSafeArea);
    }

    void SetSafeArea()
    {
        orientation = Screen.orientation;

        var safeArea = Screen.safeArea;
        switch (Screen.orientation)
        {
            case ScreenOrientation.LandscapeRight:
                safeArea = new Rect(0, 0, safeArea.width, safeArea.height);
                break;
            case ScreenOrientation.LandscapeLeft:
                safeArea = new Rect(safeArea.x, 0, safeArea.width, safeArea.height);
                break;
            case ScreenOrientation.Portrait:
                safeArea = new Rect(0, 0, safeArea.width, safeArea.height);
                break;
            case ScreenOrientation.PortraitUpsideDown:
                safeArea = new Rect(0, safeArea.y, safeArea.width, safeArea.height);
                break;
            default:
                throw new System.Exception("Orientation is not support now" + Screen.orientation.ToString());
        }

        var scalerRect = uiFrame.MainCanvasScaler.referenceResolution;
        var scalerRatio = scalerRect.x / scalerRect.y;
        var screenRatio = (float)Screen.width / Screen.height;
        
        Vector2 resolution;
        if(screenRatio > scalerRatio)
        {
            resolution = new Vector2(scalerRect.y * screenRatio, scalerRect.y);
        }
        else
        {
            resolution = new Vector2(scalerRect.x, scalerRect.x / screenRatio);
        }

        rectTransform.offsetMin = new Vector2((safeArea.x / Screen.width) * resolution.x, (safeArea.y / Screen.height) * resolution.y);
        rectTransform.offsetMax = new Vector2((safeArea.width / Screen.width - 1) * resolution.x, (safeArea.height / Screen.height - 1) * resolution.y);
    }
}
