using UnityEngine;
using System.Collections;

public class RenderQueueAdjustment : MonoBehaviour 
{
	public int queueAdjustment;

	void Start() 
	{
		Renderer renderer = GetComponent<Renderer>();
		if (renderer != null)
		{
			renderer.material.renderQueue += queueAdjustment;
		}
	}
}
