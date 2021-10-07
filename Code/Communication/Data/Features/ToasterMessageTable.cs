using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

namespace Com.HitItRich.Feature.VirtualPets
{
    public class ToasterMessageTable
    {
        private Dictionary<string, List<float>> eventToasterWeightTable = new Dictionary<string, List<float>>();
        private Dictionary<string, List<string>> eventToasterMessageTable = new Dictionary<string, List<string>>();

        private Dictionary<string, List<string>>
            eventTosterMessageStringSub = new Dictionary<string,List<string>>(); //key is eventName_message

        private string getSubstituionKey(string eventName, string message)
        {
            return eventName + "_" + message;
        }

        private string getDataForKey(string key)
        {
            //TODO fill these in with more data
            string data = "";
            switch (key)
            { 
                case "pet_name":
                    data = VirtualPetsFeature.instance.petName;   
                    break;
                case "user_name":
                    data = SlotsPlayer.instance.socialMember.fullName;
                    break;
                default:
                    data = "";
                    break;
            }

            return data;
        }

        private object[] getSubValues(string key)
        {
            object[] returnData = null;
            if (eventTosterMessageStringSub.ContainsKey(key))
            {
                List<string> dataValues = new List<string>();
                foreach (string subString in eventTosterMessageStringSub[key])
                {
                    dataValues.Add(getDataForKey(subString));
                }
                returnData = dataValues.ToArray<object>();
            }

            return returnData;
        }

        public void addMessage(string eventName, string message, float weight, List<string> stringKeySubList = null)
        {
            if (!eventToasterWeightTable.ContainsKey(eventName))
            {
                eventToasterWeightTable[eventName] = new List<float>();
            }
            eventToasterWeightTable[eventName].Add(weight);

            if (!eventToasterMessageTable.ContainsKey(eventName))
            {
                eventToasterMessageTable[eventName] = new List<string>();
            }
            eventToasterMessageTable[eventName].Add(message);
            
            if (stringKeySubList != null)
            {
                string key = getSubstituionKey(eventName, message);
                if (!eventTosterMessageStringSub.ContainsKey(key))
                {
                    eventTosterMessageStringSub[key]  = new List<string>();
                }
                eventTosterMessageStringSub[key] = stringKeySubList;
            }
        }

        public void removeMessage(string eventName, string message)
        {
            List<string> messages = eventToasterMessageTable[eventName];
            for (int index = messages.Count - 1; index >= 0; ++index)
            {
                if (messages[index] == message)
                {
                    eventToasterWeightTable[eventName].RemoveAt(index);
                    eventToasterMessageTable[eventName].RemoveAt(index);
                }
            }
        }

        public void clearMessagesForEvent(string eventName)
        {
            eventToasterWeightTable.Remove(eventName);
        }

        private string getMessage(string eventName, int messageIndex)
        {
            string returnMessage = "";
            if (eventToasterMessageTable.ContainsKey(eventName) &&
                eventToasterMessageTable[eventName].Count > messageIndex)
            {
                string message = eventToasterMessageTable[eventName][messageIndex];
                object[] subs = getSubValues(getSubstituionKey(eventName, message));
                if (subs != null)
                {
                    returnMessage = string.Format(message, subs);
                }
                else
                {
                    returnMessage = Localize.text(message);
                    returnMessage = string.Format(returnMessage, VirtualPetsFeature.instance.petName);
                }
            }

            return returnMessage;
        }

        public string getRandomWeightedMessage(string eventName)
        {
            string returnMessage = "";
            int indexChosen = CommonMath.chooseRandomWeightedValue(eventToasterWeightTable[eventName]); 
            returnMessage = getMessage(eventName, indexChosen);
            return Localize.text(returnMessage);

        }

#if !ZYNGA_PRODUCTION
        public string testForceEvent(string eventName, int messageIndex)
        {
            return getMessage(eventName, messageIndex);
        }

        public string[] testEvents()
        {
            return eventToasterMessageTable.Keys.ToArray();
        }
#endif
        public int eventCount()
        {
            return eventToasterMessageTable.Count;
        }
        
        public int eventMessageCount(string eventName)
        {
            if (eventToasterMessageTable.ContainsKey(eventName))
            {
                return eventToasterMessageTable[eventName].Count;
            }

            return 0;
        }

        public bool hasEvent(string eventName)
            {
                return eventToasterWeightTable.ContainsKey(eventName) && eventToasterMessageTable.ContainsKey(eventName);
            }
        }
}