using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Free Spin bonus for Gen11 Tiger Temptress
 * Uses the PickMajorFreeSpins base class
 */ 
public class Gen11FreeSpins : PickMajorFreeSpins 
{
	// inspector variables
	[SerializeField] private GameObject[] bannerMajorSymbols = null;	// Show the banner for this game
	[SerializeField] private ReelGameBackground backgroundScript;		// Need this to force the wings to show up correctly

	[SerializeField] private float REVEAL_CHARACTER_VO_DELAY;
	[SerializeField] private float SPIN_STAGE_INTRO_VO_DELAY;
	[SerializeField] private float SUMMARY_VO_DELAY;
	[SerializeField] private float MAJOR_SYMBOL_VO_SOUNDS_DELAY;

	private const string OBJECT_PICKME_ANIM_NAME = "pickme";
	private const string OBJECT_REVEAL_MAJOR_ANIM_NAME = "reveal_";
	private const string OBJECT_UNPICKED_MAJOR_ANIM_NAME = "unpicked_";
	private const string BANNER_ANIM_NAME = "zynga02_FreeSpins_BannerAnimation_";

	private const string KEY_PICKME_SOUND = "PickMeKey";
	private const string KEY_PICK_REVEAL_SOUND = "PickAKeyRevealSymbol";
	private const string REVEAL_OTHERS_SOUND_KEY = "reveal_not_chosen";
	private const string PICK_A_KEY_BG_MUSIC = "IdleFreespinTiger";
	private const string PICK_STAGE_INTRO_VO_SOUND = "PickAKeyVOTiger";
	private const string SPIN_STAGE_INTRO_VO_SOUND = "FreespinIntroVOTiger";
	private const string FREESPIN_INTRO_MUSIC = "IntroFreespinTiger";
	private const string FREESPIN_BG_MUSIC_KEY = "freespin";
	private const string SUMMARY_VO = "FreespinSummaryVOTiger";
	private readonly string[] MAJOR_SYMBOL_VO_SOUNDS = { 	"TTIAmTheQueenOfTheTemple",
															"TTMyBengalWarriorServesMeWell",
															"TTSiberianTigressWatchesOverTheTreasure",
															"TTLittleOneEnsuresFutureOfTemple" };

	private const float OBJECT_PICKME_ANIM_LENGTH = 0.8f;
	private const float OBJECT_REVEAL_MAJOR_ANIM_LENGTH = 0.867f;
	

	public override void initFreespins()
	{
		backgroundScript.forceShowFreeSpinWings();

		base.initFreespins();
		
		Audio.switchMusicKeyImmediate(PICK_A_KEY_BG_MUSIC);
		Audio.play(PICK_STAGE_INTRO_VO_SOUND);

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

		PickGameButton gameButton = randomObject.GetComponentInChildren<PickGameButton>();

		Audio.play(KEY_PICKME_SOUND);
		gameButton.animator.Play(OBJECT_PICKME_ANIM_NAME);
		yield return new TIWaitForSeconds(OBJECT_PICKME_ANIM_LENGTH);
	}

	/// Handle an object being picked
	protected override IEnumerator showPick(GameObject button)
	{
		PickGameButton gameButton = button.GetComponentInChildren<PickGameButton>();
		Audio.play(KEY_PICK_REVEAL_SOUND);
		Audio.play(MAJOR_SYMBOL_VO_SOUNDS[stageTypeAsInt()], 1, 0, MAJOR_SYMBOL_VO_SOUNDS_DELAY);
		gameButton.animator.Play(OBJECT_REVEAL_MAJOR_ANIM_NAME + stageTypeAsString());
		yield return new TIWaitForSeconds(OBJECT_REVEAL_MAJOR_ANIM_LENGTH);
	}

	/// Handle showing the unpicked reveals
	protected override IEnumerator showReveal(GameObject button)
	{
		PickGameButton gameButton = button.GetComponentInChildren<PickGameButton>();
		Audio.play(Audio.soundMap(REVEAL_OTHERS_SOUND_KEY));
		gameButton.animator.Play(OBJECT_UNPICKED_MAJOR_ANIM_NAME + PickMajorFreeSpins.convertStage1TypeToString(getNextRandomizedStageType()));
		yield break;
	}

	/// Overriding to handle showing the right banner
	protected override IEnumerator transitionIntoStage2()
	{
		yield return StartCoroutine(base.transitionIntoStage2());
		spinStageObjects.transform.position = Vector3.zero;
		int pickedMajorIndex = stageTypeAsInt();
		for (int i = 0; i < bannerMajorSymbols.Length; i++)
		{
			if (i == pickedMajorIndex)
			{
				bannerMajorSymbols[i].SetActive(true);
			}
			else
			{
				bannerMajorSymbols[i].SetActive(false);
			}
		}

		banner.SetActive(true);

		Audio.play(SPIN_STAGE_INTRO_VO_SOUND, 1, 0, SPIN_STAGE_INTRO_VO_DELAY);
	}

	/// Overriding to handle the intro music that transitions into the free spin music
	protected override void beginFreeSpinMusic()
	{
		// play free spin audio and start the spin
		if (!cameFromTransition)
		{
			Audio.playMusic(FREESPIN_INTRO_MUSIC);
			Audio.switchMusicKey(Audio.soundMap(FREESPIN_BG_MUSIC_KEY));
		}
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		//Audio.play(Audio.soundMap("prespin_idle_loop"));
		// Play the summary VO .6 seconds after the game has ended.
		Audio.play(SUMMARY_VO, 1.0f, 0.0f, SUMMARY_VO_DELAY);
		base.gameEnded();
	}
}
