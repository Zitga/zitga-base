using UnityEngine;
#if TRACKING_FIREBASE
using Firebase.Analytics;
using Firebase;
using Firebase.Extensions;
using Firebase.Crashlytics;
#endif
using System;

namespace Zitga.TrackingFirebase
{
    public class FirebaseUtils
    {
#if TRACKING_FIREBASE
        DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
#endif
        public bool Initialized { get; private set; }

        private bool isDebugMode = false;
        // When the app starts, check to make sure that we have
        // the required dependencies to use Firebase, and if not,
        // add them if possible.
        public FirebaseUtils(bool isDebugMode)
        {
#if TRACKING_FIREBASE
            Initialized = false;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    // Set default session duration values.
                    FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(0, 30, 0));
                    Initialized = true;
                    this.isDebugMode = isDebugMode;
                    // Init Crashlytics
                    Application.RequestAdvertisingIdentifierAsync(
                       (string advertisingId, bool trackingEnabled, string error) =>
                       {
                       //Log.Info("advertisingId " + advertisingId + " " + trackingEnabled + " " + error);
                       if (trackingEnabled)
                           {
                               Crashlytics.SetCustomKey("advertising_id", advertisingId);
                           }
                       }
                       );
                }
                else
                {
                    Debug.LogError(
                      "Could not resolve all Firebase dependencies: " + dependencyStatus);
                }
            });
#endif
        }

        public void SetUserId(string userId)
        {
            if (isDebugMode)
            {
                Debug.LogWarning("[Firebase] USER_ID : " + userId);
                return;
            }
#if TRACKING_FIREBASE
            FirebaseAnalytics.SetUserId(userId);
#endif
        }
        public void SetUserProperty(string name, string property)
        {
            if (isDebugMode)
            {
                Debug.LogWarning("[Firebase] PROPERTY : NAME " + name + " VALUE:" + property);
                return;
            }
#if TRACKING_FIREBASE
            FirebaseAnalytics.SetUserProperty(name.ToLower(), property.ToLower());
#endif
        }


        /// <summary>
        /// For Custom tracking - Please check tracking exist?
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="agrs"></param>
        public void SetUserEvent(string eventName, string content)
        {
#if TRACKING_FIREBASE
            if (isDebugMode)
            {
                return;
            }
            var arr = content.Split('|');
            if (arr.Length == 2 || arr.Length == 3)
            {
                var category = new Parameter("category", arr[0]);
                var name = new Parameter("name", arr[1]);
                if (arr.Length == 2)
                {
                    //XDebug.Log(string.Format("category: {0}, name: {1}", arr[0], arr[1]));
                    FirebaseAnalytics.LogEvent(eventName, category, name);
                }
                else if (arr.Length == 3)
                {
                    long idLong;
                    long idDouble;
                    if (long.TryParse(arr[2], out idLong))
                    {
                        //XDebug.Log(string.Format("firebase id long: {0}", idLong));
                        FirebaseAnalytics.LogEvent(eventName, category, name, new Parameter("id", idLong));
                    }
                    else if (long.TryParse(arr[2], out idDouble))
                    {
                        //XDebug.Log(string.Format("firebase id double: {0}", idDouble));
                        FirebaseAnalytics.LogEvent(eventName, category, name, new Parameter("id", idDouble));
                    }
                    else
                    {
                        //XDebug.Log(string.Format("firebase id string: {0}", arr[2]));
                        FirebaseAnalytics.LogEvent(eventName, category, name, new Parameter("id", arr[2]));
                    }
                }
            }
            else
            {
                Debug.LogWarning(string.Format("content is invalid: {0}", content));
            }
#endif
        }

    }
}