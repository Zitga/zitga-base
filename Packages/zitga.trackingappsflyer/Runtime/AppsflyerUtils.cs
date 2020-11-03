using System.Collections.Generic;
#if TRACKING_APPSFLYER
using AppsFlyerSDK;
#endif
#if UNITY_IOS
using UnityEngine;
#endif

public class AppsflyerUtils
{
    public bool tokenSent = false;
    public AppsflyerUtils(string appsflyerDevKey, string appId, bool isDebugMode)
    {
#if TRACKING_APPSFLYER
        AppsFlyer.setIsDebug(isDebugMode);
        AppsFlyer.anonymizeUser(isDebugMode);
        AppsFlyer.initSDK(appsflyerDevKey, appId);
        AppsFlyer.startSDK();

#if UNITY_ANDROID
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
#endif

#if UNITY_IOS
        UnityEngine.iOS.NotificationServices.RegisterForNotifications(UnityEngine.iOS.NotificationType.Alert | UnityEngine.iOS.NotificationType.Badge | UnityEngine.iOS.NotificationType.Sound);
#endif
#endif
    }

    public void TrackingEvent(string eventName, Dictionary<string, string> eventValues)
    {
#if TRACKING_APPSFLYER
        AppsFlyer.sendEvent(eventName, eventValues);
#endif
    }


    public void Update()
    {
#if UNITY_IOS && TRACKING_APPSFLYER
        if (!tokenSent)
        { // tokenSent needs to be defined somewhere (bool tokenSent = false)
            byte[] token = UnityEngine.iOS.NotificationServices.deviceToken;
            if (token != null)
            {
                AppsFlyeriOS.registerUninstall(token);
                tokenSent = true;
            }
        }
#endif
    }

    public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
#if UNITY_ANDROID && TRACKING_APPSFLYER
        AppsFlyerAndroid.updateServerUninstallToken(token.Token);
#endif
    }



    public string GetAppsflyerId()
    {
#if TRACKING_APPSFLYER
        return AppsFlyer.getAppsFlyerId();
#else
        return "";
#endif
    }

}
