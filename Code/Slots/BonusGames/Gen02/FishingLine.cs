using UnityEngine;
using System.Collections;

public class FishingLine : TICoroutineMonoBehaviour 
{
	public LineRenderer renderedLine; //< The actual fishing line. The object is set in editor;
	public GameObject hook; //< Hook at the end of the line.
	[HideInInspector]public Transform rodAnchor; //< Empty game object in the penguin animations used as the start location of the line/end of fishing rod

	[HideInInspector]private GameObject _fish; //< The currently active Fish

	private Transform fishMouth; //< Transform object on the location of the currently active fishes mouth.
	private bool reeledIn = false; //< If true the hook and line are both kept at the rod anchor point

	private Vector3 lineEnd;
	private Vector3 tempPos;
	
	void Awake()
	{
		reeledIn = false;
		renderedLine.useWorldSpace = true;
	}
	
	/// <summary>
	/// Lowers the hook and line into the water to a set y position.
	/// </summary>
	public void lower()
	{
		reeledIn = false;
		Vector3 temp = hook.transform.localPosition;
		temp.y -= 644;
		iTween.MoveTo(hook, iTween.Hash("position", temp,
										"time", 1.5f,
										"easetype", iTween.EaseType.linear,
										"oncompletetarget", this.gameObject,
										"islocal", true));
	}

	
	// Update is called once per frame
	void Update () 
	{
		// Setup a var to store the end position for the line.
		lineEnd = Vector3.zero;
		// Only update things if the rod anchor is set. Without a line anchor none of this can make any sense.
		if (rodAnchor != null)
		{
			// keep the hook moving around relative to the rod
			tempPos = hook.transform.position;
			tempPos.x = rodAnchor.position.x;
			hook.transform.position = tempPos;
			CommonTransform.setZ(hook.transform, 0);
			// set the link end poitn to the hook
			lineEnd = hook.transform.position;

			// If fish is set draw the line to the fish's mouth instead of the hook and move the hook to the fish
			if (fish != null && !reeledIn)
			{
				hook.transform.localPosition = fish.transform.localPosition;
				lineEnd = fishMouth.position;
				// Once the fish reached a certain height, have the line stop following it and just go to the rod anchor position.
				if(fish.transform.localPosition.y > 600)
				{
					reeledIn = true;
				}
			}
			else if (reeledIn)
			{
				// Draw the line to the end of the fishing rod
				hook.transform.position = rodAnchor.position;
			}
			else
			{
				// Default case draws from the Rod anchor down 640 units and has the hook placed there.
				hook.transform.position = rodAnchor.position;
				tempPos = hook.transform.localPosition;
				tempPos.y -= 640;
				tempPos.z = 0;
				hook.transform.localPosition = tempPos;
			}
			// Set the start of the line to the rod anchor
			renderedLine.SetPosition(0, rodAnchor.position);
		}

		// Move the line end down slightly to better reach the eye on the hook.
		lineEnd.y -= 0.03f;
		// Setup the second line point to whereever the if block has determined.
		renderedLine.SetPosition(1, lineEnd);
	}

	/// <summary>
	/// Gets or sets the fish. On set the fishMouth transform is automatically gotten from the fish.
	/// </summary>
	public GameObject fish {
		get {
			return _fish;
		}
		set {
			_fish = value;
			if(fish != null)
			{
				fishMouth = _fish.transform.Find("LineTarget");
			}
			else
			{
				fishMouth = null;
			}
		}
	}
}
