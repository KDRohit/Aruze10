using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Asset bundle container class that contains an asset bundle.
/// </summary>
public class AssetBundleContainer : TICoroutineMonoBehaviour
{
	public AssetBundle bundle;
	public string bundleName; // used for more readable debug messages
	public AssetBundleMapping assetBundleMapping = null;
	public bool skippedBundleMapping = false;
	public int forLevel = -1;

	// Bundle Dependencies...
	public List<AssetBundleDownloader> referencedBy = new List<AssetBundleDownloader>();

	// Returns 'inUse' status for this bundle
	public bool isInUse()
	{
		return forLevel==-1 || (forLevel == UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex) || isReferenced();
	}

	// Mark this bundle in-use for the current loaded level, won't get purged until the next scene is loaded
	public void touch()
	{
		if (forLevel != -1) 
		{
			forLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
		}
	}

	//Mark the bundle as not being used so we can clean it up
	public void unTouch()
	{
		//-1 means the bundle is supposed to stay cached so not going to untouch it
		if (forLevel != -1)
		{
			forLevel = -2;
		}
	}

	// Returns true if we are referenced by any active downloader
	public bool isReferenced() 
	{
		foreach(var downloader in this.referencedBy)
		{
			if (downloader != null)
			{
				return true;
			}
		}
		return false;
	}

}
