using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FeatureOrchestrator
{
    public class XPProgressCounter : BaseDataObject
    {
	    protected const string STARTING_VALUE = "startingValue";
	    protected const string CURRENT_VALUE = "currentValue";
	    protected const string COMPLETE_VALUE = "completeValue";
	    protected const string LEVEL_DATA = "levelData";
	    protected const string LEVELS_TO_TARGET = "levelsLeftToTarget";
	    protected const string COMPLETE_LEVEL = "completeLevel";
	    
	    public long startingValue { get; private set; }
	    public long currentValue { get; private set; }
	    public long completeValue { get; private set; }
	    public int levelsLeftToTarget { get; private set; }
	    public int completeLevel { get; private set; }
	    
	    public SortedDictionary<int, long> levelData { get; private set; }
	    
	    public event System.Action valueUpdated;

	    ~XPProgressCounter()
        {
	        Server.unregisterEventDelegate("leveled_up", levelUpEvent, true);
        }

	    public XPProgressCounter(string keyName, JSON json) : base(keyName, json)
        {
	        Server.registerEventDelegate("leveled_up", levelUpEvent, true);
        }

        public override void updateValue(JSON json)
        {
	        if (json == null)
	        {
		        return;
	        }
	        jsonData = json;
	        startingValue = jsonData.getLong(STARTING_VALUE, 0);
	        currentValue = jsonData.getLong(CURRENT_VALUE, 0);
	        completeValue = jsonData.getLong(COMPLETE_VALUE, 0);

	        if (levelData == null)
	        {
		        levelData = new SortedDictionary<int, long>();
	        }
	        else
	        {
		        levelData.Clear();
	        }
	        //Make sure the levels data is stored in a sorted order by level number
	        foreach (var kvp in jsonData.getIntLongDict(LEVEL_DATA))
	        {
		        levelData[kvp.Key] = kvp.Value;
	        }

	        if (levelData.Count > 0)
	        {
		        completeLevel = levelData.Keys.Last();
		        if (completeLevel > SlotsPlayer.instance.socialMember.experienceLevel)
		        { 
			        levelsLeftToTarget = completeLevel - SlotsPlayer.instance.socialMember.experienceLevel;
		        }
		        jsonData.jsonDict[LEVELS_TO_TARGET] = completeLevel;
		        jsonData.jsonDict[COMPLETE_LEVEL] = completeLevel;
	        }
        }

        private void levelUpEvent(JSON data)
        {
	        if (data != null)
	        {
		        int newLevel = data.getInt("level", 0);
		        if (newLevel > 0)
		        {
			        levelsLeftToTarget = completeLevel - newLevel;
			        
			        if (valueUpdated != null)
			        {
				        valueUpdated();
			        }
		        }
	        }
        }
        
        public static ProvidableObject createInstance(string keyname, JSON json)
        {
	        return new XPProgressCounter(keyname, json);
        }
    }
}
