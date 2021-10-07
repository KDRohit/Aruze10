using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Basic data structure for the appearance of a game piece on the YBR bonus game.
*/

public class OzYBRPiece : TICoroutineMonoBehaviour
{
	public UISprite icon;
	public GameObject glow;
	public UILabel label;	// To be removed when prefabs are updated.
	public LabelWrapperComponent labelWrapperComponent;

	public LabelWrapper labelWrapper
	{
		get
		{
			if (_labelWrapper == null)
			{
				if (labelWrapperComponent != null)
				{
					_labelWrapper = labelWrapperComponent.labelWrapper;
				}
				else
				{
					_labelWrapper = new LabelWrapper(label);
				}
			}
			return _labelWrapper;
		}
	}
	private LabelWrapper _labelWrapper = null;
	
	public int finalValue = 0;
	
	/// Animate the covering piece away.
	public IEnumerator fadeIcon()
	{
		float duration = .5f;
		iTween.ValueTo(gameObject, iTween.Hash("from", 1, "to", 0, "time", duration, "onupdate", "updateRevealingAlpha"));
		iTween.ScaleTo(icon.gameObject, iTween.Hash("scale", icon.transform.localScale * 1.5f, "time", duration, "easetype", iTween.EaseType.linear, "oncomplete", "onTweenComplete",
			"oncompletetarget", this.gameObject));
	
		// Wait for the animation to finish.
		yield return new WaitForSeconds(duration + .1f);	// Add a little more to make sure the tweening is done before setting inactive.

		icon.gameObject.SetActive(false);
	}

	private void onTweenComplete()
	{
		icon.MakePixelPerfect();
	}

	/// iTween update callback for fading the picked piece to reveal the thing under it.
	private void updateRevealingAlpha(float alpha)
	{
		icon.alpha = alpha;
	}
	
	public bool EnableGlow
	{
		get { return this.glow != null && this.glow.activeSelf; }
		set
		{
			if(this.glow != null)
			{
				this.glow.SetActive(value);
			}
		}
	}
}

