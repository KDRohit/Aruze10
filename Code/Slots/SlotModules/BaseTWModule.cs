using UnityEngine;
using System.Collections;

public class BaseTWModule : SlotModule {

	[SerializeField] private bool isGettingWdSymbolNameFromMutationData = false;
	[SerializeField] private int TRIGGER_REEL = 2; //Middle Reel
	[SerializeField] private float TIME_BETWEEN_MUTATIONS = 0.0f;
	[SerializeField] private float MUTATION_ANIMATION_LENGTH = 0.0f;
	//This is needed in bb01 due to the way sound wants the animations to land
	[SerializeField] private float PRE_MUTATIONS_WAIT = 0.0f;
	[SerializeField] private float POST_MUTATIONS_WAIT = 0.0f;
	[SerializeField] private float TWEEN_TIME = 0.0f;
	[SerializeField] private Vector3 tweenOffset;
	[SerializeField] private float PRE_MUTATION_SYMBOL_RELEASE_DELAY = 0.0f;

	[SerializeField] private string mutatingWdName = "WD";
	[SerializeField] private string twPaylineSymbolOutcomeName = ""; //The version of the TW symbol we want to use when animating on the paylines. If this is blank then we're assuming the TW_Outcome symbol is fine.

	[SerializeField] private bool PLAY_TW_ANIMATION_ON_OVERLAY = false;		

	private string FLATTENED_POSTFIX = "";

	[SerializeField] private string TW_SYMBOL_LAND_SOUND = "trigger_symbol";
	[SerializeField] private string TW_SYMBOL_LAND_VO = "tw_symbol_vo";
	[SerializeField] private string TW_SYMBOL_ANIMATE_SOUND = "trigger_symbol_fanfare";
	[SerializeField] private float TW_SYMBOL_ANIMATE_SOUND_DELAY = 0.0f;
	[SerializeField] private string TW_SYMBOL_ANIMATE_VO = "tw_effect_land_vo";
	[SerializeField] private string TW_SYMBOL_PRE_MUTATE_SOUND = "";
	[SerializeField] private float TW_SYMBOL_PRE_MUTATE_SOUND_DELAY = 0.0f;
	[SerializeField] private string TW_SYMBOL_MUTATE_SOUND = "trigger_symbol_effect";
	[SerializeField] private float TW_SYMBOL_MUTATE_SOUND_DELAY = 0.0f;
	[SerializeField] private string TW_SYMBOL_MUTATE_ANIM_SOUND = "trigger_symbol_effect";
	[SerializeField] private string TW_SYMBOL_MUTATE_END_SOUND = "tw_effect_land3";
	[SerializeField] private float MUTATE_END_SOUND_DELAY = 0.0f;
	[SerializeField] private string TW_FEATURE_FINISH_FANFARE = "tw_final_symbol_transform";
	[SerializeField] private string TW_FEATURE_FINISH_VO = "tw_sequence_end_vo";
	[SerializeField] private float TW_FEATURE_FINISH_FANFARE_DELAY = 0.0f;
	[SerializeField] private bool playOneTWMutationSoundAtATime = false;
	[SerializeField] private float TW_OUTCOME_ANIM_LENGTH = 1.333f;

	private bool triggerWildsFound = false;
	private PlayingAudio currentPlayingMutationsound;

	private const string DEFAULT_MOVING_WD_SYMBOL_NAME = "TWWD";

	public override void Awake ()
	{
		base.Awake();
		if(reelGame.isGameUsingOptimizedFlattenedSymbols)
		{
			FLATTENED_POSTFIX = SlotSymbol.FLATTENED_SYMBOL_POSTFIX;
		}
	}

	public override bool needsToExecuteOnSpecificReelStop (SlotReel stoppedReel)
	{
        // Fixfor HIR-60292: getTWSymbol() will check the TRIGGER_REEL for a TW symbol. 
        // Doing this on a reelstop other than TRIGGER_REEL can yield a false positive
        // as TRIGGER_REEL might not yet have stopped. Make sure stoppedReel here is
        // TRIGGER_REEL before checking for a TW symbol. -csweet
        if(stoppedReel.reelID == TRIGGER_REEL)
        {
            SlotSymbol twSymbol = getTWSymbol();
            if (twSymbol != null)
            {
                triggerWildsFound = true;
                return true;
            }
        }
        
		return false;
	}

