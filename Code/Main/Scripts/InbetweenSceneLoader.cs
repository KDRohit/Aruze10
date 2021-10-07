using UnityEngine;
using UnityEngine.SceneManagement;

/**
Forwards from this scene to whatever is next.
These scene then acts as an intermediate loader,
for purposes of clearing memory.
*/
public class InbetweenSceneLoader : TICoroutineMonoBehaviour
{
	public static string nextScene = "";
	private static bool isFirstSceneLoad = true;
	
	void Awake()
	{
		Debug.Log("Intermediate loading scene is happening now, next up: " + nextScene);
		
		if (!isFirstSceneLoad)
		{
			//Now is a good time to clean up the texture cache, textures can always be reloaded from disk fast later.
			//Because of how the Loading screen works, this and Start() will actually be called before its
			//hideMe() and finishHiding() routines get called 
			DisplayAsset.cleanupSessionCache();
			AssetBundleManager.Instance.unloadUnusedBundles();
		}
	}

	void Start()
	{
		string scene = nextScene;
		nextScene = "";
		SceneManager.LoadScene(scene);
		isFirstSceneLoad = false;
	}
}