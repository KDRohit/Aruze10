using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This module can be used on FreeSpin games which grant freespins on certain outcomes and need to display an animation.
// NOTE : This is now DEPRECATED, use the new version which will still be called AnimatedGrantFreespinsModule
public class DeprecatedAnimatedGrantFreespinsModule : GrantFreespinsModule 
{
	[SerializeField] private GameObject freespinAwardAnimation;				// Animation that plays over the main game area (reels)
	[SerializeField] private GameObject winBoxAnimation;					// Animation that plays on the Spin Panel
	[SerializeField] private float delayBeforeIncrementingSpinCount = 0.0f;	// Delay before the player is credited the extra spins, may want this to occur before the animation finishes
	[SerializeField] private float delayBeforeProceeding = 1.0f;			// Delay before the effect is over and the game proceeds
	[SerializeField] private bool instantiatePrefab = true;
	[SerializeField] private Vector3 awardAnimationOffset = new Vector3();
	[SerializeField] private string freespinAwardAnimationSoundName;
	[SerializeField] private AudioListController.AudioInformationList freespinAwardVOList;
	[SerializeField] private LabelWrapperComponent amountTextLabel;
	[SerializeField] private LabelWrapperComponent amountTextLabelShadow;
	[SerializeField] private bool animateReelSymbols = false;				// Play the bonus outcome symbol animation again (useful for anticipation-swaps)
	[SerializeField] private bool shouldAlignWinboxAnimation = false;

	private GameObject currentFreespinAwardAnimation;
	
	public override void Awake()
	{
		base.Awake();
		if (instantiatePrefab)
		{
			currentFreespinAwardAnimation = (GameObject)CommonGameObject.instantiate(freespinAwardAnimation);
			currentFreespinAwardAnimation.transform.parent = gameObject.transform;
			currentFreespinAwardAnimation.transform.localPosition = Vector3.zero;
			currentFreespinAwardAnimation.transform.localScale = Vector3.one;
			currentFreespinAwardAnimation.SetActive(false);
			Vector3 pos = currentFreespinAwardAnimation.transform.localPosition;
			pos.x += awardAnimationOffset.x;
			pos.y += awardAnimationOffset.y;
			pos.z += awardAnimationOffset.z;
			currentFreespinAwardAnimation.transform.localPosition = pos;
		}
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (animateReelSymbols)
		{
			// animate bonus symbols on reels for freespin awards
			SlotReel[] allReels = this.reelGame.engine.getAllSlotReels();
			for(int i = 0; i < allReels.Length; i++)
			{
				allReels[i].animateBonusSymbols(null);
			}
		}

		if (!string.IsNullOrEmpty(freespinAwardAnimationSoundName))
		{
			Audio.playSoundMapOrSoundKey(freespinAwardAnimationSoundName);
			if (freespinAwardVOList != null && freespinAwardVOList.Count != 0)
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(freespinAwardVOList));
			}
		}

		if (instantiatePrefab)
		{
			currentFreespinAwardAnimation.SetActive(true);
		}
		else
		{
			freespinAwardAnimation.SetActive(true);
		}

		if (amountTextLabel != null)
		{
			amountTextLabel.text = numberOfFreeSpins.ToString();	
		}

		if (amountTextLabelShadow != null)
		{
			amountTextLabelShadow.text = numberOfFreeSpins.ToString();	
		}

		if(!Audio.isPlaying(Audio.soundMap("freespin")))
		{
			Audio.play(Audio.soundMap("freespin"));
		}

		yield return new TIWaitForSeconds(delayBeforeIncrementingSpinCount);

		if(winBoxAnimation != null)
		{
			if (shouldAlignWinboxAnimation)
			{
				winBoxAnimation.transform.position = BonusSpinPanel.instance.spinCountLabel.transform.position;

			}
			winBoxAnimation.SetActive(true);
		}

		incrementFreespinCount();

		yield return new TIWaitForSeconds(delayBeforeProceeding);
		
		if(winBoxAnimation != null)
		{
			winBoxAnimation.SetActive(false);
		}

		if (instantiatePrefab)
		{
			currentFreespinAwardAnimation.SetActive(false);
		}
		else
		{
			freespinAwardAnimation.SetActive(false);
		}
	}
}