	public override IEnumerator executeOnSpecificReelStop (SlotReel stoppedReel)
	{
		SlotSymbol triggerWild = getTWSymbol();
		if (triggerWild != null)
		{
			Audio.play(Audio.soundMap(TW_SYMBOL_LAND_SOUND));
			triggerWild.animateAnticipation();
			if (PLAY_TW_ANIMATION_ON_OVERLAY)
			{
				CommonGameObject.setLayerRecursively(triggerWild.gameObject, Layers.ID_SLOT_OVERLAY);
			}
		}
		Audio.tryToPlaySoundMap(TW_SYMBOL_LAND_VO);
		return base.executeOnSpecificReelStop(stoppedReel);
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (isTwMutationPresent() && triggerWildsFound) //Only play our feature if we have mutations from the server and the TW symbol landed
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	// Check to see if we actually have a mutation that this module would operate on
	// This will ensure that we don't make it to the point where we are actually trying
	// to execute on the mutation and realize we only have other kinds of mutations
	private bool isTwMutationPresent()
	{
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			StandardMutation standardMutation = mutation as StandardMutation;
			if (standardMutation.isTWmutation == true)
			{
				return true;
			}
		}

		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback ()
	{
		SlotSymbol twSymbol = getTWSymbol();
		if (twSymbol != null)
		{
			while (twSymbol.isAnimatorDoingSomething)
			{
				yield return null;
			}
		}

		StandardMutation currentMutation = null;
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			currentMutation = mutation as StandardMutation;
			if (currentMutation.isTWmutation == true)
			{
				break;
			}
			else
			{
				currentMutation = null;
			}		
		}
		//StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;		
		if (currentMutation == null || currentMutation.triggerSymbolNames == null || currentMutation.triggerSymbolNames.Length < 2)
		{
			Debug.LogError("The mutations came down from the server incorrectly. The backend is broken.");
			yield break;
		}

		Audio.play(Audio.soundMap(TW_SYMBOL_ANIMATE_SOUND));
		Audio.tryToPlaySoundMap(TW_SYMBOL_ANIMATE_VO);
		if (twSymbol != null)
		{
			twSymbol.animateOutcome();
		}

		for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
		{
			for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
			{
				if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
				{
					if (isGettingWdSymbolNameFromMutationData)
					{
						string finalWdSymbolName = currentMutation.triggerSymbolNames[i, j];
						// Going to create movingWdSymbolName as the final symbol with "WD" tacked onto the end, so that they look similar to the standard TWWD usage
						// For instance if the final symbol name is "TW2X" then the symbol that moves will be called "TW2XWD"
						string movingWdSymbolName = finalWdSymbolName + "WD";
						StartCoroutine(placeTWAnimationAndPlay(i, j, twSymbol, movingWdSymbolName, finalWdSymbolName)); 
					}
					else
					{
						StartCoroutine(placeTWAnimationAndPlay(i, j, twSymbol, DEFAULT_MOVING_WD_SYMBOL_NAME, mutatingWdName)); 
					}
					
					yield return new TIWaitForSeconds(TIME_BETWEEN_MUTATIONS);
				}
			}
		}
		Audio.playWithDelay(Audio.soundMap(TW_SYMBOL_MUTATE_END_SOUND), MUTATE_END_SOUND_DELAY);
		yield return new TIWaitForSeconds(POST_MUTATIONS_WAIT);
		Audio.playWithDelay(Audio.soundMap(TW_FEATURE_FINISH_FANFARE), TW_FEATURE_FINISH_FANFARE_DELAY);
		Audio.playWithDelay(Audio.soundMap(TW_FEATURE_FINISH_VO), TW_FEATURE_FINISH_FANFARE_DELAY);
		if (twPaylineSymbolOutcomeName != "")
		{
			yield return new TIWaitForSeconds(TW_OUTCOME_ANIM_LENGTH);
			twSymbol.mutateTo(twPaylineSymbolOutcomeName);
			CommonGameObject.setLayerRecursively(twSymbol.gameObject, Layers.ID_SLOT_REELS);
		}
	}

	//Override this for different types of effect choreography
	private IEnumerator placeTWAnimationAndPlay(int mutationReelIndex, int symbolIndex, SlotSymbol twSymbol, string movingWdSymbolName, string finalWdSymbolName)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();
		
		SlotSymbol targetSymbol = reelArray[mutationReelIndex].visibleSymbolsBottomUp[symbolIndex]; //Symbol that we need to mutate

		//Get our tween position
		Vector3 offset = Vector3.zero;
		Vector3 position = Vector3.zero;
	
		if (reelGame.findSymbolInfo(finalWdSymbolName) != null)
		{
			offset = reelGame.findSymbolInfo(finalWdSymbolName).positioning;
		}		 

		if (reelGame.engine.getReelRootsAt(mutationReelIndex, symbolIndex) != null)
		{
			position = reelGame.engine.getReelRootsAt(mutationReelIndex, symbolIndex).transform.position; 
		}
		position += new Vector3(offset.x, (symbolIndex * reelGame.symbolVerticalSpacingWorld) + offset.y, offset.z);

