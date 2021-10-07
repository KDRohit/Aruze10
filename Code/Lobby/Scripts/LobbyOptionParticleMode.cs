using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Chooses the particle mode to display on a lobby option.
*/

public class LobbyOptionParticleMode : MonoBehaviour
{
	public GameObject normalMode;			// The normal lobby mode, when touching a game to launch it normally.
	public GameObject confirmSelectionMode;	// Used when choosing a game like a radio-set, that has to be confirmed with another button, such as Likely To Lapse mode.
	
	void Awake()
	{
		confirmSelectionMode.SetActive(false);
		normalMode.SetActive(true);
	}
}
