using System;
using UnityEngine;
using Zynga.Zdk;
using System.Collections.Generic;
using System.Collections;
using Zynga.Core.Util;
using Zynga.Core.JsonUtil;
using Zynga.Zdk.Services.Track;
using Zynga.Zdk.Services.Common;

public class ZyngaUnityCallbacks : MonoBehaviour
{
    private const string BUNDLE_MARKER = "Bundle[{";
    private const string PAYLOAD_MARKER = "payload=";
    private const string NOTIFICATION_RECEIVED = "Notification_Received";

    void HandleCallbacks(string dataJson)
    {
        ZyngaCallbackHelper.HandleCallbacks(dataJson);
    }

#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
    
    public static event Action<Dictionary<string, object>> OnLocalNotificationReceived;
    public static void RaiseLocalNotificationReceived(Dictionary<string, object> userInfo)
    { if (OnLocalNotificationReceived != null) OnLocalNotificationReceived(userInfo); }
    
    public static event Action<Dictionary<string, object>> OnPushNotificationReceived;
    public static void RaisePushNotificationReceived(Dictionary<string, object> userInfo)
    { if (OnPushNotificationReceived != null) OnPushNotificationReceived(userInfo); }
    
    // unitySendMessage(gameObject, gameMethod)
    public static string GAME_OBJECT = "ZyngaUnityCallbacks";
    public static string LOCAL_NOTIFICATION = "ReceiveLocalNotification";
    public static DateTime Epoch = Convert.ToDateTime("1/1/1970 0:00:00 AM");
    
