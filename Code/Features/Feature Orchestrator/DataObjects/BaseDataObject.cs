using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
    public abstract class BaseDataObject : ProvidableObject
    {
        public abstract void updateValue(JSON json);
        
        public BaseDataObject(string keyName, JSON json) : base(keyName, json)
        {
        }
        
        public BaseDataObject() : base()
        {
        }

        public virtual bool tryReplaceString(string propertyKey, out string result)
        {
            result = "";
            if (jsonData.jsonDict.TryGetValue(propertyKey, out object val))
            { 
                result = System.Convert.ToString(val);
                return true;
            }

            return false;
        }
    }
}
