using System.Collections;
using System.Collections.Generic;
using Zynga.Core.Util;

	public class LocalStoragePreferences : PreferencesBase
	{
		private bool mIsChanged;
		private Dictionary<string, string> mUnsavedChanges = new Dictionary<string, string>();

		/// <inheritdoc/>
		public override bool IsChanged
		{
			get
			{
				return mIsChanged;
			}
		}

		private string getDataForKey(string key)
		{
			string data = "";
			if (mUnsavedChanges.ContainsKey(key))
			{
				data = mUnsavedChanges[key];
			}
#if UNITY_WEBGL && !UNITY_EDITOR
			else
			{
				data = WebGLFunctions.getLocalStorageItem(key);
			}
#endif
			return data;
		}

		/// <inheritdoc/>
		public override int GetInt(string key, int defaultVal) 
		{
			string data = getDataForKey(key);
			int value = 0;
			if (!int.TryParse(data, out value))
			{
				value = defaultVal;
			}
			return value;
		}

		/// <inheritdoc/>
		public override long GetLong(string key, long defaultVal) 
		{
			string data = getDataForKey(key);
			long value = 0;
			if (!long.TryParse(data, out value))
			{
				value = defaultVal;
			}
			return value;
		}

		/// <inheritdoc/>
		public override float GetFloat(string key, float defaultVal)
		{
			string data = getDataForKey(key);
			float value = 0;
			if (!float.TryParse(data, out value))
			{
				value = defaultVal;
			}
			return value;
		}

		/// <inheritdoc/>
		public override double GetDouble(string key, double defaultVal) 
		{
			string data = getDataForKey(key);
			double value = 0;
			if (!double.TryParse(data, out value))
			{
				value = defaultVal;
			}
			return value;
		}

		/// <inheritdoc/>
		public override string GetString(string key, string defaultVal)
		{
			string data = getDataForKey(key);
			if (string.IsNullOrEmpty(data))
			{
				return defaultVal;
			}

			return data;
		}

		/// <inheritdoc/>
		public override void SetInt(string key, int val)
		{
			mUnsavedChanges[key] = val.ToString();
			mIsChanged = true;
		}

		/// <inheritdoc/>
		public override void SetFloat(string key, float val)
		{
			mUnsavedChanges[key] = val.ToString();
			mIsChanged = true;
		}

		/// <inheritdoc/>
		public override void SetDouble(string key, double val)
		{
			mUnsavedChanges[key] = val.ToString();
			mIsChanged = true;
		}

		/// <inheritdoc/>
		public override void SetLong(string key, long val)
		{
			mUnsavedChanges[key] = val.ToString();
			mIsChanged = true;
		}

		/// <inheritdoc/>
		public override void SetString(string key, string val)
		{
			mUnsavedChanges[key] = val;
			mIsChanged = true;
		}

		/// <inheritdoc/>
		public override bool HasKey(string key)
		{
			return mUnsavedChanges.ContainsKey(key) 
#if UNITY_WEBGL && !UNITY_EDITOR
				|| WebGLFunctions.localStorageHasKey(key)
#endif
				;
		}

		/// <inheritdoc/>
		public override void DeleteKey(string key)
		{
			if (mUnsavedChanges.ContainsKey(key))
			{
				mUnsavedChanges.Remove(key);
			}
#if UNITY_WEBGL && !UNITY_EDITOR
			WebGLFunctions.removeLocalStorageItem(key);
#endif
			mIsChanged = true;
		}

		/// <inheritdoc/>
		public override void DeleteAll()
		{
			mUnsavedChanges.Clear();
#if UNITY_WEBGL && !UNITY_EDITOR
			WebGLFunctions.clearLocalStorage();
#endif
			mIsChanged = true;
		}

		/// <inheritdoc/>
		public override void Save()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			//write all unsaved changes
			foreach(KeyValuePair<string, string> kvp in mUnsavedChanges)
			{
				if (string.IsNullOrEmpty(kvp.Key) || kvp.Value == null)
				{
					continue;
				}
				WebGLFunctions.setLocalStorageItem(kvp.Key, kvp.Value.ToString());

			}
			mUnsavedChanges.Clear();
#endif
			mIsChanged = false;
		}
	}