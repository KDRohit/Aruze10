using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class TimePeriod : BaseDataObject
    {
        public GameTimerRange durationTimer { get; private set; }

        public TimePeriod(string keyName, JSON json) : base(keyName, json)
        {
            setTimer(json);
        }
        
        public override void updateValue(JSON json)
        {
            if (json == null)
            {
                return;
            }

            setTimer(json);

            if (Data.debugMode)
            {
                jsonData = json;
            }
        }

        private void setTimer(JSON data)
        {
            int startTime = data.getInt("startTime", -1);
            int endTime = data.getInt("endTime", -1);

            if (durationTimer != null && endTime == durationTimer.endTimestamp && startTime == durationTimer.startTimestamp)
            {
                return; //Don't bother updating the timer if the start/end times didn't actually change
            }
            durationTimer = new GameTimerRange(startTime, endTime);
        }
        
        public static ProvidableObject createInstance(string keyname, JSON json)
        {
            return new TimePeriod(keyname, json);
        }
    }
}
