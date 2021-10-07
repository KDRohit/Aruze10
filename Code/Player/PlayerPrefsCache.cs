#define PLAYER_PREFS_FALLBACK       //Migrates from existing PlayerPrefs file
using System;
using UnityEngine;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerPrefsCache : MonoBehaviour
{
	private static string PREFS_FILENAME_BIN;

	private Hashtable prefCache;
	private bool cacheDirty = false;
	private static GameObject go;
	private static PlayerPrefsCache _in;
	private static PlayerPrefsCache Inst
	{
		get
		{
			if (_in == null)
			{
#if UNITY_EDITOR
				//determine if the editor is playing
				bool playing = UnityEditor.EditorApplication.isPlaying;

				//if it's not, we're altering player prefs through Editor UI
				if (!playing)
				{
					//so we need to be able to cleanup the anchor object if someone saves the scene
					//or starts playing
					UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSave;
					UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanging;
					go = GameObject.Find("PlayerPrefsCache");
				}

				if (go == null)
				{
					go = new GameObject("PlayerPrefsCache");
				}
				
				if (playing)
				{
					DontDestroyOnLoad(go);
				}
#else
				go = new GameObject("PlayerPrefsCache");
				DontDestroyOnLoad(go);
#endif

				_in = (go.AddComponent<PlayerPrefsCache>());
				//this should get overwritten by a file, but needs to exists
				//for the case where first run without save exists
				_in.prefCache = new Hashtable(); 
				_in.Init();
			}
			return _in;
		}
	}
	
	
#if UNITY_EDITOR
	//these are the cleanup calls for running PlayerPrefsCache in Editor while not
	//actually playing (Editor scripts, etc.)
	private static void OnSceneSave(UnityEngine.SceneManagement.Scene s, string path)
	{
		DeleteAnchorObject();
	}
	
	private static void OnPlayModeChanging(PlayModeStateChange state)
	{
		DeleteAnchorObject();
	}

	private static void DeleteAnchorObject()
	{
		if (go != null && !UnityEditor.EditorApplication.isPlaying)
		{
			UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanging;
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSave;
			DestroyImmediate(go);
		}
	}
#endif

	void OnDisable()
	{
		Save();
	}

	private void Init()
	{
		PREFS_FILENAME_BIN = Application.persistentDataPath + "/PlayerPrefsBin.zng";
		
		if (File.Exists(PREFS_FILENAME_BIN))
		{
			//file info gets cached.  Make sure this is up to date before we attempt to deserialize data
			FileInfo info = new FileInfo(PREFS_FILENAME_BIN);
			info.Refresh();
			if (info.Length > 0)
			{
				DeserializeBin();
			}
		}
	}

	public static bool HasKey(string key)
	{
#if PLAYER_PREFS_FALLBACK
		if (PlayerPrefs.HasKey(key))
		{
			return true;
		}
#endif
		return Inst.prefCache.ContainsKey(key);
	}
	
	static void SetValue<T>(string key, T val)
	{
		if (!Inst.prefCache.ContainsKey(key))
		{
			Inst.prefCache.Add(key, val);
		}
		else
		{
			Inst.prefCache[key] = val;
		}

		Inst.cacheDirty = true;

#if PLAYER_PREFS_FALLBACK
		if (typeof(T) == typeof(string))
		{
			PlayerPrefs.SetString(key, (string)(object)val);
		}
		else if (typeof(T) == typeof(int))
		{
			PlayerPrefs.SetInt(key, (int)(object)val);
		}
		else if (typeof(T) == typeof(float))
		{
			PlayerPrefs.SetFloat(key, (float)(object)val);
		}
#endif
	}

	public static void SetInt(string key, int value)
	{
		SetValue(key, value);
	}

	public static void SetFloat(string key, float value)
	{
		SetValue(key, value);
	}

	public static void SetString(string key, string value)
	{
		SetValue(key, value);
	}

	static T GetValue<T>(string key, T defaultValue)
	{
		bool isValueAlreadyCached = false;

		if (Inst.prefCache.Contains(key))
		{
			defaultValue = (T)(object)Inst.prefCache[key];
			isValueAlreadyCached = true;
		}
		//ideally this block will come out after some time as it is currently going to
		//be used to migrate from PlayerPrefs file to our own data file and save
#if PLAYER_PREFS_FALLBACK
		if (PlayerPrefs.HasKey(key))
		{
			if(typeof(T) == typeof(int))
			{
				int d = (int)(object)defaultValue;
				defaultValue = (T)(object)PlayerPrefs.GetInt(key, d);
			}
			else if(typeof(T) == typeof(float))
			{
				float d = (float)(object)defaultValue;
				defaultValue = (T)(object)PlayerPrefs.GetFloat(key, d);
			}
			else if(typeof(T)== typeof(string))
			{
				string d = (string)(object)defaultValue;
				defaultValue = (T)(object)PlayerPrefs.GetString(key, d);
			}
		}
#endif

		// if the value isn't already cached, call SetValue to add it to the cache
		// do this even in the case where the value was in PlayerPrefs already since
		// we want to make sure we have the cached value available
		if (!isValueAlreadyCached)
		{
			SetValue(key, defaultValue);
		}
		
		return defaultValue;
	}

	public static int GetInt(string key, int defaultValue = 0)
	{
		return GetValue(key, defaultValue);
	}

	public static float GetFloat(string key, float defaultValue = 0)
	{
		return GetValue(key, defaultValue);
	}

	public static string GetString(string key, string defaultValue = "")
	{
		return GetValue<string>(key, defaultValue);
	}

	public static void DeleteKey(string key)
	{
		PlayerPrefs.DeleteKey(key);
		PlayerPrefs.Save();

		Inst.prefCache.Remove(key);
		Inst.cacheDirty = true;
	}

	public static void DeleteAll()
	{
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();

		Inst.prefCache.Clear();
		Inst.cacheDirty = true;
	}

	public static void Save()
	{
		if (Inst.cacheDirty)
		{
			SerializeBin();
			Inst.cacheDirty = false;
		}
		
		PlayerPrefs.Save();
	}

	static void SerializeBin()
	{
		PREFS_FILENAME_BIN = Application.persistentDataPath + "/PlayerPrefsBin.zng";
		var binformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
		using (var fs = File.Create(PREFS_FILENAME_BIN))
		{
			binformatter.Serialize(fs, Inst.prefCache);
		}

#if UNITY_WEBGL
		//cause an external call to update the indexdb
		//via  FS.syncfs(false, (function(err) {syncing_fs = false;}))
		//https://forum.unity.com/threads/how-does-saving-work-in-webgl.390385/
		Application.ExternalEval("_JS_FileSystem_Sync();");
#endif
	}
	
	static void DeserializeBin()
	{
		PREFS_FILENAME_BIN = Application.persistentDataPath + "/PlayerPrefsBin.zng";

		FileStream fs = null;
		try
		{
			//open file
			fs = File.Open(PREFS_FILENAME_BIN, FileMode.Open);

			//reset to start of stream in case file was opened earlier.
			fs.Position = 0;

			//deserialize
			var binformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			Inst.prefCache = (Hashtable)binformatter.Deserialize(fs);
		}
		catch (Exception e)
		{
			//log the error
			Debug.LogError(e.Message + System.Environment.NewLine + "Resetting player preferences.  Data is corrupt");
			//reset to an empty player cache (so game is still playable).  If the binary data is bad it's impossible to recover any of it at this point.
			Inst.prefCache = new Hashtable();
		}
		finally
		{
			if (fs != null)
			{
				fs.Dispose();
				fs = null;
			}
		}
	}
}
