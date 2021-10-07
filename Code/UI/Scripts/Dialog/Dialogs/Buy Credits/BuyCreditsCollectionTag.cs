using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuyCreditsCollectionTag : MonoBehaviour
{
	[SerializeField] private TextMeshPro textLine1;
	[SerializeField] private TextMeshPro textLine2;
	[SerializeField] private Animator tagAnimator;

	public void setText(string locKey1, string locKey2)
	{
		if (textLine1 == null || textLine2 == null)
		{
			// If we are in a game reset anything can be null, bail out early if that is happening.
			return;
		}

		if (!string.IsNullOrEmpty(locKey1))
		{
			textLine1.text = Localize.text(locKey1);
		}
		else
		{
			textLine1.gameObject.SetActive(false);
		}

		if (!string.IsNullOrEmpty(locKey2))
		{
			textLine2.text = Localize.text(locKey2);
		}
		else
		{
			textLine2.gameObject.SetActive(false);
		}

		if (!textLine2.gameObject.activeSelf && !textLine1.gameObject.activeSelf)
		{
			// If both aren't active, throw an error log and turn the object off.
			if (gameObject != null)
			{
				gameObject.SetActive(false);
			}
		}
		else if (!textLine2.gameObject.activeSelf || !textLine1.gameObject.activeSelf)
		{
			// If we are only using one of the labels then we want to
			// move the active label to be in the middle of the two positions.
			float targetY = (textLine1.transform.localPosition.y + textLine2.transform.localPosition.y)/ 2f;
			TextMeshPro targetLabel = textLine1.gameObject.activeSelf ? textLine1 : textLine2;
			CommonTransform.setY(targetLabel.transform, targetY);
		}
	}

	public void show()
	{
		if (tagAnimator != null)
		{
			tagAnimator.Play("hold");
		}
	}

	public void fadeIn()
	{
		if (tagAnimator != null)
		{
			tagAnimator.Play("intro");
		}
	}

	public void fadeOut()
	{
		if (tagAnimator != null)
		{
			tagAnimator.Play("outro");
		}
	}
}