    public void ReceiveLocalNotification(string userInfoJson)
    {
        //message is a json string, {"trackerClass":"apple","creativeId":"1_1"}
        Debug.Log("ZyngaUnityCallbacks> Received LN: " + userInfoJson);
        
        try
        {
            Dictionary<string, object> userInfo = DecodeJson(userInfoJson) as Dictionary<string, object>;
            
            // send up the log message
            string subtype = userInfo["subtype"].ToString(); 
            string uniqueId = userInfo["uniqueId"].ToString();      
            string day = userInfo["day"].ToString();
        
			AnalyticsManager.Instance.LogLocalNotificationClicked(subtype, uniqueId, day);

            RaiseLocalNotificationReceived(userInfo);
        }
        catch (Exception e)
        {
            Debug.Log("ZyngaUnityCallbacks> message not Json decodable, exception : " + e);
        }
        
    }
    public void ReceivePushNotification(string userInfoJson)
    {
        //for ios:
        //message is a json string, {"aps":{"alert":"zdkalpha test pn"},"z":"pp123"}
        // keyword "z" matches PushNotification.body.ios.appData
        
        //for android
        /*Bundle[{payload={"title":"this is a title","creativeId":"1_1","android":{"alert":"zdk
                                  alpha test pn1","badge":1},"trackerClass":"apple","sendkey":"611eb58932e470758b1e98f9d48e06fa$$
                                  eiO!UZQZ*2d4Pl4Q57XNY5zX)a8LZZlzSn34.LZZkdF(RPXT)!f4A(FMPV53bjJ2SYGU1YpzBQxMGV37dcN*UVX.XlFx,WS
                                  UOS34e4-q07G7rjw4-q07G7rjw"}, android.support.content.wakelockid=1, collapse_key=com.zynga.coll
                                  apse.1385074654, from=652329355464}]*/
        
        Debug.LogFormat("ZyngaUnityCallbacks> ReceivePushNotification PN Received on C# side with the payload: {0}", userInfoJson);
        Userflows.flowStart(NOTIFICATION_RECEIVED);
        try
        {
            Dictionary<string, object> userInfo = DecodeJson(userInfoJson) as Dictionary<string, object>;

            string origin = userInfo.GetString("origin", "[Origin Key Missing]");
            Debug.LogFormat("PN> OnPushNotificationReceivedn() origin: {0}", origin);

            Dictionary<string, object> cleanedUserInfo = new Dictionary<string, object>();

            // Force all the keys to be lower case
            foreach (string key in userInfo.Keys)
            {
                object value = userInfo.GetObject(key, null);

                if (Data.debugMode)
                {
                    if (value is String)
                    {
                        Debug.LogFormat("PN> OnPushNotificationReceived Payload Key:Value = {0}:{1}", 
                            key, value.ToStringInvariant());                    
                    }
                    else
                    {
                        Debug.LogFormat("PN> OnPushNotificationReceived Payload Key:Value = {0}:[Non-String Object]", 
                            key);                   
                    }
                }

                string newKey = key.ToLower();
                if (value != null && !cleanedUserInfo.ContainsKey(newKey))
                {
                    cleanedUserInfo.Add(newKey, value );
                }
                else
                {
                    Debug.LogErrorFormat("PN> Push Notification from {0} contains the key {1} multiple times!!!", origin, key);
                }
            }

            // Push Notification Tracking
            if (cleanedUserInfo != null && cleanedUserInfo.ContainsKey("sendkey"))
            {
                Taxonomy taxonomy = null;
                if (cleanedUserInfo.ContainsKey("overridekingdom"))
                {
                    taxonomy = new Taxonomy(cleanedUserInfo.GetString("overridekingdom", null),
                        cleanedUserInfo.GetString("overridephylum", null),
                        cleanedUserInfo.GetString("overrideclass", null),
                        cleanedUserInfo.GetString("overridefamily", null),
                        cleanedUserInfo.GetString("overridegenus", null));
                }
                else if (cleanedUserInfo.ContainsKey("overridekey1"))
                {
                    taxonomy = new Taxonomy(cleanedUserInfo.GetString("overridekey1", null),
                        cleanedUserInfo.GetString("overridekey2", null),
                        cleanedUserInfo.GetString("overridekey3", null),
                        cleanedUserInfo.GetString("overridekey4", null),
                        cleanedUserInfo.GetString("overridekey5", null));
                }

                if(taxonomy != null)
                {
                    string snidStringOverride = cleanedUserInfo.GetString("overridefromsn", null);
                    Snid? snidOverride = null;
                    if(!String.IsNullOrEmpty(snidStringOverride) && Enum.IsDefined(typeof(Snid), snidStringOverride))
                    {
                        snidOverride = Enum.Parse(typeof(Snid), snidStringOverride) as Snid?;
                    }

                    string zidStringOverride = cleanedUserInfo.GetString("overridefromzid", null);
                    Zid? zidOverride = null;
                    if (!String.IsNullOrEmpty(zidStringOverride))
                    {
                        zidOverride = new Zid(zidStringOverride);
                    }

                    int? sendId = null;
                    if(cleanedUserInfo.ContainsKey("overrideexternalsendid"))
                    {
                        sendId = cleanedUserInfo.GetInt("overrideexternalsendid", int.MaxValue);
                    }

                    PackageProvider.Instance.Track.Service.LogMessageClick(cleanedUserInfo.GetString("sendkey", null),
                        cleanedUserInfo.GetString("overridechannel", null),
                        taxonomy.Kingdom,
                        taxonomy.Phylum,
                        taxonomy.Class,
                        taxonomy.Family,
                        taxonomy.Genus,
                        cleanedUserInfo.GetString("clicktype1", null),
                        cleanedUserInfo.GetString("clicktype2", null),
                        cleanedUserInfo.GetString("clicktype3", null),
                        taxonomy,
                        sendId,
                        snidOverride,
                        zidOverride,
                        ZdkManager.Instance.Zsession.Snid,
                        ZdkManager.Instance.Zsession.Zid);

                    if (Data.debugMode)
                    {
                        Debug.LogFormat("PN> Push Notification LogMessageClick called with sendkey {0}", 
                            cleanedUserInfo.GetString("sendkey", "Empty"));
                    }
                }
                else
                {
                   PackageProvider.Instance.Track.Service.LogMessageClickSendKey(cleanedUserInfo.GetString("sendkey", null),
                        cleanedUserInfo.GetString("clicktype1", null),
                        cleanedUserInfo.GetString("clicktype2", null),
                        cleanedUserInfo.GetString("clicktype3", null),
                        ZdkManager.Instance.Zsession.Snid,
                        ZdkManager.Instance.Zsession.Zid);
                
                    if (Data.debugMode)
                    {
                        Debug.LogFormat("PN> Push Notification LogMessageClickSendKey called with sendkey {0}", 
                            cleanedUserInfo.GetString("sendkey", "Empty"));
                    }
                }
            }
            else if (Data.debugMode)
            {
                Debug.LogWarningFormat("PN> Push Notification Received that did NOT have a sendKey! origin = {0}", origin);
            }
            string rewardkey = cleanedUserInfo.GetString("rewardkey", null);
            if (!String.IsNullOrEmpty(rewardkey))
            {
                Userflows.addExtraFieldToFlow(NOTIFICATION_RECEIVED, "rewardKey", rewardkey);
                if (Data.debugMode)
                {
                    Debug.LogFormat("PN> Push Notification validateReward with rewardkey {0}", rewardkey);
                }

                RewardAction.validateReward(rewardkey);
            } else {
                Userflows.addExtraFieldToFlow(NOTIFICATION_RECEIVED, "rewardKey", "empty");
            }

            RaisePushNotificationReceived(userInfo);
            Userflows.flowEnd(NOTIFICATION_RECEIVED);
        }
        catch (Exception e)
        {
            Debug.Log("ZyngaUnityCallbacks> message not Json decodable, exception : " + e);
        }
    }
#endif

