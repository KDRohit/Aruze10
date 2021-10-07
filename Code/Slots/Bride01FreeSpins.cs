using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bride01FreeSpins : LayeredSlotFreeSpinGame
{
	[SerializeField] private GameObject[] wildExpands = new GameObject[NUMBER_OF_REELS];
	[SerializeField] private Animator[] wildOverlays = new Animator[NUMBER_OF_REELS];

	[SerializeField] private Animator reelsAnim;
	[SerializeField] private float wildExpandAnimTime = 1.0f;
	[SerializeField] private bool wildExpandAndOverlayAreTheSame = false;

	private bool[] hasTW = new bool[NUMBER_OF_REELS];
	private bool[] isTransforming = new bool[NUMBER_OF_REELS];

	private const float SUMMARY_VO_SOUND_DELAY = 0.6f;							// delay before the summary VO is played after the game has ended

	// Bride01 Sound Consts
	private const string MEGA_SYMBOL_LAND_SOUND = "MegaSymbolInitFreespinBride01";
	private const string WILD_INIT_SOUND = "WildInitFreespinBride01";
	[SerializeField] private string WILD_EXPAND_SOUND = "WildExpandFreespinBride01";

	private const string SUMMARY_VO_SOUND = "BonusSummaryVOBride01";				// Sound name played once the summary screen comes up for this game.

	private const int NUMBER_OF_REELS = 7;

	private const string WILD_INIT_SOUNDMAP = "trigger_symbol";
	private const string WILD_EXPAND_SOUNDMAP = "expanding_symbol";
	private const string MEGA_SYMBOL_LAND_SOUNDMAP = "overlay_mega_symbol_fanfare_freespin";


	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = true;
		if (GameState.game.keyName.Contains ("elvira03"))
		{
			reelsAnim.Play("reel_drop");
		}
	}

	protected override IEnumerator prespin()
	{
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			if (hasTW[reel.reelID] && wildOverlays[reel.reelID] != null)
			{
				wildOverlays[reel.reelID].gameObject.SetActive(true);
			}
		}

		yield return StartCoroutine(base.prespin());
	}

	// Check and see if all the symbols are going to be blank, on the top layer.
	// If they are going to be blank then we don't want to play the reel stop sound.
	public override void onSpecificReelStopping(SlotReel reel)
	{
		base.onSpecificReelStopping(reel);

		if (reel.layer == 1)
		{
			reel.shouldPlayReelStopSound = false;
			if (reel.reelID == 2)
			{
				if (GameState.game.keyName.Contains("bride01"))
				{
					reel.reelStopSoundOverride = MEGA_SYMBOL_LAND_SOUND;
				}
				else
				{
					reel.reelStopSoundOverride = MEGA_SYMBOL_LAND_SOUNDMAP;
				}

				string[] stopSymbolNames = reel.getFinalReelStopsSymbolNames();
				foreach (string name in stopSymbolNames)
				{
					if (!SlotSymbol.isBlankSymbolFromName(name))
					{
						// We want to play a sound
						reel.shouldPlayReelStopSound = true;
						break;
					}
				}
			}
		}

		// Don't play the stop sound for reels with the TW symbols on them.
		if (hasTW[reel.reelID])
		{
			reel.shouldPlayReelStopSound = false;
		}
	}

	/// Custom handling for specific reel features
	protected override IEnumerator handleSpecificReelStop(SlotReel reel)
	{
		yield return StartCoroutine(base.handleSpecificReelStop(reel));

		// Make the symbols small.
		if (reel.layer == 1)
		{
			if (reel.reelID == 2)
			{
				// Reel 2 is the only reel that can have mega symbols on it.
				foreach (SlotSymbol symbol in reel.visibleSymbols)
				{
					if (symbol.isMegaSymbolPart && !symbol.isWhollyOnScreen)
					{
						if (symbol.canBeSplit ())
						{
							symbol.splitSymbol ();
							break;
						}
					}
				}

			}
		}
		yield return null;
	}

	private IEnumerator playTWSYmbolAnimations()
	{
		bool playingTWWild = false;
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			if (reel.reelID == 1 || reel.reelID == 6)
			{
				// Do all of the TW stuff.
				if (!hasTW[reel.reelID])
				{
					foreach (SlotSymbol symbol in reel.visibleSymbols)
					{
						if (symbol.serverName == "TW")
						{
							if (GameState.game.keyName.Contains("bride01"))
							{
								symbol.animateAnticipation();
							}

							playingTWWild = true;
							break;
						}
					}
				}
			}
		}
		if (playingTWWild)
		{
			// Wait for the animation to play.
			SymbolInfo info = findSymbolInfo("TW");
			if (info != null)
			{
				yield return new TIWaitForSeconds(info.customAnimationDurationOverride);
			}
			else
			{
				Debug.LogWarning("TW symbol is not defined.");
			}
		}
	}

	private IEnumerator doTWExpands()
	{
		yield return StartCoroutine(playTWSYmbolAnimations());
		bool playedOnce = false;
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			if (reel.reelID == 1 || reel.reelID == 6)
			{
				// Do all of the TW stuff.
				if (hasTW[reel.reelID])
				{
					foreach (SlotSymbol symbol in reel.visibleSymbols)
					{
						symbol.mutateTo("WD");
					}
				}
				else
				{
					foreach (SlotSymbol symbol in reel.visibleSymbols)
					{
						if (symbol.serverName == "TW")
						{
							StartCoroutine(expandWildOn(reel, !playedOnce));
							playedOnce = true;
							break;
						}
					}
				}
			}
		}

		bool shouldwait = true;
		while (shouldwait)
		{
			shouldwait = false;
			foreach (bool reelTransforming in isTransforming)
			{
				if (reelTransforming)
				{
					shouldwait = true;
				}
			}
			yield return null;
		}
	}

	private IEnumerator expandWildOn(SlotReel reel, bool withSound)
	{
		isTransforming[reel.reelID] = true;
		hasTW[reel.reelID] = true;
		// Do the animation
		wildExpands[reel.reelID].SetActive(true);
		if (withSound)
		{
			if (GameState.game.keyName.Contains("bride01"))
			{
				Audio.play(WILD_INIT_SOUND);
			}
			else
			{
				Audio.playSoundMapOrSoundKey(WILD_EXPAND_SOUND);
			}
		}
		yield return new TIWaitForSeconds(wildExpandAnimTime);

		if (GameState.game.keyName.Contains("bride01"))
		{
			Audio.playSoundMapOrSoundKey(WILD_EXPAND_SOUND);
		}

		// If the expand and overlay animation are the same (see harvey01) then we don't need to hide the expand.
		if (!wildExpandAndOverlayAreTheSame)
		{
			wildExpands[reel.reelID].SetActive(false);
			wildOverlays[reel.reelID].gameObject.SetActive(true);
		}

		foreach (SlotSymbol symbol in reel.visibleSymbols)
		{
			symbol.mutateTo("WD");
		}
		isTransforming[reel.reelID] = false;
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		// Play the summary VO 0.6 seconds after the game has ended.
		if (GameState.game.keyName.Contains("bride01"))
		{
			Audio.play(SUMMARY_VO_SOUND, 1.0f, 0.0f, SUMMARY_VO_SOUND_DELAY);
		}
		
		base.gameEnded();
	}

	private IEnumerator reelsStoppedCoroutine()
	{
		yield return StartCoroutine(doTWExpands());
		base.reelsStoppedCallback();
	}

	protected override void reelsStoppedCallback()
	{
		StartCoroutine(reelsStoppedCoroutine());
	}
}