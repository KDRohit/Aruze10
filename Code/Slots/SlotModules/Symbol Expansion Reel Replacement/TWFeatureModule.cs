using UnityEngine;
using System.Collections;

/// <summary>
/// TW or Trigger Wild feature, a wild symbol lands and triggers an animation that replaces the reel.
/// </summary>
public class TWFeatureModule : SymbolExpansionReelReplacementModule
{	
	[Header("TRIGGER WILD (TW)")]
	[SerializeField] protected Animator[] wildAnimators = null;

	[Header("Tween TW to Reel Bottom")]
	[SerializeField] protected bool tweenTWSymbol = true;

	[Header("TW Animations")]
	[SerializeField] protected string WILD_DEFAULT_ANIMATOR_NAME;
	[SerializeField] protected string WILD_EXPAND_ANIMATOR_NAME;
	[SerializeField] protected string WILD_OPEN_ANIMATOR_NAME;
	[SerializeField] protected string WILD_1X6_ANIMATOR_NAME;
	[SerializeField] protected string WILD_1X5_ANIMATOR_NAME;
	[SerializeField] protected string WILD_1X4_ANIMATOR_NAME;
	[SerializeField] protected string WILD_LAUNCHER_ANIMATION_NAME;	
	[SerializeField] protected string WILD_END_ANIMATION_NAME;	
	[SerializeField] protected int 	TW_LAUNCH_REEL_ID = 4;					// this is the id of the reel that launches all the action
	[SerializeField] protected float WILD_LAUNCHER_EJECT_DELAY;	
	[SerializeField] protected float WILD_ANTICIPATION_DURATION;	
	[SerializeField] protected float TIME_SLIDE_SC_SYMBOL;

	[Header("TW Sound names")]
	[SerializeField] protected string SYMBOL_LANDED_SOUND;	
	[SerializeField] protected string SYMBOL_LANDED_VO;
	[SerializeField] protected float SYMBOL_LANDED_SOUND_DELAY = 0.0f;
	[SerializeField] protected bool PLAY_SYMBOL_LANDING_ON_REEL_STOPPING = false;
	[SerializeField] protected string SYMBOL_EXPAND_SOUND;
	[SerializeField] protected string SYMBOL_EXPAND_VO;
	[SerializeField] protected string SYMBOL_FILL_SOUND_START;
	[SerializeField] protected float SYMBOL_FILL_SOUND_START_DELAY;
	[SerializeField] protected float SYMBOL_HIDE_DELAY;	
	[SerializeField] protected Layers.LayerID SYMBOL_LAYER_DURING_EXPAND = Layers.LayerID.ID_SLOT_FOREGROUND;	    // 26 is ID_SLOT_FOREGROUND
	[SerializeField] protected string SYMBOL_FILL_SOUND;


	[SerializeField] protected float SYMBOL_FILL_SOUND_DELAY;
	[SerializeField] protected string SYMBOL_FILL_VO;
	[SerializeField] protected float TW_FILL_VO_DELAY;
	[SerializeField] protected string FEATURE_END_SOUND;
	[SerializeField] protected string FEATURE_END_VO;

	[Header("Final Wilds Animations")]
	[SerializeField] protected string FINAL_TW_ANIMATOR_NAME;
	[SerializeField] protected string FINAL_1X6_ANIMATOR_NAME;
	[SerializeField] protected string FINAL_1X5_ANIMATOR_NAME;
	[SerializeField] protected string FINAL_1X4_ANIMATOR_NAME;


	private const string SOUND_MAPPED_SYMBOL_FILL_SOUND = "tw_effect_land";
	private const string SOUND_MAPPED_SYMBOL_LANDED_VO = "TW_symbol_vo";

	private bool[] reelsWithWilds = new bool[5];

	public override void Awake()
	{
		Debug.Log ("TW AWake");
		base.Awake();

		for (int i = 0; i < wildAnimators.Length; i++)
		{
			Animator animator = wildAnimators[i];
			if (animator != null)
			{
				animator.Play(WILD_DEFAULT_ANIMATOR_NAME);
			}
		}
	}
	
	protected override void OnEnable()
	{
		base.OnEnable();

		if (!needsToExecuteOnReelsStoppedCallback())
		{
			foreach (Animator animator in wildAnimators)
			{
				if (animator != null)
				{
					animator.Play(WILD_DEFAULT_ANIMATOR_NAME);
				}
			}
		}
	}
	
	// Custom behavior override for playing symbol landing sound before reel stop
	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		bool isTWReel = false;
		foreach (string symbolName in stoppingReel.getFinalReelStopsSymbolNames())
		{
			if (symbolName == "TW")
			{
				isTWReel = true;
			}
		}
		
