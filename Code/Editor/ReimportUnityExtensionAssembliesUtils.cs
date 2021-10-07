using UnityEngine;
using UnityEditor;

public class ReimportUnityExtensionAssembliesUtils {

	/// <summary>
	/// Menu item to manually handle the dreaded "DLL is in timestamps but is not known in guidmapper..." errors that 
	/// pop up from time to time.
	/// </summary>
	/// <remarks>
	/// Adapted from http://forum.unity3d.com/threads/unityengine-ui-dll-is-in-timestamps-but-is-not-known-in-assetdatabase.274492/
	/// http://forum.unity3d.com/threads/unityengine-ui-dll-is-in-timestamps-but-is-not-known-in-assetdatabase.274492/page-4
	/// </remarks>
	[MenuItem( "Zynga/Editor Tools/Reimport Unity Extension Assemblies")]
	public static void ReimportUnityExtensionAssemblies()
	{
		// Locate the directory of Unity extensions
		string extensionsPath = System.IO.Path.Combine(EditorApplication.applicationContentsPath, "UnityExtensions");

		// Walk the directory tree, looking for DLLs
		var dllPaths = System.IO.Directory.GetFiles(extensionsPath, "*.dll", System.IO.SearchOption.AllDirectories);

		// Reimport any extension DLLs
		int numReimportedAssemblies = 0;
		foreach (string dllPath in dllPaths) {
			//UnityEngine.Debug.LogFormat("Reimport DLL: {0}", dllPath);
			if (ReimportExtensionAssembly(dllPath)) {
				numReimportedAssemblies++;
			}
		}

		UnityEngine.Debug.LogWarningFormat("Reimported ({0}) Unity extension DLLs." +
		                                   " Please restart Unity for the changes to take effect.", numReimportedAssemblies);
	}

	private static bool ReimportExtensionAssembly(string dllPath) {

		// Check to see if this assembly exists in the asset database
		string assemblyAssetID = AssetDatabase.AssetPathToGUID(dllPath);
		if (!string.IsNullOrEmpty(assemblyAssetID)) {
			// Assembly exists in asset database, so force a reimport
			AssetDatabase.ImportAsset(dllPath, ImportAssetOptions.ForceUpdate|ImportAssetOptions.DontDownloadFromCacheServer);
			return true;
		}
		return false;
	}
}
