using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/**
This is a purely static class of generic useful functions that relate to math.
*/
public static class CommonDataStructures
{

	/// Universal interface to help in the creation of Hashtables out of key/value pairs.
	/// This code was stolen from iTween, but put here so we're relying on iTween for hashes unrelated to iTween.
	public static Hashtable hash(params object[] args)
	{
		Hashtable hashTable = new Hashtable(args.Length / 2);
		if (args.Length % 2 != 0)
		{
			Debug.LogError("Tween Error: Hash requires an even number of arguments!");
			return null;
		}
		else
		{
			int i = 0;
			while(i < args.Length - 1)
			{
				hashTable.Add(args[i], args[i+1]);
				i += 2;
			}
			return hashTable;
		}
	}

	/// Returns an array, using a possible recycled array if the size is okay.
	/// This is used for speed optimizations (particularly in RageSpline).
	/// Please be cautious adding extra logic to this function, instead
	/// consider writing a new function for whatever you are doing.
	public static T[] resizedArray<T>(T[] oldArray, int newSize, bool isLargerOkay = false)
	{
		if (oldArray == null || oldArray.Length < newSize || (!isLargerOkay && (oldArray.Length > newSize)))
		{
			return new T[newSize];
		}
		return oldArray;
	}

	public static Dictionary<TKey,TValue> mergeDictionary<TKey,TValue>(Dictionary<TKey,TValue> into, Dictionary<TKey,TValue> from)
	{
		if (into == null)
		{
			into = from;
			return into;
		}
		if (from == null)
		{
			return into;
		}

		foreach (KeyValuePair<TKey,TValue> kvp in from)
		{
			if (into.ContainsKey(kvp.Key))
			{
				Debug.LogWarningFormat("Issue merging in another value and found a conflict, taking new value for {0}", kvp.Key.ToString());
			}
			into[kvp.Key] = kvp.Value;
		}

		return into;
	}

	/// KeyValuePair clas to be used with the SerializableDictionary
	[System.Serializable] 
	public class SerializableKeyValuePair<TKey, TValue>
	{
		public TKey key;
		public TValue value;

		public SerializableKeyValuePair() {}

		public SerializableKeyValuePair(TKey key, TValue value)
		{
			this.key = key;
			this.value = value;
		}
	}

	/// Generic way of having a serializable Dictionary, you will have to specify explicit derived classes though
	[System.Serializable]
	public class SerializableDictionary<TKeyValuePair, TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, ISerializationCallbackReceiver where TKeyValuePair : SerializableKeyValuePair<TKey, TValue>, new()
	{
		[SerializeField] public List<TKeyValuePair> keyValuePairList = new List<TKeyValuePair>();

		// This dictionary is having its functionality wrapped by operators/methods in this class
		// This was to fix an issue where Unity had a bug that prevented classes from serializing if they derived from Dictionary
		private Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
		public Dictionary<TKey, TValue> dictionary
		{
			get { return _dictionary; }
		}

		public int Count
		{
			get { return _dictionary.Count; }
		}

		/// Function that can be called to force update the serialized list
		public void convertDictionaryToSerializedList()
		{
			keyValuePairList.Clear();

			foreach(KeyValuePair<TKey, TValue> pair in _dictionary)
			{
				TKeyValuePair newValuePair = new TKeyValuePair();
				newValuePair.key = pair.Key;
				newValuePair.value = pair.Value;
				keyValuePairList.Add(newValuePair);
			}
		}

		/// Saves the dictionary back into the keyValuePairList
		public void OnBeforeSerialize()
		{
			// only serialize the other direction while the game is running
			if (Application.isPlaying)
			{
				convertDictionaryToSerializedList();
			}
		}

		/// Loads the dictionary from the keyValuePairList
		public void OnAfterDeserialize()
		{
			_dictionary.Clear();

			for (int i = 0; i < keyValuePairList.Count; i++)
			{
				if (!_dictionary.ContainsKey(keyValuePairList[i].key))
				{
					_dictionary.Add(keyValuePairList[i].key, keyValuePairList[i].value);
				}
				else
				{
					_dictionary[keyValuePairList[i].key] = keyValuePairList[i].value;
					Debug.LogError("keyValuePairList contains entries with duplicate Keys, a dictionary doesn't support this, so just overwriting the value!");
				}
			}
		}

		public void Add(TKey key, TValue value)
		{
			_dictionary.Add(key, value);
		}

