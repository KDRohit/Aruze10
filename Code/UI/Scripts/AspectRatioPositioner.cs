using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Positions the object toward the goal position based on how close to the target aspect ratio
the device is compared to iPad, which is treated as the basis for UI layout.
*/
public class AspectRatioPositioner : TICoroutineMonoBehaviour
{
	private const float IPAD_ASPECT = 1.33f;
	private const float PORTRAIT_IPAD_ASPECT = 0.75f;

	public Vector2 goalPosition;
	public float goalAspectRatio;

	public Vector2 portraitGoalPosition;
	public float portraitGoalAspectRatio;
	
	private Vector3 originalPosition;
	
	private UIAnchor[] anchorsInChildren;
	private bool waitingToReanchor = false;
	private bool isAttachedToDialog = false;
	
	public void Awake()
	{
		originalPosition = transform.localPosition;
		
		//If this rescaler is linked to a dialog then grab the uianchors below us so we can reanchor after scaling and being tweened by the Dialog class
		isAttachedToDialog = GetComponentInParent<DialogBase>() != null || GetComponent<DialogBase>() != null;
		anchorsInChildren = GetComponentsInChildren<UIAnchor>(true);
	}
	
	public void Update()
	{
		if (!waitingToReanchor)
		{
			if (goalAspectRatio < IPAD_ASPECT)
			{
				Debug.LogError("Come on, man. You can't specify a goal aspect ratio that's smaller than the baseline iPad aspect.");
			}

			if (ResolutionChangeHandler.isInPortraitMode && portraitGoalPosition != Vector2.zero)
			{
				Vector3 posDifference = originalPosition - new Vector3(portraitGoalPosition.x, portraitGoalPosition.y, originalPosition.z);
				float scalePercent = 0.0f;
				scalePercent = Mathf.Clamp01((PORTRAIT_IPAD_ASPECT - NGUIExt.aspectRatio) / (PORTRAIT_IPAD_ASPECT - portraitGoalAspectRatio));
				posDifference *= scalePercent;
				transform.localPosition = originalPosition - posDifference;
			}
			else
			{
				float normalized = CommonMath.normalizedValue(IPAD_ASPECT, goalAspectRatio, NGUIExt.aspectRatio);
				Vector3 pos = new Vector3(goalPosition.x, goalPosition.y, originalPosition.z);
				transform.localPosition = Vector3.Lerp(originalPosition, pos, normalized);
			}

			// Don't destroy, so we can re-enable if resolution changes.
			if (anchorsInChildren != null && anchorsInChildren.Length > 0)
			{
				if (isAttachedToDialog)
				{
					StartCoroutine(updateChildAnchorsAfterDialogIntro());
				}
				else
				{
					updateChildAnchors();
					enabled = false;
				}
			}
			else
			{
				enabled = false;
			}
		}
	}
	
	private IEnumerator updateChildAnchorsAfterDialogIntro()
	{
		waitingToReanchor = true;
		
		while (Dialog.instance.isOpening)
		{
			yield return null;
		}

		updateChildAnchors();

		enabled = false;
		waitingToReanchor = false;
	}

	private void updateChildAnchors()
	{
		for (int i = 0; i < anchorsInChildren.Length; i++)
		{
			anchorsInChildren[i].reposition();
		}
	}
}
