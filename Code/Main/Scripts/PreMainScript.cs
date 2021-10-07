using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is for doing initialization and update calls that must happen before MainScript's update calls.

THIS BEHAVIOUR EXISTS FOR BOTH THE LOBBY AND GAME SCENES, SO CODE ACCORDINGLY.
*/
public class PreMainScript : TICoroutineMonoBehaviour
{
	void Awake()
	{
		TouchInput.init();
#if ZYNGA_TRAMP
		AutomatedPlayer.init();
#endif // ZYNGA_TRAMP
	
	}
		
	// Get the mouse input here, always before MainScript.
	void Update()
	{
		ServerAction.processPendingActions();
		TouchInput.update();
		Server.handlePendingSplunkEvents(); // Handles Splunk Log batching
	}
}