		public bool ContainsKey(TKey key)
		{
			return _dictionary.ContainsKey(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return _dictionary.TryGetValue(key, out value);
		}

		public TValue this[TKey i]
		{
		    get { return _dictionary[i]; }
		    set { _dictionary[i] = value; }
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _dictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	/// Serializable Dictionary of string to strings explicityly derived so that it will work in the Unity inspector
	[System.Serializable] public class KeyValuePairOfStringToString : SerializableKeyValuePair<string, string> {}
	[System.Serializable] public class SerializableDictionaryOfStringToString : SerializableDictionary<KeyValuePairOfStringToString, string, string> {}

	/// Serializable Dictionary of string to ints explicitly derived so that it will work in the Unity inspector
	[System.Serializable] public class KeyValuePairOfStringToInt : SerializableKeyValuePair<string, int> {}
	[System.Serializable] public class SerializableDictionaryOfStringToInt : SerializableDictionary<KeyValuePairOfStringToInt, string, int> {}

	/// Serializable Dictionary of int to a list of strings explicitly derived so that it will work in the Unity inspector
	[System.Serializable] public class KeyValuePairOfIntToStringList : SerializableKeyValuePair<int, List<string>> {}
	[System.Serializable] public class SerializableDictionaryOfIntToStringList : SerializableDictionary<KeyValuePairOfIntToStringList, int, List<string>> {}
	
	/// Serializable Dictionary of int to ints explicitly derived so that it will work in the Unity inspector
	[System.Serializable] public class KeyValuePairOfIntToInt : SerializableKeyValuePair<int, int> 
	{
		public KeyValuePairOfIntToInt() {}
		public KeyValuePairOfIntToInt(int key, int value) : base(key, value) {}
	}
	[System.Serializable] public class SerializableDictionaryOfIntToInt : SerializableDictionary<KeyValuePairOfIntToInt, int, int> {}

	// Serializable Dictionary of int to a list of ints explicitly derived so that it will work in the Unity inspector
	[System.Serializable] public class KeyValuePairOfIntToIntList : SerializableKeyValuePair<int, List<int>> {}
	[System.Serializable] public class SerializableDictionaryOfIntToIntList : SerializableDictionary<KeyValuePairOfIntToIntList, int, List<int>> {}

	/// Function which randomizes the entries in a list
	public static void shuffleList<T>(IList<T> list)
	{
		int listCount = list.Count;  
		while (listCount > 1) 
		{  
			listCount--;  
			int randomIndex = Random.Range(0, listCount);  
			T value = list[randomIndex];  
			list[randomIndex] = list[listCount];  
			list[listCount] = value;  
		}  
	}
	#if UNITY_EDITOR
	/// <summary>
	/// Used to debug log a Dictionary<int, int[]> in one line.
	/// </summary>
	/// <param name="jsonDict"></param>
	public static void debugLogDict(Dictionary<int, int[]> jsonDict)
	{
		Debug.Log("|----Start of Dictionary<int, int[]>----|");
		string output;
		int[] val;
		int i;
		foreach (KeyValuePair<int, int[]> kvp in jsonDict)
		{
			output = "| kvp= Key: " + kvp.Key + "  Value: ";
			val = kvp.Value;
			for (i = 0; i < val.Length; i++)
			{
				output += val[i] + ",";
			}
			
			Debug.Log(output);
		}
		Debug.Log("|----End of Dictionary<int, int[]>----|");
	}

	/// <summary>
	/// Used to debug log a Dictionary<int, int[]> in one line.
	/// </summary>
	/// <param name="jsonDict"></param>
	public static void debugLogDict(Dictionary<string, string> jsonDict)
	{
		Debug.Log("|----Start of Dictionary<string,string>----|");
		string output;
		foreach (KeyValuePair<string, string> kvp in jsonDict)
		{
			output = "| kvp= Key: " + kvp.Key + "  Value: " + kvp.Value;
			Debug.Log(output);
		}
		Debug.Log("|----End of Dictionary<int, int[]>----|");
	}
	
	#endif
	public static void selfDestructObject(Object obj)
	{
		#if UNITY_EDITOR
		if (obj != null && !Data.debugMode)
		{
			Object.Destroy(obj);
		}
		#else
		if (obj != null)
		{
			Object.Destroy(obj);
		}
		#endif
	}

	public static bool arrayContains<T>(T[] array, T obj)
	{
		if (array == null)
		{
			return false;
		}

		for (int i = 0; i < array.Length; i++)
		{
			if (Equals(array[i], obj))
			{
				return true;
			}
		}

		return false;
	}

	// Takes in a string array from a CSV source. start index is where you want to start on the CSV and length ideally is the length of one data set.
	// ex: "name","date", "favorite color", "name,"date, ....
	// start index 1 and length 3 would give you dates. 2 and 3 gives color, etc.
	public static string[] getValuesFromCSVArray(string[] array, int startIndex, int length)
	{
		if (array != null && array.Length > 0 && array.Length % length == 0)
		{
			// Size our value array
			string[] values = new string[array.Length / length];

			for (int i = 0; i < values.Length; i++)
			{
				values[i] = array[startIndex + (length * i)];
			}

			return values;
		}
		else
		{
			if (array.Length % length != 0)
			{
				Debug.LogError("CommonDataStructures::getValuesFromCSVArray - Record legnth didnt fit evently into array length, couldn't get proper data.");
			}
			return new string[0];
		}
	}

	public static string[] getValuesFromCSVArray(string stringToParse, int startIndex, int length)
	{
		if (!string.IsNullOrEmpty(stringToParse))
		{
			string[] array = stringToParse.Split(',');

			return getValuesFromCSVArray(array, startIndex, length);
		}

		// This might spam depending on how it's being used. Removed for now 
		//Debug.LogError("CommonDataStructures::getValuesFromCSVArray - string to parse was empty");
		return new string[0];
	}

	public static string getRecordElementWithSpecificKey(string[] arrayToParse, string fieldName, int elementNumber, int length)
	{
		if (arrayToParse != null && arrayToParse.Length > 0)
		{
			for (int i = 0; i < arrayToParse.Length; i += length)
			{
				if (arrayToParse[i] == fieldName && (i + elementNumber) < arrayToParse.Length)
				{
					return arrayToParse[i + elementNumber];
				}
			}
		}

		return "";
	}
}
