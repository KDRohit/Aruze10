using System;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.JsonUtil;

namespace Zynga.Zdk
{
    public delegate void BFCallback(Dictionary<string,object> data, string errorMessage);
    
    public class ZyngaCallbackHelper 
    {		
        private static Dictionary<string,object> callbackDictionary = new Dictionary<string, object>();
        private static int id = 1;
        
        public static string AddCallbackObjectToCallbackDictionary(BFCallback callback)
        {
            // not very random... but it doesn't need to be.
            // just needs to be unique
            // and we were seeing duplicates/clashes with the previous impl
            string randomstring = Convert.ToString(id);
            id++;

            callbackDictionary.Add(randomstring,callback);
    
            return randomstring;
        }
    
        public static void HandleCallbacks(string dataJson)
        {
            Dictionary<string,object> responseDictionary = (Dictionary<string,object>)Json.Deserialize(dataJson);
            string callbackKey = null;
            if (responseDictionary.ContainsKey("callbackKey"))
            {
                try
                {
                    callbackKey = responseDictionary["callbackKey"] as String;
                
                    if(!String.IsNullOrEmpty(callbackKey) && callbackDictionary.ContainsKey(callbackKey))
                    {
                        BFCallback callback = callbackDictionary[callbackKey] as BFCallback;
            
                        if (callback != null)
                        {
                            if(responseDictionary.ContainsKey("response"))
                            {
                                Dictionary<string,object> response = responseDictionary["response"] as Dictionary<string,object>;
                                callback(response,null);
                            }
                            else if(responseDictionary.ContainsKey("responseError"))
                            {
                                string errorMessage = responseDictionary["responseError"] as String;

                                if (String.IsNullOrEmpty(errorMessage))
                                {
                                    Debug.LogFormat("PN> HandleCallbacks callbackKey responseError found and is null or empty!");

                                    callback(null,null);
                                }
                                else
                                {
                                    Debug.LogFormat("PN> HandleCallbacks callbackKey responseError = {0}", errorMessage == null ? "null" : errorMessage);

                                    callback(null,Json.Serialize(errorMessage));
                                }
                            }
                        }
                        else
                        {
                            Debug.LogErrorFormat("PN> Callback with the key {0} is not a BFCallback it's a: {1}\n{2}", 
                                callbackKey == null ? "null" : callbackKey, callback == null ? "null" : callbackDictionary[callbackKey].GetType().ToString());
                        }							

                        // remove the callbackobject once we process the callback
                        callbackDictionary.Remove(callbackKey);
                    }
                    else
                    {
                        Debug.LogFormat("PN> CallbackKey {0} not found in callbackDictionary.", callbackKey == null ? "null" : callbackKey);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("PN> ZyngaCallbackHelper.HandleCallbacks failed to extract the callback using the key {0}: {1}\n{2}", 
                        callbackKey == null ? "null" : callbackKey, e.Message, e.StackTrace);
                }
            }
            else
            {
                Debug.LogError("PN> ZyngaCallbackHelper.HandleCallbacks response doesn't contain the callbackKey!");
            }
        }
    }
}
