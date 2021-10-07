using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Unity.Attributes;

/// <summary>
/// A good starting point for transitions that offers a few commonly needed methods.
/// </summary>
public class BaseTransitionModule : SlotModule 
{
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[Tooltip("The kind of Bonus(es) to apply the transitions to")]
	[SerializeField] protected TransitionBonusType transitionBonusType = TransitionBonusType.FREESPINS;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[Tooltip("List of bonus game names that use this transition")]
	[SerializeField] protected List<string> namedTransitions; // used to trigger a transition that comes from bonus_game
	
	[SerializeField] protected ReelGameBackground background;
	[SerializeField] protected float GAME_TRANSITION_SLIDE_TIME;
	[SerializeField] protected float OVERLAY_TRANSITION_SLIDE_TIME;
	[SerializeField] protected bool shouldResizeForBasegameFreespins;
	[SerializeField] protected Camera baseWingsCamera;
	[SerializeField] protected int TWEEN_WINGS_CAMERA_DEPTH;
	[SerializeField] protected int NORMAL_WINGS_CAMERA_DEPTH;
	[SerializeField] protected bool SHOULD_TWEEN_WINGS_UP;
	[SerializeField] protected float WING_TWEEN_TIME;
	[SerializeField] protected bool isScatterForBonus;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected float TRANSITION_SOUND_DELAY = 1.0f;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected bool MUTE_FREESPIN_MUSIC;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected bool MUTE_PORTAL_MUSIC = true;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected bool MUTE_PORTAL_TRANSITION_SOUND = false;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private bool switchTransitionMusicImmediately = false;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected float FREESPIN_INTRO_SOUND_DELAY = 1.0f;
	[SerializeField] protected bool shouldPlayBonusAquiredEffects = true;
	[SerializeField] protected bool hasBonusInSubOutcome = false;
	[SerializeField] private float DELAY_BETWEEN_BONUS_ACQUIRED_AND_TRANSITION = 0.0f; // allows for a delay for sounds to finish from bonus acquired before transition audio starts
	
	protected bool isTransitionComplete = false;
	protected bool isTransitionStarted = false;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected string FREESPIN_TRANSITION_SOUND_KEY = "bonus_freespin_wipe_transition";
	protected const string FREESPIN_INTRO_SOUND_KEY = "freespinintro";

	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private float FREESPIN_MUSIC_SOUND_DELAY = 0.0f;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected string FREESPIN_MUSIC_SOUND_KEY = "freespin";
	protected const string PORTAL_MUSIC_SOUND_KEY = "bonus_portal_bg";
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected string PICKEM_TRANSITION_SOUND_KEY = "bonus_challenge_wipe_transition";
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected string PORTAL_TRANSITION_SOUND_KEY = "bonus_portal_transition_to_portal";
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected string TRANSITION_VO_KEY = "bonus_transition_vo";
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected float TRANSITION_VO_DELAY = 0.0f;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected string ADDITIONAL_FREESPIN_TRANSITION_SOUND_KEY = "";
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] protected float ADDITIONAL_FREESPIN_TRANSITION_SOUND_DELAY = 0.0f;

	protected float viewportRectY;
	protected float viewportRectH;

	/// What kind of Bonues to apply the transitions to
	protected enum TransitionBonusType
	{
		FREESPINS	= 0,
		PICKEM		= 1,
		BOTH		= 2,
		PORTAL		= 3,
		SCATTER		= 4,
		NAMED		= 5,
	}

	
// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		isTransitionStarted = false;
		yield break;
	}

	/// <summary>
	/// More than likely you a triggering based on the kind of Bonus,
	/// but it is overridable.
	/// </summary>
	/// <returns><c>true</c>, if to execute on pre bonus game created is needsed, <c>false</c> otherwise.</returns>
	public override bool needsToExecuteOnPreBonusGameCreated()
	{
		// If we have queued bonuses and this is called we need to reset the isTransitionStarted flag, 
		// because we might need to transition again within the same base game spin
		if (isTransitionStarted && reelGame.outcome.hasQueuedBonuses)
		{
			isTransitionStarted = false;
		}

		if (!isTransitionStarted && needsToExecuteForThisOutcome())
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override IEnumerator executeOnPreBonusGameCreated()
	{
		isTransitionStarted = true;
		yield return StartCoroutine(startTransition());		
	}
		
	public override bool needsToLetModuleCreateBonusGame()
	{
		return needsToExecuteForThisOutcome();
	}
	
	// force updates reel size when continuing to a free spin game that shares the basegame prefab
	// free spins should not account for overlay and base game prefab does by default
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		if (reelGame.isDoingFreespinsInBasegame() && shouldResizeForBasegameFreespins)
		{
			return true;
		}
		return false;
	}
	
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		background = reelGame.reelGameBackground;
		background.updateGameSize(ReelGameBackground.GameSizeOverrideEnum.Freespins);
	}
	
	public override bool needsToExecuteOnReturnToBasegameFreespins()
	{
		if (reelGame.playFreespinsInBasegame && shouldResizeForBasegameFreespins)
		{
			return true;
		}
		return false;
	}
	
	public override IEnumerator executeOnReturnToBasegameFreespins()
	{
		background = reelGame.reelGameBackground;
		background.updateGameSize(ReelGameBackground.GameSizeOverrideEnum.Basegame);
		yield return null;
	}
	
	protected bool needsToExecuteForThisOutcome()
	{
		bool layeredBonus = false;
		if (hasBonusInSubOutcome)
		{
			List<SlotOutcome> layeredOutcomes = reelGame.outcome.getReevaluationSubOutcomesByLayer();
			foreach (SlotOutcome subOutcome in layeredOutcomes)
			{
				if (subOutcome.isBonus)
				{
					layeredBonus = true;
					break;
				}
			}
		}

		if (reelGame.getCurrentOutcome().isBonus || layeredBonus)
		{
			// Check and see if it's the freespins bonus.
			if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) 
				&& BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null 
				&& (transitionBonusType == TransitionBonusType.FREESPINS || transitionBonusType == TransitionBonusType.BOTH))
			{
				return true;
			}
			else if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE) 
				&& BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] != null 
				&& (transitionBonusType == TransitionBonusType.PICKEM || transitionBonusType == TransitionBonusType.BOTH))
			{
				return true;
			}
			else if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.SCATTER) 
				&& BonusGameManager.instance.outcomes[BonusGameType.SCATTER] != null
				&& transitionBonusType == TransitionBonusType.SCATTER)
			{
				return true;
			}
			else if (transitionBonusType == TransitionBonusType.PORTAL)
			{
				// portals trigger in a couple of ways, so check if a portal would trigger
				PortalScript portalScript = SlotBaseGame.instance.GetComponentInChildren<PortalScript>();
				if (portalScript != null)
				{
					// has a portal script attached to the base game, so this game has a portal for sure
					return true;
				}
				else
				{
					// check if this game has a portal prefab, and if it does, we are assuming it should be doing the portal transition for it
					if (SlotResourceMap.hasPortalPrefabPath(GameState.game.keyName))
					{
						return true;
					}
				}
			}
			else if (transitionBonusType == TransitionBonusType.NAMED)
			{
				return needsToExecuteFromNamedTransition();
			}
		}
		
		// doesn't match any criteria, so we aren't going to have this trigger
		return false;
	}
	
	// determines if there is a type of transition defined in a reevaluation and if this
	// is the correct transition we should execute.
	private bool needsToExecuteFromNamedTransition()
	{
		if (BonusGameManager.instance.outcomes == null)
		{
			return false;
		}

		foreach (KeyValuePair<BonusGameType, BaseBonusGameOutcome> bonusGameOutcomes in BonusGameManager.instance.outcomes)
		{
			if (namedTransitions.Contains(bonusGameOutcomes.Value.bonusGameName))
			{
				return true;
			}
		}

		return false;
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		// For now just going to ignore doing this if we have stacked bonuses going.
		// That way it should only trigger when all stacked bonuses are handled.  Technically
		// there could be a case in the future where we want to be able to handle a transition
		// back in the middle of a stack of bonuses, but more thought will need to be put
		// in to how that would actually work.
		return isTransitionStarted && (!BonusGameManager.instance.hasStackedBonusGames() || BonusGameManager.instance.isTopOfBonusGameStackFreespinsInBase());
	}
	
	public override IEnumerator executeOnBonusGameEnded()
	{		
		Overlay.instance.top.restorePosition();
		SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
		if (background != null && background.wings != null)
		{
			CommonGameObject.setLayerRecursively(background.wings.gameObject, Layers.ID_SLOT_FRAME); //hack to keep wings on top of frame during transition		
			background.setWingsTo(reelGame.reelGameBackground.wingType); //wings are now reset back to what the basegame wants them to be
			background.forceUpdate(); //Does a one time update to the position and scale of the wings while the application is running
		}
		yield break;
	}
	
		/// Making this a seperate function so we can reduce the copied code in here
	protected IEnumerator startTransition()
	{
		// Make sure all of the reels are stopped, or else everything may not fade out.
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			float time = 0.0f;	// Keep a timer here so the game doesn't stall out.
			while (!reel.isStopped)
			{
				yield return null;
				if (time > 0.5f)
				{
					break;
				}
				time += Time.deltaTime;
			}
		}
		
		// handle playing this early, so that it happens before the transition starts
		if (shouldPlayBonusAquiredEffects)
		{
			reelGame.isScatterForBonus = isScatterForBonus;
			yield return StartCoroutine(((SlotBaseGame)reelGame).doPlayBonusAcquiredEffects());
		}

		// Allow for a slight extra delay in case sounds extend a little after doPlayBonusAcquiredEffects and they should complete before the transition starts
		if (DELAY_BETWEEN_BONUS_ACQUIRED_AND_TRANSITION > 0.0f)
		{
			yield return new TIWaitForSeconds(DELAY_BETWEEN_BONUS_ACQUIRED_AND_TRANSITION);
		}

		RoutineRunner.instance.StartCoroutine(doTransition());
	}	
	
	protected virtual IEnumerator doTransition()
	{
		yield return null;
	}
	
	protected IEnumerator tweenWingsUp()
	{
		if (SHOULD_TWEEN_WINGS_UP)
		{
			if (background == null)
			{
				Debug.LogWarning("background needs to be set to tween wings");
			}
			baseWingsCamera.depth = TWEEN_WINGS_CAMERA_DEPTH;
			CommonGameObject.setLayerRecursively(background.wings.gameObject, Layers.ID_SLOT_PAYLINES); //hack to keep wings on top of frame during transition

			viewportRectH = baseWingsCamera.rect.height;
			viewportRectY = baseWingsCamera.rect.y;
			tweenViewportRectToDefault();
			if (FreeSpinGame.instance != null)
			{
				yield return StartCoroutine(background.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Freespins, 1.0f, iTween.EaseType.linear));
			}
			else if (ChallengeGame.instance != null)
			{
				yield return StartCoroutine(background.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Fullscreen, 1.0f, iTween.EaseType.linear));			
			}
		}
	}
	
	protected void resetWings()
	{
		if (SHOULD_TWEEN_WINGS_UP)
		{
			if (background == null)
			{
				Debug.LogWarning("background needs to be set to tween wings");
			}
			baseWingsCamera.depth = NORMAL_WINGS_CAMERA_DEPTH;
			Rect rect = baseWingsCamera.rect;
			baseWingsCamera.rect = new Rect(rect.x, viewportRectY, rect.width, viewportRectH);
			Transform wingsTransform = background.wings.gameObject.transform;
			
			// HACK - not a fan of setting this manually, but gen13 transition breaks (on 2nd try) without it. This really shouldn't be necessary though...
			wingsTransform.localPosition = new Vector3(0.0f, wingsTransform.localPosition.y, wingsTransform.localPosition.z);
		}
	}
	
	protected void startBonusGameNonModuleTransition()
	{
		BonusGameTransitionBaseNonModule bonusGameModule = null;
		if (FreeSpinGame.instance != null)
		{
			bonusGameModule = FreeSpinGame.instance.gameObject.GetComponentInChildren<BonusGameTransitionBaseNonModule>();
		}
		else if (ChallengeGame.instance != null)
		{
			bonusGameModule = ChallengeGame.instance.gameObject.GetComponentInChildren<BonusGameTransitionBaseNonModule>();			
		}
		
		if (bonusGameModule != null)
		{
			bonusGameModule.doTransition();
		}
	}
	
	protected void playTransitionSounds()
	{
		//@todo : Handle possible starting of audio keys for the bonus
		if (transitionBonusType == TransitionBonusType.PICKEM && !string.IsNullOrEmpty(PICKEM_TRANSITION_SOUND_KEY) && Audio.canSoundBeMapped(PICKEM_TRANSITION_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(PICKEM_TRANSITION_SOUND_KEY), 1, 0, TRANSITION_SOUND_DELAY);
			tryPlayTransitionVO();
		}
		else if (transitionBonusType == TransitionBonusType.PORTAL && Audio.canSoundBeMapped(PORTAL_TRANSITION_SOUND_KEY))
		{
			if(!MUTE_PORTAL_TRANSITION_SOUND)
			{
				Audio.play(Audio.soundMap(PORTAL_TRANSITION_SOUND_KEY), 1, 0, TRANSITION_SOUND_DELAY);
			}
			tryPlayTransitionVO();
			if (!MUTE_PORTAL_MUSIC)
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(PORTAL_MUSIC_SOUND_KEY));
			}	
		}
		else
		{
			Audio.play(Audio.soundMap(FREESPIN_TRANSITION_SOUND_KEY), 1, 0, TRANSITION_SOUND_DELAY);		
			//This gives us the abilty to play extra sounds, this scripts uses the old amination calls so this is the most unobstrusive way to achieve this functionality
			if(!string.IsNullOrEmpty(ADDITIONAL_FREESPIN_TRANSITION_SOUND_KEY) && Audio.canSoundBeMapped(ADDITIONAL_FREESPIN_TRANSITION_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(ADDITIONAL_FREESPIN_TRANSITION_SOUND_KEY), 1, 0, ADDITIONAL_FREESPIN_TRANSITION_SOUND_DELAY);
			}
			tryPlayTransitionVO();
			
			if (!MUTE_FREESPIN_MUSIC)
			{
				if (Audio.canSoundBeMapped(FREESPIN_INTRO_SOUND_KEY))
				{
					Audio.play(Audio.soundMap(FREESPIN_INTRO_SOUND_KEY), 1, 0, FREESPIN_INTRO_SOUND_DELAY);
				}
				
				if(FREESPIN_MUSIC_SOUND_DELAY > 0)
				{
					RoutineRunner.instance.StartCoroutine(playFreeSpinMusicOnDelay());
				}
				else
				{
					if (!switchTransitionMusicImmediately)
					{
						Audio.switchMusicKey(Audio.soundMap(FREESPIN_MUSIC_SOUND_KEY));
					}
					else
					{
						Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_MUSIC_SOUND_KEY));
					}
				}
			}		
		}
	}
	
	private IEnumerator playFreeSpinMusicOnDelay()
	{
		yield return new WaitForSeconds(FREESPIN_MUSIC_SOUND_DELAY);
		if (!switchTransitionMusicImmediately)
		{
			Audio.switchMusicKey(Audio.soundMap(FREESPIN_MUSIC_SOUND_KEY));
		}
		else
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_MUSIC_SOUND_KEY));
		}
		yield break;
	}

	private void tryPlayTransitionVO()
	{
		if (!string.IsNullOrEmpty(TRANSITION_VO_KEY) && Audio.canSoundBeMapped(TRANSITION_VO_KEY))
		{
			Audio.playWithDelay(Audio.soundMap(TRANSITION_VO_KEY), TRANSITION_VO_DELAY);
		}
	}
		
	protected void tweenViewportRectToDefault()
	{
		iTween.ValueTo(this.gameObject, iTween.Hash("from", viewportRectY, "to", 0.0f, "time", 1.0f, "onupdate", "updateViewPortY", "oncomplete", "onViewportTweenComplete"));
		iTween.ValueTo(this.gameObject, iTween.Hash("from", viewportRectH, "to", 1.0f, "time", 1.0f, "onupdate", "updateViewPortH"));
	}
	
	public void updateViewPortY(float yAmount)
	{
		Rect rect = baseWingsCamera.rect;
		baseWingsCamera.rect = new Rect(rect.x, yAmount, rect.width, rect.height);
	}
	
	public void updateViewPortH(float hAmount)
	{
		Rect rect = baseWingsCamera.rect;
		baseWingsCamera.rect = new Rect(rect.x, rect.y, rect.width, hAmount);		
	}
	
	public void onViewportTweenComplete()
	{
		Rect rect = baseWingsCamera.rect;
		baseWingsCamera.rect = new Rect(rect.x, 0.0f, rect.width, 1.0f);		
	}
}
