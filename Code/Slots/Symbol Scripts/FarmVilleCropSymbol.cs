using UnityEngine;
using System.Collections;

public class FarmVilleCropSymbol : MonoBehaviour 
{
	[SerializeField] private GameObject[] cropMeshes;

	// basically just go through our crop meshes and give them instance materials from the beginning
	void Awake()
	{
		foreach (GameObject cropMesh in cropMeshes) 
		{
			cropMesh.GetComponent<Renderer>().material = new Material (cropMesh.GetComponent<Renderer>().material);
			cropMesh.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(cropMesh.GetComponent<Renderer>().material.mainTextureOffset.x, 0.0f);
		}
	}
}
