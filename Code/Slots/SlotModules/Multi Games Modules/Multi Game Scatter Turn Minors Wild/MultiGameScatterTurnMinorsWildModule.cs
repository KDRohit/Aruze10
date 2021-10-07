using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * MultiGameScatterTurnMinorsWildModule.cs
 * This module mutates symbols as part of the Scatter feature in multi games like gwtw01
 * Author: Nick Reynolds
 */ 
public class MultiGameScatterTurnMinorsWildModule : SlotModule
{
	[SerializeField] private GameObject wildChangeAnimPrefab = null;
	[SerializeField] private float WILD_CHANGE_Z_DEPTH = -3.0f;					// how far out to put the wild change anim so it doesn't overlap other things
	[SerializeField] private float WILD_CHANGE_ANIM_LENGTH = 0.0f;				// how long the wild change anim is	
	[SerializeField] private float SC_OUTCOME_ANIM_LENGTH = 1.25f;				// Outcome animation length
	[SerializeField] private bool isStaggeringWildTransformAnims = false;
	[SerializeField] private float WILD_STAGGER_TIME = 0.1f;				// transform sound, only used if isStaggeringWildTransformAnims is enabled, otherwise will probaby sound insane
	[SerializeField] private Vector3 MUTATE_PREFAB_SCALE_OVERRIDE = Vector3.one;
	[Tooltip("[MultiGame Only] Scatter new wilds panning the reels left to right.")]
	[SerializeField] private bool SHOULD_PAN_MULTIGAME_SCATTER_LEFT_TO_RIGHT = false;
	[Header("Wild Transform Sound")]
	[SerializeField] private string WILD_TRANSFORM_SOUND = "";					// transform sound, only used if isStaggeringWildTransformAnims is enabled, otherwise will probaby sound insane
	[SerializeField] private bool SHOULD_PLAY_SOUND_FOR_EACH_MUTATION = true;
	[SerializeField] private float DELAY_BEFORE_WILD_TRANSFORM_SOUND;
	[Tooltip("Delay between scatter starting and playing intro VO")]
	[SerializeField] private float SCATTER_INTRO_VO_DELAY;
	[SerializeField] private float SCATTER_FEATURE_END_VO_DELAY;
	
	// Private variables
	private bool hasPlayedSoundThisSpin = false;
	private GameObjectCacher wildChangeAnimCacher = null;
	
	// Constants
	private const string SCATTER_SYMBOL_ANIMATE_SOUND_KEY = "scatter_symbol_animate";
	private const string SCATTER_SYMBOL_FANFARE_SOUND_KEY_PREFIX = "scatter_symbol_fanfare";	// Append number to the end
	private const float EPSILON_FOR_REELS_PAN_TOGTHER = 0.001f;
	protected const string SCATTER_INTRO_VO = "scatter_intro_vo";
	protected const string SCATTER_FEATURE_END_VO = "scatter_feature_end_vo";

	public override void Awake()
	{
		base.Awake();

		if (wildChangeAnimPrefab != null)
		{
			wildChangeAnimCacher = new GameObjectCacher(this.gameObject, wildChangeAnimPrefab);
		}
		else
		{
			Debug.LogError("wildChangeAnimPrefab was null, no effects will display!");
		}
	}
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();
		if (reevaluations == null || reevaluations.Length == 0)
		{
			Debug.LogError("Reevaluations did not come down correctly in the outcome");
		}
		else
		{
			JSON scatterWildReplacementData = reevaluations[0].getJSON("replace_scatter_symbols");
			if (scatterWildReplacementData != null)
			{
				return true;
			}
		}

