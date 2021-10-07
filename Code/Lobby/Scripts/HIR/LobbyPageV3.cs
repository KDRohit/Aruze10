using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPageV3 : MonoBehaviour
{
	[SerializeField] public List<Transform> spotsLocations;
	
	[System.NonSerialized] public List<LobbyOption> lobbyOptions = new List<LobbyOption>();
}
