using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Wrapper class for accessing LiveData values safely (based on ZRuntime.cs)
*/

public class LiveData
{
	private JSON json = null;
	
	public static readonly string[] EMPTY_ARRAY = new string[0];

	public LiveData(JSON json)         { this.json = json; }
	public override string ToString()  { return json.ToString(); }

	public bool     hasKey   (string key)                  { return isValid(null) && json.hasKey(key); }
	public JSON     getJSON  (string key) 				   { return isValid(key) ? json.getJSON (key) : null; }
	public int      getInt   (string key, int defVal)      { return isValid(key) ? json.getInt   (key, defVal) : defVal; }
	public float    getFloat (string key, float defVal)    { return isValid(key) ? json.getFloat (key, defVal) : defVal; }
	public long     getLong  (string key, long defVal)     { return isValid(key) ? json.getLong  (key, defVal) : defVal; }
	public bool     getBool  (string key, bool defVal)     { return isValid(key) ? json.getBool  (key, defVal) : defVal; }
	public string[] getArray (string key, string[] defVal) { return isValid(key) ? json.getStringArray(key)    : defVal; }
	public string   getString(string key, string defVal, string defaultIfEmptyString = "")   { return isValid(key) ? json.getString(key, defVal, defaultIfEmptyString) : defVal; }

	// Adding some overrides for getting a value form liveData that we want to allow to not exist without spamming errors.
	public string   safeGetString(string key, string defVal, string defaultIfEmptyString = "")   { return isValid(key, false) ? json.getString(key, defVal, defaultIfEmptyString) : defVal; }

	// Check for valid json & (optional) livedata key
	// Emit err msg if either missing, returns 'true' if everything okay.
	private bool isValid(string key, bool shouldLogError = true)
	{
		if (json == null || !json.isValid)
		{
			Debug.LogError("LiveData json is null or invalid.");
			return false;
		}

		if (key != null  &&  !json.hasKey(key) && shouldLogError)
		{
			Debug.LogError("LiveData does not contain key: " + key);
			return false;
		}

		// everything's okay
		return true;
	}

	public List<string> keys
	{
		get
		{
			return json.getKeyList();
		}
	}
#if UNITY_EDITOR
	public JSON getAsJSON()
	{
		return json;
	}
#endif
}