		return false;
	}

	private static int ComparisonWithEpsilon(SlotReel a, SlotReel b)
	{
		float aX = a.getReelGameObject().transform.position.x;
		float bX = b.getReelGameObject().transform.position.x;
			
		if (Math.Abs(aX - bX) < EPSILON_FOR_REELS_PAN_TOGTHER)
		{
			return 0; // Equal within the Epsilon config
		}
		else
		{
			return aX.CompareTo(bX);
		}			
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();
		JSON scatterWildReplacementData = reevaluations[0].getJSON("replace_scatter_symbols");	
		Dictionary<string, string> replacementSymbolMap = new Dictionary<string, string>();

		List<GameObject> wildChangeAnimObjects = new List<GameObject>();

		if (scatterWildReplacementData != null)
		{
			foreach (KeyValuePair<string, string> normalReplaceInfo in scatterWildReplacementData.getStringStringDict("normal_symbols"))
			{
				replacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
			}
		}
		Audio.play(Audio.soundMap(SCATTER_SYMBOL_ANIMATE_SOUND_KEY));

		// If there is intro VO play it
		if (Audio.canSoundBeMapped(SCATTER_INTRO_VO))
		{
			Audio.play(Audio.soundMap(SCATTER_INTRO_VO), 1.0f, 0.0f, SCATTER_INTRO_VO_DELAY);
		}

		// first animate the wild overlay change effects and scatter symbols animations
		SlotReel[] allReels = reelGame.engine.getAllSlotReels();

		foreach (SlotReel reel in allReels)
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (symbol.isScatterSymbol)
				{
					// Animate the scatter symbols at the same time we add the overlays
					symbol.animateOutcome();
					CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_OVERLAY); // SLOT_OVERLAY seems to work well
				}
			}		
		}

		// wait for the SC outcome animation to finish
		yield return new TIWaitForSeconds(SC_OUTCOME_ANIM_LENGTH);

		// Determine the order to play the SC wilds animations on the reels
		List<SlotReel> animationOrder = new List<SlotReel>();
		animationOrder.AddRange(allReels);

		// No need to sort if not Multigame because the order 
		// the reels are added is the order of animating
		if (SHOULD_PAN_MULTIGAME_SCATTER_LEFT_TO_RIGHT)
		{
			// Sort so reels are panning from left to right across games.
			// Note: This only makes sense for MultiGame
			animationOrder.Sort(ComparisonWithEpsilon);
		}
		
		// Now trigger the wild transformation animations
		for (int index = 0; index < animationOrder.Count; index ++)
		{
			SlotReel reel = animationOrder[index];

			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (replacementSymbolMap.ContainsKey(symbol.serverName))
				{
					GameObject wildChangeAnim = wildChangeAnimCacher.getInstance();
					wildChangeAnim.transform.parent = symbol.gameObject.transform;
					CommonGameObject.setLayerRecursively(wildChangeAnim, symbol.gameObject.layer);
					wildChangeAnim.transform.localScale = MUTATE_PREFAB_SCALE_OVERRIDE;
					wildChangeAnim.transform.localPosition = new Vector3(0, 0, WILD_CHANGE_Z_DEPTH);
					wildChangeAnim.SetActive(true);
					wildChangeAnimObjects.Add(wildChangeAnim);
					
					if (isStaggeringWildTransformAnims)
					{
						bool isFinalSymbolInPanningColumn = true;
						if ((index + 1) < animationOrder.Count)
						{
							if (ComparisonWithEpsilon(animationOrder[index], animationOrder[index + 1]) == 0)
							{
								isFinalSymbolInPanningColumn = false;
							}
						}

						if (isFinalSymbolInPanningColumn)
						{
							if (WILD_TRANSFORM_SOUND != "" && (SHOULD_PLAY_SOUND_FOR_EACH_MUTATION || !hasPlayedSoundThisSpin))
							{
								hasPlayedSoundThisSpin = true;
								Audio.play(WILD_TRANSFORM_SOUND, 1.0f, 0.0f, DELAY_BEFORE_WILD_TRANSFORM_SOUND);
							}
							yield return new TIWaitForSeconds(WILD_STAGGER_TIME);
						}
					}
				}
			}
		}

		hasPlayedSoundThisSpin = false;

		// wait for the wild transformation animations to finish
		yield return new TIWaitForSeconds(WILD_CHANGE_ANIM_LENGTH);

		// clean up the effects
		foreach (GameObject wildChangeAnim in wildChangeAnimObjects)
		{
			wildChangeAnimCacher.releaseInstance(wildChangeAnim);
		}

		// now that the animations have happened, show the wilds
		foreach (SlotReel reel in allReels)
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (replacementSymbolMap.ContainsKey(symbol.serverName))
				{
					symbol.showWild();
				}
			}
		}

		// If there is ending VO play it
		if (Audio.canSoundBeMapped(SCATTER_FEATURE_END_VO))
		{
			Audio.play(Audio.soundMap(SCATTER_FEATURE_END_VO), 1.0f, 0.0f, SCATTER_FEATURE_END_VO_DELAY);
		}

		yield break;
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return needsToExecuteOnReelsStoppedCallback();
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();
		JSON scatterWildReplacementData = reevaluations[0].getJSON("replace_scatter_symbols");	
		Dictionary<string, string> replacementSymbolMap = new Dictionary<string, string>();

		if (scatterWildReplacementData != null)
		{
			foreach (KeyValuePair<string, string> normalReplaceInfo in scatterWildReplacementData.getStringStringDict("normal_symbols"))
			{
				replacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
			}
		}

		// Alter the debug symbol names now so we don't have a symbol mismatch exception
		foreach (SlotSymbol symbol in stoppedReel.visibleSymbols)
		{
			if (replacementSymbolMap.ContainsKey(symbol.serverName))
			{
				// changing this name early so we don't cause a mismatch exception
				symbol.debugName = "WD";
			}
			else if (symbol.serverName == "SC")
			{
				// don't actually need to do anything here, sound and animation are played automatically elsewhere
			}
		}

		yield break;
	}
}
