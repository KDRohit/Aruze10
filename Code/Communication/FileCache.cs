using UnityEngine;

public class FileCache
{
	// The path used for caching files
	public static string path
	{
		get
		{
			if (_path == null)
			{
#if UNITY_EDITOR
				// When in the editor, put the file in an easy to find place... the project's Temp folder.
				_path = Application.dataPath + "/../Temp/";
#else
				// This looks weird because of a reported Unity bug with Application.temporaryCachePath
				// where it will only return a correct path string once on some Android installs.
				_path = Application.temporaryCachePath;
				if (string.IsNullOrEmpty(_path))
				{
					// We don't support caching if we get here.
					_path = "";
				}
				else
				{
					_path = _path + "/";
				}
#endif
			}
			return _path;
		}
	}
	private static string _path = null;
}
