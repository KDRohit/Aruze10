using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RomeBonusPoolsComponent : BaseBonusPoolsComponent 
{
	[System.Serializable]
	public class ButtonElement {
		public Animator pickAnimator;
		public GameObject[] wildSymbols;
	}

	// Selectable bonus pool buttons.
	[SerializeField] private ButtonElement[] buttons;

	// State names for animation.
	[SerializeField] private string idleState = "idle";
	[SerializeField] private string basePickState = "pickme";
	[SerializeField] private string wildPickedState = "reveal_wild";
	[SerializeField] private string wildNotPickedState = "wild gray";
	[SerializeField] private string multiplier3XPickedState = "reveal_3X";
	[SerializeField] private string multiplier3XNotPickedState = "3X gray";
	[SerializeField] private string multiplier5XPickedState = "reveal_5X";
	[SerializeField] private string multiplier5XNotPickedState = "5X gray";

	// Wait times for animations.
	[SerializeField] private float pickAnimWaitTime;
	[SerializeField] private float postAnimWaitTime;
	[SerializeField] private float preRevealWaitTime;
	[SerializeField] private float postRevealWaitTime;
	[SerializeField] private float introVoiceOverWaitTime;

	// Audio keys.
	private const string INTRO_VOICEOVERS_AUDIO_KEY = "basegame_feature_vo";
	private const string BACKGROUND_MUSIC_AUDIO_KEY = "basegame_feature_bg";
	private const string INTRO_AUDIO_KEY = "basegame_feature_intro";
	private const string PICKME_AUDIO_KEY = "basegame_feature_pickme";
	private const string WILD_REVEAL_AUDIO_KEY = "basegame_feature_pick_reveal_wild";
	private const string MULTIPLIER_REVEAL_AUDIO_KEY = "basegame_feature_pick_reveal_multiplier";
	private const string REVEAL_OTHERS_AUDIO_KEY = "basegame_feature_reveal_others";

	private CoroutineRepeater pickMeController;

	private const string REELSPIN_BASE = "reelspin_base";

	private const float PICKME_MIN_RANGE = 3.0f;
	private const float PICKME_MAX_RANGE = 5.0f;

	// override to play sounds when the game is starting
	protected override void playBonusStartSounds()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(BACKGROUND_MUSIC_AUDIO_KEY));
		Audio.tryToPlaySoundMap(INTRO_AUDIO_KEY);

		// Play one of several possible intro voiceovers.
		Audio.tryToPlaySoundMapWithDelay(INTRO_VOICEOVERS_AUDIO_KEY, introVoiceOverWaitTime);
	}

	// handle stuff a derived class might need to init, not making init() virtual since I always want to make sure that is called
	protected override void derivedInit()
	{
		pickMeController = new CoroutineRepeater(PICKME_MIN_RANGE, PICKME_MAX_RANGE, pickMeAnimCallback);
	}

	// run the pickMeController
	public void Update()
	{
		// Play the pickme animation.
		if (!didChoose && pickMeController != null)
		{
			pickMeController.update();
		}
	}

	/// Pick me animation player
	protected IEnumerator pickMeAnimCallback()
	{
		// If the player did not select a button to reveal
		if (!didChoose)
		{
			// Reveal a random button
			Animator elementAnimator = buttons[Random.Range(0, buttons.Length)].pickAnimator;
			elementAnimator.Play(basePickState);
			Audio.tryToPlaySoundMap(PICKME_AUDIO_KEY);

			yield return new WaitForSeconds(pickAnimWaitTime);

			if (!didChoose)
			{
				elementAnimator.Play(idleState);
			}
		}

		yield return new WaitForSeconds(postAnimWaitTime);
	}
	
	/// Opens a single object.
	protected override IEnumerator revealButtonObject(int index, BonusPoolItem poolItem, bool selected = true)
	{
		if (indicesRemaining.IndexOf(index) == -1)
		{
			yield break;
		}
			
		ButtonElement selectedButton = buttons[index];
		Animator pickAnimator = selectedButton.pickAnimator;
		
		indicesRemaining.Remove(index);
		
		if (poolItem != pick)
		{
			// This is a reveal, not the pick. So remove it from the reveals list.
			reveals.Remove(poolItem);
		}
		
		yield return new WaitForSeconds(preRevealWaitTime);

		// If the Wild should reveal.
		if (poolItem.reevaluations != null)
		{
			// Play audio.
			if (selected)
			{
				Audio.tryToPlaySoundMap(WILD_REVEAL_AUDIO_KEY);
			}
			// Reveal the other two non-selected symbols.
			else
			{
				Audio.tryToPlaySoundMap(REVEAL_OTHERS_AUDIO_KEY, 0.5f); // Not selected, so play at half volume since this will play twice.
			}
			
			// Iterates through all possible symbols that can become wild.
			for (int i = 0; i < selectedButton.wildSymbols.Length; i++)
			{
				// Lowers the from symbol string so we can check what it matches up with.
				string fromSymbolComparisonString = fromSymbol.ToLower();

				if (fromSymbolComparisonString.Contains(selectedButton.wildSymbols[i].name))
				{
					selectedButton.wildSymbols[i].SetActive(true);
				}
				else
				{
					// Disable any symbols that should not be shown.
					selectedButton.wildSymbols[i].SetActive(false);
				}
			}
			// If this object is not a user selection.
			if (!selected)
			{
				pickAnimator.Play(wildNotPickedState);
			}

			// If the player selected this button.
			else
			{
				pickAnimator.Play(wildPickedState);
			}
		}

		// If a multiplier should reveal.
		else if (poolItem.multiplier > 1)
		{
			// If the player did not select this button.
			if (!selected)
			{
				Audio.tryToPlaySoundMap(REVEAL_OTHERS_AUDIO_KEY, 0.5f);

				// If this is a 3X multiplier.
				if (poolItem.multiplier == 3)
				{
					pickAnimator.Play(multiplier3XNotPickedState);
				}

				// If this is a 5X multiplier.
				else
				{
					pickAnimator.Play(multiplier5XNotPickedState);
				}
			}

			// If the player did select this button
			else
			{
				Audio.tryToPlaySoundMap(MULTIPLIER_REVEAL_AUDIO_KEY);

				if (poolItem.multiplier == 3)
				{
					pickAnimator.Play(multiplier3XPickedState);
				}
				else
				{
					pickAnimator.Play(multiplier5XPickedState);
				}
			}
		}

		// Not enough information to determine what the button should reveal.
		else
		{
			Debug.LogWarning("poolItem doesn't have anything useful.", gameObject);
		}
		
		yield return new WaitForSeconds(postRevealWaitTime);
	}

	// Handle stuff that needs to happen before the bonus ends, i.e. switching an audio key for instance
	protected override void doBeforeBonusIsOver()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(REELSPIN_BASE));
	}
}
