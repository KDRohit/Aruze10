using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Free Spin class for elvira04 The Witch Is Back.
 * Based on ani03, but using Animators instead of Animations.
 */
 
public class Elvira04FreeSpins : PickMajorFreeSpins 
{
	// Picking Tunables
	
	[SerializeField] private string PICKING_INTRO_VO_NAME = "PortalVOEL04";                // Intro VO when pickem starts.
	[SerializeField] private string PICKING_BG_MUSIC_NAME = "IdleFreespinEL04";            // Picking background music.
	[SerializeField] private string PICK_ME_SOUND_NAME = "PickMePotionEL04";               // Pick me sound.
	[SerializeField] private string PICK_SOUND_NAME = "PickAPotionRevealSymbolEL04";       // Sound when you pick a pickem.
	[SerializeField] private string UNPICKED_SOUND_NAME = "PickAPotionRevealOthersEL04";   // Sound when reveal an unpicked pickem.
	
	[SerializeField] private Animator[] pickAnimators = null;   // Picks (they're animators, not animations).
	[SerializeField] private Animator bannerAnimator = null;

	// Member Variables
	private int pickIndex = -1;   // Which pickem did you pick?

	// Animations
	// Rename animator states to match the anim names.
	// Add more entries if your game has more picks.
	
	private const string PICK_ME_ANIM_NAME = "pickme"; // Pick me animation name.
	
	private readonly string[] PICK_ANIM_NAMES = new string[4]
	{
		"reveal1",
		"reveal2",
		"reveal3",
		"reveal4",
	};
	
	private readonly string[] UNPICKED_ANIM_NAMES = new string[4]
	{
		"reveal1_gray",
		"reveal2_gray",
		"reveal3_gray",
		"reveal4_gray",
	};

	private readonly string[] BANNER_ANIM_NAMES = new string[4]
	{
		"banner1",
		"banner2",
		"banner3",
		"banner4"
	};
	
	protected override void setupStage()
	{
		BonusGameManager.instance.wings.forceShowFreeSpinWings(showWingsInForeground);
		
		if (buttonSelections.Count == 0)
		{
			foreach (Animator pickAnimator in pickAnimators)
			{
				buttonSelections.Add(pickAnimator.gameObject);
			}
		}
		
		if (BonusGameManager.instance.bonusGameName.Contains("_M1"))
		{
			revealSpriteIndex = 0;
			bannerAnimator.Play("label 1");
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M2"))
		{
			revealSpriteIndex = 1;
			bannerAnimator.Play("label 2");
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M3"))
		{
			revealSpriteIndex = 2;
			bannerAnimator.Play("label 3");
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M4"))
		{
			revealSpriteIndex = 3;
			bannerAnimator.Play("label 4");
		}
		else
		{
			revealSpriteIndex = -1;
			
			Debug.LogError(
				"There was an unexpected format for the name of the elvira04 Free Spins game, " +
				"don't know what symbol to reveal for " + BonusGameManager.instance.summaryScreenGameName);
		}
		
		if (revealSpriteIndex != -1)
		{
			Audio.play(PICKING_INTRO_VO_NAME);
			Audio.switchMusicKeyImmediate(PICKING_BG_MUSIC_NAME, 0.0f);
			
			setupAnimationComponents();
		}
		else
		{
			inputEnabled = false;
			StartCoroutine(transitionIntoStage2());
		}
	}

	protected override IEnumerator pickMeCallback()
	{
		if (pickIndex == -1)
		{
			int pickMeIndex = Random.Range(0, pickAnimators.Length);
			
			Animator pickMeAnimator = pickAnimators[pickMeIndex];
			Audio.play(PICK_ME_SOUND_NAME);
			
			yield return StartCoroutine(
				CommonAnimation.playAnimAndWait(pickMeAnimator, PICK_ME_ANIM_NAME));
		}
	}

	protected override IEnumerator knockerClickedCoroutine(GameObject clickedObject)
	{
		if (pickIndex == -1)
		{
			pickIndex = buttonSelections.IndexOf(clickedObject);
			Animator pickAnimator = pickAnimators[pickIndex];		
			
			pickAnimator.Play(PICK_ANIM_NAMES[revealSpriteIndex]);
			Audio.play(PICK_SOUND_NAME);
			
			yield return StartCoroutine(
				CommonAnimation.waitForAnimDur(pickAnimator));
			yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS);
			yield return StartCoroutine(revealOthers());
			
			// Transition into the freespins game
			
			bannerAnimator.gameObject.SetActive(true);
			bannerAnimator.Play(BANNER_ANIM_NAMES[revealSpriteIndex]);
			
			yield return StartCoroutine(transitionIntoStage2());
		}
	}
	
	private IEnumerator revealOthers()
	{
		int iGraySprite = 0;
		
		for (int iReveal = 0; iReveal < pickAnimators.Length; iReveal++)
		{
			if (iReveal != pickIndex)
			{
				if (iGraySprite == revealSpriteIndex)
				{
					iGraySprite++;
				}
				
				Animator revealAnimator = pickAnimators[iReveal];
				revealAnimator.Play(UNPICKED_ANIM_NAMES[iGraySprite]);
				
				Audio.play(UNPICKED_SOUND_NAME);
				
				StartCoroutine(CommonAnimation.waitForAnimDur(revealAnimator));
				yield return new TIWaitForSeconds(TIME_BETWEEN_REVEALS);
					
				iGraySprite++;
			}
		}
	}
	
}
