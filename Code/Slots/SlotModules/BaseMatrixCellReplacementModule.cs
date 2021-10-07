using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseMatrixCellReplacementModule : SlotModule
{
	[SerializeField] protected AnimationListController.AnimationInformationList introAnimation;
	[SerializeField] protected AnimationListController.AnimationInformationList outroAnimation;
	[SerializeField] protected ParticleTrailController particleTrail;
	 //Used in situations where theres a pop going from the idle state to the outro state and we need to manaully control idle looping
	[SerializeField] protected AnimationListController.AnimationInformationList manuallyLoopingIdleAnimations;
	[SerializeField] protected float TIME_BETWEEN_MUTATE_EFFECTS = 0.0f;
	[SerializeField] protected float TIME_BEFORE_MUTATE_EFFECTS_START = 0.0f;
	[SerializeField] protected string FEATURE_INTRO_MUSIC_KEY = "special_bg";
	[SerializeField] protected string FEATURE_END_MUSIC_KEY = "";
	protected const string BASE_GAME_MUSIC_KEY = "reelspin_base";
	[SerializeField] protected string WD_TRANSFORM_SOUND = "";
	[SerializeField] private float DELAY_AFTER_MUTATIONS_UNTIL_REELS_STOP = 0.0f;
	
	protected bool playingIdleAnimations = false;
	protected bool readyForOutro = true;	
	protected bool executeOnReelEnd = true;
	protected int numSymbolsCurrentlyMutating = 0;
	
	protected abstract IEnumerator mutateSymbol(SlotSymbol symbol, GameObject mutatingSymbol, int row);

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return executeOnReelEnd;		
	}
	
	public override bool needsToExecutePreReelsStopSpinning()
	{		
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
			reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation mutation = baseMutation as StandardMutation;

				if (mutation.type == "matrix_cell_replacement")
				{
					executeOnReelEnd = true;
					return true;
				}
			}
		}

		executeOnReelEnd = false;
		return false;
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		if (Audio.canSoundBeMapped(FEATURE_INTRO_MUSIC_KEY))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(FEATURE_INTRO_MUSIC_KEY));
		}
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimation));
		StartCoroutine(playLoopingIdleAnimations());
		GameObject mutatingSymbol = null;
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		yield return new TIWaitForSeconds(TIME_BEFORE_MUTATE_EFFECTS_START);

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;

				if (currentMutation != null && currentMutation.type == "matrix_cell_replacement")
				{
					foreach (KeyValuePair<int, int[]> mutationKvp in currentMutation.singleSymbolLocations)
					{
						int reel = mutationKvp.Key - 1;
						foreach (int row in mutationKvp.Value)
						{
							SlotSymbol symbol = reelArray[reel].visibleSymbolsBottomUp[row - 1];
							StartCoroutine(mutateSymbol(symbol, mutatingSymbol, row));
							yield return new TIWaitForSeconds(TIME_BETWEEN_MUTATE_EFFECTS);
						}
					}
				}
			}
		}

		if (Audio.canSoundBeMapped(FEATURE_END_MUSIC_KEY))
		{
			Audio.playMusic(Audio.soundMap(FEATURE_END_MUSIC_KEY));
		}

		readyForOutro = true; //All our symbol have mutated so we're ready to play our outro animations once the idles are in a safe state to transition
		while (playingIdleAnimations)
		{
			yield return null;
		}
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimation));

		// Restart basegame music only if it was changed to feature specific music
		if (Audio.canSoundBeMapped(FEATURE_INTRO_MUSIC_KEY))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(BASE_GAME_MUSIC_KEY));
		}

		if (DELAY_AFTER_MUTATIONS_UNTIL_REELS_STOP > 0.0f)
		{
			yield return new TIWaitForSeconds(DELAY_AFTER_MUTATIONS_UNTIL_REELS_STOP);
		}
	}

	protected IEnumerator playLoopingIdleAnimations()
	{
		if (manuallyLoopingIdleAnimations.Count > 0)
		{
			playingIdleAnimations = true;
			readyForOutro = false;
			while (!readyForOutro) //Keep looping the idle animations until the mutations are finished
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(manuallyLoopingIdleAnimations));
			}
		}

		playingIdleAnimations  = false; //Now its safe to play the outro animations
	}
}
