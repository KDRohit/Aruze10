using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReplacementCellWildModule : SlotModule
{
	[Header("Intro")]
	[SerializeField] private GameObject introPrefab;
	[SerializeField] private bool shouldSkipVO = false;
	[SerializeField] private float INTRO_ANIMATION_TIME;
	[SerializeField] private float POST_VO_WAIT_TIME;
	[SerializeField] private float MUTATE_STAGGER_TIME;
	[SerializeField] private float MUTATE_WAIT_TIME_1;
	[SerializeField] private float MUTATE_WAIT_TIME_2;
	[SerializeField] private float MUTATE_WAIT_TIME_3;
	[SerializeField] private Vector3 INTRO_PREFAB_OFFSET;
	[SerializeField] private Vector3 MUTATED_SYMBOL_OFFSET;
	[SerializeField] private MutationEffect[] mutationEffects;
	[SerializeField] private string[] introAnimations;
	[SerializeField] private string[] introAnimationSounds;
	[SerializeField] private AudioListController.AudioInformationList animationSounds;
	[SerializeField] private float DEPTH_ADJUSTMENT = -3f;
	[SerializeField] private bool adjustOverlayEffectsByDepth = false;

	private int gameType = 0; // 0 = basegame, 1 = freespin
	private string[] WILD_INIT_KEY = {"basegame_vertical_wild_init", "freespin_vertical_wild_init"};
	[SerializeField] private float INIT_SOUND_DELAY;
	[SerializeField] private float INIT_BG_MUSIC_DELAY = 0.0f;
	private string[] WILD_REVEAL_VO_KEY = {"basegame_vertical_wild_reveal_vo", "freespin_vertical_wild_reveal_vo"};
	[SerializeField] private float WILD_REVEAL_VO_DELAY = 0.0f;
	[SerializeField] private string WILD_REVEAL_COLLECTION_KEY;
	[SerializeField] private float WILD_REVEAL_SOUND_DELAY;
	[SerializeField] private string FEATURE_BG_MUSIC;
	private bool executeOnReelEnd = false;
	protected List<StandardMutation.ReplacementCell> replacements = new List<StandardMutation.ReplacementCell>();
	
	[System.Serializable]
	public class MutationEffect
	{
		public string newSymbolName;
		public string revealSoundKeyOverride;
		public GameObject effectPrefab;
	}

	protected Dictionary<string, string> wildRevealSoundOverrides = new Dictionary<string, string>();

	// Private variables
	private GameObjectCacher introPrefabCacher = null;
	protected List<GameObject> effects = new List<GameObject>();

	public override void Awake()
	{
		base.Awake();

		if (introPrefab != null)
		{
			introPrefabCacher = new GameObjectCacher(this.gameObject, introPrefab);
		}
		
		if (reelGame is FreeSpinGame)
		{
			gameType = 1;
		}

		// Build the sound override dictionary here
		foreach(MutationEffect eff in mutationEffects)
		{
			// cache any sound key overrides we find
			if (!string.IsNullOrEmpty(eff.revealSoundKeyOverride) && !wildRevealSoundOverrides.ContainsKey(eff.newSymbolName))
			{
				wildRevealSoundOverrides.Add(eff.newSymbolName, eff.revealSoundKeyOverride);
			}
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback ()
	{
		return executeOnReelEnd;
	}
	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override IEnumerator executeOnReelsStoppedCallback ()
	{
		foreach (GameObject introPrefab in effects)
		{
			Destroy(introPrefab);
		}
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach (StandardMutation.ReplacementCell position in replacements)
		{
			SlotSymbol targetSymbol = reelArray[position.reelIndex].visibleSymbolsBottomUp[position.symbolIndex];;
			targetSymbol.mutateTo(position.replaceSymbol);
		}
		replacements.Clear ();
		effects.Clear ();

		return base.executeOnReelsStoppedCallback ();
	}
	
	public override bool needsToExecutePreReelsStopSpinning()
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
			reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation mutation = baseMutation as StandardMutation;

				if ((mutation.type == "replacement_cell_wild" || mutation.type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT) &&
					mutation.replacementCells != null && mutation.replacementCells.Count > 0)
				{
					executeOnReelEnd = true;
					return true;
				}
			}
		}

		executeOnReelEnd = false;
		return false;
	}

	public virtual void PlayIntroSounds()
	{
		if (Audio.canSoundBeMapped (FEATURE_BG_MUSIC))
		{
			Audio.play (Audio.soundMap (FEATURE_BG_MUSIC), 1.0f, 0.0f, INIT_BG_MUSIC_DELAY);
		}
		else
		{
			Audio.play (FEATURE_BG_MUSIC, 1.0f, 0.0f, INIT_BG_MUSIC_DELAY);
		}
		Audio.play(Audio.soundMap(WILD_INIT_KEY[gameType]), 1.0f, 0.0f, INIT_SOUND_DELAY);
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		PlayIntroSounds();

		GameObject introPrefabCacherObject = null;

		if (introPrefabCacher != null)
		{
			// Show the opening animation
			introPrefabCacherObject = introPrefabCacher.getInstance();
			introPrefabCacherObject.SetActive(true);
			introPrefabCacherObject.transform.parent = transform;
			introPrefabCacherObject.transform.localPosition = INTRO_PREFAB_OFFSET;
			if (introAnimations != null && introAnimations.Length > 0)
			{
				int index = Random.Range(0, introAnimations.Length);
				Audio.play (introAnimationSounds[index]);
				StartCoroutine(CommonAnimation.playAnimAndWait(introPrefabCacherObject.GetComponent<Animator>(), introAnimations[index]));
			}

			if (animationSounds != null && animationSounds.Count > 0)
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(animationSounds));
			}

			yield return new TIWaitForSeconds(INTRO_ANIMATION_TIME);
		}

		if (!shouldSkipVO)
		{
			Audio.play (Audio.soundMap(WILD_REVEAL_VO_KEY[gameType]), 1.0f, 0.0f, WILD_REVEAL_VO_DELAY);
		}

		yield return new TIWaitForSeconds(POST_VO_WAIT_TIME);

		List<TICoroutine> routines = new List<TICoroutine>();
		foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
		{
			StandardMutation mutation = baseMutation as StandardMutation;
			if (mutation.type == "replacement_cell_wild" || mutation.type == SlotOutcome.REEVALUATION_TYPE_SPOTLIGHT)
			{
				GameObject symbolEffect = null;
				string symbolName = "";
				foreach (StandardMutation.ReplacementCell position in mutation.replacementCells)
				{
					foreach(MutationEffect eff in mutationEffects)
					{
						if (eff.newSymbolName == position.replaceSymbol)
						{
							symbolEffect = eff.effectPrefab;
							symbolName = eff.newSymbolName;
						}
					}
					replacements.Add(position);
					SlotSymbol targetSymbol = reelGame.engine.getVisibleSymbolsAt(position.reelIndex)[position.symbolIndex];
					routines.Add(StartCoroutine(mutateOneSymbol(targetSymbol, symbolEffect, symbolName, position.reelIndex, position.symbolIndex)));
					yield return new TIWaitForSeconds(MUTATE_STAGGER_TIME);	
				}
			}
		}			
		yield return new TIWaitForSeconds(MUTATE_WAIT_TIME_3);
		if (introPrefabCacher != null && introPrefabCacherObject != null)
		{
			introPrefabCacher.releaseInstance(introPrefabCacherObject);
		}
		foreach(TICoroutine routine in routines)
		{
			yield return routine;
		}
	}

	protected virtual IEnumerator mutateOneSymbol(SlotSymbol targetSymbol, GameObject effectPrefab, string symbolName, int reelIndex, int symbolIndex)
	{
		GameObject instancedEffect = null;
		if (effectPrefab != null)
		{
			instancedEffect = CommonGameObject.instantiate(effectPrefab) as GameObject;
		}
		Vector3 position = reelGame.engine.getReelRootsAt (reelIndex, symbolIndex).transform.position;
		if (instancedEffect != null)
		{
			if (adjustOverlayEffectsByDepth)
			{
				instancedEffect.transform.position = new Vector3 (position.x, position.y + (symbolIndex * reelGame.symbolVerticalSpacingWorld), position.z - (effects.Count * DEPTH_ADJUSTMENT));
			}
			else
			{
				instancedEffect.transform.position = new Vector3 (position.x, position.y + (symbolIndex * reelGame.symbolVerticalSpacingWorld), position.z);
			}
		}
		if (instancedEffect != null && MUTATED_SYMBOL_OFFSET != Vector3.zero)
		{
			instancedEffect.transform.position += MUTATED_SYMBOL_OFFSET; 
		}
		if(instancedEffect != null)
		{
			instancedEffect.transform.parent = reelGame.getReelGameObject (reelIndex).transform;
			instancedEffect.transform.localScale = reelGame.findSymbolInfo (symbolName).scaling;
			effects.Add(instancedEffect);
		}

		string soundKey = WILD_REVEAL_COLLECTION_KEY;

		if (wildRevealSoundOverrides.ContainsKey(symbolName))
		{
			soundKey = wildRevealSoundOverrides[symbolName];
		}

		if (Audio.canSoundBeMapped(soundKey))
		{
			Audio.play(Audio.soundMap(soundKey));
		}
		else
		{
			Audio.play(WILD_REVEAL_COLLECTION_KEY, 1.0f, 0.0f, WILD_REVEAL_SOUND_DELAY);
		}
		yield return new TIWaitForSeconds(MUTATE_WAIT_TIME_1);
	}
}