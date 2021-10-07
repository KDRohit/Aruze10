using UnityEngine;
using System.Collections;

/**
This is just a placeholder MonoBehaviour that is put on a persistent GameObject
for the purpose of running Coroutines. (because only MonoBehaviours can run Coroutines)
*/
public class RoutineRunner : TICoroutineMonoBehaviour
{
	public static RoutineRunner instance = null;
	private static event GenericDelegate onRoutineRunnerReady;
	
	void Awake()
	{
		instance = this;
		
		// This is created in the Startup scene, which needs to be persistent even during a game reset.
		DontDestroyOnLoad(gameObject);

		if (onRoutineRunnerReady != null)
		{
			onRoutineRunnerReady();
		}
	}

	public static void addCallback(GenericDelegate callback)
	{
		onRoutineRunnerReady -= callback;
		onRoutineRunnerReady += callback;
	}

	public static void removeCallback(GenericDelegate callback)
	{
		onRoutineRunnerReady -= callback;
	}
}