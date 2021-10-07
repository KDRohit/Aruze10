using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Class to handle legacy quad symbol animaiton reseting
*/
public class ResetBonePose : TICoroutineMonoBehaviour 
{
	Dictionary<Transform, ArrayList> defaultBonePose;

	public void saveDefaultPose()
	{
		if (defaultBonePose == null)
		{
			defaultBonePose = new Dictionary<Transform, ArrayList>();
		}
	
		saveDefaultPose(this.transform);
	}
	
	private void saveDefaultPose(Transform t)
	{
		ArrayList listBonePose = new ArrayList();
		listBonePose.Add(t.localPosition);
		listBonePose.Add(t.localRotation);
		listBonePose.Add(t.localScale);
			
		this.defaultBonePose.Add(t, listBonePose);
			
		foreach (Transform child in t)
		{			
			saveDefaultPose(child);
		}
	}
	
	public void resetToDefaultPose()
	{
		foreach (KeyValuePair<Transform, ArrayList> kvp in this.defaultBonePose)
		{		
			kvp.Key.localPosition = (Vector3)  kvp.Value[0];
			kvp.Key.localRotation = (Quaternion)  kvp.Value[1];
			kvp.Key.localScale = (Vector3)  kvp.Value[2];
		
		}
	}
	
}
