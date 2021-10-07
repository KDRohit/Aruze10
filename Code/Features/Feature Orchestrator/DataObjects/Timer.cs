using System.Collections;
using System.Collections.Generic;

namespace FeatureOrchestrator
{
    public class Timer : BaseDataObject
    {
        protected const string EXPIRATION = "expiration";
        
        public long expiration { get; private set; }
        public GameTimerRange durationTimer { get; private set; }

        public Timer(string keyName, JSON json) : base(keyName, json)
        {
        }
        
        public override void updateValue(JSON json)
        {
            if (json == null)
            {
                return;
            }

            jsonData = json;
            expiration = jsonData.getLong(EXPIRATION, 0);
            if (expiration > 0)
            {
                durationTimer = GameTimerRange.createWithTimeRemaining((int)expiration - GameTimer.currentTime);    
            }
        }
        
        public static ProvidableObject createInstance(string keyname, JSON json)
        {
            return new Timer(keyname, json);
        }
    }
}
