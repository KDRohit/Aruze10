using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LionBonusPoolsComponent : BaseBonusPoolsComponent 
{
	[System.Serializable]
	public class ButtonElements
	{
		public GameObject button;
		public Animator presentAnimator;
		public GameObject[] gameSymbols;
		public GameObject[] grayGameSymbols;
	}

	public ButtonElements[] buttons;
	private CoroutineRepeater pickMeController;

	private const string BG_MUSIC = "BasketBg";
	private const string INTRO_BARK = "TotoBarkBasketIntroLion";
	private const string PICK_SFX = "BasketPickMe";
	private const string PICK_REVEAL = "BasketPickBasket";
	private const string BASKET_FANFARE = "BasketRevealWildFanfare";
	private const string MULTIPLIER_FLOURISH = "BasketRevealMultiplierFlourish";
	private const string REELSPIN_BASE = "reelspin_base";

	private const string PICKME_ANIM = "Picking object_Pick me";
	private const string PICKME_STILL = "Picking object_Still";
	private const string REVEAL_TOTO_3 = "Picking object_reveal toto X3";
	private const string REVEAL_TOTO_5 = "Picking object_reveal toto X5";
	private const string REVEAL_TOTO_3_GRAY = "Picking object_reveal not selected toto X3";
	private const string REVEAL_TOTO_5_GRAY = "Picking object_reveal not selected toto X5";
	private const string REVEAL_SYMBOL = "Picking object_reveal symbols";
	private const string REVEAL_SYMBOL_GRAY = "Picking object_reveal not selected symbols";

	private const float PICKME_MIN_RANGE = 3.0f;
	private const float PICKME_MAX_RANGE = 5.0f;
	private const float PICKME_ANIM_WAIT = 0.75f;
	private const float PICKME_POST_WAIT = 4.0f;
	private const float PRE_REVEAL_WAIT = 0.5f;
	private const float POST_REVEAL_WAIT = 2.0f;

	// override to play sounds when the game is starting
	protected override void playBonusStartSounds()
	{
		Audio.switchMusicKeyImmediate(BG_MUSIC);
		Audio.play(INTRO_BARK);
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
		// If a pick item hasn't been selected, play some wait animations.
		if (!didChoose)
		{
			Animator basketAnimator = buttons[Random.Range(0,2)].presentAnimator;
			
			basketAnimator.Play(PICKME_ANIM);
			Audio.play(PICK_SFX);
				
			yield return new WaitForSeconds(PICKME_ANIM_WAIT);

			if (!didChoose)
			{
				basketAnimator.Play(PICKME_STILL);
			}
		}

		yield return new WaitForSeconds(PICKME_POST_WAIT);
	}
	
	/// Opens a single object.
	protected override IEnumerator revealButtonObject(int index, BonusPoolItem poolItem, bool selected = true)
	{
		if (indicesRemaining.IndexOf(index) == -1)
		{
			yield break;
		}
		
		ButtonElements casket = buttons[index];	// Shorthand.
		
		indicesRemaining.Remove(index);
		
		if (poolItem != pick)
		{
			// This is a reveal, not the pick. So remove it from the reveals list.
			reveals.Remove(poolItem);
		}
		
		yield return new WaitForSeconds(PRE_REVEAL_WAIT);

		if (selected)
		{
			Audio.play(PICK_REVEAL);
		}
		else
		{
			Audio.play(PICK_REVEAL, .5f); // Play it at half volume because we're gonna play it twice, kinda hacky but so is this whole game.
		}

		if (poolItem.reevaluations != null && selected)
		{
			Audio.play(BASKET_FANFARE);
		}
		else if (selected)
		{
			Audio.play(MULTIPLIER_FLOURISH);
		}
		
		if (poolItem.reevaluations != null)
		{
			int symbolIndex = int.Parse(fromSymbol.Substring(fromSymbol.Length-1, 1)) - 1;

			for (int i = 0; i < 9; i++)
			{
				if (i == symbolIndex)
				{
					casket.gameSymbols[i].SetActive(true);
					casket.grayGameSymbols[i].SetActive(true);
				}
				else
				{
					casket.gameSymbols[i].SetActive(false);
					casket.grayGameSymbols[i].SetActive(false);
				}
			}
			
			if (!selected)
			{
				casket.presentAnimator.Play(REVEAL_SYMBOL_GRAY);
			}
			else
			{
				casket.presentAnimator.Play(REVEAL_SYMBOL);
			}
		}
		else if (poolItem.multiplier > 1)
		{
			if (!selected)
			{
				if (poolItem.multiplier == 3)
				{
					casket.presentAnimator.Play(REVEAL_TOTO_3_GRAY);
				}
				else
				{
					casket.presentAnimator.Play(REVEAL_TOTO_5_GRAY);
				}
			}
			else
			{
				if (poolItem.multiplier == 3)
				{
					casket.presentAnimator.Play(REVEAL_TOTO_3);
				}
				else
				{
					casket.presentAnimator.Play(REVEAL_TOTO_5);
				}
			}
		}
		else
		{
			Debug.LogWarning("poolItem doesn't have anything useful.", gameObject);
		}
		
		yield return new WaitForSeconds(POST_REVEAL_WAIT);
	}

	// Handle stuff that needs to happen before the bonus ends, i.e. switching an audio key for instance
	protected override void doBeforeBonusIsOver()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(REELSPIN_BASE));
	}
}
