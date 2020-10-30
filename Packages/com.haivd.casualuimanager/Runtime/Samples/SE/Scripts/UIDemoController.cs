using System;
using deVoid.UIFramework;
using UnityEngine;

namespace SE.UIFramework.Examples
{
    public class UIDemoController : MonoBehaviour
    {
        private UIFrame uiFrame;

        private void Awake() 
        {
            uiFrame = UIFrame.Instance;
        }

        private void Start()
        {
            uiFrame.OpenWindow(ScreenUtils.IsLandscape ? ScreenIds.TapToPlayWindow : ScreenIds.TapToPlayWindowV);
        }

        private void Update()
        {
            
        }

        private void OnDestroy() 
        {

        }
    }
}