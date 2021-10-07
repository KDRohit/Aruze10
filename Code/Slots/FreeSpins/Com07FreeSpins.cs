using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Free Spin bonus for Com07 Archie
 * Uses the PickMajorFreeSpins base class
 * Clone of zynga02
 */ 
public class Com07FreeSpins : PickMajorFreeSpins 
{
	// inspector variables
	[SerializeField] private Animator bannerAnimator = null;

	[SerializeField] private string OBJECT_PICKME_ANIM_NAME = "";
	[SerializeField] private string OBJECT_REVEAL_MAJOR_ANIM_NAME = "";
	[SerializeField] private string OBJECT_UNPICKED_MAJOR_ANIM_NAME = "";
	[SerializeField] private string BANNER_ANIM_NAME = "";

	[SerializeField] private string PICKOBJECT_PICKME_SOUND = "";
	[SerializeField] private string PICKOBJECT_REVEAL_OTHERS_SOUND = "";
	[SerializeField] private string PICK_A_MAJOR_BG_MUSIC = "";
	[SerializeField] private string PICK_A_MAJOR_INTRO_VO_SOUND = "";
	[SerializeField] private string REVEAL_CHARACTER_SWEETENER_SOUND = "";
	[SerializeField] private string REVEAL_CHARACTER_VO_POSTFIX = "";
	[SerializeField] private string FREESPIN_INTRO_SOUND = "";

	[SerializeField] private string SPIN_STAGE_INTRO_VO_SOUND = "";
	[SerializeField] private string FREESPIN_BG_MUSIC = "";
	[SerializeField] private string FREESPIN_SUMMARY_FANFARE_SOUND = "";

	[SerializeField] private float OBJECT_PICKME_ANIM_LENGTH = 1.0f;
	[SerializeField] private float OBJECT_REVEAL_MAJOR_ANIM_LENGTH = 1.0f;
	[SerializeField] private float REVEAL_CHARACTER_VO_DELAY = 0.0f;
	[SerializeField] private float SPIN_STAGE_INTRO_VO_DELAY = 0.0f;
	[SerializeField] private float SUMMARY_VO_DELAY = 0.0f;

	public override void initFreespins()
	{
		base.initFreespins();
		playPickMajorSound(PICK_A_MAJOR_BG_MUSIC, 0, true);
		playPickMajorSound(PICK_A_MAJOR_INTRO_VO_SOUND);
		// Cache a bunch of major symbols to the pool in a coroutine, that way it doesn't take a performance hit when spinning
		cacheSymbolsToPool(stageTypeAsString(), 26, true);
	}

	/// Play a pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomObjectIndex = 0;

		randomObjectIndex = Random.Range(0, buttonSelections.Count);
		GameObject randomObject = buttonSelections[randomObjectIndex];

		Zynga02FreeSpinButton gameButton = randomObject.GetComponent<Zynga02FreeSpinButton>();

		playPickMajorSound(PICKOBJECT_PICKME_SOUND);
		gameButton.animator.Play(OBJECT_PICKME_ANIM_NAME);
		yield return new TIWaitForSeconds(OBJECT_PICKME_ANIM_LENGTH);
	}

	/// Handle an object being picked
	protected override IEnumerator showPick(GameObject button)
	{
		Zynga02FreeSpinButton gameButton = button.GetComponent<Zynga02FreeSpinButton>();
		playPickMajorSound(PICKOBJECT_REVEAL_OTHERS_SOUND);
		playPickMajorSound(REVEAL_CHARACTER_SWEETENER_SOUND, REVEAL_CHARACTER_VO_DELAY);
		playPickMajorSound(stageTypeAsString() + REVEAL_CHARACTER_VO_POSTFIX, REVEAL_CHARACTER_VO_DELAY);
		gameButton.animator.Play(OBJECT_REVEAL_MAJOR_ANIM_NAME + stageTypeAsString());
		yield return new TIWaitForSeconds(OBJECT_REVEAL_MAJOR_ANIM_LENGTH);
	}

	/// Handle showing the unpicked reveals
	protected override IEnumerator showReveal(GameObject button)
	{
		Zynga02FreeSpinButton gameButton = button.GetComponent<Zynga02FreeSpinButton>();
		gameButton.greyOutMajorSymbols();
		playPickMajorSound(PICKOBJECT_REVEAL_OTHERS_SOUND);
		gameButton.animator.Play(OBJECT_UNPICKED_MAJOR_ANIM_NAME + PickMajorFreeSpins.convertStage1TypeToString(getNextRandomizedStageType()));
		yield break;
	}

	/// Overriding to handle showing the right banner
	protected override IEnumerator transitionIntoStage2()
	{
		yield return StartCoroutine(base.transitionIntoStage2());

		bannerAnimator.Play(BANNER_ANIM_NAME + stageTypeAsString());

		playPickMajorSound(SPIN_STAGE_INTRO_VO_SOUND, SPIN_STAGE_INTRO_VO_DELAY);
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		playPickMajorSound(FREESPIN_SUMMARY_FANFARE_SOUND);
		playPickMajorSound(FREESPIN_SUMMARY_FANFARE_SOUND, SUMMARY_VO_DELAY);
		base.gameEnded();
	}

	protected override void beginFreeSpinMusic()
	{
		// play free spin audio and start the spin
		if (!cameFromTransition)
		{
			playPickMajorSound(FREESPIN_INTRO_SOUND);
			playPickMajorSound(FREESPIN_BG_MUSIC, 0.0f, true);
		}
	}

	//Used to handle playing sound/music via soundmap or key names
	private void playPickMajorSound(string key, float delay = 0.0f, bool changeMusicKey = false)
	{
		if (key != "")
		{
			if (changeMusicKey)
			{
				if (Audio.canSoundBeMapped(key))
				{
					Audio.switchMusicKeyImmediate(Audio.soundMap(key), delay);
				}
				else
				{
					Audio.switchMusicKeyImmediate(key, delay);
				}
			}
			else
			{
				if (Audio.canSoundBeMapped(key))
				{
					Audio.playWithDelay(key, delay);
				}
				else
				{
					Audio.playWithDelay(key, delay);
				}
			}
		}
	}
}

