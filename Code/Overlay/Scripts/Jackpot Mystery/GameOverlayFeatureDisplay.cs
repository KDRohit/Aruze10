using UnityEngine;
using System.Collections;

public class GameOverlayFeatureDisplay : MonoBehaviour
{
	public enum FeatureType
	{
		MYSTERY_GIFT,
		BIG_SLICE,
		PROGRESSIVE,
		MULTI_PROGRESSIVE
	}

	public FeatureType featureType;

	[SerializeField] private Animator animator;
	[SerializeField] private GameObject qualifiedParent;
	[SerializeField] private GameObject unqualifiedParent;
	[SerializeField] private BoxCollider2D reelsBoundsLimit;
	[SerializeField] private Transform hi03OffsetSizeTransform;

	private bool isPositionOffsetForHi03 = false;

	private bool isQualified = false;
	private const string TOOLTIP_GREEN_INTRO = "introGreen";
	private const string TOOLTIP_RED_INTRO = "introRed";
	private const string TOOLTIP_GREEN_OUTRO = "outroGreen";
	private const string TOOLTIP_RED_OUTRO = "outroRed";
	private const string OFF = "off";

	public virtual void init()
	{
		// Override for registering labels for jackpots/etc..
		gameObject.SetActive(shouldShow);
	}

	public virtual void show()
	{
		if (!shouldShow) { return; }
		// Show all elements.
		gameObject.SetActive(true);
		animator.Play(isQualified ? TOOLTIP_GREEN_INTRO : TOOLTIP_RED_INTRO);
	}

	public virtual void hide()
	{
		// Hide all elements
		gameObject.SetActive(false);
		animator.Play(OFF);
	}

	public virtual void hideTooltip()
	{
		// Hide the "tooltip" elements
		animator.Play(isQualified ? TOOLTIP_GREEN_OUTRO : TOOLTIP_RED_OUTRO);
	}	

	public virtual void setQualified(bool isQualified)
	{
		this.isQualified = isQualified;
		if (!shouldShow) { return; }
		Audio.play(isQualified ? "WildInWinningPayline" : "windowscreen0");
		animator.Play(isQualified ? TOOLTIP_GREEN_INTRO : TOOLTIP_RED_INTRO);
	}

	public virtual void setButtons(bool isEnabled)
	{
		// Override for anything that has buttons to disable.
	}

	public virtual bool shouldShow
	{
		get
		{
			return false; // false by default.
		}
	}

	// Offsets the feature to the side (only used by hi03).  Newer games should just scale down instead.
	public void offsetToSideForHi03(Vector3 offsetAnchorPos)
	{
		if (hi03OffsetSizeTransform != null)
		{
			gameObject.transform.localPosition = new Vector3(offsetAnchorPos.x - (hi03OffsetSizeTransform.localScale.x / 2.0f), 0.0f, 0.0f);
			isPositionOffsetForHi03 = true;
		}
	}

	// Called to undo the offset
	public void resetPosition()
	{
		gameObject.transform.localPosition = Vector3.zero;
		isPositionOffsetForHi03 = false;
	}
	
	// Update the position when the resolution changes
	public void updatePosition(Vector3 offsetAnchorPos)
	{
		if (isPositionOffsetForHi03)
		{
			offsetToSideForHi03(offsetAnchorPos);
		}
	}

	public Bounds getBounds()
	{
		if (reelsBoundsLimit != null)
		{
			return reelsBoundsLimit.bounds;
		}
		else
		{
			return new Bounds(Vector3.zero, Vector3.zero);
		}
	}
}
