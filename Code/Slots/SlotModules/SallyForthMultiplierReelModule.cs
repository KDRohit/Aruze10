using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SallyAnimationInfo
{
	public string symbolName;
	public string animationName;
}

public class SallyForthMultiplierReelModule : MultiplierReelAnimationModule
{
	public Animator character;

	//Basegame Only
	public string spinAnim;	
	public string fingerTapAnim;
	public string blinkAnim;

	//Basegame and Freespins
	public string idleAnim;
	[Header("Symbol name and the character animationto play when that symbol lands")]
	public List<SallyAnimationInfo> charaterAnimationInfo = new List<SallyAnimationInfo>();
	
	private Dictionary<string, string> symbolToAnim = new Dictionary<string, string>();
	private SlotBaseGame baseReelGame = null;

	[SerializeField] private float SPIN_ANIM_LENGTH_OVERRIDE = 1.0f; //Don't want to wait the full length of the animation so it looks like the character spins the reel
	[SerializeField] private float PRE_BONUS_WAIT = 1.0f;

	public override void Awake()
	{
		base.Awake();

		baseReelGame = reelGame as SlotBaseGame;

		foreach(SallyAnimationInfo sai in charaterAnimationInfo)
		{
			symbolToAnim.Add(sai.symbolName, sai.animationName);
		}		
	}
	
	public override bool needsToExecuteOnPreSpin()
	{
		return spinAnim != "";
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (baseReelGame != null)
		{
			// try to play the music here so that it syncs up with the spin animation
			baseReelGame.playSpinMusic();
		}

		//We check this right now since the freespins game doesn't have a spin animation on its character
		if (spinAnim != "")
		{
			StartCoroutine(playAnimationAndReturnToIdle(spinAnim));
			Audio.play(Audio.soundMap(SPECIAL_REEL_SPIN_START_SOUND_KEY));
			yield return new TIWaitForSeconds(SPIN_ANIM_LENGTH_OVERRIDE);
		}
	}

	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		yield return StartCoroutine(base.executePlayBonusAcquiredEffectsOverride());
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(character, symbolToAnim["BN"]));
		yield return new TIWaitForSeconds(PRE_BONUS_WAIT);
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Grab the middle multiplier symbol
		SlotSymbol symbol = reelGame.engine.getVisibleSymbolsAt(multiplierReelID)[multiplierSymbolIndex];

		if (symbol.serverName == "BN" && reelGame.isFreeSpinGame())
		{
			// trigger a BN effect in the free spin game which will also play an animation showing the player they got more spins
			yield return StartCoroutine(base.executePlayBonusAcquiredEffectsOverride());
		}
		else
		{
			yield return StartCoroutine(playCharacterAnimation(symbol));
		}
	}

	// Animate the character and play the sound
	private IEnumerator playCharacterAnimation(SlotSymbol multiplierSymbol)
	{
		if (!multiplierSymbol.isBlankSymbol && symbolToAnim.ContainsKey(multiplierSymbol.serverName))
		{
			playMultiplierSymbolSound(multiplierSymbol.serverName);
			yield return StartCoroutine(playAnimationAndReturnToIdle(symbolToAnim[multiplierSymbol.serverName]));
		}
	}

	private IEnumerator playAnimationAndReturnToIdle(string anim)
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(character, anim));
		character.Play(idleAnim);
	}
}
