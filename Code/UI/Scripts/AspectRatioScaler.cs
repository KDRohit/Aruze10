using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Scales the object toward the goal scale based on how close to the target aspect ratio
the device is compared to iPad, which is treated as the basis for UI layout.
*/
public class AspectRatioScaler : TICoroutineMonoBehaviour
{
	private const float PORTRAIT_IPAD_ASPECT = 0.75f;
	public const float IPAD_ASPECT = 1.33f;

	public Vector2 goalScale;
	public float goalAspectRatio;

	public Vector2 portraitGoalScale;
	public float portraitGoalAspectRatio;
	
	private Vector3 originalScale;
	protected List<Vector2> tmpScales = new List<Vector2>();

	[SerializeField] private TextMeshProMasker[] tmpMaskers;

	private UIAnchor[] anchorsInChildren;
	private bool waitingToReanchor = false;
	private bool isAttachedToDialog = false;

	public void Awake()
	{
		originalScale = transform.localScale;

		for (int i = 0; i < tmpMaskers.Length; i++)
		{
			TextMeshProMasker masker = tmpMaskers[i];
			if (masker == null)
			{
				tmpScales.Add(Vector2.zero); //add empty vector so indexs match
				continue;
			}
			
			if (masker.currentOrientation == TextMeshProMasker.Orientation.VERTICAL)
			{
				tmpScales.Add(new Vector2(masker.verticalMaskTopOffset, masker.verticalMaskBottomOffset));
			}
			else
			{
				tmpScales.Add(new Vector2(masker.horizantalMaskLeftOffset, masker.horizantalMaskRightOffset));
			}
		}

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

			Vector3 oldScale = transform.localScale;

			if (ResolutionChangeHandler.isInPortraitMode && portraitGoalScale != Vector2.zero)
			{
				Vector3 scaleDifference = originalScale - new Vector3(portraitGoalScale.x, portraitGoalScale.y, originalScale.z);
				float scalePercent = Mathf.Clamp01((PORTRAIT_IPAD_ASPECT-NGUIExt.aspectRatio)/(PORTRAIT_IPAD_ASPECT-portraitGoalAspectRatio));
				scaleDifference *= scalePercent;
				transform.localScale = originalScale-scaleDifference;
			}
			else
			{
				float normalized = CommonMath.normalizedValue(IPAD_ASPECT, goalAspectRatio, NGUIExt.aspectRatio);
				Vector3 scale = new Vector3(goalScale.x, goalScale.y, originalScale.z);
				transform.localScale = Vector3.Lerp(originalScale, scale, normalized);
			}

			adjustMaskingSize(oldScale);

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
			if (anchorsInChildren[i] != null)
			{
				anchorsInChildren[i].reposition();
			}
		}
	}

	private void adjustMaskingSize(Vector3 oldScale)
	{
		if (tmpMaskers != null)
		{
			float xScale = transform.localScale.x / goalScale.x;
			float yScale = transform.localScale.y / goalScale.y;

			for (int i = 0; i < tmpMaskers.Length; i++)
			{
				TextMeshProMasker masker = tmpMaskers[i];
				if (masker == null)
				{
					continue;
				}

				if (masker.currentOrientation == TextMeshProMasker.Orientation.VERTICAL)
				{
					// restore the original size before scaling
					masker.verticalMaskTopOffset = (int)tmpScales[i].x;
					masker.verticalMaskBottomOffset = (int)tmpScales[i].y;
					
					masker.verticalMaskTopOffset = (int)((float)masker.verticalMaskTopOffset * yScale);
					masker.verticalMaskBottomOffset = (int)((float)masker.verticalMaskBottomOffset * yScale);
				}
				else
				{
					// restore the original size before scaling
					masker.horizantalMaskLeftOffset = (int)tmpScales[i].x;
					masker.horizantalMaskRightOffset = (int)tmpScales[i].y;
					
					masker.horizantalMaskLeftOffset = (int)((float)masker.horizantalMaskLeftOffset * xScale);
					masker.horizantalMaskRightOffset = (int)((float)masker.horizantalMaskRightOffset * xScale);
				}
			}
		}
	}
}
