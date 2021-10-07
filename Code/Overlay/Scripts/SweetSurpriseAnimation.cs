using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SweetSurpriseAnimation : TICoroutineMonoBehaviour 
{
	public GameObject qualifiedElementsParent;
	public GameObject nonQualifiedElementsParent;
	public GameObject rushImageQualified;
	public GameObject rushImageNonQualified;
	public GameObject rushFlashQualified;
	public GameObject rushFlashNonQualified;

	//used for spacing purposes.
	public GameObject backgroundSprite;

	private static Vector3 VERY_SMALL = new Vector3(0.01f, 0.01f, 0.01f);

	private bool isAnimationPlaying = false;

	// Animation pieces
	private GameObject parent;
	private GameObject rushIcon;
	private GameObject rushFlash;

	void Start()
	{
		
		resetAnimation(false);
		setAnimationPieces();
		// Start both qualified and nonqualified animations to always run forever.
		// The one that is currently visible will be controlled by whether the bet is qualified or not.
		StartCoroutine(loopAnimation());

	}

	private void resetAnimation(bool isHardReset = true)
	{
		CommonTransform.setY(qualifiedElementsParent.transform, backgroundSprite.transform.localScale.y * 1.5f);
		CommonTransform.setY(nonQualifiedElementsParent.transform, backgroundSprite.transform.localScale.y * 1.5f);

		rushFlashQualified.transform.localScale = VERY_SMALL;
		rushFlashNonQualified.transform.localScale = VERY_SMALL;
		rushImageNonQualified.transform.localScale = VERY_SMALL;
		rushImageQualified.transform.localScale = VERY_SMALL;

		rushFlashQualified.SetActive(false);
		rushFlashNonQualified.SetActive(false);
		rushImageNonQualified.SetActive(false);
		rushImageQualified.SetActive(false);

		if (isHardReset)
		{
			qualifiedElementsParent.SetActive(false);
			nonQualifiedElementsParent.SetActive(false);
		}
	}

	private void setAnimationPieces()
	{
		bool isQualified;
		if (SlotBaseGame.instance == null || GameState.game == null)
		{
			isQualified = false;
		}
		else
		{
			isQualified = (SlotBaseGame.instance.currentWager >= GameState.game.specialGameMinQualifyingAmount);
		}
		
		parent  = (isQualified) ? qualifiedElementsParent : nonQualifiedElementsParent;
		rushIcon = (isQualified) ? rushImageQualified : rushImageNonQualified;
		rushFlash  = (isQualified) ? rushFlashQualified : rushFlashNonQualified;
	}

	public void prepareAnimation(bool isQualifiedBet)
	{
		qualifiedElementsParent.SetActive(isQualifiedBet);
		nonQualifiedElementsParent.SetActive(!isQualifiedBet);

		setAnimationPieces();
	}

	private IEnumerator loopAnimation()
	{
		isAnimationPlaying = true;

		while (MysteryGift.isIncreasedMysteryGiftChance)
		{

			yield return null;

			iTween.MoveTo(parent, iTween.Hash("position", Vector3.zero, "islocal", true, "time", 1f, "easetype", iTween.EaseType.easeInOutQuad));
			
			yield return new WaitForSeconds(1f);

			// Make these active only right before they animate, to make sure we don't see little dots of the small scale versions.
			rushFlash.SetActive(true);
			rushIcon.SetActive(true);

			iTween.ScaleTo(rushFlash, iTween.Hash("scale", Vector3.one, "islocal", true, "time", 0.5f, "easetype", iTween.EaseType.easeOutElastic));
			iTween.ScaleTo(rushIcon, iTween.Hash("scale", Vector3.one, "islocal", true, "time", 2f, "easetype", iTween.EaseType.easeOutElastic));

			yield return new WaitForSeconds(5f);

			// Reset
			rushFlash.transform.localScale = VERY_SMALL;
			rushIcon.transform.localScale = VERY_SMALL;
			rushFlash.SetActive(false);
			rushIcon.SetActive(false);
			CommonTransform.setY(parent.transform, backgroundSprite.transform.localScale.y * 1.5f);

			yield return new WaitForSeconds(5f);
		}

		isAnimationPlaying = false;
		resetAnimation();
		yield return null;
	}
}