		//Getting an instance of our TWWD symbol and setting some basic properties
		SlotSymbol newWDSymbol = reelGame.createSlotSymbol(movingWdSymbolName, symbolIndex, reelGame.engine.getSlotReelAt(mutationReelIndex));
		SymbolAnimator mutatingWDSymbol = newWDSymbol.getAnimator();
		
		if (mutatingWDSymbol != null)
		{
			CommonGameObject.setLayerRecursively(mutatingWDSymbol.gameObject, Layers.ID_SLOT_OVERLAY);
			if (twSymbol != null)
			{
				//If we are the bottom half of a symbol we wont have an animator, the SlotSymbol class looks up the appropreiate tranform for symbols that are not 1x1
				mutatingWDSymbol.transform.parent = twSymbol.transform.parent;							
			}

			mutatingWDSymbol.gameObject.transform.localScale = Vector3.one;
			mutatingWDSymbol.scaling = Vector3.one;

			if (twSymbol != null)
			{
				mutatingWDSymbol.positioning = twSymbol.transform.localPosition; //This effect starts at the location of the Trigger Wild symbol in the middle reel
			}
			yield return new TIWaitForSeconds(PRE_MUTATIONS_WAIT);
			newWDSymbol.animateOutcome(); 
			if (!string.IsNullOrEmpty(TW_SYMBOL_PRE_MUTATE_SOUND))
			{
				Audio.play(Audio.tryConvertSoundKeyToMappedValue(TW_SYMBOL_PRE_MUTATE_SOUND), 1, 0, TW_SYMBOL_PRE_MUTATE_SOUND_DELAY);
			}
			if (currentPlayingMutationsound == null || !playOneTWMutationSoundAtATime || !currentPlayingMutationsound.isPlaying)
			{
				currentPlayingMutationsound = Audio.play(Audio.soundMap(TW_SYMBOL_MUTATE_SOUND), 1, 0, TW_SYMBOL_MUTATE_SOUND_DELAY);
			}
			if (targetSymbol != null)
			{
				mutatingWDSymbol.transform.parent = targetSymbol.transform.parent;
			}
			iTween.MoveTo(mutatingWDSymbol.gameObject, iTween.Hash("position", (position + tweenOffset), "time", TWEEN_TIME, "islocal", false, "easetype", iTween.EaseType.linear));
			yield return new TIWaitForSeconds(MUTATION_ANIMATION_LENGTH);
			if (targetSymbol != null)
			{
				targetSymbol.mutateTo(finalWdSymbolName); //Once the mutation is over, mutate to our regular version of the WD symbol.
				if (!string.IsNullOrEmpty(TW_SYMBOL_MUTATE_ANIM_SOUND))
				{
					Audio.play(Audio.soundMap(TW_SYMBOL_MUTATE_ANIM_SOUND));
				}
			}
			yield return new TIWaitForSeconds(PRE_MUTATION_SYMBOL_RELEASE_DELAY);
			newWDSymbol.cleanUp();
		}
	}

	public override bool needsToExecuteOnPreSpin ()
	{
		return triggerWildsFound;
	}

	public override IEnumerator executeOnPreSpin ()
	{
		SlotSymbol twSymbol = getTWSymbol();
		if (twSymbol != null && PLAY_TW_ANIMATION_ON_OVERLAY)
		{
			CommonGameObject.setLayerRecursively(twSymbol.gameObject, Layers.ID_SLOT_REELS);
		}
		triggerWildsFound = false;
		yield break;
	}

	private SlotSymbol getTWSymbol()
	{
		SlotSymbol triggerWildSymbol = null;
		reelGame.engine.getSlotReelAt(TRIGGER_REEL).refreshVisibleSymbols();
		SlotSymbol[] visibleSymbols = reelGame.engine.getSlotReelAt(TRIGGER_REEL).visibleSymbols;
		foreach (SlotSymbol symbol in visibleSymbols)
		{
			if (symbol.name.Contains("TW") && symbol.isWildSymbol)
			{				
				triggerWildSymbol = symbol;
				break;
			}
		}

		string[] finalSymbols = reelGame.engine.getSlotReelAt(TRIGGER_REEL).getFinalReelStopsSymbolNames();
		bool twFound = false;
		for (int i = 0; i < finalSymbols.Length; i++)
		{
			if (finalSymbols[i].Contains("TW"))
			{
				twFound = true;
			}
		}

		if (twFound)
		{
			return triggerWildSymbol;
		}
		else
		{
			return null;
		}
	}
}
