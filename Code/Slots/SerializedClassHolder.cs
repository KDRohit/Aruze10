using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SerializedClassHolder : TICoroutineMonoBehaviour 
{
	
}

[System.Serializable]
public class AnimatorInstruction
{
	public Animator animator;
	public string ANIMATION_NAME;
	public float DELAY_TIME; // how long to wait before starting animation
	public float POST_START_ANIMATION_WAIT; // how long to wait after starting animation
	public GameObject startingLocation;
}

[System.Serializable]
public class TweenInstruction
{
	public enum TweenType { MOVE_TO, SCALE_TO }
	public TweenType tweenType;
	public iTween.EaseType easetype;
	public GameObject tweenTargetGO; // the GameObject to use as the first parameter in the tween call
	public GameObject paramsTargetGO; // the GameObject to use as the "target" param in the hash, if any
	public float time = -1.0f; // when negative, don't use this field
	public float speed = -1.0f; // when negative, don't use this field
	public float delay = -1.0f; // when negative, don't use this field
	public GameObject MoveDestinationGO; // if set, we'll use this (global position)
	public Vector3 movePos; // otherwise we'll use this
	public Vector3[] movePath; // all these values should be around between 0 and 1 (inclusive) in which 0 would be the start pos, and 1 would be the end pos.
	public bool shouldDestroyOnComplete;
	[Tooltip("Allows step-by-step tween instructions, instead of playing at the same time.")]
	public bool shouldWaitToComplete = false;

	public IEnumerator executeTween()
	{
		if (tweenTargetGO != null)
		{
			Hashtable tweenParams = buildParams();
			if (tweenType == TweenType.MOVE_TO)
			{
				yield return new TITweenYieldInstruction(iTween.MoveTo(tweenTargetGO, tweenParams));
			}
		}
		
		if (shouldDestroyOnComplete)
		{
			GameObject.Destroy(tweenTargetGO);
		}
	}
	
	public Hashtable buildParams()
	{
		Hashtable tweenHashParams = new Hashtable();
		tweenHashParams.Add("easetype", easetype);
		if (MoveDestinationGO != null && movePath.Length == 0)
		{
			tweenHashParams.Add("position", MoveDestinationGO.transform.position);
		}
		else if (movePath.Length > 0)
		{
			Vector3[] path = new Vector3[movePath.Length];
			for(int i = 0; i < movePath.Length; i++)
			{
				Vector3 newPathPos = new Vector3((MoveDestinationGO.transform.position.x*movePath[i].x + tweenTargetGO.transform.position.x*(1-movePath[i].x)), (MoveDestinationGO.transform.position.y*movePath[i].y + tweenTargetGO.transform.position.y*(1-movePath[i].y)), (MoveDestinationGO.transform.position.z*movePath[i].z + tweenTargetGO.transform.position.z*(1-movePath[i].z))); 
				path[i] = newPathPos;
			}
			tweenHashParams.Add("path", path);
		}
		if (time > 0.0f)
		{			
			tweenHashParams.Add("time", time);
		}
		if (speed > 0.0f)
		{			
			tweenHashParams.Add("speed", speed);
		}
		if (delay > 0.0f)
		{
			tweenHashParams.Add("delay", delay);			
		}
		
		return tweenHashParams;
	}
}

/// <summary>
/// This is used by Inspector2DFloatArray to get around the inspector
/// not support multidemensional arrays, i.e. float[][]
/// </summary>
[System.Serializable]
public class InspectorFloatArray
{
	public float[] floatArray;	
	
	public float this[int index]
	{
		get
		{
			return floatArray[index];
		}
		set
		{
			floatArray[index] = value;
		}
	}
	
	public int Length
	{
		get
		{
			return floatArray.Length;
		}
	}
}

/// <summary>
/// This allows you to fake a multidemensional arrays, i.e. float[][] which the 
/// inspector not support.
/// </summary>
[System.Serializable]
public class Inspector2DFloatArray
{
	public InspectorFloatArray[] floatArrays;

	public float[] this[int index]
	{
		get
		{
			return floatArrays[index].floatArray;
		}
		set
		{
			floatArrays[index].floatArray = value;
		}
	}

	public int Length
	{
		get
		{
			return floatArrays.Length;
		}
	}
}

/// <summary>
/// This is used by Inspector2DGameObjectArray to get around the inspector
/// not support multidemensional arrays, i.e. float[][]
/// </summary>
[System.Serializable]
public class InspectorGameObjectArray
{
	public GameObject[] gameObjectArray;	
	
	public GameObject this[int index]
	{
		get
		{
			return gameObjectArray[index];
		}
		set
		{
			gameObjectArray[index] = value;
		}
	}
	
	public int Length
	{
		get
		{
			return gameObjectArray.Length;
		}
	}
}

/// <summary>
/// This allows you to fake a multidemensional arrays, i.e. float[][] which the 
/// inspector not support.
/// </summary>
[System.Serializable]
public class Inspector2DGameObjectArray
{
	public InspectorGameObjectArray[] gameObjectArrays;
	
	public GameObject[] this[int index]
	{
		get
		{
			return gameObjectArrays[index].gameObjectArray;
		}
		set
		{
			gameObjectArrays[index].gameObjectArray = value;
		}
	}
	
	public int Length
	{
		get
		{
			return gameObjectArrays.Length;
		}
	}
}

