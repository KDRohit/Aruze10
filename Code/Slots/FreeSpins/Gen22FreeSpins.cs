using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Free Spin bonus for Gen22 Viking's Hoard
 * Uses the PickMajorFreeSpins base class
 */ 
public class Gen22FreeSpins : PickMajorFreeSpins 
{
	// inspector variables
	[SerializeField] private GameObject[] bannerMajorSymbols = null;	// Show the banner for this game
	[SerializeField] private ReelGameBackground backgroundScript;		// Need this to force the wings to show up correctly
	
	[SerializeField] private float REVEAL_CHARACTER_VO_DELAY;
	[SerializeField] private float SPIN_STAGE_INTRO_VO_DELAY;
	[SerializeField] private float SUMMARY_VO_DELAY;
	[SerializeField] private float MAJOR_SYMBOL_VO_SOUNDS_DELAY;
	
	[SerializeField] private string OBJECT_PICKME_ANIM_NAME = "pickme";
	[SerializeField] private string OBJECT_REVEAL_MAJOR_ANIM_NAME = "reveal_";
	[SerializeField] private string OBJECT_UNPICKED_MAJOR_ANIM_NAME = "unpicked_";
	
	private const string WEAPON_PICKME_SOUND = "PickMeWeaponViking";
	private const string WEAPON_PICK_REVEAL_SOUND = "PickAWeaponRevealSymbolViking";
	private const string REVEAL_OTHERS_SOUND = "WeaponRevealOthersViking";
	private const string PICK_A_WEAPON_BG_MUSIC = "IdleFreespinViking";
	private const string SPIN_STAGE_INTRO_VO_KEY = "freespin_intro_vo";
	private const string FREESPIN_INTRO_MUSIC = "IntroFreespinViking";
	private const string FREESPIN_BG_MUSIC_KEY = "freespin";
	private const string SUMMARY_VO = "freespin_summary_vo";

	private readonly string[] WEAPON_PICK_INTRO_VO = { 	"PickAWeaponVO_01_Viking",
		"PickAWeaponVO_02_Viking"};

	private readonly string[] MAJOR_SYMBOL_VO_SOUNDS = { "RevealWeaponVO_01_Viking",
		"RevealWeaponVO_02_Viking",
		"RevealWeaponVO_03_Viking",
		"RevealWeaponVO_04_Viking"};

	
	private const float OBJECT_PICKME_ANIM_LENGTH = 0.8f;
	private const float WAIT_FOR_PICK_REVEAL_DUR = 2.25f; // Wait for the reveal and the voice-over.
	
	
	public override void initFreespins()
	{
		base.initFreespins();
		StartCoroutine(showWings());
		BonusGameManager.instance.summaryScreenGameName = "gen22_free_spin_m1";

		Audio.switchMusicKeyImmediate(PICK_A_WEAPON_BG_MUSIC);
		Audio.play(WEAPON_PICK_INTRO_VO[Random.Range(0,2)]);
		
		// Cache a bunch of major symbols to the pool in a coroutine, that way it doesn't take a performance hit when spinning
		cacheSymbolsToPool(stageTypeAsString(), 26, true);
	}

	private IEnumerator showWings()
	{
		//Need this since BonusGameManager is yielding a frame and hiding our wings by default
		yield return null;
		BonusGameManager.instance.wings.forceLoadFreeSpinIntroTextures(true);
		BonusGameManager.instance.wings.gameObject.SetActive(true);
	}
	
	/// Play a pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomObjectIndex = 0;
		
		randomObjectIndex = Random.Range(0, buttonSelections.Count);
		GameObject randomObject = buttonSelections[randomObjectIndex];
		
		PickGameButton gameButton = randomObject.GetComponentInChildren<PickGameButton>();
		
		Audio.play(WEAPON_PICKME_SOUND);
		gameButton.animator.Play(OBJECT_PICKME_ANIM_NAME);
		yield return new TIWaitForSeconds(OBJECT_PICKME_ANIM_LENGTH);
	}
	
	/// Handle an object being picked
	protected override IEnumerator showPick(GameObject button)
	{
		PickGameButton gameButton = button.GetComponentInChildren<PickGameButton>();
		Audio.play(WEAPON_PICK_REVEAL_SOUND);
		Audio.play(MAJOR_SYMBOL_VO_SOUNDS[stageTypeAsInt()], 1, 0, MAJOR_SYMBOL_VO_SOUNDS_DELAY);
		gameButton.animator.Play(OBJECT_REVEAL_MAJOR_ANIM_NAME + stageTypeAsString());
		yield return new TIWaitForSeconds(WAIT_FOR_PICK_REVEAL_DUR);
	}
	
	/// Handle showing the unpicked reveals
	protected override IEnumerator showReveal(GameObject button)
	{
		PickGameButton gameButton = button.GetComponentInChildren<PickGameButton>();
		Audio.play(REVEAL_OTHERS_SOUND);
		gameButton.animator.Play(OBJECT_UNPICKED_MAJOR_ANIM_NAME + PickMajorFreeSpins.convertStage1TypeToString(getNextRandomizedStageType()));
		yield break;
	}
	
	/// Overriding to handle showing the right banner
	protected override IEnumerator transitionIntoStage2()
	{
		yield return StartCoroutine(base.transitionIntoStage2());

		//No need to do this if our current game doesnt have a banner
		if (banner != null)
		{
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
		}
		
		Audio.play(Audio.soundMap(SPIN_STAGE_INTRO_VO_KEY), 1, 0, SPIN_STAGE_INTRO_VO_DELAY);
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
}
