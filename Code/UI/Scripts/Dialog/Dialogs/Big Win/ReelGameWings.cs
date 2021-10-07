using UnityEngine;
using System.Collections;

/**
Handles loading the texture for the wings on reel games.
*/

public class ReelGameWings : TICoroutineMonoBehaviour
{
	public MeshFilter leftMeshFilter;
	public MeshFilter rightMeshFilter;

	void Awake()
	{
		if (GameState.game != null)
		{
			leftMeshFilter.mesh = BonusGameWings.leftWingMesh;
			rightMeshFilter.mesh = BonusGameWings.rightWingMesh;
			BonusGameWings.loadTextures(leftMeshFilter, rightMeshFilter, false);
		}
	}

	void Update()
	{
		// If the wings are shown in the lobby somehow,
		// destroy them. This could happen in a weird situation
		// where the game is reset in the middle of a big win.
		// due to loss of internet connectivity.
		if (GameState.isMainLobby)
		{
			Destroy(gameObject);
		}
	}
}