		return isTWReel && needsToExecuteOnReelsStoppedCallback();
	}

	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		if (PLAY_SYMBOL_LANDING_ON_REEL_STOPPING)
		{
			Audio.play(SYMBOL_LANDED_SOUND, 1, 0, SYMBOL_LANDED_SOUND_DELAY);			
		}
	}

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by reelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// Go through the mutations from the spin and see if there is one for this type of mutation.
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			foreach (MutationBase mutation in reelGame.mutationManager.mutations)
			{
				if (mutation.type == "symbol_expansion_reel_replacement")
				{
					return true;
				}
			}
		}
		else
		{
			Debug.LogError("Mutation manager not properly set up.");
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (!PLAY_SYMBOL_LANDING_ON_REEL_STOPPING)
		{
			Audio.play(SYMBOL_LANDED_SOUND, 1, 0, SYMBOL_LANDED_SOUND_DELAY);			
		}

		if (string.IsNullOrEmpty(SYMBOL_LANDED_VO))
		{
			Audio.play(Audio.soundMap(SOUND_MAPPED_SYMBOL_LANDED_VO));    // preferred method
		}
		else
		{
			Audio.play(SYMBOL_LANDED_VO);
		}
		
		SlotSymbol twSymbol = null;
		// We want to go through the last reel
		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(4))
		{
			if (symbol.serverName == "TW")
			{
				twSymbol = symbol;
			}
		}


		
		if (twSymbol != null)
		{
			if (WILD_ANTICIPATION_DURATION > 0)
			{
				twSymbol.animateAnticipation();
				yield return new TIWaitForSeconds(WILD_ANTICIPATION_DURATION);  
			}

			CommonGameObject.setLayerRecursively(twSymbol.gameObject, (int)SYMBOL_LAYER_DURING_EXPAND);
			if (tweenTWSymbol)
			{
				// Move the scatterSymbol to the bottom of the reel.
				yield return new TITweenYieldInstruction(iTween.MoveTo(twSymbol.gameObject, iTween.Hash(
					"position", reelGame.engine.getVisibleSymbolsBottomUpAt(4)[0].transform.position,
					"time", TIME_SLIDE_SC_SYMBOL,
					"islocal", false,
					"easetype", iTween.EaseType.easeInQuad)));
			}
		}
		else
		{
			Debug.LogError("No TW symbol found.");
		}
		
		if (wildAnimators.Length > TW_LAUNCH_REEL_ID)
		{
			Animator wildAnimator = wildAnimators[TW_LAUNCH_REEL_ID];
			if (wildAnimator != null)
			{
				Audio.play(SYMBOL_EXPAND_SOUND);

				if(!string.IsNullOrEmpty(SYMBOL_EXPAND_VO))
				{
					Audio.play(SYMBOL_EXPAND_VO, 1.0f, 0.0f, 2.5f);
				}

				wildAnimator.Play(WILD_EXPAND_ANIMATOR_NAME);			

				if(!string.IsNullOrEmpty(SYMBOL_FILL_VO))
				{
					Audio.play(SYMBOL_FILL_VO, 1.0f, 0.0f, TW_FILL_VO_DELAY);
				}

				while (!wildAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_EXPAND_ANIMATOR_NAME))
				{
					yield return null;
				}

				yield return new TIWaitForSeconds(SYMBOL_HIDE_DELAY);  
				twSymbol.gameObject.SetActive(false);					
				
				// now wait for the animation to finish and go back to the idle state
				while (wildAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_EXPAND_ANIMATOR_NAME))
				{
					yield return null;
				}
				// Now we should pop to cork
				
				while (wildAnimator.GetCurrentAnimatorStateInfo(0).IsName(WILD_OPEN_ANIMATOR_NAME))
				{
					yield return null;
				}
				// Now we should move on to the rest of the stuff.
			}
		}

		CommonGameObject.setLayerRecursively(twSymbol.gameObject, Layers.ID_SLOT_REELS);
		twSymbol.gameObject.SetActive(true);
		
		yield return StartCoroutine(base.executeOnReelsStoppedCallback());
	}
	
	protected override IEnumerator expandReelFromMutation(StandardMutation mutation)
	{
		yield return StartCoroutine(base.expandReelFromMutation(mutation));
		Audio.play(FEATURE_END_SOUND);

		if (wildAnimators.Length > TW_LAUNCH_REEL_ID && wildAnimators[TW_LAUNCH_REEL_ID] != null && !string.IsNullOrEmpty(WILD_END_ANIMATION_NAME))
		{
			// play the animation on the 5th reel after all the squirting is done
			wildAnimators[TW_LAUNCH_REEL_ID].Play(WILD_END_ANIMATION_NAME);  
		}			
	}
	
	protected override IEnumerator expandReelAt(int reelID)
	{
		reelsWithWilds[reelID] = true;

		if (reelID == lastReel)
		{			
			Audio.play(FEATURE_END_VO, 1.0f, 0.0f, 0.7f);
		}
		
		bool wildAnimatorWorked = false;
		string animationName = WILD_1X6_ANIMATOR_NAME;
		if (reelID == 4)
		{
			// We want to skip this one because it's already done.
			wildAnimatorWorked = true;
		}
		else if (reelID == 3)
		{
			animationName = WILD_1X6_ANIMATOR_NAME;
		}
		else if (reelID == 2)
		{
			animationName = WILD_1X5_ANIMATOR_NAME;	
		}
		else if (reelID == 1)
		{
			animationName = WILD_1X4_ANIMATOR_NAME;
		}
		
		if (!wildAnimatorWorked && wildAnimators != null && wildAnimators.Length > reelID)
		{
			Animator wildAnimator = wildAnimators[reelID];
			if (wildAnimator != null)
			{
				Audio.play(SYMBOL_FILL_SOUND_START, 1.0f, 0.0f, SYMBOL_FILL_SOUND_START_DELAY);

				if (wildAnimators.Length > TW_LAUNCH_REEL_ID &&  wildAnimators[TW_LAUNCH_REEL_ID] != null && !string.IsNullOrEmpty(WILD_LAUNCHER_ANIMATION_NAME))
				{
					// play the animation on the 5th reel that launches the squirt
					wildAnimators[TW_LAUNCH_REEL_ID].Play(WILD_LAUNCHER_ANIMATION_NAME);  // for peewee01 this is when Randy jumps up and down to squirt chocolate
					yield return new TIWaitForSeconds(WILD_LAUNCHER_EJECT_DELAY); // this is so we can wait for Randy to land on the bottle before starting the squirt
				}	

				string symbolFillSound = SYMBOL_FILL_SOUND;
				if (string.IsNullOrEmpty(symbolFillSound))
				{
					symbolFillSound = Audio.soundMap(SOUND_MAPPED_SYMBOL_FILL_SOUND);    // preferred method
				}
				if (!string.IsNullOrEmpty(symbolFillSound))
				{
					Audio.play(symbolFillSound, 1.0f, 0.0f, SYMBOL_FILL_SOUND_DELAY);
				}				
				
				wildAnimator.Play(animationName);

				while (!wildAnimator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
				{
					yield return null;
				}
				
				// now wait for the animation to finish and go back to the idle state
				while (wildAnimator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
				{
					yield return null;
				}
				
				wildAnimatorWorked = true;
			}
			else
			{
				Debug.LogError("Trying to mutate a reel that doesn't have a wild animator.");
			}
		}
		
		if (!wildAnimatorWorked)
		{
			// It's not an error if it got set.
			Debug.LogError("No wild Animators set for " + reelID);
		}
		
		if (wildAnimatorWorked)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
			{
				symbol.debugName = "WD";
			}
			SlotReel reel = reelGame.engine.getSlotReelAt(reelID);
			if (reel != null)
			{
				reel.slideSymbols(0);
			}
		}
		else
		{
			yield return StartCoroutine(base.expandReelAt(reelID));
		}
	}
	
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		reelsWithWilds = new bool[5];

		foreach (Animator animator in wildAnimators)
		{
			if (animator != null)
			{
				animator.Play(WILD_DEFAULT_ANIMATOR_NAME);
			}
		}
		yield break;
	}
	
	// executeOnBigWinEnd() section
	// Functions here are executed after the big win has been removed from the screen.
	public override bool needsToExecuteOnBigWinEnd()
	{
		return true;
	}
	
	public override void executeOnBigWinEnd()
	{
		for (int reelID = 0; reelID < wildAnimators.Length; reelID++)
		{
			Animator animator = wildAnimators[reelID];
			if (animator != null)
			{
				if (reelsWithWilds.Length > reelID && reelsWithWilds[reelID])
				{
					string animationName = FINAL_TW_ANIMATOR_NAME;
					if (reelID == TW_LAUNCH_REEL_ID)
					{
						animationName = FINAL_TW_ANIMATOR_NAME;
					}
					else if (reelID == 3)
					{
						animationName = FINAL_1X6_ANIMATOR_NAME;
					}
					else if (reelID == 2)
					{
						animationName = FINAL_1X5_ANIMATOR_NAME;	
					}
					else if (reelID == 1)
					{
						animationName = FINAL_1X4_ANIMATOR_NAME;
					}
					animator.Play(animationName);
				}
				else
				{
					animator.Play(WILD_DEFAULT_ANIMATOR_NAME);
				}
			}
		}
	}
}