    public void ReceiveIosFbLaunchUrl(string url)
    {
        Debug.Log("ZyngaUnityCallbacks> ReceiveIosFbLaunchUrl: "+url);
    }

    public void ReceiveLaunchData(string dataString)
    {
        Debug.Log("ZyngaUnityCallbacks> ReceiveLaunchData: "+dataString);
    }

    public static Dictionary<string, object> DecodeJson(string json)
    {
#if UNITY_ANDROID

        // Correct for getting non-json message when all we want is the payload
        // Bundle[{google.sent_time=1471042402104, payload={"rewardkey":"ilink_CeZFAOQFSncBbJLXuxZUXoW3huHMeXBN7rJbTRvMD17qv","sendkey":"c105b6713a2bdf10b412586033fa7615$$5eI4UM38qgq4xWFVSZ!!feJ(PMP,X!3dJ(Q_OU20g7TjJGyNqcDFXj_MVU*3dlK1QZVKkswxx1NUQU423fJ4FZOS03ckxj971K27ehO!U.WT6!ieF3RUU","src":"app_to_user","android":{"alert":"dfasdfsfdf"},"TestVar1":"TestVar1Value","TestVar2":"TestVar2Value"}, from=163313002460, google.message_id=0:1471042402114522%43ba98fb484f467e, android.support.content.wakelockid=1, collapse_key=5003518}]

        int startIndex = json.IndexOf(PAYLOAD_MARKER);
        int payloadStartIndex = startIndex + PAYLOAD_MARKER.Length;

        if(json.Length > 0
            && startIndex >= 0
            && json[payloadStartIndex] == '{')
        {
            int payloadStopIndex = -1;

            // Loop for each { there should be a closing }
            int openBrackets = 0;
            bool isInString = false;
            bool isEscaped = false;
            for( int index = payloadStartIndex; index < json.Length; index ++)
            {
                char currentChar = json[index];

                // Escaped characters in string don't matter to us
                if(isInString && isEscaped)
                {
                    isEscaped = false;
                }
                else if( isInString 
                         && currentChar == '"')
                {
                    isInString = false;
                }
                else if( isInString 
                    && currentChar == '\\')
                {
                        isEscaped = true;
                }
                else if( currentChar == '{' )
                {
                    openBrackets++;
                }
                else if ( currentChar == '}' )
                {
                    openBrackets--;
                    if(openBrackets == 0)
                    {
                        // This is it!
                        payloadStopIndex = index;
                        break;
                    }
                }
            }
                
            if(payloadStopIndex > payloadStartIndex)
            {
                json = json.Substring(payloadStartIndex, payloadStopIndex - payloadStartIndex + 1);
            }
        }
#endif

        return Json.Deserialize(json) as Dictionary<string, object>;
    }

    public static Dictionary<string, object> DecodeBundle(string data)
    {
        /*
            Bundle[{google.sent_time=1475730175046,
                origin=helpshift, 
                cid=ca3c6d99-dae6-4c47-9a98-9fe0ad7eb76d,
                from=716587603114,
                alert=Philip PN Test 10 PM Android -> Conversation,
                hsp.a=4, 
                hsp.d=, 
                google.message_id=0:1475730175052958%a6f3d1e8f9fd7ecd, 
                android.support.content.wakelockid=1, 
                collapse_key=do_not_collapse}]
        */

        data = data.Trim()
            .Replace(BUNDLE_MARKER, "")
            .TrimEnd(new char[] { ']', '}' }); // remove the }] at the end

        string[] parts = data.Split( new char[] { '=', ','}, StringSplitOptions.None );

        Dictionary<string, object> returnResults = new Dictionary<string, object>();
        for (int index = 1; index < parts.Length; index += 2)
        {
            returnResults.Add(parts[index - 1].Trim(), parts[index].Trim());
        }

        return returnResults;
    }
}

