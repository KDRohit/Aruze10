using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Google.Apis.Json;

/*
This class acts as a wrapper for a JSON parser (currently Google's opensource one)

The "key" input argument is always dotted notation of the data hierarchy, such as "data.player.name".

----------------------------------------------------------------------------------------------------------	
Properties
----------------------------------------------------------------------------------------------------------	

	bool	isValid

----------------------------------------------------------------------------------------------------------	
Get single values
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	JSON			getJSON				(string key);
	int				getInt				(string key, int default);
	long			getLong				(string key, long default);
	float			getFloat			(string key, float default);
	string			getString			(string key, string default);
	bool			getBool				(string key, bool default);
	
STATIC METHODS:
	JSON			getJSONStatic		(JSON json, string key);
	int				getIntStatic		(JSON json, string key, int default);
	long			getLongStatic		(JSON json, string key, long default);
	float			getFloatStatic		(JSON json, string key, float default);
	string			getStringStatic		(JSON json, string key, string default);
	bool			getBoolStatic		(JSON json, string key, bool default);

----------------------------------------------------------------------------------------------------------	
Get one-dimensional arrays
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	JSON[]			getJsonArray			(string key)
	string[]		getStringArray			(string key)
	int[]			getIntArray				(string key)
	long[]			getLongArray			(string key)
	float[]			getFloatArray			(string key)
	List<string>	getKeyList				()
	List<JSON>		getRetardedJsonList		(string key)

STATIC METHODS:
	JSON[]			getJsonArrayStatic		(JSON json, string key)
	string[]		getStringArrayStatic	(JSON json, string key)
	int[]			getIntArrayStatic		(JSON json, string key)

----------------------------------------------------------------------------------------------------------	
Get two-dimensional arrays
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	List<List<int>>		getIntListList			(string key)
	List<List<string>>	getStringListList		(string key)

STATIC METHODS:
	List<List<string>>	getStringListListStatic	(JSON json, string key)

----------------------------------------------------------------------------------------------------------	
Get dictionaries
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	Dictionary<int, string>		getIntStringDict	(string key)
	Dictionary<int, int>		getIntIntDict		(string key)
	Dictionary<string, string>	getStringStringDict	(string key)
	Dictionary<string, int>		getStringIntDict	(string key)
	Dictionary<string, long>	getStringLongDict	(string key)
	Dictionary<string, JSON>	getStringJSONDict	(string key)

STATIC METHODS:
	Dictionary<int, string>		getIntStringDictStatic		(JSON json, string key)
	Dictionary<int, int>		getIntIntDictStatic			(JSON json, string key)
	Dictionary<string, string>	getStringStringDictStatic	(JSON json, string key)
	Dictionary<string, int>		getStringIntDictStatic		(JSON json, string key)
	Dictionary<string, long>	getStringLongDictStatic		(JSON json, string key)
	Dictionary<string, JSON>	getStringJSONDictStatic		(JSON json, string key)

----------------------------------------------------------------------------------------------------------	
Other functionality
----------------------------------------------------------------------------------------------------------	

INSTANCE METHODS:
	bool		hasKey				(string key)
	string		ToString			()
	JSON	 	getDiff				(JSON mine, JSON theirs)

STATIC METHODS:
	string		createJsonString	(string name, object value)
	string		sanitizeString		(string input)

*/
public class JSON
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Properties
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public bool isValid { get; private set; }

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Constructors
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public JSON(string jsonString)
	{		
		if (jsonString.IsNullOrWhiteSpace())
		{
			string stack = UnityEngine.StackTraceUtility.ExtractStackTrace().Replace("\n", " -> ");
			Debug.LogError("Null or empty JSON sent. Stack: " + stack);
			isValid = false;
			return;
		}
		
		try
		{
			jsonDict = JsonReader.Parse(jsonString) as JsonDictionary;
			
			if(jsonDict == null)
			{
				Debug.LogError("Invalid JSON: " + jsonString);
				isValid = false;
				return;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError("Bad JSON string in JSON constructor: " + jsonString);
			Debug.LogException(e);
			isValid = false;
			return;
		}

		// Please never ever commit this line uncommented!
		//Debug.Log ("Just parsed json string " + jsonString);

		isValid = true;
	}
	
	// Construct JSON from custom byte array (uses JsonBinaryReader)
	public JSON(byte[] bytes)
	{
		if (bytes == null || bytes.Length == 0)
		{
			Debug.LogError("Null or empty bytes sent");
			isValid = false;
			return;
		}
		
		try
		{
			var reader = new JsonBinaryReader();
			jsonDict = reader.binaryToJson(bytes);
			
			if(jsonDict == null)
			{
				Debug.LogError("Invalid JSON byte array");
				isValid = false;
				return;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError("Bad JSON byte array in JSON constructor");
			Debug.LogException(e);
			isValid = false;
			return;
		}
		
		isValid = true;
	}	

	// This constructor is only used internally,
	// since we don't use JsonDictionary objects outside of this class.
	private JSON(JsonDictionary jsonDict)
	{
		this.jsonDict = jsonDict;
		isValid = true;
	}
	
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Create custom byte array representation of current json
	////////////////////////////////////////////////////////////////////////////////////////////////////
	
	public enum Format
	{
		UTF8,									// UTF8 is smaller...
		UNICODE									// Unicode is faster
	}
	
	public byte[] toBinary(Format format = Format.UTF8, string extraInfoString = "")
	{
		var writer = new JsonBinaryWriter();
		return writer.jsonToBinary(this.jsonDict, format, extraInfoString);
	}
	
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Get single values
	////////////////////////////////////////////////////////////////////////////////////////////////////

	// Static function for use with generic calling
	// Public method to simply get a sub-JSON object from the JSON object.
	public static JSON getJSONStatic(JSON json, string key)
	{
		return json.getJSON(key);
	}

	// Public method to simply get a sub-JSON object from the JSON object.
	public JSON getJSON(string key)
	{
		object obj = getObject(key);

		if (obj is JsonDictionary)
		{
			return new JSON((JsonDictionary)obj);
		}

		return null;
	}

	// Helper method to allow for generic calls
	// Public method to simply get string data from the JSON object.
	public static string getStringStatic(JSON json, string key, string defaultVal)
	{
		return json.getString(key, defaultVal);
	}

	// Public method to simply get string data from the JSON object.
	public string getString(string key, string defaultVal, string defaultValIfEmptyString = "")
	{
		object obj = getObject(key);

		if (obj is string)
		{
			string text = (string)obj;
			if (text == "")
			{
				return defaultValIfEmptyString;
			}
			else
			{
				return (string)obj;
			}
		}
		else if (obj is bool)
		{
			return ((bool)obj).ToString();
		}

		// This value should only be returned if the value is null or the key doesn't exist.
		return defaultVal;
	}

	// Helper method to allow for generic calls
	// Public method to simply get int data from the JSON object.
	public static int getIntStatic(JSON json, string key, int defaultVal)
	{
		return json.getInt(key, defaultVal);
	}

	// Public method to simply get int data from the JSON object.
	public int getInt(string key, int defaultVal)
	{
		object obj = getObject(key);

		if (obj is string)
		{
			string str = (string)obj;
			if (str.Contains('.'))
			{
				// If there is a decimal point, only use the part before it,
				// since int.TryParse() fails on decimal strings.
				str = str.Substring(0, str.IndexOf('.'));
			}
			int value;
			if (int.TryParse(str, out value))
			{
				return value;
			}
		}

		return defaultVal;
	}

	// Helper method to allow for generic calls
	// Public method to simply get long data from the JSON object.
	public static long getLongStatic(JSON json, string key, long defaultVal)
	{
		return json.getLong(key, defaultVal);
	}

	// Public method to simply get long data from the JSON object.
	public long getLong(string key, long defaultVal)
	{
		object obj = getObject(key);

		if (obj is string)
		{
			long value;
			if (long.TryParse((string)obj, out value))
			{
				return value;
			}
			else
			{
				// this may be using exponential notation so try to convert it
				double exponentValue;

				if (double.TryParse((string)obj, System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out exponentValue))
				{
					try 
					{
						value = System.Convert.ToInt64(exponentValue);
						return value;
					}
					catch (System.OverflowException)
					{
						Debug.LogError("JSON.getLong() - Decimal value that was in exponential notation of a long was too large to be converted into a long!");
					}
				}
			}
		}

		return defaultVal;
	}

	// Helper method to allow for generic calls
	// Public method to simply get boolean data from the JSON object.
	public static bool getBoolStatic(JSON json, string key, bool defaultVal)
	{
		return json.getBool(key, defaultVal);
	}

	// Public method to simply get boolean data from the JSON object.
	public bool getBool(string key, bool defaultVal)
	{
		object obj = getObject(key);

		if (obj is bool)
		{
			return (bool)obj;
		}
		else if (obj is string)
		{
			string s = (string)obj;
			if (s.Length > 0)
			{
				switch (s[0])
				{
					case '0':
					case 'f':
					case 'F':
						return false;

					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case 't':
					case 'T':
						return true;
				}
			}
		}

		return defaultVal;
	}

	// Helper method to allow for generic calls
	// Public method to simply get float data from the JSON object.
	public static float getFloatStatic(JSON json, string key, float defaultVal)
	{
		return json.getFloat(key, defaultVal);
	}

	// Public method to simply get float data from the JSON object.
	public float getFloat(string key, float defaultVal)
	{
		object obj = getObject(key);

		if (obj is string)
		{
			float value;
			if (float.TryParse((string)obj, out value))
			{
				return value;
			}
		}

		return defaultVal;
	}

	// Public method to simply get double data from the JSON object.
	public double getDouble(string key, double defaultVal)
	{
		object obj = getObject(key);

		if (obj is string)
		{
			double value;
			if (double.TryParse((string)obj, out value))
			{
				return value;
			}
		}

		return defaultVal;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Get one-dimensional arrays
	////////////////////////////////////////////////////////////////////////////////////////////////////

	// Static helper for generic calls
	// Gathers an array of sub-JSON objects from the JSON
	public static JSON[] getJsonArrayStatic(JSON json, string key)
	{
		return json.getJsonArray(key);
	}

	// Gathers an array of sub-JSON objects from the JSON
	public JSON[] getJsonArray(string key, bool returnNullIfMissing=false)
	{
		List<object> list = getObject(key) as List<object>;

		if (list == null)
		{
			// normally a new empty JSON array is returned to avoid NREs in caller code.
			// but some callers may want to distinguish between a missing section and an empty section for error-reporting purposes
			if (returnNullIfMissing)
				return null;
			
			return new JSON[0];
		}

		JSON[] data = new JSON[list.Count];
		int i = 0;

		foreach (object obj in list)
		{
			if (obj is JsonDictionary)
			{
				data[i] = new JSON((JsonDictionary)obj);
			}
			else
			{
				data[i] = null;
			}
			i++;
		}

		return data;
	}

	// Helper function to allow generic calls
	// Gathers a string array from the JSON
	public static string[] getStringArrayStatic(JSON json, string key)
	{
		return json.getStringArray(key);	
	}

	// Gathers a string array from the JSON
	public string[] getStringArray(string key)
	{
		List<object> list = getObject(key) as List<object>;

		if (list == null)
		{
			return new string[0];
		}

		string[] data = new string[list.Count];
		int i = 0;

		foreach (object obj in list)
		{
			if (obj is int)
			{
				data[i] = ((int)obj).ToString();
			}
			else if (obj is bool)
			{
				data[i] = ((bool)obj).ToString();
			}
			else if (obj is float)
			{
				data[i] = ((float)obj).ToString();
			}
			else if (obj is string)
			{
				data[i] = (string)obj;
			}
			else
			{
				data[i] = "";
			}
			i++;
		}

		return data;
	}

	// Static helper function to allow it to work with generic function references
	public static int[] getIntArrayStatic(JSON json, string key)
	{
		return json.getIntArray(key);
	}

	// Gathers an integer array from the JSON
	public int[] getIntArray(string key)
	{
		List<object> list = getObject(key) as List<object>;

		if (list == null)
		{
			return new int[0];
		}

		int[] data = new int[list.Count];
		int i = 0;

		foreach (object obj in list)
		{
			if (obj is string)
			{
				int value;
				if (int.TryParse((string)obj, out value))
				{
					data[i] = value;
				}
				else
				{
					data[i] = 0;
				}
			}
			else if (obj is bool)
			{
				data[i] = (bool)obj ? 1 : 0;
			}
			else
			{
				data[i] = 0;
			}
			i++;
		}

		return data;
	}
	
	// Gathers an array of long ints from the JSON
	public long[] getLongArray(string key)
	{
		List<object> list = getObject(key) as List<object>;

		if (list == null)
		{
			return new long[0];
		}

		long[] data = new long[list.Count];
		int i = 0;

		foreach (object obj in list)
		{
			if (obj is string)
			{
				long value;
				if (long.TryParse((string)obj, out value))
				{
					data[i] = value;
				}
				else
				{
					data[i] = 0;
				}
			}
			else if (obj is bool)
			{
				data[i] = (bool)obj ? 1L : 0L;
			}
			else
			{
				data[i] = 0L;
			}
			i++;
		}

		return data;
	}

	// Gathers an float array from the JSON
	public float[] getFloatArray(string key)
	{
		List<object> list = getObject(key) as List<object>;

		if (list == null)
		{
			return new float[0];
		}

		float[] data = new float[list.Count];
		int i = 0;

		foreach (object obj in list)
		{
			if (obj is string)
			{
				float value;
				if (float.TryParse((string)obj, out value))
				{
					data[i] = value;
				}
				else
				{
					data[i] = 0f;
				}
			}
			else if (obj is bool)
			{
				data[i] = (bool)obj ? 1f : 0f;
			}
			else
			{
				data[i] = 0f;
			}
			i++;
		}

		return data;
	}
	
	// Returns all the keys that are in the current JSON object.
	public List<string> getKeyList()
	{
		return new List<string>(jsonDict.Keys);
	}
	
	/// <summary>
	/// // Gets a list of JSON objects that are sub-objects of the given key,
	/// but are keyed themselves on data that we don't know in advance,
	/// so we must get the key list first and build it the hard way.
	/// I strongly recommend against this json format but backend doesn't listen to my advice.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public List<JSON> deprecatedGetKeyedJSONList(string key)
	{
		List<JSON> list = new List<JSON>();
		JSON json = getJSON(key);
		if (json != null)
		{
			List<string> keys = json.getKeyList();
			foreach (string subKey in keys)
			{
				list.Add(json.getJSON(subKey));
			}
		}
		return list;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Get two-dimensional arrays
	////////////////////////////////////////////////////////////////////////////////////////////////////

	// Returns a list of embedded arrays of integers, where the embedded array is a simple array with no key/name pairings.
	// Example of what raw json string looks like:
	// "cards_picked":[
	//    [
	//       "10",
	//       "15",
	//       "15"
	//    ],
	//    [
	//       "40",
	//       "55",
	//       "70"
	//    ]
	// ]
	public List<List<int>> getIntListList(string key)
	{
		List<List<int>> list = new List<List<int>>();

		List<object> outerObjects = getObject(key) as List<object>;

		if (outerObjects == null)
		{
			return new List<List<int>>();
		}

		for (int i = 0; i < outerObjects.Count; i++)
		{
			List<int> inner = new List<int>();
			list.Add(inner);

			List<object> innerObjects = outerObjects[i] as List<object>;
			for (int j = 0; j < innerObjects.Count; j++)
			{
				inner.Add(int.Parse(innerObjects[j] as string));
			}
		}

		return list;
	}

	// Helper function to allow generic calls
	public static List<List<string>> getStringListListStatic(JSON json, string key)
	{
		return json.getStringListList(key);
	}
	
	// Returns a list of embedded arrays of strings, where the embedded array is a simple array with no key/name pairings.
	public List<List<string>> getStringListList(string key)
	{
		List<List<string>> list = new List<List<string>>();

		List<object> outerObjects = getObject(key) as List<object>;

		if (outerObjects == null)
		{
			return new List<List<string>>();
		}

		for (int i = 0; i < outerObjects.Count; i++)
		{
			List<string> inner = new List<string>();
			list.Add(inner);

			List<object> innerObjects = outerObjects[i] as List<object>;
			for (int j = 0; j < innerObjects.Count; j++)
			{
				inner.Add(innerObjects[j] as string);
			}
		}

		return list;
	}
	
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Get dictionaries
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public static Dictionary<int, string> getIntStringDictStatic(JSON json, string key)
	{
		return json.getIntStringDict(key);
	}

	/*
	Returns a List of keys and values that embeded in a JSON object.
	"initial_reel_sets": {
			"1": "elvira02_reelset_foreground"
	},
	*/
	public Dictionary<int, string> getIntStringDict(string key)
	{
		Dictionary<int,string> returnVal = new Dictionary<int,string>();

		JSON dictionaryJson = getJSON(key);
		if (dictionaryJson != null)
		{
			foreach (string subKey in dictionaryJson.getKeyList())
			{
				int intKey;
				if (int.TryParse(subKey, out intKey))
				{
					returnVal[intKey] = dictionaryJson.getString(subKey, "");
				}
			}
		}

		return returnVal;
	}

	public static Dictionary<int, int> getIntIntDictStatic(JSON json, string key)
	{
		return json.getIntIntDict(key);
	}

	/*
	Returns a List of keys and values that embeded in a JSON object.
	"some_key": {
			"1": "500"
	},
	*/
	public Dictionary<int, int> getIntIntDict(string key)
	{
		Dictionary<int, int> returnVal = new Dictionary<int, int>();

		JSON dictionaryJson = getJSON(key);
		if (dictionaryJson != null)
		{
			foreach (string subKey in dictionaryJson.getKeyList())
			{
				int intKey;
				if (int.TryParse(subKey, out intKey))
				{
					returnVal[intKey] = dictionaryJson.getInt(subKey, 0);
				}
			}
		}

		return returnVal;
	}
	
	public Dictionary<int, long> getIntLongDict(string key)
	{
		Dictionary<int, long> returnVal = new Dictionary<int, long>();

		JSON dictionaryJson = getJSON(key);
		if (dictionaryJson != null)
		{
			foreach (string subKey in dictionaryJson.getKeyList())
			{
				int intKey;
				if (int.TryParse(subKey, out intKey))
				{
					returnVal[intKey] = dictionaryJson.getLong(subKey, 0);
				}
			}
		}

		return returnVal;
	}

	public static Dictionary<string, string> getStringStringDictStatic(JSON json, string key)
	{
		return json.getStringStringDict(key);
	}

	public Dictionary<string, string> getStringStringDict(string key)
	{
		Dictionary<string, string> returnVal = new Dictionary<string, string>();
		
		JSON dictionaryJson = getJSON(key);
		if (dictionaryJson != null)
		{
			foreach (string subKey in dictionaryJson.getKeyList())
			{
				returnVal[subKey] = dictionaryJson.getString(subKey, "");
			}
		}
		
		return returnVal;
	}
	
	public static Dictionary<string, int> getStringIntDictStatic(JSON json, string key)
	{
		return json.getStringIntDict(key);
	}

	public Dictionary<string, int> getStringIntDict(string key)
	{
		Dictionary<string, int> returnVal = new Dictionary<string, int>();
		
		JSON dictionaryJson = getJSON(key);
		if (dictionaryJson != null)
		{
			foreach (string subKey in dictionaryJson.getKeyList())
			{
				returnVal[subKey] = dictionaryJson.getInt(subKey, 0);
			}
		}
		
		return returnVal;
	}

	public static Dictionary<string, long> getStringLongDictStatic(JSON json, string key)
	{
		return json.getStringLongDict(key);
	}

	public Dictionary<string, long> getStringLongDict(string key)
	{
		Dictionary<string, long> returnVal = new Dictionary<string, long>();
		
		JSON dictionaryJson = getJSON(key);
		if (dictionaryJson != null)
		{
			foreach (string subKey in dictionaryJson.getKeyList())
			{
				returnVal[subKey] = dictionaryJson.getLong(subKey, 0L);
			}
		}
		
		return returnVal;
	}

	public static Dictionary<string, JSON> getStringJSONDictStatic(JSON json, string key)
	{
		return json.getStringJSONDict(key);
	}

	public Dictionary<string, JSON> getStringJSONDict(string key)
	{
		Dictionary<string, JSON> returnVal = new Dictionary<string, JSON>();

		JSON dictionaryJson = getJSON(key);
		if (dictionaryJson != null)
		{
			foreach (string subKey in dictionaryJson.getKeyList())
			{
				returnVal[subKey] = dictionaryJson.getJSON(subKey);
			}
		}

		return returnVal;
	}
	
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Other functionality
	////////////////////////////////////////////////////////////////////////////////////////////////////

	// Creates a JSON string out of any (supported) type.
	public static string createJsonString(string name, object value)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		buildJsonString(builder, name, value);
		return builder.ToString();
	}
	
	// Sanitize a string for use of quotes
	public static string sanitizeString(string input)
	{
		if (input == null)
		{
			return null;
		}

		// Escape backslashes, quotes, HTTP newlines and newlines to prevent JSON weirdness:
		return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\r\\n").Replace("\t", "\\t").Replace("\n", "\\n");
	}

	// Does the given key exist
	public bool hasKey(string key)
	{
		return jsonDict.ContainsKey(key);
	}

	// Exactly like hasKey, but logs an error if an expected key doesn't exist.
	public bool validateHasKey(string key)
	{
		if (hasKey(key))
		{
			return true;
		}
		else
		{
			Debug.LogError("JSON.validateHasKey() - Unexpected data format, JSON was missing expected key: " + key);
			return false;
		}
	}

	// Returns the string representation of this JSON object.
	public override string ToString()
	{
		if (isValid)
		{
			return createJsonString("", jsonDict);
		}
		return "";
	}

	// Return JSON of differences between two JSON blocks.  NOTE: The returned JSON is not the actual differing objects, but a
	// textual representation of the differences.
	public static JSON getDiff(JSON mine, JSON theirs)
	{
		return getDiffInternal("", mine, theirs);
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Private stuff
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public JsonDictionary jsonDict;

	// Gets an object at the given key, central to all JSON class functionality.
	// Only used internally. DO NOT make public. Use getJSON if you need to get sub-JSON.
	private object getObject(string key)
	{
		if (jsonDict == null)
		{
			return null;
		}

		// Fix to allow keys with a period in them, cause we (HIR) need that (who wouldn't?)
		object resultObj;
		if (jsonDict != null && jsonDict.TryGetValue(key, out resultObj))
		{
			return resultObj;
		}
		// If no key match, continue with the original key-splitting code...

		string[] parts = key.Split('.');
		JsonDictionary currentDict = jsonDict;

		object nextDictObj;
		for (int i = 0; i < parts.Length - 1; i++)
		{
			if (currentDict != null && currentDict.TryGetValue(parts[i], out nextDictObj))
			{
				currentDict = nextDictObj as JsonDictionary;
			}
			else
			{
				return null;
			}
		}
		
		// We need to check currentDict for null before trying to use it, because
		// it's possible for a JSON object to be null even when there is a key.
		// This happens with an empty array in the JSON string like this:
		// "parameters": []

		if (currentDict != null && currentDict.TryGetValue(parts[parts.Length - 1], out resultObj))
		{
			return resultObj;
		}

		return null;
	}
	
	// Builds a JSON string out of any supported types, including optional tab indents and comma at the end of the line.
	// Useful for building huge blocks of JSON, one line at a time.
	public static void buildJsonStringLine(System.Text.StringBuilder builder, int indents, string name, object value, bool doAddComma)
	{
		// Add the tabs.
		for (int i = 0; i < indents; i++)
		{
			builder.Append("\t");
		}
		
		// Add the data.
		buildJsonString(builder, name, value, true);
		
		// End the line.
		builder.AppendLine(doAddComma ? "," : "");
	}
	
	// Builds a JSON string out of any supported types, appending it to the given StringBuilder.
	// Called internally by createJsonString().
	private static void buildJsonString(System.Text.StringBuilder builder, string name, object value, bool doAddSpaceAfterColon = false)
	{
		if (!string.IsNullOrEmpty(name))
		{
			// If the name passed in is empty or null, then assume the user wants just the array
			// and don't append it as a property name.
			builder.Append("\"");
			builder.Append(name);
			builder.Append("\":");
			if (doAddSpaceAfterColon)
			{
				builder.Append(" ");
			}
		}

		if (value == null)
		{
			builder.Append("null");
		}
		else if (value is int)
		{
			builder.Append(value.ToString());
		}
		else if (value is long)
		{
			builder.Append(value.ToString());
		}
		else if (value is float)
		{
			builder.Append(string.Format("{0:0.000}", (float)value));
		}
		else if (value is double)
		{
			builder.Append(string.Format("{0:0.000}", (double)value));
		}
		else if (value is bool)
		{
			builder.Append((bool)value ? "true" : "false");
		}
		else if (value is string)
		{
			builder.Append(string.Format("\"{0}\"", sanitizeString(value as string)));
		}
		else if (value is IList)
		{
			buildArray(builder, value as IList);
		}
		else if (value is IDictionary)
		{
			buildDictionary(builder, value as IDictionary);
		}
		else if (value is JSON)
		{
			buildJsonString(builder, "", (value as JSON).jsonDict);
		}
		else
		{
			Debug.LogError("JSON.buildJsonString() Error with value type: " + value.GetType().ToString()); 
		}
	}
		
	// Builds a JSON string from an object that implements IList. Called internally by buildJsonString().
	private static void buildArray(System.Text.StringBuilder builder, IList list)
	{
		builder.Append("[");
		bool first = true;
		foreach (var value in list)
		{
			// Comma-separate pairs by inserting comma before every pair but the first.
			if (first)
			{
				first = false;
			}
			else
			{
				builder.Append(",");
			}
			buildJsonString(builder, "", value);
		}

		builder.Append("]");
	}

	// Builds a JSON string from an object that implements IDictionary. Called internally by buildJsonString().
	private static void buildDictionary(System.Text.StringBuilder builder, IDictionary dict)
	{
		builder.Append("{");
		bool first = true;
		foreach (DictionaryEntry p in dict)
		{
			// Comma-separate pairs by inserting comma before every pair but the first. 
			if (first)
			{
				first = false;
			}
			else
			{
				builder.Append(",");
			}
			buildJsonString(builder, p.Key.ToString(), p.Value);
		}

		builder.Append("}");
	}

	// Helper function for getDiff that does most of the work and recursion.
	private static JSON getDiffInternal(string parentKey, JSON mine, JSON theirs)
	{
		// Get the set's of all of the data.
		HashSet<string> allMyKeys = new HashSet<string>(mine.getKeyList());
		HashSet<string> allTheirKeys = new HashSet<string>(theirs.getKeyList());

		// Find what's only mine, only theirs, or both.
		HashSet<string> onlyMine = new HashSet<string>(allMyKeys);
		onlyMine.ExceptWith(allTheirKeys);
		// Theirs
		HashSet<string> onlyTheirs = new HashSet<string>(allTheirKeys);
		onlyTheirs.ExceptWith(allMyKeys);
		// Both
		HashSet<string> both = new HashSet<string>(allMyKeys);
		both.IntersectWith(allTheirKeys);

		JsonDictionary mismatched = new JsonDictionary();

		foreach (string key in onlyMine)
		{
			mismatched.Add(key, string.Format("<mine>{0}", mine.getObject(key).ToString()));
		}
		foreach (string key in onlyTheirs)
		{
			mismatched.Add(key, string.Format("<theirs>{0}", theirs.getObject(key).ToString()));
		}

		// Go through the intersecting values and determine what's been added / changed / removed
		foreach (string key in both)
		{
			object mineObject = mine.getObject(key);
			object theirObject = theirs.getObject(key);
			if (mineObject is JsonDictionary && theirObject is JsonDictionary)
			{
				JSON diff = getDiffInternal(key, new JSON(mineObject as JsonDictionary), new JSON(theirObject as JsonDictionary));
				if (diff.jsonDict.Keys.Count > 0)
				{
					// There is a difference.
					foreach (string diffKey in diff.getKeyList())
					{
						string fullKey = diffKey;
						if (!string.IsNullOrEmpty(parentKey))
						{
							fullKey = parentKey + "." + diffKey;
						}
						mismatched.Add(fullKey, diff.getObject(diffKey));
					}
				}
			}
			else if (mineObject is List<object> && theirObject is List<object>)
			{
				string[] myStringArray = mine.getStringArray(key);
				string[] theirStringArray = theirs.getStringArray(key);
				int maxLength = Mathf.Max(myStringArray.Length, theirStringArray.Length);
				List<string> diffs = new List<string>();
				for (int i=0; i < maxLength; i++)
				{
					if (i >= myStringArray.Length || i >= theirStringArray.Length
						|| myStringArray[i] != theirStringArray[i])
					{
						// There is a difference.
						string myDiff = "null";
						string theirDiff = "null";
						if (i < myStringArray.Length)
						{
							myDiff = myStringArray[i];
						}
						if (i < theirStringArray.Length)
						{
							theirDiff = theirStringArray[i];
						}
						diffs.Add(string.Format("[{0}]:<mine>{1}<theirs>{2}", i, myDiff, theirDiff));
					}
				}
				if (diffs.Count > 0)
				{
					string fullKey = key;
					if (!string.IsNullOrEmpty(parentKey))
					{
						fullKey = parentKey + "." + key;
					}
					mismatched.Add(fullKey, string.Join(";", diffs.ToArray()));
				}
			}
			else
			{
				string myStringForKey = mine.getString(key, null, "");
				string theirStringForKey = theirs.getString(key, null, "");
				if (myStringForKey != theirStringForKey)
				{
					// There is a difference.
					string fullKey = key;
					if (!string.IsNullOrEmpty(parentKey))
					{
						fullKey = parentKey + "." + key;
					}
					string diff = string.Format("<mine>{0}<theirs>{1}", mine.getObject(key).ToString(), theirs.getObject(key).ToString());
					mismatched.Add(fullKey, diff);
				}
			}
		}
		return new JSON(mismatched);
	}
}
