using UnityEngine;
using UnityEngine.SceneManagement;

public class DestructionScript : TICoroutineMonoBehaviour 
{
	// Use this for initialization
	void Start()
	{
		Glb.reinitializeGame();
		Debug.Log("Done destroying, lets load up the startup scene.");
		SceneManager.LoadScene(Glb.STARTUP_LOGIC_SCENE);
	}
}
