// credit hacks
//#define HACK_OUTCOME

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

/**
* GenericPickemGame.cs
* The point of this class is to handle all pickem game implementations in a generic but powerful way.
* All specific behavior for a game should be controlled through inspector variables.
* All sounds are mapped.
* Everything (ideally, at least) is defined by which round the player is on. 
* If new or modified functionality needs to be added for a game, please either create a new PickemStageType 
* if it is a truly new type of round, or try to implement it in the existing functions, with inspector variables
* that can control the flow. Only if that seems completely unwieldy should you make a new class that extends this one.
* Note that this class is actually abstract and if you want to extend it for your game, you will need to extend the 
* existing class that implements this class with the correct BaseBonusGameOutcome type defined.
* Those in-between classes, however, should never have pickem game logic in them.
* Authors: Nick Reynolds & Scott Lepthien
*/
abstract public class GenericPickemGame<T> : PickingGame<T> where T : BaseBonusGameOutcome
{
	public BonusOutcomeTypeEnum outcomeType = BonusOutcomeTypeEnum.Undefined;
	
	NewBaseBonusGameOutcome newOutcomeType;
	protected RoundPicks roundPicks;
	public CorePickData pickData;
	protected long jackpotCredits;
	protected int numberOfBadsPicked;   // index to use when bads trigger audio or animations
	[HideInInspector] public int currentPickIndex = -1;    // Remember the index of this pick.
	[HideInInspector] public int previousPickIndex = -1;   // Remember the index of your previous pick (not implemented everywhere yet).
	
	protected string defaultPickmeAnimName;
	private PlayingAudio jackpotAdvanceRollupLoopSound; //Need this stored in a variable so we can stop the sound from looping later on
	private bool canPlayJackpotAdvancingRollupLoopSounds = false;
	
	[SerializeField] protected UILabel[] jackpotLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] jackpotLabelWrappers;

	[SerializeField] private List<GameObject> onBonusGamePresenterFinalCleanupAnimatedObjectToTurnOn = new List<GameObject>(); // List of objects to turn on when the game is ending as part of the transition out
	[SerializeField] private AnimationListController.AnimationInformationList onBonusGamePresenterFinalCleanupAnimationList; // Animation list which can be played before BonusGamePresenter calls finalCleanup for transitions out of the bonus 
	
	public List<LabelWrapper> jackpotLabelWrappersNew	// Needed to use "New" because existing component variable uses this name.
	{
		get
		{
			if (_jackpotLabelWrappersNew == null)
			{
				_jackpotLabelWrappersNew = new List<LabelWrapper>();

				if (jackpotLabelWrappers != null)
				{
					foreach (LabelWrapperComponent wrapperComponent in jackpotLabelWrappers)
					{
						_jackpotLabelWrappersNew.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in jackpotLabels)
					{
						_jackpotLabelWrappersNew.Add(new LabelWrapper(label));
					}
				}
			}
			return _jackpotLabelWrappersNew;
		}
		set
		{
			_jackpotLabelWrappersNew = value;
		}
	}
	private List<LabelWrapper> _jackpotLabelWrappersNew = null;	
	
	
	[SerializeField] protected UILabel[] messagingLabels;	// To be removed when prefabs are updated.
	[SerializeField] protected LabelWrapperComponent[] messagingLabelsWrapperComponent;
	
	public List<LabelWrapper> messagingLabelsWrapper
	{
		get
		{
			if (_messagingLabelsWrapper == null)
			{
				_messagingLabelsWrapper = new List<LabelWrapper>();

				if (messagingLabelsWrapperComponent != null && messagingLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in messagingLabelsWrapperComponent)
					{
						_messagingLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in messagingLabels)
					{
						_messagingLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _messagingLabelsWrapper;
		}
		set
		{
			_messagingLabelsWrapper = value;
		}
	}
	private List<LabelWrapper> _messagingLabelsWrapper = null;	
	
	
	[SerializeField] protected string PICK_ITEM_LOCALIZATION_KEY;
	[SerializeField] protected string VALUE_INCREASED_LOCALIZATION_KEY;
	[SerializeField] protected string GAME_OVER_LOCALIZATION_KEY;
	[SerializeField] protected bool SHOULD_CAPITALIZE_LOCALIZATIONS;
	[SerializeField] protected bool ALWAYS_REVEAL_PICKS_ON_GAME_END = true;
	[SerializeField] protected bool FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS = false;

	[SerializeField] protected float waitToEnableInputDur = 0.0f;

	public PickemOutcome pickemOutcome;
	protected WheelOutcome wheelOutcome;

	private List<FadeAnimationDefinition> picksToFade = new List<FadeAnimationDefinition>();
	// types of pickem rounds

	/***************************************************************************************************
	* EVERYTHING SHOULD GO INTO PICKING ROUNDS
	***************************************************************************************************/	
	// Instead of all these arrays by rounds,
	// everything should be organized into picking round game objects.
	// And make sure you use GenericPickemGameGO.
	[SerializeField] protected List<PickingRoundGO> pickingRoundGos;
	
	// Don't use picking rounds directly anymore, they're only here for backwards compatibility.
	[SerializeField] protected List<PickingRound> pickingRounds;

	/***************************************************************************************************
	* INSPECTOR VARIABLES FOR ROUNDS IN GENERAL
	***************************************************************************************************/	
	
	[SerializeField] protected int[] numPickMeAnimsByRound;
	[SerializeField] protected List<PickButtonTemplateDefinition> buttonDefinitions;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLES FOR OBJECTS THAT NEED TO BE TURNED ON OR OFF WHEN GOING TO A NEW ROUND
	***************************************************************************************************/	
	[SerializeField] protected RoundGameObjects[] objectsToActivateOrDeactivateByRound;

	[SerializeField] protected PickemStageType[] PICKEM_STAGE_TYPES;
	
	[SerializeField] protected PickItemStartBehavior[] pickItemStartBehaviorByRound;

	[SerializeField] protected RevealDelaysDefintion[] revealDelaysByRound;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR SPARKLE TRAIL DEFINITIONS
	***************************************************************************************************/	
	[SerializeField] protected SparkleTrailDefinition[] sparkleTrailDefinitionsByRound;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR EFFECTS THAT OCCUR AFTER THE SPARKLE TRAIL ARRIVES DEFINITIONS
	***************************************************************************************************/
	[SerializeField] protected PostSparkleTrailEffectsDefinition[] postSparkleTrailEffectsByRound;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR REVEALS
	***************************************************************************************************/	
	[SerializeField] protected RevealDefinition[] revealEffectDefinitionsByRound;
	
	[SerializeField] protected LabelsToLocalize2[] labelsToLocalizeByRound;
	
	[SerializeField] protected WingInformation[] wingsByRound;

	[SerializeField] protected RemainingPicksRevealBehavior[] remainingPicksRevealBehaviorByRound;

	[SerializeField] protected float[] postRevealAnimationWaitOverrideByRound;

	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR ANIMATION NAMES OF PICKED and UNPICKED REVEALS, KEYED BY ROUND
	***************************************************************************************************/		
	[SerializeField] protected RoundAnimationNames[] animationNamesByRound;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLE SPECIFIC TO PICKEM ROUNDS WITH "FIGHTS"
	***************************************************************************************************/	
	[SerializeField] protected FightRoundDefinition[] fightDefinitionsByRound;

	[SerializeField] protected MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition[] multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR PICK SOUND OVERRIDES
	***************************************************************************************************/		
	[SerializeField] protected PickSoundOverrides[] soundOverridesByRound;

	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR SOUND DELAYS
	***************************************************************************************************/	
	[SerializeField] protected SoundDelaysDefinition[] soundDelaysByRound;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR SCENE ANIMATIONS AND SCENE SOUNDS
	***************************************************************************************************/			
	[SerializeField] protected AnimationDefinitions[]  animationDefinitionsByRound;
	[SerializeField] protected SoundDefinitions[] soundDefinitionsByRound;

	/***************************************************************************************************
	* INSPECTOR VARIABLE FOR FIND DEFINITIONS
	***************************************************************************************************/		
	[SerializeField] protected FindDefinition[] findDefinitionByRound;
	private int numFound = 0;
	private long collectAllCreditTotal;
	private bool wonAllCredits = false;
	private List<CorePickData> remainingReveals = new List<CorePickData>();
	private bool revealingCredits = false;
	private long wonCredits;
	/***************************************************************************************************
	* SOUND CONSTANTS FOR MAPPINGS
	* These sound mappings all should be given the suffix of the round we are currently on (+1 because sounds are 1-indexed), except for the first round in which case the suffix is ignored
	***************************************************************************************************/	
	protected const string PICKEM_BG_MUSIC_MAPPING_PREFIX = "pickem_bg_music";
	protected const string PICKEM_INTRO_VO_MAPPING_PREFIX = "pickem_intro_vo";
	protected const string PICKEM_PICKME_MAPPING_PREFIX = "pickem_pickme";
	protected const string PICKEM_PICKED_PREFIX = "pickem_pick_selected";
	protected const string PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX = "pickem_special1_pick";
	protected const string PICKEM_PICK_SPECIAL_2_MAPPING_PREFIX = "pickem_special2_pick";
	protected const string PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX = "pickem_special1_vo_pick";
	protected const string PICKEM_PICK_SPECIAL_2_VO_MAPPING_PREFIX = "pickem_special2_vo_pick";
	protected const string PICKEM_PICK_CREDITS_MAPPING_PREFIX = "pickem_credits_pick";
	protected const string PICKEM_PICK_CREDITS_VO_MAPPING_PREFIX = "pickem_credits_vo_pick";
	protected const string PICKEM_PICK_MULTIPLIER_MAPPING_PREFIX = "pickem_multiplier_pick";
	protected const string PICKEM_PICK_MULTIPLIER_VO_MAPPING_PREFIX = "pickem_multiplier_vo_pick";
	protected const string PICKEM_PICK_BAD_MAPPING_PREFIX = "pickem_reveal_bad";
	protected const string PICKEM_PICK_REVEAL_ADVANCE_MAPPING_PREFIX = "pickem_pick_advance_selected";
	protected const string PICKEM_PICK_REVEAL_INCREASE_MAPPING_PREFIX = "pickem_pick_increase_selected";
	protected const string PICKEM_PICK_BAD_VO_MAPPING_PREFIX = "pickem_reveal_bad_vo";
	protected const string PICKEM_MULTIPLIER_TRAVEL_MAPPING_PREFIX = "pickem_multiplier_travel";
	protected const string PICKEM_MULTIPLIER_ARRIVE_MAPPING_PREFIX = "pickem_multiplier_arrive";
	protected const string PICKEM_ADVANCE_MULTIPLIER_PREFIX = "pickem_advance_multiplier";
	protected const string PICKEM_INCREASE_MULTIPLIER_PREFIX = "pickem_increase_multiplier";
	protected const string MULTIPLIER_SPARKLE_TRAIL_TRAVEL_MAPPING_PREFIX = "pickem_multiplier_travel";
	protected const string MULTIPLIER_SPARKLE_TRAIL_ARRIVE_MAPPING_PREFIX = "pickem_multiplier_arrive";
	protected const string CHANGE_MULTIPLIER_TO_CREDITS_MAPPING_PREFIX = "pickem_multiplier_to_credits";
	protected string REVEAL_SOUND_NAME_PREFIX = "reveal_not_chosen";
	protected string ROLLUP_SOUND_PREFIX = "rollup_bonus_loop";
	protected string ROLLUP_TERM_SOUND_PREFIX = "rollup_bonus_end";

	/***************************************************************************************************
	* SOUND CONSTANTS FOR MAPPINGS THAT AREN'T BY ROUND
	***************************************************************************************************/	
	
	/*==========================================================================================================*\
	* Init
	\*==========================================================================================================*/	
	public override void init()
	{
		defaultPickmeAnimName = pickMeAnimName;
		
		initPickingRounds();
		initButtons();
		
		base.init();
		initStage();
	}
	
	// Copy picking round data into picking game data.
	private void initPickingRounds() // init rounds
	{
		int numRounds = pickingRoundGos.Count;
		
		if (pickingRoundGos.Count > 0)
		{
			pickingRounds = new List<PickingRound>();
			
			foreach (PickingRoundGO pickingRoundGo in pickingRoundGos)
			{
				pickingRoundGo.genericPickemGame = this as GenericPickemGameGO;
				pickingRounds.Add(pickingRoundGo.pickingRound);
			}
		}
	
		numRounds = pickingRounds.Count;
		
		if (numRounds > 0)
		{
			PickingRound firstPickingRound = pickingRounds[0];
			
			if (firstPickingRound.PICKEM_STAGE_TYPE != PickemStageType.None)
			{
				PICKEM_STAGE_TYPES = new PickemStageType[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					PICKEM_STAGE_TYPES[iRound] = pickingRound.PICKEM_STAGE_TYPE;
				}
			}				
			
			if (firstPickingRound.stageObject != null)
			{
				stageObjects = new GameObject[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					stageObjects[iRound] = pickingRound.stageObject;
				}
			}

			if (currentMultiplierLabels.Length == 0)
			{
				currentMultiplierLabels = new UILabel[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					currentMultiplierLabelsWrapper[iRound] = pickingRound.multiplierLabelWrapper;
				}
			}
			
			if (currentWinAmountTextsWrapper.Count == 0 ||
				currentWinAmountTextsWrapper.Count == 1 && currentWinAmountTextsWrapper[0] == null)
			{
				currentWinAmountTextsWrapper = new List<LabelWrapper>();
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					currentWinAmountTextsWrapper.Add(pickingRound.currentWinAmountTextWrapper.labelWrapper);
				}
			}
			
			if (currentWinAmountTextWrappers.Length == 0 ||
			    currentWinAmountTextWrappers.Length == 1 && currentWinAmountTextWrappers[0] == null)
			{
				currentWinAmountTextWrappers = new LabelWrapperComponent[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					currentWinAmountTextWrappers[iRound] = pickingRound.currentWinAmountTextWrapper;
				}
			}
			
			if (jackpotLabelWrappersNew.Count == 0)
			{
				jackpotLabelWrappersNew = new List<LabelWrapper>();
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					jackpotLabelWrappersNew.Add(pickingRound.jackpotLabelWrapperNew);
				}
			}
			
			if (messagingLabelsWrapper.Count == 0)
			{
				messagingLabelsWrapper = new List<LabelWrapper>();
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					messagingLabelsWrapper.Add(pickingRound.messagingLabelWrapper);
				}
			}
			
			if (wingsByRound.Length == 0)
			{
				wingsByRound = new WingInformation[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					WingInformation wingInfo = new WingInformation();
					wingInfo.challengeStage = WingInformation.WingChallengeStage.None;
					
					PickingRound pickingRound = pickingRounds[iRound];
					
					if (pickingRound.hasWings)
					{
						wingInfo.challengeStage = (WingInformation.WingChallengeStage)iRound;
					}
					else
					{
						wingInfo.challengeStage = WingInformation.WingChallengeStage.None;
					}
					
					wingsByRound[iRound] = wingInfo;
				}
			}
			
			if (objectsToActivateOrDeactivateByRound.Length == 0)
			{
				objectsToActivateOrDeactivateByRound = new RoundGameObjects[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					objectsToActivateOrDeactivateByRound[iRound] = pickingRound.objectsToActivateOrDeactivateByRound;
				}
			}
			
			if (animationDefinitionsByRound.Length == 0)
			{
				animationDefinitionsByRound = new AnimationDefinitions[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					animationDefinitionsByRound[iRound] = new AnimationDefinitions();
					animationDefinitionsByRound[iRound].animations = pickingRound.animationDefinitions;
				}
			}

			if (soundDefinitionsByRound.Length == 0)
			{
				soundDefinitionsByRound = new SoundDefinitions[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					soundDefinitionsByRound[iRound] = new SoundDefinitions();
					soundDefinitionsByRound[iRound].sounds = pickingRound.soundDefinitions;
				}
			}
			
			if (firstPickingRound.pickemGos.Length > 0)
			{
				newPickGameButtonRounds = new NewPickGameButtonRound[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					newPickGameButtonRounds[iRound] = new NewPickGameButtonRound();
					
					int numPickemGos = pickingRound.pickemGos.Length;
					newPickGameButtonRounds[iRound].pickGameObjects = new GameObject[numPickemGos];
	
					for (int iGo = 0; iGo < numPickemGos; iGo++)
					{
						newPickGameButtonRounds[iRound].pickGameObjects[iGo] = pickingRound.pickemGos[iGo];
					}
				}
			}
			
			if (animationNamesByRound.Length == 0)
			{
				animationNamesByRound = new RoundAnimationNames[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					animationNamesByRound[iRound] = pickingRound.animationNames;
				}
			}
			
			if (numPickMeAnimsByRound.Length == 0)
			{
				numPickMeAnimsByRound = new int[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					numPickMeAnimsByRound[iRound] = pickingRound.numPickMeAnims;
				}
			}
			
			if (jackpotWinEffects.Length == 0)
			{
				jackpotWinEffects = new GameObject[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					jackpotWinEffects[iRound] = pickingRound.jackpotWinEffect;
				}
			}
			
			if (sparkleTrailDefinitionsByRound.Length == 0)
			{
				sparkleTrailDefinitionsByRound = new SparkleTrailDefinition[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					sparkleTrailDefinitionsByRound[iRound] = pickingRound.sparkleTrailDefinition;
				}
			}
			
			if (revealEffectDefinitionsByRound.Length == 0)
			{
				revealEffectDefinitionsByRound = new RevealDefinition[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					revealEffectDefinitionsByRound[iRound] = pickingRound.revealDefinition;
				}
			}
			
			if (soundOverridesByRound.Length == 0)
			{
				soundOverridesByRound = new PickSoundOverrides[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					soundOverridesByRound[iRound] = pickingRound.soundOverrides;
				}
			}
			
			if (soundDelaysByRound.Length == 0)
			{
				soundDelaysByRound = new SoundDelaysDefinition[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					soundDelaysByRound[iRound] = pickingRound.soundDelays;
				}
			}
			
			if (revealDelaysByRound.Length == 0)
			{
				revealDelaysByRound = new RevealDelaysDefintion[numRounds];
				
				for (int iRound = 0; iRound < numRounds; iRound++)
				{
					PickingRound pickingRound = pickingRounds[iRound];
					revealDelaysByRound[iRound] = pickingRound.revealDelays;
				}
			}
		}
	}
	
	private void initButtons()
	{
		if (buttonDefinitions != null && buttonDefinitions.Count > 0)
		{
			newPickGameButtonRounds = new NewPickGameButtonRound[PICKEM_STAGE_TYPES.Length];

			foreach (PickButtonTemplateDefinition template in buttonDefinitions)
			{
				newPickGameButtonRounds[template.round] = new NewPickGameButtonRound();
				newPickGameButtonRounds[template.round].pickGameObjects = new GameObject[template.objectsToApplyTemplateTo.Count];
				
				for (int i = 0; i < template.objectsToApplyTemplateTo.Count; i++)
				{
					GameObject go = template.objectsToApplyTemplateTo[i];

					PickGameButton pick = go.GetComponent<PickGameButton>();

					if (pick == null)
					{
						pick = go.AddComponent<PickGameButton>();
					}
					
					if (!string.IsNullOrEmpty(template.PATH_TO_BUTTON))
					{
						Transform t = null;
						
						bool objectIsSelf = template.PATH_TO_BUTTON == "." ? true : false;
						
						// If 'PATH_TO_BUTTON' is a '.', then set the transform to self
						if (objectIsSelf)
						{
							t = go.transform;
						}
						else
						{
							t = go.transform.Find(template.PATH_TO_BUTTON);
						}

						if (t != null)
						{
							// The button has the collider and the UI button message.
							if (objectIsSelf)
							{
								pick.button = go;
							}
							else
							{
								pick.button = go.transform.Find(template.PATH_TO_BUTTON).gameObject;
							}
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_BUTTON' failed to return a valid transform!\npath: " + template.PATH_TO_BUTTON);
						}
					}
					
					if (!string.IsNullOrEmpty(template.PATH_TO_ANIMATOR))
					{
						Transform t = null;
						
						// If 'PATH_TO_ANIMATOR' is a '.', then set the transform to self
						if (template.PATH_TO_ANIMATOR == ".")
						{
							t = go.transform;
						}
						else
						{
							t = go.transform.Find(template.PATH_TO_ANIMATOR);
						}


						if (t != null)
						{
							pick.animator = t.gameObject.GetComponent<Animator>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_ANIMATOR' failed to return a valid transform!\npath: " + template.PATH_TO_ANIMATOR);
						}
					}
					
					if (!string.IsNullOrEmpty(template.PATH_TO_REVEALNUMBERLABEL))
					{
						Transform t = go.transform.Find(template.PATH_TO_REVEALNUMBERLABEL);

						if (t != null)
						{
							pick.revealNumberLabel = t.gameObject.GetComponent<UILabel>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_REVEALNUMBERLABEL' failed to return a valid transform!\npath: " + template.PATH_TO_REVEALNUMBERLABEL);
						}
					}

					if (!string.IsNullOrEmpty(template.PATH_TO_REVEALNUMBERLABELWRAPPER))
					{
						Transform wrapperTransform = go.transform.Find(template.PATH_TO_REVEALNUMBERLABELWRAPPER);

						if (wrapperTransform != null)
						{
							pick.revealNumberWrapper = wrapperTransform.gameObject.GetComponent<LabelWrapperComponent>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_REVEALNUMBERLABELWRAPPER' failed to return a valid transform!\npath: " + template.PATH_TO_REVEALNUMBERLABELWRAPPER);
						}
					}
					
					if (!string.IsNullOrEmpty(template.PATH_TO_REVEALNUMBEROUTLINE))
					{
						Transform t = go.transform.Find(template.PATH_TO_REVEALNUMBEROUTLINE);

						if (t != null)
						{
							pick.revealNumberOutlineLabel = t.gameObject.GetComponent<UILabel>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_REVEALNUMBEROUTLINE' failed to return a valid transform!\npath: " + template.PATH_TO_REVEALNUMBEROUTLINE);
						}
					}

					if (!string.IsNullOrEmpty(template.PATH_TO_GRAYREVEALNUMBER))
					{
						Transform t = go.transform.Find(template.PATH_TO_GRAYREVEALNUMBER);

						if (t != null)
						{
							pick.revealGrayNumberLabel = t.gameObject.GetComponent<UILabel>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_GRAYREVEALNUMBER' failed to return a valid transform!\npath: " + template.PATH_TO_GRAYREVEALNUMBER);
						}
					}

					if (!string.IsNullOrEmpty(template.PATH_TO_GRAYREVEALNUMBERLABELWRAPPER))
					{
						Transform t = go.transform.Find(template.PATH_TO_GRAYREVEALNUMBERLABELWRAPPER);

						if (t != null)
						{
							pick.revealGrayNumberWrapper = t.gameObject.GetComponent<LabelWrapperComponent>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_GRAYREVEALNUMBERLABELWRAPPER' failed to return a valid transform!\npath: " + template.PATH_TO_GRAYREVEALNUMBER);
						}
					}
					
					if (!string.IsNullOrEmpty(template.PATH_TO_GRAYREVEALNUMBEROUTLINE))
					{
						Transform t = go.transform.Find(template.PATH_TO_GRAYREVEALNUMBEROUTLINE);

						if (t != null)
						{
							pick.revealGrayNumberOutlineLabel = t.gameObject.GetComponent<UILabel>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_GRAYREVEALNUMBEROUTLINE' failed to return a valid transform!\npath: " + template.PATH_TO_GRAYREVEALNUMBEROUTLINE);
						}
					}
					
					if (!string.IsNullOrEmpty(template.PATH_TO_MULTIPLIERLABEL))
					{
						Transform t = go.transform.Find(template.PATH_TO_MULTIPLIERLABEL);

						if (t != null)
						{
							pick.multiplierLabel = t.gameObject.GetComponent<UILabel>();
						}
						else
						{
							Debug.LogError("The provided 'template.PATH_TO_GRAYREVEALNUMBEROUTLINE' failed to return a valid transform!\npath: " + template.PATH_TO_GRAYREVEALNUMBEROUTLINE);
						}
					}

					if (pick.button == null && pick.animator != null)
					{
						// If the button (with the collider and UI button message) hasn't been wired-up yet,
						// and the animator has been wired-up, then use the animator as the button.
						pick.button = pick.animator.gameObject;
						
						if (pick.animator.gameObject.GetComponent<BoxCollider>() == null)
						{
							BoxCollider box = pick.animator.gameObject.AddComponent<BoxCollider>();
							box.size = template.boxColliderSize;
							box.center = template.boxColliderCenter;
						}

						if (pick.animator.gameObject.GetComponent<UIButtonMessage>() == null)
						{
							UIButtonMessage buttonMessage = pick.animator.gameObject.AddComponent<UIButtonMessage>();
							buttonMessage.target = gameObject;
							buttonMessage.functionName = "pickemButtonPressed";
						}
					}

					newPickGameButtonRounds[template.round].pickGameObjects[i] = pick.gameObject;
				}
			}
		}
	}	
	
	// An outcome name means this stage uses a different outcome or outcome type.
	// But it only works if you set the bonus outcome types in the prefab.
	private void determineOutcomeType(string outcomeName=null)
	{
		PickingRound pickingRound = getPickingRound();

		if (pickingRound != null)
		{
			if (string.IsNullOrEmpty(outcomeName))
			{
				outcomeName = pickingRound.BONUS_OUTCOME_NAME;
			}
		}	
		
		if (pickingRound != null &&
			!string.IsNullOrEmpty(outcomeName) &&
			pickingRound.BONUS_OUTCOME_TYPE != BonusOutcomeTypeEnum.Undefined)
		{
			outcomeType = pickingRound.BONUS_OUTCOME_TYPE;
			
			switch (outcomeType)
			{
				case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
					SlotOutcome newBonusGame =
						SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, outcomeName);
					
					newOutcomeType = new NewBaseBonusGameOutcome(newBonusGame);
					break;
				
				case BonusOutcomeTypeEnum.PickemOutcomeType:
					SlotOutcome pickemGame =
						SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, outcomeName);

					pickemOutcome = new PickemOutcome(pickemGame);
					break;

				case BonusOutcomeTypeEnum.WheelOutcomeType:
					SlotOutcome wheelGame =
						SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, outcomeName);
					
					wheelOutcome = new WheelOutcome(wheelGame);
					break;
			}
			
			// The original outcome is not valid anymore.
			// Make sure nothing else is using it.
			outcome = null;
		}
		else
		if (outcome is NewBaseBonusGameOutcome)
		{
			newOutcomeType = outcome as NewBaseBonusGameOutcome;
			outcomeType = BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType;
			
			// If you're hacking the outcome for testing purposes,
			// then you may make it to a round you weren't supposed to play,
			// so don't try to get the round picks.
			if (currentStage < newOutcomeType.roundPicks.Count)
			{
				roundPicks = newOutcomeType.roundPicks[currentStage];
			}
		}
		else if (outcome is PickemOutcome)
		{
			pickemOutcome = outcome as PickemOutcome;
			outcomeType = BonusOutcomeTypeEnum.PickemOutcomeType;
		}
		else if (outcome is WheelOutcome)
		{
			wheelOutcome = outcome as WheelOutcome;
			outcomeType = BonusOutcomeTypeEnum.WheelOutcomeType;
		}
	}

	// Hack the outcome for testing purposes.
	// Put common hacks here, for example, change all picks to award credits.
	// Override this function for game-specific outcome hacks.
	protected virtual void hackOutcomeForTesting()
	{
#if HACK_OUTCOME
		PickemCheat pickemCheat = new PickemCheat();
/*
		if (currentStage == 0)
		{
			pickemCheat.shouldBadEnd = true;
		}
*/		
		if (0 <= currentStage && currentStage < 3)
		{		
			pickemCheat.shouldJackpotAdvance = true;
		}
		if (currentStage == 3)
		{
		//	pickemCheat.shouldCredits = true;
			pickemCheat.shouldMultiplier = true;
		}
		
		
		PickemStageType pickemStageType = PICKEM_STAGE_TYPES[currentStage];
		
		if (pickemCheat.shouldBadEnd)
		{
			roundPicks.entries = new List<BasePick>();
			roundPicks.reveals = new List<BasePick>();
			
			switch (pickemStageType)
			{
				case PickemStageType.CreditsWithJackpotAdvanceAndBadEnd:
				{
					switch (outcomeType)
					{
						case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
						{							
							BasePick basePick = new BasePick();
							basePick.credits = 0;
							roundPicks.entries.Add(basePick);
						}
						break;
					}
				}
				break;
			}
		}
		
		if (pickemCheat.shouldCredits)
		{
			roundPicks.entries = new List<BasePick>();
			roundPicks.reveals = new List<BasePick>();
			
			switch (pickemStageType)
			{
				case PickemStageType.SinglePickCreditsOrMultiplier:
				{	
					switch (outcomeType)
					{
						case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
						{
							BasePick basePick = new BasePick();
							basePick.credits = 100;
							roundPicks.entries.Add(basePick);
							
							roundPicks.reveals.Add(basePick);
							roundPicks.reveals.Add(basePick);
				}			
						break;
					}
				}	
				break;
			}
		}
		
		if (pickemCheat.shouldMultiplier)
		{
			roundPicks.entries = new List<BasePick>();
			roundPicks.reveals = new List<BasePick>();
			
			switch (pickemStageType)
			{
				case PickemStageType.SinglePickCreditsOrMultiplier:
				{	
					switch (outcomeType)
					{
						case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
						{
							BasePick basePick = new BasePick();							
							basePick.credits = 100;
							basePick.multiplier = 2;	
							roundPicks.entries.Add(basePick);
							
							basePick = new BasePick();
							basePick.credits = 100;
							roundPicks.reveals.Add(basePick);
							roundPicks.reveals.Add(basePick);
						}
						break;
					}
				}	
				break;
			}
		}
		
		if (pickemCheat.shouldJackpotAdvance)
		{
			roundPicks.entries = new List<BasePick>();
			roundPicks.reveals = new List<BasePick>();
			
			switch (pickemStageType)
			{
				case PickemStageType.CreditsWithJackpotAdvanceAndBadEnd:
				{	
					switch (outcomeType)
					{
						case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
						{
							BasePick basePick = new BasePick();
							basePick.credits = 200;						
							roundPicks.entries.Add(basePick);
							
						}
						break;
					}
				}	
				break;
			}
		}
#endif
	}	
	
	protected void playNotChosenAudio(bool doSkipCheck = true, int round = -1)
	{
		if (!doSkipCheck || (doSkipCheck && !revealWait.isSkipping))
		{
			string mapStr = getSoundMappingByRound(REVEAL_SOUND_NAME_PREFIX, round);
			if (string.IsNullOrEmpty(mapStr))     // if we don't map to anything just use the prefix name
			{
				mapStr = Audio.soundMap(REVEAL_SOUND_NAME_PREFIX);
			}
				
			Audio.play(mapStr);
		}
	}

	protected PickingRound getPickingRound(int round = -1)
	{
		PickingRound pickingRound = null;
		
		if (round == -1)
		{
			round = currentStage;
		}
		
		if (0 <= round && round < pickingRounds.Count)
		{
			pickingRound = pickingRounds[round];
		}
		
		return pickingRound;
	}

	protected virtual bool hasMorePicksThisGame()
	{
		if (outcomeType == BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType)
		{
			return (getNumEntries() > 0 || newOutcomeType.hasPicksLeft());
		}
		
		return getNumEntries() > 0 || currentStage < stageObjects.Length;
	}	
	

	protected int getNumEntries()
	{
		switch (outcomeType)
		{
			case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
				return roundPicks.entryCount;
			case BonusOutcomeTypeEnum.PickemOutcomeType:
				return pickemOutcome.entryCount;
			case BonusOutcomeTypeEnum.WheelOutcomeType:
				return wheelOutcome.entryCount;
			case BonusOutcomeTypeEnum.Undefined:
				return 0;
			default:
				return 0;
		}
	}

	public CorePickData getNextEntry()
	{
		switch (outcomeType)
		{
			case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
				return roundPicks.getNextEntry();
			case BonusOutcomeTypeEnum.PickemOutcomeType:
				return pickemOutcome.getNextEntry();
			case BonusOutcomeTypeEnum.WheelOutcomeType:
				return wheelOutcome.getNextEntry();
			case BonusOutcomeTypeEnum.Undefined:
				return null;
			default:
				return null;
		}
	}

	private CorePickData getNthEntry(int index)
	{
		if (0 <= index && index < getNumEntries() )
		{
			switch (outcomeType)
			{
				case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
					return roundPicks.entries[index];
				case BonusOutcomeTypeEnum.PickemOutcomeType:
					return pickemOutcome.entries[index];
				case BonusOutcomeTypeEnum.WheelOutcomeType:
					return wheelOutcome.entries[index];
			}
		}

		return null;
	}

	private int getNumReveals()
	{
		switch (outcomeType)
		{
			case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
				return roundPicks.reveals.Count;

			case BonusOutcomeTypeEnum.PickemOutcomeType:
				return pickemOutcome.revealCount;

			case BonusOutcomeTypeEnum.WheelOutcomeType:
				return wheelOutcome.revealCount;
		}

		return 0;
	}

	public CorePickData getNextReveal()
	{
		switch (outcomeType)
		{
			case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
				return roundPicks.getNextReveal();
				
			case BonusOutcomeTypeEnum.PickemOutcomeType:
				return pickemOutcome.getNextReveal();
				
			case BonusOutcomeTypeEnum.WheelOutcomeType:
				return wheelOutcome.getNextReveal();
				
			default:
				return null;
		}
	}

	private CorePickData getNthReveal(int index)
	{
		if (0 <= index && index < getNumReveals() )
		{
			switch (outcomeType)
			{
			case BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType:
				return roundPicks.reveals[index];
				
			case BonusOutcomeTypeEnum.PickemOutcomeType:
				return pickemOutcome.reveals[index];
				
			case BonusOutcomeTypeEnum.WheelOutcomeType:
				return wheelOutcome.reveals[index];
			}
		}
		
		return null;
	}

	// An outcome name means this stage uses a different outcome or outcome type.
	protected void initStage(string outcomeName = null) // init stage
	{
		determineOutcomeType(outcomeName);

#if UNITY_EDITOR
		hackOutcomeForTesting();
#endif

		numPicksSoFar = 0;
		// If we have a pickme anim override, set the current pickMe anim name to the one stored in the names by round list
		if(!string.IsNullOrEmpty(animationNamesByRound[currentStage].PICKME_ANIM_NAME_OVERRIDE))
		{
			pickMeAnimName = animationNamesByRound[currentStage].PICKME_ANIM_NAME_OVERRIDE;
		}
		else
		{
			pickMeAnimName = defaultPickmeAnimName;
		}
		
		if (currentStage < numPickMeAnimsByRound.Length)
		{
			numPickMeAnims = numPickMeAnimsByRound[currentStage];
		}

		if (currentStage < soundDefinitionsByRound.Length)
		{
			SoundDefinitions soundDefinition = soundDefinitionsByRound[currentStage];
			SoundDefinition[] sounds = soundDefinition.sounds;
			
			for (int iSound = 0; iSound < sounds.Length; iSound++)
			{
				SoundDefinition sound = sounds[iSound];
				
				// Unity ignores the default values in the sound definition, so fix them up here.
				// (Note that 0 is not a valid value for these tunables, so that's how it knows it should assign the defaults).
				if (sound.counter == 0)
				{
					sound.counter = 1;
				}
				if (sound.frequency == 0)
				{
					sound.frequency = 1;
				}
				if (sound.percent == 0)
				{
					sound.percent = 100;
				}
				
				if (sound.counter < sound.maxCounter)
				{
					sound.counter = Random.Range(sound.counter, sound.maxCounter + 1);
				}
				
				if (sound.isCollection)
				{
					if (sound.shouldResetCollection)
					{
						Audio.resetCollectionBySoundMapOrSoundKey(sound.soundName);
					}
					
					Audio.setCollectionCycling(sound.soundName, sound.shouldCycleCollection);
				}
			}
		}

		if (currentStage < soundOverridesByRound.Length)
		{
			PickSoundOverrides soundOverrides = soundOverridesByRound[currentStage];
			
			if (soundOverrides != null)
			{
				// Credits.
				if (soundOverrides.SHOULD_RESET_CREDITS_SOUND && soundOverrides.CREDITS_SOUND_NAME != "")
				{
					Audio.resetCollectionBySoundMapOrSoundKey(soundOverrides.CREDITS_SOUND_NAME);
				}
				if (soundOverrides.SHOULD_RESET_CREDITS_VO && soundOverrides.CREDITS_VO_NAME != "")
				{
					Audio.resetCollectionBySoundMapOrSoundKey(soundOverrides.CREDITS_VO_NAME);
				}
		
				// Special 1.
				if (soundOverrides.SHOULD_RESET_SPECIAL1_SOUND && soundOverrides.SPECIAL1_VO_NAME != "")
				{
					Audio.resetCollectionBySoundMapOrSoundKey(soundOverrides.SPECIAL1_SOUND_NAME);
				}
				if (soundOverrides.SHOULD_RESET_SPECIAL1_VO && soundOverrides.SPECIAL1_VO_NAME != "")
				{
					Audio.resetCollectionBySoundMapOrSoundKey(soundOverrides.SPECIAL1_VO_NAME);
				}
			}
		}

		if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsWithJackpotAdvanceAndBadEnd)
		{			
			if (outcomeType == BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType)
			{
				jackpotCredits = roundPicks.getHighestPossibleCreditValue();
				foreach (UILabel jackpotLabel in jackpotLabels)
				{
					if (jackpotLabel != null)
					{
						jackpotLabel.text = CreditsEconomy.convertCredits(jackpotCredits);
					}
				}

				foreach (LabelWrapperComponent jackpotLabel in jackpotLabelWrappers)
				{
					if (jackpotLabel != null)
					{
						jackpotLabel.text = CreditsEconomy.convertCredits(jackpotCredits);
					}
				}
			}
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsOrAdvanceOrJackpotEnd)
		{
			if (outcomeType == BonusOutcomeTypeEnum.PickemOutcomeType)
			{
				jackpotCredits = pickemOutcome.jackpotBaseValue;

				foreach (UILabel jackpotLabel in jackpotLabels)
				{
					if (jackpotLabel != null)
					{
						jackpotLabel.text = CreditsEconomy.convertCredits(jackpotCredits);
					}
				}
			}
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.MultiplierAdvanceOrIncreaseWithCreditsOnEnd)
		{
			if (outcomeType == BonusOutcomeTypeEnum.NewBaseBonusGameOutcomeType)
			{
				Dictionary<string, JSON> paytablePools = newOutcomeType.paytablePools;
				
				MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef =
					multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions[currentStage];

				// Grab all the items in our paytable pool so we can populate later.
				JSON paytablePool = paytablePools[multAdvanceDef.PAYTABLE_POOL_NAME];
				JSON[] items = paytablePool.getJsonArray("items");

				multAdvanceDef.paytableMultipliers = new Dictionary<int, List<int>>();

				for (int i = 0; i < items.Length; i++)
				{
					multAdvanceDef.paytableMultipliers.Add(i, new List<int>());
				}

				// index on the sort index, and add in the multipliers
				foreach (JSON item in items)
				{
					multAdvanceDef.paytableMultipliers[item.getInt("horizontal_sort_index", 0)].Add(item.getInt("multiplier", 0));
				}

				// Now sort them so we are certain they're in order.
				for (int i = 0; i < items.Length; i++)
				{
					multAdvanceDef.paytableMultipliers[i].Sort();
				}

				// Let's now populate the visible indexes according to what we got here.
				for (int i = 0; i < multAdvanceDef.multiplierCells.Count; i++)
				{
					multAdvanceDef.multiplierCells[i].setValue(multAdvanceDef.paytableMultipliers[multAdvanceDef.horizontalMultiplierIndex][i]);
				}
				
				// If we are using dynamic cell bursts, clone the burst object and put the clones in the propper places
				if (multAdvanceDef.usesDynamicCellBursts)
				{
					for (int i = 0; i < multAdvanceDef.multiplierCells.Count; i++)
					{
						GameObject newBurst = null;
						GameObject container = multAdvanceDef.multiplierCells[i].container;
						
						//Vector3 targetPos = container.transform.position;
						
						if (i != multAdvanceDef.multiplierCells.Count-1)
						{
							newBurst = CommonGameObject.instantiate(multAdvanceDef.dynamicCellBurst) as GameObject;
						}
						else
						{
							newBurst = CommonGameObject.instantiate(multAdvanceDef.bigDynamicCellBurst) as GameObject;
						}
						
						// Parent the burst to the relevant container
						newBurst.transform.parent = container.transform;
						
						// Store the cell burst animation
						multAdvanceDef.multiplierCells[i].cellBurst = newBurst.GetComponent<Animator>();
						
						// Set the bursts position to that of the container
						newBurst.transform.position = new Vector3(container.transform.position.x, container.transform.position.y, container.transform.position.z);
						
						// Set the bursts scale to default
						newBurst.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
						
					}
				}
				
				multAdvanceDef.currentRoundPick = newOutcomeType.roundPicks[currentStage];
				StartCoroutine(activateCurrentMultiplierCell());
			}
		}

		if (currentStage < objectsToActivateOrDeactivateByRound.Length)
		{
			foreach (GameObject go in objectsToActivateOrDeactivateByRound[currentStage].objectsToActivateThisRound)
			{
				go.SetActive(true);
			}
			foreach (GameObject go in objectsToActivateOrDeactivateByRound[currentStage].objectsToDeactivateThisRound)
			{
				go.SetActive(false);
			}
		}
		
		if (currentStage < wingsByRound.Length)
		{
			switch (wingsByRound[currentStage].challengeStage)
			{
				case WingInformation.WingChallengeStage.First:
					BonusGameManager.instance.wings.show();
					BonusGameManager.instance.wings.forceShowChallengeWings(wingsInForeground);
					break;
				case WingInformation.WingChallengeStage.Secondary:
					BonusGameManager.instance.wings.show();
					BonusGameManager.instance.wings.forceShowSecondaryChallengeWings(wingsInForeground);
					break;
				case WingInformation.WingChallengeStage.Third:
					BonusGameManager.instance.wings.show();
					BonusGameManager.instance.wings.forceShowThirdChallengeWings(wingsInForeground);
					break;
				case WingInformation.WingChallengeStage.Fourth:
					BonusGameManager.instance.wings.show();
					BonusGameManager.instance.wings.forceShowFourthChallengeWings(wingsInForeground);
					break;
				case WingInformation.WingChallengeStage.None:
					BonusGameManager.instance.wings.hide();
					break;
			}
		}
		
		if (currentStage < pickItemStartBehaviorByRound.Length)
		{
			initializePickItemBehavior(pickItemStartBehaviorByRound[currentStage]);
		}
		
		
		foreach (LabelWrapper winAmount in currentWinAmountTextsWrapper)
		{
			if (winAmount != null)
			{
				winAmount.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			}
		}

		if (currentNumPicksLabelWrapper != null)
		{
			currentNumPicksLabelWrapper.text = CommonText.formatNumber(currentNumPicks);
		}

		if (labelsToLocalizeByRound.Length > 0)
		{
			foreach (LabelToLocalize lblToLoc in labelsToLocalizeByRound[currentStage].labelDefs)
			{
				string localization = Localize.text(lblToLoc.localizationKey);
				if (lblToLoc.toUpper)
				{
					lblToLoc.labelWrapper.text = Localize.toUpper(localization);
				}
				else
				{
					lblToLoc.labelWrapper.text = localization;
				}
			}
		}

		pickMeSoundName = getSoundMappingByRound(PICKEM_PICKME_MAPPING_PREFIX);		
		Audio.switchMusicKeyImmediate(getSoundMappingByRound(PICKEM_BG_MUSIC_MAPPING_PREFIX));		
		string introVoName = getSoundMappingByRound(PICKEM_INTRO_VO_MAPPING_PREFIX);
		if (!string.IsNullOrEmpty(introVoName))
		{
			Audio.play(introVoName);
		}
		
		if (currentStage < animationDefinitionsByRound.Length)
		{
			StartCoroutine(
				playSceneAnimations(
					animationDefinitionsByRound[currentStage].animations,
					AnimationDefinition.PlayType.Idle));
		}
		
		if (currentStage < soundDefinitionsByRound.Length)
		{
			playSceneSounds(SoundDefinition.PlayType.Idle);
		}
		
		if (currentStage < pickingRoundGos.Count)
		{
			pickingRoundGos[currentStage].initRound();
		}
		
		// If it's the first round, then maybe wait to enable input.
		// (For example, if you're waiting for an intro animation or a transition animation).
		if (currentStage == 0 && waitToEnableInputDur > 0.0f)
		{
			inputEnabled = false;
			StartCoroutine(waitToEnableInput());
		}
		else
		{
			inputEnabled = true;
		}
	}

	private IEnumerator waitToEnableInput()
	{	
		yield return new WaitForSeconds(waitToEnableInputDur);
		inputEnabled = true;
	}
	
	private void initializePickItemBehavior(PickItemStartBehavior behavior)
	{
		if (behavior.shouldFloatItems)
		{
			foreach (GameObject item in newPickGameButtonRounds[currentStage].pickGameObjects)
			{
				StartCoroutine(floatPickItemLoop(item, behavior));
			}
		}
	}
	
	private IEnumerator floatPickItemLoop(GameObject item, PickItemStartBehavior behavior)
	{
		while (!hasGameEnded)
		{
			float actualTime = Random.Range(behavior.floatPunchTimeLowerBound, behavior.floatPunchTimeUpperBound);
			float startPosY = item.transform.localPosition.y;
			if (isButtonAvailableToSelect(item))
			{
				float actualDistanceY = Random.Range(behavior.floatDistanceYLowerBound, behavior.floatDistanceYUpperBound);
				iTween.MoveBy(item, iTween.Hash("y", actualDistanceY, "time", actualTime * 0.25f, "islocal", true, "easetype", behavior.easeTypeFirstFourth));
				iTween.MoveBy(item, iTween.Hash("y", -2.0f * actualDistanceY, "time", actualTime * 0.5f, "islocal", true, "delay", actualTime * 0.25f, "easetype", behavior.easeTypeMiddleHalf));
				iTween.MoveTo(item, iTween.Hash("y", startPosY, "time", actualTime * 0.25f, "islocal", true, "delay", actualTime * 0.75f, "easetype", behavior.easetypeLastFourth));
			}			
			
			yield return new TIWaitForSeconds(actualTime);
			item.transform.localPosition = new Vector3(item.transform.localPosition.x, startPosY, item.transform.localPosition.z);
		}
	}
	
	public override void continueToNextStage()
	{
		int nextStage = currentStage + 1;
		
		PickingRound pickingRound = getPickingRound();
		if (pickingRound != null)
		{
			switch (pickingRound.nextStageRule)
			{
				case NextStageRule.NextStagePlusCurrentPick:
					nextStage += currentPickIndex;
					break;
			}

			switchToStage(nextStage, pickingRound.keepShowingCurrentStage);
		}
		else
		{
			switchToStage(nextStage, false);
		}

		previousPickIndex = currentPickIndex;
		currentPickIndex = -1;
	}
	
/*==========================================================================================================*\
	Pickem Button Pressed Coroutine
\*==========================================================================================================*/
	
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject pickButton)
	{
		//Decrement number of picks left
		if (currentNumPicksLabelWrapper != null)
		{
			currentNumPicks--;
			currentNumPicksLabelWrapper.text = CommonText.formatNumber(currentNumPicks);
		}
		
		// Increment the number of picks you've made so far.
		numPicksSoFar++;
		
		playSceneSounds(SoundDefinition.PlayType.Pick);
		
		if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsWithJackpotAdvanceAndBadEnd)
		{
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			yield return StartCoroutine(creditsWithJackpotAdvanceAndBadEndButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.SinglePickCreditsOrMultiplier)
		{
			yield return StartCoroutine(singlePickCreditsOrMultiplierButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsOrFightWithLoseOrMultiplier)
		{
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			yield return StartCoroutine(creditsOrFightWithLoseOrMultiplierButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.SinglePickAdvance)
		{
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			yield return StartCoroutine(singlePickAdvanceButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsOrAdvanceOrJackpotEnd)
		{
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			yield return StartCoroutine(creditsOrAdvanceOrJackpotEndButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.RetreatOrEnd)
		{
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			yield return StartCoroutine(retreatOrEndButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.MultiplierAdvanceOrIncreaseWithCreditsOnEnd)
		{
			yield return StartCoroutine(multiplierAdvanceOrIncreaseCreditsOnEndCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsWithAdvanceAndBadEnd)
		{
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			yield return StartCoroutine(creditsWithAdvanceAndBadEndButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.FindToAdvanceWithCreditsAndBadEnd)
		{
			yield return StartCoroutine(findToAdvanceWithCreditsAndBadEndButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.PickNumPicks)
		{
			yield return StartCoroutine(pickNumPicksButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsAndMultipliers)
		{
			yield return StartCoroutine(creditsAndMultipliersButtonPressedCoroutine(pickButton));
		}
		else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.SinglePickAdvanceWithCreditsOrCollectAllOrBadEnd)
		{
			yield return StartCoroutine(singlePickAdvanceWithCreditsOrCollectAllOrBadEnd(pickButton));
		}
		else if (currentStage < pickingRoundGos.Count)
		{
			PickingRoundGO pickingRoundGo = pickingRoundGos[currentStage];
			
			if (pickingRoundGo != null)
			{
				yield return StartCoroutine(pickingRoundGo.pickemButtonPressedCoroutine(pickButton));
			}
		}
		
		// If you picked all the picks,
		// but it didn't already end the round or end the game,
		// then do something so players don't get stuck.
		if ((getNumEntries() == 0 && !hasGameEnded) || (!anyButtonsAvailableToSelect()))
		{
			inputEnabled = false;
			
			PickingRound pickingRound = getPickingRound();

			// If the game has ended, none of the code in these blocks needs to execute...
			if(!hasGameEnded)
			{
				if (pickingRound != null && pickingRound.nextStageRule == NextStageRule.LastStage)
				{
					hasGameEnded = true;
				}
				else if (shouldLastPickContinueToNextRound && currentStage + 1 < stageObjects.Length)
				{
					continueToNextStage();
					initStage();
					inputEnabled = true;
				}
				else if (shouldLastPickEndGame)
				{
					hasGameEnded = true;
				}
				else if (pickingRound != null && pickingRound.nextStageRule == NextStageRule.NextStage)
				{
					continueToNextStage();
					initStage();
					inputEnabled = true;
				}
			}
			
			if (hasGameEnded)
			{
				setMessagingToGameOver();

				if (ALWAYS_REVEAL_PICKS_ON_GAME_END)
				{
					yield return StartCoroutine(revealRemainingPicks());
				}
				else
				{
					BonusGamePresenter.instance.gameEnded();
				}
			}
		}
	}

/*==========================================================================================================*\
	Reveal Picks Coroutine
\*==========================================================================================================*/

	public IEnumerator revealRemainingPicks()
	{
		bool shouldEndGame = hasGameEnded || !hasMorePicksThisGame();  // use number of picks left to decide if the game is over
		
#if HACK_OUTCOME
		shouldEndGame = hasGameEnded;
#endif

		revealWait.reset();
		
		PickGameButtonData pick = removeNextPickGameButton();
		while (pick != null)
		{
			yield return StartCoroutine(revealWait.wait(revealDelaysByRound[currentStage].WAIT_TO_REVEAL_DUR));
			
			if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsWithJackpotAdvanceAndBadEnd)
			{
				creditsWithJackpotAdvanceAndBadEndRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.SinglePickCreditsOrMultiplier)
			{
				singlePickCreditsOrMultiplierRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsOrFightWithLoseOrMultiplier)
			{
				creditsOrFightWithLoseOrMultiplierRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.SinglePickAdvance)
			{
				singlePickAdvanceRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsOrAdvanceOrJackpotEnd)
			{
				creditsOrAdvanceOrJackpotEndRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.RetreatOrEnd)
			{
				retreatOrEndRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.MultiplierAdvanceOrIncreaseWithCreditsOnEnd)
			{
				multiplierAdvanceOrIncreaseCreditsOnEndRevealRemainingPick(pick);
			}

			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsWithAdvanceAndBadEnd)
			{
				creditsWithAdvanceAndBadEndRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.FindToAdvanceWithCreditsAndBadEnd)
			{
				findToAdvanceWithCreditsAndBadEndRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.PickNumPicks)
			{
				pickNumPicksRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.CreditsAndMultipliers)
			{
				creditsAndMultipliersRevealRemainingPick(pick);
			}
			else if (PICKEM_STAGE_TYPES[currentStage] == PickemStageType.SinglePickAdvanceWithCreditsOrCollectAllOrBadEnd)
			{
				singlePickAdvanceWithCreditsOrCollectAllOrBadEndRevealRemainingPick(pick);
			}			
			else if (currentStage < pickingRoundGos.Count)
			{
				PickingRoundGO pickingRoundGo = pickingRoundGos[currentStage];
				
				if (pickingRoundGo != null)
				{
					pickingRoundGo.revealRemainingPick(pick);
				}
			}
			
			pick = removeNextPickGameButton();
		}		


		if (pick == null && PICKEM_STAGE_TYPES[currentStage] == PickemStageType.MultiplierAdvanceOrIncreaseWithCreditsOnEnd)
		{
			MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef = multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions[currentStage];
			long credits = (BonusGamePresenter.instance.currentPayout * multAdvanceDef.paytableMultipliers[multAdvanceDef.horizontalMultiplierIndex][multAdvanceDef.verticalMultiplierIndex]) - 
				BonusGamePresenter.instance.currentPayout;
			yield return StartCoroutine(doSparkleTrail(multAdvanceDef.multiplierCells[multAdvanceDef.verticalMultiplierIndex].animator.gameObject, multAdvanceDef.winBox.gameObject, true));
			multAdvanceDef.winBox.GetComponent<Animator>().Play(multAdvanceDef.WINBOX_CELEBRATION_ANIMATION_NAME);
			yield return StartCoroutine(addCredits(credits));
			BonusGameManager.instance.multiBonusGamePayout += (BonusGamePresenter.instance.currentPayout * BonusGameManager.instance.currentMultiplier);
		}

		if (shouldEndGame)
		{			
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_END_GAME_DUR);			
			BonusGamePresenter.instance.gameEnded();
		}

	}

/*==========================================================================================================*\
	Different Stage Type Implementations Below
\*==========================================================================================================*/

	

/*==========================================================================================================*\
	Multiplier Advance or Increase Credits on End Coroutine Section
\*==========================================================================================================*/

	private IEnumerator playCellBurstAnimation(MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition def, MultiplierCell cell, float delay)
	{
		yield return new WaitForSeconds(delay);
		cell.cellBurst.Play(def.CELL_BURST_ANIMATION_NAME);
	}
	
	private IEnumerator activateCurrentMultiplierCell(bool deactivatePrevious = false)
	{
		MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef = multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions[currentStage];

		if (deactivatePrevious)
		{
			int lastMultiplierCellIndex = multAdvanceDef.verticalMultiplierIndex - 1;

			if (lastMultiplierCellIndex >= 0)
			{
				multAdvanceDef.multiplierCells[lastMultiplierCellIndex].animator.Play(multAdvanceDef.MULTIPLIER_DEACTIVATE_ANIMATION_NAME);
			}

			if (multAdvanceDef.verticalMultiplierIndex < multAdvanceDef.multiplierCells.Count)
			{
				MultiplierCell cell = multAdvanceDef.multiplierCells[multAdvanceDef.verticalMultiplierIndex];
				StartCoroutine(playCellBurstAnimation(multAdvanceDef, cell, multAdvanceDef.CELL_BURST_ADVANCE_DELAY));
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(cell.animator, multAdvanceDef.MULTIPLIER_ADVANCE_ANIMATION_NAME));
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(cell.animator, multAdvanceDef.MULTIPLIER_ACTIVATE_ANIMATION_NAME));
			}
		}
		else
		{
			MultiplierCell cell = multAdvanceDef.multiplierCells[multAdvanceDef.verticalMultiplierIndex];
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(cell.animator, multAdvanceDef.MULTIPLIER_ACTIVATE_ANIMATION_NAME));
		}
	}

	public IEnumerator increaseMultiplierCellValue(int index)
	{
		Audio.play(getSoundMappingByRound(PICKEM_INCREASE_MULTIPLIER_PREFIX));
		MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef = multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions[currentStage];
		yield return new TIWaitForSeconds(multAdvanceDef.MULTIPLIER_INCREASE_DELAY);
		multAdvanceDef.multiplierCells[index].setValue(multAdvanceDef.paytableMultipliers[multAdvanceDef.horizontalMultiplierIndex][index]);
	}

	private IEnumerator playIncreaseAnimation()
	{
		string animName = "";
		
		MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef = multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions[currentStage];
		for (int i = 0; i < multAdvanceDef.multiplierCells.Count; i++)
		{
			StartCoroutine(increaseMultiplierCellValue(i));
			
			if (i == multAdvanceDef.verticalMultiplierIndex)
			{
				animName = multAdvanceDef.MULTIPLIER_INCREASE_ACTIVE_ANIMATION_NAME;
			}
			else
			{
				animName = multAdvanceDef.MULTIPLIER_INCREASE_INACTIVE_ANIMATION_NAME;
			}
		
			StartCoroutine(playCellBurstAnimation(multAdvanceDef, multAdvanceDef.multiplierCells[i], 0.0f));
			multAdvanceDef.multiplierCells[i].animator.Play(animName);
			
			yield return new TIWaitForSeconds(multAdvanceDef.MULTIPLIER_INCREASE_ANIMATION_DELAY);
		}
	}
	
	protected IEnumerator playMultiplierAdvanceRevealCelebrationDealy(MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef)
	{
		yield return new TIWaitForSeconds(multAdvanceDef.MULTIPLIER_ADVANCE_REVEAL_CELEBRATION_DELAY);
		multAdvanceDef.advanceWinBox.GetComponent<Animator>().Play(multAdvanceDef.ADVANCE_WIN_BOX_ANIMATION_NAME);
	}
	
	protected IEnumerator playMultiplierIncreaseRevealCelebrationDealy(MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef)
	{
		yield return new TIWaitForSeconds(multAdvanceDef.MULTIPLIER_INCREASE_REVEAL_CELEBRATION_DELAY);
		multAdvanceDef.increaseWinBox.GetComponent<Animator>().Play(multAdvanceDef.INCREASE_WIN_BOX_ANIMATION_NAME);
	}
	
	protected IEnumerator multiplierAdvanceOrIncreaseCreditsOnEndCoroutine(GameObject pickButton)
	{
		inputEnabled = false;

		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);

		MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef = multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions[currentStage];
		//MultiplierCell mc = multAdvanceDef.multiplierCells[currentStage];

		multAdvanceDef.currentPick = multAdvanceDef.currentRoundPick.getNextEntry();

		if (multAdvanceDef.currentPick.credits != 0)
		{
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX));
			pick.setNumberText(CreditsEconomy.convertCredits(multAdvanceDef.currentPick.credits));
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pick.animator, animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME));
			yield return StartCoroutine(addCredits(multAdvanceDef.currentPick.credits));
		}
		else if (!string.IsNullOrEmpty(multAdvanceDef.currentPick.poolKeyName))
		{
			if (multAdvanceDef.currentPick.verticalShift != 0)
			{
				Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX));
				Audio.play(getSoundMappingByRound(PICKEM_PICK_REVEAL_ADVANCE_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
				multAdvanceDef.verticalMultiplierIndex++;
				Audio.play(getSoundMappingByRound(PICKEM_ADVANCE_MULTIPLIER_PREFIX));
				StartCoroutine(CommonAnimation.playAnimAndWait(pick.animator, animationNamesByRound[currentStage].REVEAL_PICKED_MULTIPLIER_ADVANCE_ANIM_NAME));
				if (multAdvanceDef.advanceWinBox != null)
				{
					StartCoroutine(playMultiplierAdvanceRevealCelebrationDealy(multAdvanceDef));
				}
				Audio.play(getSoundMappingByRound(PICKEM_MULTIPLIER_TRAVEL_MAPPING_PREFIX));
				yield return new TIWaitForSeconds(multAdvanceDef.ADVANCE_REVEAL_SPARKLE_TRAIL_DELAY);
				yield return StartCoroutine(doSparkleTrail(pick.animator.gameObject, multAdvanceDef.multiplierCells[multAdvanceDef.verticalMultiplierIndex].animator.gameObject, true));
				Audio.play(getSoundMappingByRound(PICKEM_MULTIPLIER_ARRIVE_MAPPING_PREFIX));
				StartCoroutine(activateCurrentMultiplierCell(true));
				yield return new TIWaitForSeconds(multAdvanceDef.MULTIPLIER_ADVANCE_ANIMATION_DELAY);
			}
			else
			{
				Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_2_MAPPING_PREFIX));
				Audio.play(getSoundMappingByRound(PICKEM_PICK_REVEAL_INCREASE_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL2_SOUND_DELAY);
				multAdvanceDef.horizontalMultiplierIndex++;
				if (multAdvanceDef.increaseWinBox != null)
				{
					StartCoroutine(playMultiplierIncreaseRevealCelebrationDealy(multAdvanceDef));
				}
				// Wait till the icon is completely revealed before proceeding to the increase animation
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(pick.animator, animationNamesByRound[currentStage].REVEAL_PICKED_MULTIPLIER_INCREASE_ANIM_NAME));
				yield return StartCoroutine(playIncreaseAnimation());
			}

			inputEnabled = true;
		}
	}
	

/*==========================================================================================================*\
	Credits With Jackpot Advance and Bad End Stage Section
\*==========================================================================================================*/
	protected IEnumerator creditsWithJackpotAdvanceAndBadEndButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;

		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);

		pickData = getNextEntry();
		long credits = pickData.credits;
		string bonusGame = pickData.bonusGame;
		if (credits > 0)
		{
			if (credits == jackpotCredits)
			{
				if (FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS)
				{
					addNewFadeObject(pick, animationNamesByRound[currentStage].FADE_PICKED_SPECIAL1_ANIM_NAME);
				}
				canPlayJackpotAdvancingRollupLoopSounds = true;
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
				
				playSceneSounds(SoundDefinition.PlayType.Good);
				Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
				Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
				
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));

				if (currentStage < animationDefinitionsByRound.Length)
				{	
					yield return StartCoroutine(
						playSceneAnimations(
							animationDefinitionsByRound[currentStage].animations,
							AnimationDefinition.PlayType.Jackpot));
				}
				
				if (currentStage < jackpotWinEffects.Length)
				{
					if (jackpotWinEffects[currentStage] != null)
					{
						jackpotWinEffects[currentStage].SetActive(true);
					}
				}

				if (currentStage < sparkleTrailDefinitionsByRound.Length)
				{
					yield return StartCoroutine(doSparkleTrail());
					StartCoroutine(doPostSparkleTrailEffects());
				}
				
				if (currentStage < animationDefinitionsByRound.Length)
				{	
					yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Good));
				}

				yield return StartCoroutine(addCredits(credits));
				yield return StartCoroutine(revealRemainingPicks());
				yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
				if (FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS)
				{
					fadeOutRemainingPicksWithMultipleAnimationNames();
				}
				if (currentStage < revealEffectDefinitionsByRound.Length)
				{
					stopRevealEffects();
				}
				
				if (currentStage < jackpotWinEffects.Length)
				{
					if (jackpotWinEffects[currentStage] != null)
					{
						jackpotWinEffects[currentStage].SetActive(false);
					}
				}
				if (jackpotAdvanceRollupLoopSound != null)
				{
					Audio.stopSound(jackpotAdvanceRollupLoopSound);
					playSceneSounds(SoundDefinition.PlayType.Advance);
					jackpotAdvanceRollupLoopSound = null; //reset it for when the next stage starts
					canPlayJackpotAdvancingRollupLoopSounds = false;
				}

				if (currentStage < animationDefinitionsByRound.Length)
				{	
					yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Advance));
				}
				continueToNextStage();
				initStage(bonusGame);
			}
			else
			{
				if (FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS)
				{
					addNewFadeObject(pick, animationNamesByRound[currentStage].FADE_PICKED_CREDITS_ANIM_NAME);
				}
				pick.setNumberText(CreditsEconomy.convertCredits(credits));
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);

				playSceneSounds(SoundDefinition.PlayType.Credits);
				
				Audio.play(
					getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX),
					1.0f, 0.0f,
					soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
					
				Audio.play(
					getSoundMappingByRound(PICKEM_PICK_CREDITS_VO_MAPPING_PREFIX),
					1.0f, 0.0f,
					soundDelaysByRound[currentStage].REVEAL_CREDITS_VO_SOUND_DELAY);
				
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
				yield return StartCoroutine(addCredits(credits));
				
				inputEnabled = true;
			}
		}
		else if (credits == 0)
		{
			shouldLastPickContinueToNextRound = false;
			if (FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS)
			{
				addNewFadeObject(pick, animationNamesByRound[currentStage].FADE_PICKED_END_ANIM_NAME);
			}
			if (currentStage < animationDefinitionsByRound.Length)
			{	
				yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Bad));
			}
			
			playSceneSounds(SoundDefinition.PlayType.Bad);
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);
			hasGameEnded = true;
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(revealRemainingPicks());
		}

	}

	// play scene sounds if matchName equals matchString
	public void playMatchedSceneSounds(string matchString)
	{
		if (currentStage < soundDefinitionsByRound.Length)
		{
			SoundDefinition[] sounds = soundDefinitionsByRound[currentStage].sounds;
			
			bool hadAMatch = false;

			for (int iSound = 0; iSound < sounds.Length; iSound++)
			{
				SoundDefinition sound = sounds[iSound];
				if (sound.playType == SoundDefinition.PlayType.StringMatched && sound.matchName == matchString)
				{
					sound.playType = SoundDefinition.PlayType.IsStringMatched;
					hadAMatch = true;
				}
			}

			if (hadAMatch)
			{
				playSceneSounds(SoundDefinition.PlayType.IsStringMatched);    

				// put play type back to StringMatched
				for (int iSound = 0; iSound < sounds.Length; iSound++)
				{
					SoundDefinition sound = sounds[iSound];
					if (sound.playType == SoundDefinition.PlayType.IsStringMatched)
					{
						sound.playType = SoundDefinition.PlayType.StringMatched;
					}
				}				
			}		

		}			

	}	
	
	// play scene sounds
	public void playSceneSounds(SoundDefinition.PlayType playType)
	{
		if (currentStage < soundDefinitionsByRound.Length)
		{
			SoundDefinitions soundDefinitions = soundDefinitionsByRound[currentStage];
			SoundDefinition[] sounds = soundDefinitions.sounds;
			
			for (int iSound = 0; iSound < sounds.Length; iSound++)
			{
				SoundDefinition sound = sounds[iSound];
				
				if (sound.playType == playType)
				{
					sound.counter--;
					sound.numTries++;
					
					if (sound.counter == 0 &&
					   (sound.onlyOnNthPick == 0 || sound.onlyOnNthPick == numPicksSoFar) && 
					   (sound.onlyOnNthTry == 0 || sound.numTries == sound.onlyOnNthTry ) &&
					   (sound.startPlayingOnNthTry == 0 || sound.startPlayingOnNthTry <= sound.numTries ) &&
					   (sound.keepPlayingUntilNthTry == 0 || sound.numTries <= sound.keepPlayingUntilNthTry ))
					{
						if (Random.Range(0, 100) < sound.percent)
						{
							if (sound.soundName != "")
							{
								if (playType == SoundDefinition.PlayType.DuringRollup && sound.isLooping && canPlayJackpotAdvancingRollupLoopSounds)
								{
									jackpotAdvanceRollupLoopSound = Audio.play(sound.soundName, 1.0f, 0.0f, sound.delayBeforePlay);
								} 

								else if (!sound.isLooping)
								{
									Audio.play(sound.soundName, 1.0f, 0.0f, sound.delayBeforePlay);
								}
							}
							
							if (sound.musicName != "")
							{
								Audio.switchMusicKeyImmediate(sound.musicName);
							}
							
							sound.numPlays++;
						}
						
						sound.counter = sound.frequency;
						
						if (sound.frequency < sound.maxFrequency)
						{
							sound.counter = Random.Range(sound.frequency, sound.maxFrequency + 1);
						}
					}
					
				}
			}
		}
	}
	protected void creditsWithJackpotAdvanceAndBadEndRevealRemainingPick(PickGameButtonData pick)
	{
		pickData = getNextReveal();
		if (pickData != null)
		{
			long credits = pickData.credits;
			
			if (credits > 0)
			{
				if (credits == jackpotCredits)
				{
					if (FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS)
					{
						addNewFadeObject(pick, animationNamesByRound[currentStage].FADE_UNPICKED_SPECIAL1_ANIM_NAME);
					}
					pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
					playNotChosenAudio();
				}
				else
				{
					if (FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS)
					{
						addNewFadeObject(pick, animationNamesByRound[currentStage].FADE_UNPICKED_CREDITS_ANIM_NAME);
					}
					pick.setText(CreditsEconomy.convertCredits(credits), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
					
					pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
					playNotChosenAudio();
				}
			}
			else if (credits == 0)
			{
				if (FADE_OUT_REVEALED_PICKS_WITH_ANIMATIONS)
				{
					addNewFadeObject(pick, animationNamesByRound[currentStage].FADE_UNPICKED_END_ANIM_NAME);
				}
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_END_ANIM_NAME);
				playNotChosenAudio();
			}
		}
	}

/*==========================================================================================================*\
	 Single Pick Credits or Multiplier Section
\*==========================================================================================================*/

	protected IEnumerator singlePickCreditsOrMultiplierButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		pickData = getNextEntry();
		int multiplier = pickData.multiplier;
		long credits = pickData.credits;
		
		if (multiplier > 0)
		{
			if (currentStage < remainingPicksRevealBehaviorByRound.Length &&
			    remainingPicksRevealBehaviorByRound[currentStage].deactivateRemainingPicksBeforeRevealOnGoodOutcome)
			{
				fadeOutRemainingPicks();
			}
		
			playSceneSounds(SoundDefinition.PlayType.PreCreditsReveal);
			if (currentStage < animationDefinitionsByRound.Length)
			{
				previousPickIndex = pickIndex;
				yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.PreCreditsReveal));
				yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.PreGoodReveal));
			}
			
			playSceneSounds(SoundDefinition.PlayType.Multiplier);

			yield return new TIWaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_REVEAL_PICK_DUR);
			
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_MULTIPLIER_ANIM_NAME);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			float waitToRollup = revealDelaysByRound[currentStage].WAIT_TO_ROLLUP;
			if (waitToRollup > 0.0f)
			{
				yield return new WaitForSeconds(waitToRollup);
			}
			
			if (bonusSparkleTrail != null)
			{
				// Sparkle trail from pick to win box.
				yield return StartCoroutine(doSparkleTrail(pick.revealNumberLabel.gameObject));
			}
			
			if (currentStage < jackpotWinEffects.Length && jackpotWinEffects[currentStage] != null)
			{
				jackpotWinEffects[currentStage].SetActive(true);
			}
			
			if (currentStage < animationDefinitionsByRound.Length)
			{
				StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Credits));
			}
			
			yield return StartCoroutine(addCredits(BonusGamePresenter.instance.currentPayout));

			if (remainingPicksRevealBehaviorByRound.Length == 0 || 
			    (currentStage < remainingPicksRevealBehaviorByRound.Length &&
			     !remainingPicksRevealBehaviorByRound[currentStage].deactivateRemainingPicksBeforeRevealOnGoodOutcome &&
			     !remainingPicksRevealBehaviorByRound[currentStage].skipRevealOnGoodOutcome))
			{
				yield return StartCoroutine(revealRemainingPicks());
			}
		}
		else if (credits > 0)
		{
			pick.setNumberText(CreditsEconomy.convertCredits(credits));
			
			playSceneSounds(SoundDefinition.PlayType.PreCreditsReveal);
			if (currentStage < animationDefinitionsByRound.Length)
			{
				previousPickIndex = pickIndex;
				yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.PreCreditsReveal));
			}
			
			yield return new TIWaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_REVEAL_PICK_DUR);
			
			Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_CREDITS_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_CREDITS_VO_SOUND_DELAY);
			playSceneSounds(SoundDefinition.PlayType.Credits);

			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			float waitToRollup = revealDelaysByRound[currentStage].WAIT_TO_ROLLUP;
			if (waitToRollup > 0.0f)
			{
				yield return new WaitForSeconds(waitToRollup);
			}
			
			if (currentStage < animationDefinitionsByRound.Length)
			{
				StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Credits));
			}
			
			yield return StartCoroutine(addCredits(credits));

			if (remainingPicksRevealBehaviorByRound.Length == 0 || 
			   (currentStage < remainingPicksRevealBehaviorByRound.Length && remainingPicksRevealBehaviorByRound[currentStage].skipRevealOnBadOutcome == false))
			{
				yield return StartCoroutine(revealRemainingPicks());
			}
		}

		yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_END_GAME_DUR);
		
		if (currentStage < jackpotWinEffects.Length && jackpotWinEffects[currentStage] != null)
		{
			jackpotWinEffects[currentStage].SetActive(false);
		}
	}

	protected void singlePickCreditsOrMultiplierRevealRemainingPick(PickGameButtonData pick)
	{
		pickData = getNextReveal();
		if (pickData != null)
		{
			int multiplier = pickData.multiplier;
			long credits = pickData.credits;
			if (multiplier > 0)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_MULTIPLIER_ANIM_NAME);
				playNotChosenAudio();
			}
			else if (credits > 0)
			{
				pick.setText(CreditsEconomy.convertCredits(credits), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
				playNotChosenAudio();
			}
		}
	}

/*==========================================================================================================*\
	Credits or Fight with Lose or Multiplier
\*==========================================================================================================*/
	private IEnumerator creditsOrFightWithLoseOrMultiplierButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;

		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);

		pickData = getNextEntry();
		string pickInfoString = pickData.pick;

		if (pickInfoString == "FIGHT")
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			if (!pickData.isGameOver)
			{
				Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
				Audio.play(getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_SOUND_DELAY);
				Audio.play(getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_VO_SOUND_DELAY);
			}
			else
			{
				Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_2_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL2_SOUND_DELAY);
				Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
				Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);
			}
			FightRoundDefinition animationInfo = null;
			if (currentStage < fightDefinitionsByRound.Length)
			{
				animationInfo = fightDefinitionsByRound[currentStage];
				if (animationInfo != null)
				{
					yield return new TIWaitForSeconds(animationInfo.SPECIAL1_ANIM_WAIT_TIME);
					yield return StartCoroutine(doSpecialFightSequence(pick.animator.gameObject, pickData.isGameOver));

					if (!pickData.isGameOver)
					{				
						updateMultiplierLabel();
					}

					yield return new TIWaitForSeconds(animationInfo.FIGHT_POST_FIGHT_WAIT_TIME2);
					resetFighter();
				}
			}

			if (pickData.isGameOver)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);
				
				setMessagingToGameOver();
				hasGameEnded = true;
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
				yield return StartCoroutine(revealRemainingPicks());
			}
			else
			{
				//instructionText.text = Localize.textUpper("increases_the_multiplier");
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_MULTIPLIER_ANIM_NAME);
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));

				if (animationInfo == null)
				{
					//animation info can be null if the game does not have a fight definition. ex. lucy01
					//this would behave like a simple sparkle trail going to the multipler and updating the text
					yield return StartCoroutine(doSparkleTrail(pick.animator.gameObject, currentMultiplierLabel.gameObject));
					updateMultiplierLabel();
				}

				if (currentStage < animationDefinitionsByRound.Length)
				{
					StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.OnIncrementMultiplier));
				}
	
				if (getNumEntries() > 0)
				{
					setMessagingToPickItem();
					inputEnabled = true;
				}
				else
				{
					setMessagingToGameOver();
					hasGameEnded = true;
					yield return StartCoroutine(revealRemainingPicks());
				}
			}
		}
		else
		{
			long amount = pickData.credits;
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);
			pick.setNumberText(CreditsEconomy.convertCredits(amount));
			Audio.play(getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));

			long totalCreditsWon = amount * currentMultiplier;

			// if there's a multiplier, animate the score being multiplied
			if (currentMultiplier > 1)
			{
				setMessagingToValueIncreased();
				yield return StartCoroutine(doSparkleTrail(currentMultiplierLabel.gameObject, pick.animator.gameObject, false, 0));
				StartCoroutine(doPostSparkleTrailEffects(pick.animator));
				yield return new TIWaitForSeconds(sparkleTrailDefinitionsByRound[currentStage].SPARKLE_TRAIL_WAIT_1);
			}
			
			pick.setNumberText(CreditsEconomy.convertCredits(totalCreditsWon));
				
			if (currentMultiplier > 1)
			{		
				yield return new TIWaitForSeconds(sparkleTrailDefinitionsByRound[currentStage].SPARKLE_TRAIL_WAIT_2);
			}
			// animate the score changing
			yield return StartCoroutine(addCredits(totalCreditsWon));
			//Debug.LogWarning("You scored " + amount + "x" + multiplier + " points for a total of " + BonusGamePresenter.instance.currentPayout);

			setMessagingToPickItem();
			inputEnabled = true;
		}
	}

	private void updateMultiplierLabel()
	{
		currentMultiplier++;
		if (currentMultiplierLabels.Length > 0)
		{
			foreach (UILabel lbl in currentMultiplierLabels)
			{
				lbl.text = Localize.text("{0}X", currentMultiplier);
			}
		}
		else
		{
			currentMultiplierLabel.text = Localize.text("{0}X", currentMultiplier);
		}
	}


/*==========================================================================================================*\
	Single Pick Advance
\*==========================================================================================================*/
	protected virtual IEnumerator singlePickAdvanceWithCreditsOrCollectAllOrBadEnd(GameObject pickButton)
	{
		inputEnabled = false;
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);

		pickData = getNextEntry();
		wonAllCredits = false;
		WheelPick currentPickem = pickData as WheelPick;
		long credits = pickData.credits;		

		collectAllCreditTotal = findCollectAllAmount(currentPickem);

		if (!currentPickem.canContinue)											// bad picked
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);

			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);
		}			
		else if (collectAllCreditTotal > 0 && credits == collectAllCreditTotal)  // collect all picked
		{
			wonAllCredits = true;
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);

			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX));
			Audio.play(getSoundMappingByRound(PICKEM_PICK_REVEAL_ADVANCE_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
		}
		else if (credits > 0)  													// credits picked
		{
			pick.setNumberText(CreditsEconomy.convertCredits(credits));
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);

			Audio.play(getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX),1.0f, 0.0f,soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
					
			playSceneSounds(SoundDefinition.PlayType.Credits);
			
		}

		yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));

		//Create a list of all the reveals here so we can reveal all the won credits before doing other greyed out reveals
		//Do the roll up after all of these reveals
		for(int i = 0; i < roundButtonList[currentStage].buttonList.Length-1; i++)
		{
			remainingReveals.Add(getNextReveal());
		}

		//Do rollup then reveals, if we didn't get a "Win All".
		if (credits > 0 && !wonAllCredits)
		{
			yield return StartCoroutine(addCredits(credits));
			yield return StartCoroutine(revealRemainingPicks());
		}

		//Rollup happens after we reveal all the credit values, if we've got the "Win All".
		if (credits > 0 && wonAllCredits)
		{
			revealingCredits = true;
			TICoroutine revealCoroutine = StartCoroutine(revealRemainingPicks());
			while(revealingCredits)
			{
				yield return null;
			}
			revealCoroutine.paused = true;
			yield return new TIWaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ROLLUP);
			yield return StartCoroutine(addCredits(credits));
			//Now reveal any remaining picks after the rollup
			revealCoroutine.paused = false;
		}

		if (getNumEntries() > 0)
		{
			playSceneSounds(SoundDefinition.PlayType.Advance);
			StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Advance));	
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);

			continueToNextStage();

			initStage();

			inputEnabled = true;
		}
	}

	protected virtual void singlePickAdvanceWithCreditsOrCollectAllOrBadEndRevealRemainingPick(PickGameButtonData pick)
	{
		int index = 0;
		long credits = 0;
		string revealAnimation = "";
		WheelPick currentPickem = null;
		bool lookingForCreditReveal = true;
		bool stillHasCreditReveals = false;
		while(lookingForCreditReveal)
		{
			pickData = remainingReveals[index];
			credits = pickData.credits;
			currentPickem = pickData as WheelPick;
			if (!currentPickem.canContinue && index < remainingReveals.Count-1) //Skip the bad reveal unless we only have a bad reveal left
			{
				index++;	
			}
			else //credit reveal
			{
				lookingForCreditReveal = false;
			}
		}
		if(wonAllCredits)
		{
			revealAnimation = animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME;
		}
		else
		{
			revealAnimation = animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME;
		}
		
		if (!currentPickem.canContinue)			
		{
			revealAnimation = animationNamesByRound[currentStage].REVEAL_UNPICKED_END_ANIM_NAME;
		}			
		else if (collectAllCreditTotal > 0 && credits == collectAllCreditTotal)
		{
			revealAnimation = animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME;
		}
		else if (credits > 0)
		{
			pick.setText(CreditsEconomy.convertCredits(credits),PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		}
		pick.animator.Play(revealAnimation);
		playNotChosenAudio();
		remainingReveals.RemoveAt(index);

		//After our reveal, figure out if we need to pause to play the rollup or finish revealing
		//Check the rest of the reveals. If theres no more regular credit reveals then we need to pause before the next reveal.
		foreach(CorePickData data in remainingReveals)
		{
			currentPickem = data as WheelPick;
			if(currentPickem.canContinue && credits > 0 && credits != collectAllCreditTotal)
			{
				stillHasCreditReveals = true;
				break;
			}
		}
		if(!stillHasCreditReveals)
		{
			revealingCredits = false;
		}
	}

	private long findCollectAllAmount(WheelPick currentPickem)
	{
		for (int i = 0; i < currentPickem.wins.Count; i++)
		{
			if (isCollectAll(currentPickem, i))
			{
				return currentPickem.wins[i].credits;
			}
		}

		return 0;   // none found
	}

	// Check if the other entries equal winAmount
	private bool isCollectAll(WheelPick currentPickem, int index)
	{
		long unWonAmount = 0;
		long winAmount = currentPickem.wins[index].credits;

		for (int i = 0; i < currentPickem.wins.Count; i++)		
		{
			if (i != index)
			{			
				unWonAmount += currentPickem.wins[i].credits;
			}
		} 

		return (unWonAmount == winAmount);
	}

/*==========================================================================================================*\
	Single Pick Advance
\*==========================================================================================================*/
	protected virtual IEnumerator singlePickAdvanceButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);

		yield return StartCoroutine(
			CommonAnimation.playAnimAndWait(
				pick.animator, animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME));

		yield return StartCoroutine(revealRemainingPicks());
	}

	protected virtual void singlePickAdvanceRevealRemainingPick(PickGameButtonData pick)
	{
		pickData = getNextReveal();
		
		if (pickData != null)
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
			playNotChosenAudio();
		}
	}

/*==========================================================================================================*\
	Credits Or Advance Or Jackpot
\*==========================================================================================================*/
	protected virtual IEnumerator creditsOrAdvanceOrJackpotEndButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		pickData = getNextEntry();
		PickemPick currentPickem = pickData as PickemPick;
		long credits = pickData.credits;

		if (currentPickem.groupId == "JACKPOT")
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			if (currentStage < jackpotWinEffects.Length)
			{
				if (jackpotWinEffects[currentStage] != null)
				{
					jackpotWinEffects[currentStage].SetActive(true);
				}
			}
			
			if (currentStage < sparkleTrailDefinitionsByRound.Length)
			{
				yield return StartCoroutine(doSparkleTrail());
				StartCoroutine(doPostSparkleTrailEffects());
			}
			yield return StartCoroutine(addCredits(jackpotCredits));
			yield return StartCoroutine(revealRemainingPicks());
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
			
			if (currentStage < jackpotWinEffects.Length)
			{
				if (jackpotWinEffects[currentStage] != null)
				{
					jackpotWinEffects[currentStage].SetActive(false);
				}
			}
			
			hasGameEnded = true;
			yield return StartCoroutine(revealRemainingPicks());
		}
		else if (currentPickem.groupId == "SAVE")
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL2_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_2_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL2_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_2_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL2_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
			if (credits > 0)
			{
				yield return StartCoroutine(addCredits(credits));
			}

			continueToNextStage();
			initStage(currentPickem.bonusGame);
		}
		else if (credits > 0)
		{
			pick.setNumberText(CreditsEconomy.convertCredits(credits));
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
				
			playSceneSounds(SoundDefinition.PlayType.Credits);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(addCredits(credits));

			if (getNumEntries() > 0)
			{
				inputEnabled = true;
			}
			else
			{
				hasGameEnded = true;
			}
		}
	}

	protected void creditsOrAdvanceOrJackpotEndRevealRemainingPick(PickGameButtonData pick)
	{
		pickData = getNextReveal();
		
		if (pickData != null)
		{
			long credits = pickData.credits;

			if (pickData.pick == "JACKPOT")
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
				playNotChosenAudio();
			}
			else if (pickData.pick == "SAVE")
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL2_ANIM_NAME);
				playNotChosenAudio();
			}
			else if (credits > 0)
			{
				pick.setText(
					CreditsEconomy.convertCredits(credits),
					PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);

				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
				playNotChosenAudio();
			}
			else
			{
				Debug.Log("CreditsOrAdvanceOrJackpotEnd shouldn't have gotten here ?!");
			}
		}
	}

/*==========================================================================================================*\
	RetreatOrEnd
\*==========================================================================================================*/
	protected virtual IEnumerator retreatOrEndButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		if (pickData.credits != 0)
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(revealRemainingPicks());
		}
		else
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));

			hasGameEnded = true;
			yield return StartCoroutine(revealRemainingPicks());
		}
	}
	
/*==========================================================================================================*
		Credits With  Advance and Multiple Bad End Stage Section
\*==========================================================================================================*/
	private IEnumerator playSceneAnimation(AnimationDefinition animationDef)
	{
		if (animationDef.animator != null)
		{
			yield return new TIWaitForSeconds(animationDef.ANIM_DELAY);
			if (animationDef.useSetActive || animationDef.shouldActivateAndStayActive)
			{
				animationDef.animator.gameObject.SetActive(true);
			}
			
			string animName = animationDef.ANIM_NAME;
			
			switch (animationDef.playSource)
			{
				case AnimationDefinition.PlaySource.PreviousPick:
					if (0 <= previousPickIndex &&
					    previousPickIndex < animationDef.ANIM_NAME_ARRAY.Length)
					{
						animName = animationDef.ANIM_NAME_ARRAY[previousPickIndex];
					}
					break;
			}
			
			if (animationDef.ANIM_LENGTH_OVERRRIDE < 0)
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(animationDef.animator, animName));
			}
			else
			{
				animationDef.animator.Play(animName);
				yield return new TIWaitForSeconds(animationDef.ANIM_LENGTH_OVERRRIDE);
			}
			
			if (animationDef.useSetActive)
			{
				animationDef.animator.gameObject.SetActive(false);
			}
		}
	}

	public IEnumerator playSceneAnimations(AnimationDefinition.PlayType playType, long optionalParameter = 0)
	{
		if (currentStage < animationDefinitionsByRound.Length)
		{
			yield return StartCoroutine(
				playSceneAnimations(
					animationDefinitionsByRound[currentStage].animations,
					playType,
					optionalParameter));
		}
	}

	private IEnumerator playSceneAnimations(AnimationDefinition[] animationList, AnimationDefinition.PlayType playType, long optionalParameter = 0)
	{	
		if (animationList != null)
		{
			List<TICoroutine> runningCoroutines = new List<TICoroutine>();
			foreach (AnimationDefinition animation in animationList)
			{
				if (animation.playType == playType &&
				   (animation.optionalParameter == optionalParameter || animation.optionalParameter == -1) &&
				   checkAnimDefRules(animation))
				{
					if (animation.soundName != "")
					{
						Audio.playSoundMapOrSoundKeyWithDelay(animation.soundName, animation.soundDelay);
					}
					TICoroutine coroutine = StartCoroutine(playSceneAnimation(animation));
					if (animation.shouldBlockUntilAnimationFinished)
					{
						runningCoroutines.Add(coroutine);
					}
				}
			}
			
			// Wait for all the coroutines to end.
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
		}
	}

	private bool checkAnimDefRules(AnimationDefinition animDef)
	{
		switch (animDef.playRule)
		{
			case AnimationDefinition.PlayRule.PreviousPickEquals:
				return previousPickIndex == animDef.ruleArgument;
		}
		
		return true;
	}
	
	protected IEnumerator creditsWithAdvanceAndBadEndButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton); 
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		pickData = getNextEntry();
		long credits = pickData.credits;

		bool canAdvance = false;
		int additionalPicks = 0;
		
		BasePick bPick = pickData as BasePick; 
		if (bPick != null)
		{
			canAdvance = bPick.canAdvance;
			additionalPicks = bPick.additionalPicks;
		}

		if (credits > 0)
		{

			pick.setNumberText(CreditsEconomy.convertCredits(credits));

			if (canAdvance && additionalPicks > 0)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL3_ANIM_NAME);
			}
			else if (additionalPicks > 0)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL2_ANIM_NAME);
			}
			else if (canAdvance)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			}
			else
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);
			}

			Audio.play(getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
			playSceneSounds(SoundDefinition.PlayType.Credits);

			if (postRevealAnimationWaitOverrideByRound != null && currentStage < postRevealAnimationWaitOverrideByRound.Length && postRevealAnimationWaitOverrideByRound[currentStage] > -1)
			{
				yield return new TIWaitForSeconds(postRevealAnimationWaitOverrideByRound[currentStage]);
			}
			else
			{
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			}

			if (currentStage < sparkleTrailDefinitionsByRound.Length)
			{
				if (additionalPicks > 0)
				{
					yield return new TIWaitForSeconds(sparkleTrailDefinitionsByRound[currentStage].SPARKLE_TRAIL_WAIT_1);
					yield return StartCoroutine(doSparkleTrail(pickButton));
					currentNumPicks += additionalPicks;
					currentNumPicksLabelWrapper.text = CommonText.formatNumber(currentNumPicks);
					StartCoroutine(doPostSparkleTrailEffects());
				}
				else if (sparkleTrailDefinitionsByRound[currentStage].startPos != null)
				{
					yield return StartCoroutine(doSparkleTrail());
					StartCoroutine(doPostSparkleTrailEffects());
				}
			}
			
			if (currentStage < animationDefinitionsByRound.Length)
			{
				if(!canAdvance)
				{
					StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Credits));	
				}
				else
				{
					Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
					Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
					yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Good));
					
				}			
			}
			
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_REVEAL_DUR);
			yield return StartCoroutine(addCredits(credits));
			
			inputEnabled = true;
		}
		else if (credits == 0 && canAdvance) // advance without credits
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
				
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
				
			playSceneSounds(SoundDefinition.PlayType.Special1);
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
		}
		else if (credits == 0 && !canAdvance) // user picked a bad 
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);
			Audio.play(getSoundMappingByRoundAndIndex(PICKEM_PICK_BAD_MAPPING_PREFIX, -1, numberOfBadsPicked), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_ROUND_AND_INDEX_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
			playSceneSounds(SoundDefinition.PlayType.Bad);

			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			if (currentStage < animationDefinitionsByRound.Length)
			{	
				yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.Bad, numberOfBadsPicked));
			}
			
			if (hasGameEnded = !hasMorePicksThisGame())    // if we have more picks this game then this bad is not the end of the game nor is it time to reveal more
			{				
				yield return StartCoroutine(revealRemainingPicks());
				yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
			}
			else
			{
				inputEnabled = true;
			}
			
			numberOfBadsPicked++;
		}
		else if (!canAdvance)
		{
			Debug.LogError("Don't know how to handle this data");
		}

		if (canAdvance)
		{
			inputEnabled = false;
			if (credits == 0) //if credits > 0, reveal has already been handled
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			}

			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			if (currentStage < animationDefinitionsByRound.Length)
			{
				yield return StartCoroutine(playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.PostGoodReveal));
			}
			
			yield return StartCoroutine(revealRemainingPicks());
			
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
			yield return StartCoroutine(playSceneAnimations(AnimationDefinition.PlayType.Transition));
			
			continueToNextStage();
			initStage(pickData.bonusGame);

			inputEnabled = true;
		}	
	}
	
	protected IEnumerator findToAdvanceWithCreditsAndBadEndButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		pickData = getNextEntry();

		string groupId = "";
		long credits = pickData.credits;

		if (outcomeType == BonusOutcomeTypeEnum.PickemOutcomeType)
		{
			PickemPick pickemPick = pickData as PickemPick;
			groupId = pickemPick.groupId;
		}

		PickingRound pickingRound = getPickingRound();
		FindDefinition findDefinition = pickingRound.findDefinition;

		string findGroupId = findDefinition.findGroupId;
		string endGroupId = findDefinition.endGroupId;

		playMatchedSceneSounds(pick.button.name);   // play sounds specific to the name of this pick button object

		if (findGroupId != "" && groupId == findGroupId)
		{
			pick.setText(CreditsEconomy.convertCredits(credits));
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 
				1.0f, 0.0f, 
				soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);

			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);

			playSceneSounds(SoundDefinition.PlayType.Special1);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));

			if (currentStage < jackpotWinEffects.Length)
			{
				if (jackpotWinEffects[currentStage] != null)
				{
					jackpotWinEffects[currentStage].SetActive(true);
				}
			}
			
			if (currentStage < sparkleTrailDefinitionsByRound.Length)
			{
				yield return StartCoroutine(doSparkleTrail());
				StartCoroutine(doPostSparkleTrailEffects());
			}
			
			GameObject[] countGos = findDefinition.countGos;
			Animator countGOAnimator = null;
			
			GameObject oldCountGO = null;
			
			if (numFound < countGos.Length)
			{
				oldCountGO = countGos[numFound];
			}
			
			if (oldCountGO != null)
			{
				countGOAnimator = oldCountGO.GetComponent<Animator>();
			}
			
			if (countGOAnimator != null)
			{
				oldCountGO.SetActive(true);
				countGOAnimator.Play(findDefinition.individualAnimationName);
			}
			else if (oldCountGO != null)
			{
				oldCountGO.SetActive(false);
			}
			
			numFound++;
			GameObject newCountGO = null;
			
			if (numFound < countGos.Length)
			{
				newCountGO = countGos[numFound];
			}
			
			if (newCountGO != null)
			{
				newCountGO.SetActive(true);
			}
			
			Animator countAnimator = findDefinition.countAnimator;
			
			if (countAnimator != null)
			{
				string foundAnimName = string.Format("count{0}", numFound);
				int foundAnimHash = Animator.StringToHash(foundAnimName);
				
				if (countAnimator.HasState(0, foundAnimHash))
				{
					countAnimator.Play(foundAnimName);
				}
			}

			yield return StartCoroutine(addCredits(credits));

			if (numFound >= findDefinition.numToFind)
			{
				if (findDefinition.shouldGoodRevealBlock)
				{
					yield return StartCoroutine(revealRemainingPicks());
				}
				else
				{
					StartCoroutine(revealRemainingPicks());
				}

				yield return StartCoroutine (
					playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.PostGoodReveal)
				);
				yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
			}
			else
			{
				inputEnabled = true;
			}
		}
		else if (endGroupId != "" && groupId == endGroupId)
		{
			if (credits > 0)
			{
				pick.setText(CreditsEconomy.convertCredits(credits));
			}

			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));

			if (credits > 0)
			{
				yield return StartCoroutine(addCredits(credits));
			}
			
			if (findDefinition.shouldBadRevealBlock)
			{
				yield return StartCoroutine(revealRemainingPicks());
			}
			else
			{
				StartCoroutine(revealRemainingPicks());
			}
			
			yield return StartCoroutine (
				playSceneAnimations(animationDefinitionsByRound[currentStage].animations, AnimationDefinition.PlayType.PostBadReveal)
			);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);
			
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_END_GAME_DUR);
			
			hasGameEnded = true;
			
			BonusGamePresenter.instance.gameEnded();
		}
		else if (credits > 0)
		{
			pick.setText(CreditsEconomy.convertCredits(credits));
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);

			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_CREDITS_VO_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_CREDITS_VO_SOUND_DELAY);
				
			playSceneSounds(SoundDefinition.PlayType.Credits);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(addCredits(credits));
			
			inputEnabled = true;
		}
	}

	protected IEnumerator pickNumPicksButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		showPickemGlows(false);
		
		currentPickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(currentPickIndex);
		
		pickData = getNextEntry();
		currentNumPicks = getNumPicksFromPickData(pickData);

		PickingRound pickingRound = getPickingRound();
		
		if (pickingRound != null)
		{
			PickNumPicksDefinition pickNumPicksDef = pickingRound.PickNumPicksDefinition;
			
			if (pickNumPicksDef.possibleNumPicks.Count > 0)
			{
				pickNumPicksDef.possibleNumPicks.Remove(currentNumPicks);
			}
		}
		
		pick.setText(CommonText.formatNumber(currentNumPicks), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
		
		Audio.play(
			getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 
			1.0f, 0.0f, 
			soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
		
		Audio.play(
			getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX),
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
		
		yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
		yield return StartCoroutine(revealRemainingPicks());
		
		yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
		
		continueToNextStage();
		initStage(pickData.bonusGame);
	}
	
	protected int getNumPicksFromPickData(CorePickData cpd)
	{
		// This seems to be the only way to get the number of picks from the pick data.
		// I don't know if the next Pick Num Picks Game will work the same way, though.
		
		int numPicks = 0;
		
		if (cpd.bonusGame.FastEndsWith("five") || cpd.bonusGame.FastEndsWith("_5"))
		{
			numPicks = 5;
		}
		else if (cpd.bonusGame.FastEndsWith("six") || cpd.bonusGame.FastEndsWith("_6"))
		{
			numPicks = 6;
		}
		else if (cpd.bonusGame.FastEndsWith("seven") || cpd.bonusGame.FastEndsWith("_7"))
		{
			numPicks = 7;
		}
		else if (cpd.bonusGame.FastEndsWith("eight") || cpd.bonusGame.FastEndsWith("_8"))
		{
			numPicks = 8;
		}		
		else if (cpd.bonusGame.FastEndsWith("nine") || cpd.bonusGame.FastEndsWith("_9"))
		{
			numPicks = 9;
		}
		else if (cpd.bonusGame.FastEndsWith("ten") || cpd.bonusGame.FastEndsWith("_10"))
		{
			numPicks = 10;
		}
		
		return numPicks;
	}
	
	protected IEnumerator creditsAndMultipliersButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		showPickemGlows(false);
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		pickData = getNextEntry();
		
		UILabel multiplierLabel = null;
		if (currentStage < currentMultiplierLabels.Length)
		{
			multiplierLabel = currentMultiplierLabels[currentStage];
		}
		
		if (pickData.multiplier != 0)
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_MULTIPLIER_ANIM_NAME);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_SOUND_DELAY);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_VO_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			if (multiplierLabel != null)
			{
				SparkleTrailDefinition sparkleDef = null;
				if (currentStage < sparkleTrailDefinitionsByRound.Length)
				{
					sparkleDef = sparkleTrailDefinitionsByRound[currentStage];
				}
				if (sparkleDef == null || !sparkleDef.shouldUseMultiplierTextLocationForStart)
				{
					yield return StartCoroutine(
						doSparkleTrail(
							pickButton.gameObject,
							multiplierLabel.gameObject));
				}
				else
				{
					yield return StartCoroutine(
						doSparkleTrail(
							pick.multiplierLabel.gameObject,
							multiplierLabel.gameObject));
				}
				if (sparkleDef != null && sparkleDef.POST_TRAIL_PRE_ADD_WAIT > 0.0f)
				{
					yield return new WaitForSeconds(sparkleDef.POST_TRAIL_PRE_ADD_WAIT);
				}
			}			
			
			currentMultiplier += pickData.multiplier;
			if (multiplierLabel != null)
			{
				multiplierLabel.text = Localize.text("{0}X", currentMultiplier);
			}
			if (animationDefinitionsByRound.Length > currentStage && animationDefinitionsByRound[currentStage].animations.Length > 0)
			{
				foreach (AnimationDefinition animDef in animationDefinitionsByRound[currentStage].animations)
				{
					if (animDef.playType == (AnimationDefinition.PlayType.OnIncrementMultiplier))
					{
						animDef.animator.Play(animDef.ANIM_NAME + currentMultiplier);
					}
				}
			}
			
			if (pickData.credits != 0)
			{
				yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_CHANGE_MULTIPLIER_TO_CREDITS);
				
				pick.revealNumberLabel.text = CreditsEconomy.convertCredits(pickData.credits);
				pick.animator.Play(animationNamesByRound[currentStage].CHANGE_MULTIPLIER_TO_CREDITS_ANIM_NAME);
				
				Audio.play(
					getSoundMappingByRound(CHANGE_MULTIPLIER_TO_CREDITS_MAPPING_PREFIX),
					1.0f, 0.0f, 
					soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
				
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			}
		}
		else if (pickData.credits != 0)
		{
			pick.revealNumberLabel.text = CreditsEconomy.convertCredits(pickData.credits);
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX), 
				1.0f, 0.0f, 
				soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_CREDITS_VO_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_CREDITS_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
		}
		
		long totalCredits = currentMultiplier * pickData.credits;
				
		if (totalCredits > 0)
		{
			if (currentMultiplier > 1 && multiplierLabel != null)
			{	
				SparkleTrailDefinition sparkleTrailDef = null;
				if (currentStage < sparkleTrailDefinitionsByRound.Length)
				{
					sparkleTrailDef = sparkleTrailDefinitionsByRound[currentStage];
				}
				
				if (sparkleTrailDef != null && sparkleTrailDef.SPARKLE_TRAIL_WAIT_1 > 0.0f)
				{
					yield return new WaitForSeconds(sparkleTrailDef.SPARKLE_TRAIL_WAIT_1);
				}
				
				if (sparkleTrailDef == null || !sparkleTrailDef.shouldUseValueTextLocationForEnd)
				{
					yield return StartCoroutine(
						doSparkleTrail(
							multiplierLabel.gameObject,
							pickButton.gameObject,
							false,
							0));
				}
				else
				{
					yield return StartCoroutine(
						doSparkleTrail(
							multiplierLabel.gameObject,
							pick.revealNumberLabel.gameObject,
							false,
							0));
				}
				
				if (sparkleTrailDef != null && sparkleTrailDef.POST_TRAIL_PRE_ADD_WAIT > 0.0f)
				{
					yield return new WaitForSeconds(sparkleTrailDef.POST_TRAIL_PRE_ADD_WAIT);
				}
				
				pick.revealNumberLabel.text = CreditsEconomy.convertCredits(totalCredits);
				
				if (sparkleTrailDef != null && sparkleTrailDef.SPARKLE_TRAIL_WAIT_2 > 0.0f)
				{
					yield return new WaitForSeconds(sparkleTrailDef.SPARKLE_TRAIL_WAIT_2);
				}
			}
		
			yield return StartCoroutine(addCredits(totalCredits));
		}
		
		// If you picked everything,
		// pickemButtonPressedCoroutine will continue to the next round or end the game.
		inputEnabled = true;
		
		if (currentNumPicks > 0)
		{
			showPickemGlows(true);
		}
	}

	// Show glows when it's your turn to pick.
	// After you pick, hide the glows until it's your turn to pick again.
	public void showPickemGlows(bool shouldGlow)
	{
		List<GameObject> pickMeList = pickmeButtonList[currentStage];
		int numPickMes = pickMeList.Count;
		
		for (int i=0; i<numPickMes; i++)
		{
			GameObject pickMeGo = pickMeList[i];
			int index = getButtonIndex(pickMeGo);
			PickGameButtonData pgbd = getPickGameButton(index);
			
			if (pgbd.glowList != null)
			{
				foreach (MeshRenderer glow in pgbd.glowList)
				{
					glow.gameObject.SetActive(shouldGlow);
				}
			}
			
			showPickemGlowShadow(pgbd, !shouldGlow);
		}
	}

	public void showPickemGlowShadow(PickGameButtonData pgbd, bool shouldShowShadow)
	{
		if (pgbd.glowShadowList != null)
		{
			foreach (MeshRenderer shadow in pgbd.glowShadowList)
			{
				shadow.gameObject.SetActive(shouldShowShadow);
			}
		}		
	}
	
	// go ahead and just fade out all the remaining picks simultaneously, instead of revealing them.
	private void fadeOutRemainingPicks()
	{
		PickGameButtonData reveal = removeNextPickGameButton();
		
		while (reveal != null)
		{
			reveal.animator.Play(animationNamesByRound[currentStage].FADE_OUT_ANIM_NAME);
			reveal = removeNextPickGameButton();
		}
	}

	private void fadeOutRemainingPicksWithMultipleAnimationNames()
	{
		if(picksToFade.Count > 0)
		{
			foreach(FadeAnimationDefinition fadeObject in picksToFade)
			{
				fadeObject.objectAnimator.Play(fadeObject.fadeAnimationName);
			}
			picksToFade.Clear();
		}
	}

	private void addNewFadeObject(PickGameButtonData pick, string animationName)
	{
		FadeAnimationDefinition newFade = new FadeAnimationDefinition();
		newFade.objectAnimator = pick.animator;
		newFade.fadeAnimationName = animationName;
		picksToFade.Add(newFade);
	}
	
	protected void creditsWithAdvanceAndBadEndRevealRemainingPick(PickGameButtonData pick)
	{
		pickData = getNextReveal();
		
		if (pickData != null)
		{
			bool canAdvance = false;
			int additionalPicks = 0;
			
			BasePick bPick = pickData as BasePick;   
			if (bPick != null)
			{
				canAdvance = bPick.canAdvance;
				additionalPicks = bPick.additionalPicks;
			}

			long credits = pickData.credits;
			
			if (credits > 0)
			{
				pick.setText(CreditsEconomy.convertCredits(credits), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
				
				if (canAdvance && additionalPicks > 0)
				{
					pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL3_ANIM_NAME);
				}
				else if (additionalPicks > 0)
				{
					pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL2_ANIM_NAME);
				}
				else if (canAdvance)
				{
					pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
				}
				else
				{
					pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
				}

				playNotChosenAudio(false);
			}
			else if (credits == 0 && !canAdvance)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_END_ANIM_NAME);
				playNotChosenAudio(false);
			}
			else if (canAdvance)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
				playNotChosenAudio(false);
			}
		}
	}

	protected void multiplierAdvanceOrIncreaseCreditsOnEndRevealRemainingPick(PickGameButtonData pick)
	{
		Audio.play(getSoundMappingByRound(DEFAULT_NOT_CHOSEN_SOUND_KEY));
		MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition multAdvanceDef = multiplierAdvanceOrIncreaseWithCreditsOnEndDefinitions[currentStage];

		if (multAdvanceDef.currentPick.credits != 0)
		{
			pick.setGrayNumberText(CreditsEconomy.convertCredits(multAdvanceDef.currentPick.credits));
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
		}
		else if (multAdvanceDef.currentPick.poolKeyName != "")
		{
			if (multAdvanceDef.currentPick.verticalShift != 0)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_MULTIPLIER_ADVANCE_ANIM_NAME);
			}
			else
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_MULTIPLIER_INCREASE_ANIM_NAME);
			}
		}

		multAdvanceDef.currentPick = multAdvanceDef.currentRoundPick.getNextReveal();
	}

	protected void retreatOrEndRevealRemainingPick(PickGameButtonData pick)
	{
		PickemPick nextReveal = (pickData as PickemPick).getNextReveal();

		if (nextReveal.credits != 0)
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
			playNotChosenAudio();
		}
		else
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_END_ANIM_NAME);
			playNotChosenAudio();
		}
	}

	protected void findToAdvanceWithCreditsAndBadEndRevealRemainingPick(PickGameButtonData pick)
	{
		pickData = getNextReveal();
		
		string groupId = "";
		long credits = pickData.credits;
		
		if (outcomeType == BonusOutcomeTypeEnum.PickemOutcomeType)
		{
			PickemPick pickemPick = pickData as PickemPick;
			groupId = pickemPick.groupId;
		}

		PickingRound pickingRound = getPickingRound();
		FindDefinition findDefinition = pickingRound.findDefinition;
		
		if (groupId == findDefinition.findGroupId)
		{
			pick.setText(CreditsEconomy.convertCredits(credits));

			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
			playNotChosenAudio();
		}
		else if (groupId == findDefinition.endGroupId)
		{
			if (credits > 0)
			{
				pick.setText(CreditsEconomy.convertCredits(credits));
			}

			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_END_ANIM_NAME);
			playNotChosenAudio();
		}
		else if (credits > 0)
		{
			pick.setText(CreditsEconomy.convertCredits(credits));
			
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
			playNotChosenAudio();
		}
	}

	protected void pickNumPicksRevealRemainingPick(PickGameButtonData pick)
	{
		CorePickData revealData = getNextReveal();
		
		if (revealData != null)
		{
			int numPicks = getNumPicksFromPickData(revealData);
			pick.setText(CommonText.formatNumber(numPicks), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
	
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
			playNotChosenAudio(true, currentStage);
		}
	}
	
	protected void creditsAndMultipliersRevealRemainingPick(PickGameButtonData pick)
	{
		CorePickData revealData = getNextReveal();
		
		if (revealData != null)
		{
			if (revealData.multiplier > 0)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_MULTIPLIER_ANIM_NAME);
				playNotChosenAudio();
			}
			else
			if (revealData.credits > 0)
			{
				pick.setText(CreditsEconomy.convertCredits(revealData.credits), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
				
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
				playNotChosenAudio();
			}	
		}
	}
		
	private void setMessagingToPickItem()
	{
		foreach (LabelWrapper lbl in messagingLabelsWrapper)
		{
			lbl.text = Localize.text(PICK_ITEM_LOCALIZATION_KEY);
			if (SHOULD_CAPITALIZE_LOCALIZATIONS)
			{
				lbl.text = Localize.toUpper(lbl.text);
			}
		}
	}
	
	private void setMessagingToValueIncreased()
	{
		foreach (LabelWrapper lbl in messagingLabelsWrapper)
		{
			lbl.text = Localize.text(VALUE_INCREASED_LOCALIZATION_KEY);
			if (SHOULD_CAPITALIZE_LOCALIZATIONS)
			{
				lbl.text = Localize.toUpper(lbl.text);
			}
		}
	}
	
	private void setMessagingToGameOver()
	{
		foreach (LabelWrapper lbl in messagingLabelsWrapper)
		{
			if (lbl != null)
			{
				lbl.text = Localize.text(GAME_OVER_LOCALIZATION_KEY);
				if (SHOULD_CAPITALIZE_LOCALIZATIONS)
				{
					lbl.text = Localize.toUpper(lbl.text);
				}
			}
		}
	}

	protected virtual IEnumerator doSpecialFightSequence(GameObject buttonObject, bool isGameOver)
	{	
		if (currentStage < fightDefinitionsByRound.Length)
		{
			FightRoundDefinition animationInfo = fightDefinitionsByRound[currentStage];
			
			if (animationInfo != null && animationInfo.fighterAnimator != null && animationInfo.fighterAnimatorParent != null)
			{
				animationInfo.fighterAnimatorParent.transform.position = buttonObject.transform.position + animationInfo.FIGHTER_START_POS_OFFSET;
				if (!isGameOver && !string.IsNullOrEmpty(animationInfo.FIGHTER_GOOD_ANIM_NAME))
				{
					animationInfo.fighterAnimator.Play(animationInfo.FIGHTER_GOOD_ANIM_NAME);
				}
				else if (isGameOver && !string.IsNullOrEmpty(animationInfo.FIGHTER_BAD_ANIM_NAME))
				{
					animationInfo.fighterAnimator.Play(animationInfo.FIGHTER_BAD_ANIM_NAME);
				}
				
				doSpecialOnFighterBeforeTween(animationInfo.fighterAnimatorParent, animationInfo.fightLocations [0].fightLocationAnimator.gameObject);
				yield return null;
				iTween.MoveTo(animationInfo.fighterAnimatorParent.gameObject, iTween.Hash("position", animationInfo.fightLocations [0].fightLocationAnimator.transform.position + animationInfo.FIGHTER_END_POS_OFFSET, "islocal", false, "time", animationInfo.FIGHT_TWEEN_TIME, "easetype", iTween.EaseType.easeOutCubic));
				doPreFightAnimations();
				yield return new TIWaitForSeconds(animationInfo.FIGHT_POST_TWEEN_WAIT_TIME);
				foreach (FightLocation fightLocDef in animationInfo.fightLocations)
				{
					StartCoroutine(doFightAnimations(fightLocDef, isGameOver));
				}
				
				yield return new TIWaitForSeconds(animationInfo.FIGHT_POST_FIGHT_WAIT_TIME1);
			}
		}
	}

	protected virtual void resetFighter()
	{		
		if (currentStage < fightDefinitionsByRound.Length)
		{
			FightRoundDefinition animationInfo = fightDefinitionsByRound[currentStage];
			if (animationInfo != null && animationInfo.fighterAnimatorParent != null)
			{
				animationInfo.fighterAnimatorParent.transform.localScale = Vector3.one;
				animationInfo.fighterAnimatorParent.transform.localRotation = Quaternion.identity;
			}
		}
	}

	protected virtual void doSpecialOnFighterBeforeTween(GameObject fighterParent, GameObject location)
	{
	}

	protected void creditsOrFightWithLoseOrMultiplierRevealRemainingPick(PickGameButtonData pick)
	{
		pickData = getNextReveal();
		// show values for non-picked buttons
		if (pickData.pick == "FIGHT")
		{
			if (pickData.isGameOver)
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_END_ANIM_NAME);
				playNotChosenAudio();
			}
			else
			{
				pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_MULTIPLIER_ANIM_NAME);
				playNotChosenAudio();
			}
		}
		else
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
			long amount = pickData.credits;
			
			pick.setGrayNumberText(CreditsEconomy.convertCredits(amount));
			
			playNotChosenAudio();
		}
	}
	
	protected string getSoundMappingByRoundAndIndex(string prefix, int roundOverride = -1, int mappingIndexNumber = -1)
	{
		int mappingRoundNumber = currentStage;
		if (roundOverride != -1)
		{
			mappingRoundNumber = roundOverride;
		}
		string roundPrefix = addNumberAsSoundKeyPostfix(prefix, mappingRoundNumber);
				
		if (mappingIndexNumber != -1)
		{
			prefix = addNumberAsSecondarySoundKeyPostfix(roundPrefix, mappingIndexNumber + 1);
		}
		
		string soundStr = Audio.soundMap(prefix);
		if (soundStr != null)
		{
			return soundStr;
		}
		else
		{
			return Audio.soundMap(roundPrefix);	
		}
	}	
		
	protected string getSoundMappingByRound(string prefix, int roundOverride = -1)
	{		
		int mappingRoundNumber = currentStage;
		if (roundOverride != -1)
		{
			mappingRoundNumber = roundOverride;
		}
		
		return Audio.soundMap(addNumberAsSoundKeyPostfix(prefix, mappingRoundNumber));		
	}
	
	protected string addNumberAsSecondarySoundKeyPostfix(string srcString, int number)
	{
		return (srcString + "_" + number);
	}
	
	protected string addNumberAsSoundKeyPostfix(string prefix, int number)
	{
		if (number != 0)
		{
			prefix = prefix + (number + 1);
		}
		
		return prefix;
	}

	protected string getSoundMappingByRoundDefaultTo0(string prefix, int roundOverride = -1)
	{
		int mappingRoundNumber = currentStage;
		if (roundOverride != -1)
		{
			mappingRoundNumber = roundOverride;
		}
		string soundName = Audio.soundMap(addNumberAsSoundKeyPostfix(prefix, mappingRoundNumber));
		if (string.IsNullOrEmpty(soundName))
		{
			// Default to round 0
			soundName = Audio.soundMap(addNumberAsSoundKeyPostfix(prefix, 0));
		}
		return soundName;	
	}

	public override string getRollupTermSound(long payout)
	{
		if (rollupTermOverride != "")
		{
			return rollupTermOverride;
		}
		
		return getSoundMappingByRoundDefaultTo0(ROLLUP_TERM_SOUND_PREFIX);
	}

	public override string getRollupSound(long payout)
	{
		if (rollupSoundOverride != "")
		{
			return rollupSoundOverride;
		}

		return getSoundMappingByRoundDefaultTo0(ROLLUP_SOUND_PREFIX);
	}
	
	// SoundPostfix - play different sounds on sparkle trails and explosions (eg pickem_multiplier_travel_1 instead of pickem_multiplier_travel).
	// When we have more time, I'd rather have multiple sparkle trails that you can set sounds directly instead of going through SCAT.
	
	public IEnumerator doSparkleTrail(GameObject startPos = null, GameObject endPos = null, bool ignoreZValue = false, int soundPostfix = -1, RevealDefinition overrideRevealEffect = null)
	{
		if (currentStage < sparkleTrailDefinitionsByRound.Length &&
		    sparkleTrailDefinitionsByRound[currentStage] != null)
		{
			if (startPos == null)
			{
				startPos = sparkleTrailDefinitionsByRound[currentStage].startPos;
			}
			if (endPos == null)
			{
				endPos = sparkleTrailDefinitionsByRound[currentStage].endPos;
			}
			if (sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound != null)
			{
				if (sparkleTrailDefinitionsByRound[currentStage].shouldActivate)
				{
					sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.SetActive(true);
				}
				
				// There are cases (layering issues) where you don't want the sparkle trail Z to be modified
				Vector3 finalStartPosition = new Vector3();
				finalStartPosition.x = startPos.transform.position.x;
				finalStartPosition.y = startPos.transform.position.y;
				
				if (ignoreZValue || sparkleTrailDefinitionsByRound[currentStage].ignoreZValue)
				{
					finalStartPosition.z = sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.transform.position.z;
				}
				else
				{
					finalStartPosition.z = startPos.transform.position.z;
				}
				
				// Assign the position
				sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.transform.position = finalStartPosition;
				
				if (!string.IsNullOrEmpty(sparkleTrailDefinitionsByRound[currentStage].INSTANCED_SPARKLE_TRAIL_ANIMATION_NAME_BY_ROUND))
				{
					sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.GetComponent<Animator>().Play(sparkleTrailDefinitionsByRound[currentStage].INSTANCED_SPARKLE_TRAIL_ANIMATION_NAME_BY_ROUND);
				}
		
				if (soundDelaysByRound.Length > 0)
				{
					Audio.play(
						getSoundMappingByRoundAndIndex(MULTIPLIER_SPARKLE_TRAIL_TRAVEL_MAPPING_PREFIX, -1, soundPostfix),
						1.0f, 0.0f,
						soundDelaysByRound[currentStage].MULTIPLIER_SPARKLE_TRAIL_TRAVEL_SOUND_DELAY);
				}
				
				// There are casses (layering issues) where you don't want the sparkle trail Z to be modified
				Vector3 finalEndPosition = new Vector3();
				finalEndPosition.x = endPos.transform.position.x;
				finalEndPosition.y = endPos.transform.position.y;
				
				// Only set the z value if ignoreZValue is false
				if (ignoreZValue || sparkleTrailDefinitionsByRound[currentStage].ignoreZValue)
				{
					finalEndPosition.z = sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.transform.position.z;
				}
				else
				{
					finalEndPosition.z = endPos.transform.position.z;
				}
				if (sparkleTrailDefinitionsByRound[currentStage].shouldClearParticlesWhenTurnedOff)
				{
					foreach (ParticleSystem particles in sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.GetComponentsInChildren<ParticleSystem>())
					{
						particles.Clear();
					}
				}
				yield return new TITweenYieldInstruction(iTween.MoveTo(
					sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound, iTween.Hash(
						"position", finalEndPosition,
						"islocal", false,
						"time", sparkleTrailDefinitionsByRound[currentStage].SPARKLE_TRAIL_DUR,
						"easetype", sparkleTrailDefinitionsByRound[currentStage].easeType
					)
				));

				if (currentStage < revealEffectDefinitionsByRound.Length && revealEffectDefinitionsByRound[currentStage].instancedRevealEffect != null)
				{
					RevealDefinition revealEffect = revealEffectDefinitionsByRound[currentStage];
					
					if (overrideRevealEffect != null)
					{
						revealEffect = overrideRevealEffect;
					}
					
					if (revealEffect.SHOULD_USE_POSITION_OFFSET)
					{
						revealEffect.instancedRevealEffect.transform.position =
							finalEndPosition + revealEffectDefinitionsByRound[currentStage].POSITION_OFFSET;
					}
					
					Animator revealEffectAnimator = revealEffect.instancedRevealEffect.GetComponent<Animator>();
					revealEffectAnimator.Play(revealEffect.INSTANCED_REVEAL_EFFECT_ANIMATION_NAME);
				}
				
				if (!string.IsNullOrEmpty(sparkleTrailDefinitionsByRound[currentStage].INSTANCED_SPARKLE_TRAIL_END_ANIMATION_NAME_BY_ROUND))
				{
					sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.GetComponent<Animator>().Play(sparkleTrailDefinitionsByRound[currentStage].INSTANCED_SPARKLE_TRAIL_END_ANIMATION_NAME_BY_ROUND);
				}
		
				if (soundDelaysByRound.Length > 0)
				{
					Audio.play(
						getSoundMappingByRoundAndIndex(MULTIPLIER_SPARKLE_TRAIL_ARRIVE_MAPPING_PREFIX, -1, soundPostfix),
						1.0f, 0.0f,
						soundDelaysByRound[currentStage].MULTIPLIER_SPARKLE_TRAIL_ARRIVE_SOUND_DELAY);
				}

				if (sparkleTrailDefinitionsByRound[currentStage].shouldActivate)
				{
					sparkleTrailDefinitionsByRound[currentStage].instancedSparkleTrailByRound.SetActive(false);
				}
			}
		}
	}
	
	public IEnumerator waitAfterSparkleEffects()
	{
		if (currentStage < sparkleTrailDefinitionsByRound.Length &&
		    sparkleTrailDefinitionsByRound[currentStage] != null)
		{
			SparkleTrailDefinition sparkleTrail = sparkleTrailDefinitionsByRound[currentStage];
			
			if (sparkleTrail != null && sparkleTrail.SPARKLE_TRAIL_WAIT_1 > 0.0f)
			{
				yield return new WaitForSeconds(sparkleTrail.SPARKLE_TRAIL_WAIT_1);
			}
		}
	}
	
	private void doPreFightAnimations()
	{
		if (currentStage < fightDefinitionsByRound.Length)
		{
			foreach (FightLocation fightLocDef in fightDefinitionsByRound[currentStage].fightLocations)
			{
				if (fightLocDef.fightLocationAnimator != null && !string.IsNullOrEmpty(fightLocDef.FIGHT_LOCATION_PRE_FIGHT_ANIM_NAME))
				{
					fightLocDef.fightLocationAnimator.Play(fightLocDef.FIGHT_LOCATION_PRE_FIGHT_ANIM_NAME);
				}
			}
		}
	}
	
	private IEnumerator doFightAnimations(FightLocation fightLocDef, bool isGameOver)
	{
		if (!isGameOver)
		{
			yield return new TIWaitForSeconds(fightLocDef.GOOD_ANIM_DELAY);
			if (!string.IsNullOrEmpty(fightLocDef.FIGHT_LOCATION_GOOD_ANIM_NAME))
			{
				fightLocDef.fightLocationAnimator.Play(fightLocDef.FIGHT_LOCATION_GOOD_ANIM_NAME);
			}
		}
		else
		{
			yield return new TIWaitForSeconds(fightLocDef.BAD_ANIM_DELAY);
			if (!string.IsNullOrEmpty(fightLocDef.FIGHT_LOCATION_BAD_ANIM_NAME))
			{
				fightLocDef.fightLocationAnimator.Play(fightLocDef.FIGHT_LOCATION_BAD_ANIM_NAME);
			}
		}
	}
	
	public void stopRevealEffects()
	{
		if (currentStage < revealEffectDefinitionsByRound.Length)
		{
			RevealDefinition revealEffect = revealEffectDefinitionsByRound[currentStage];
			
			if (revealEffect != null && revealEffect.INSTANCED_END_REVEAL_EFFECT_ANIMATION_NAME != "")
			{
				Animator revealEffectAnimator = revealEffect.instancedRevealEffect.GetComponent<Animator>();
				revealEffectAnimator.Play(revealEffect.INSTANCED_END_REVEAL_EFFECT_ANIMATION_NAME);
			}
		}
	}
	
	private IEnumerator doPostSparkleTrailEffects(Animator pickAnimator = null)
	{
		if (postSparkleTrailEffectsByRound.Length > 0 &&
		    postSparkleTrailEffectsByRound[currentStage].postSparkleTrailEffect != null)
		{
			postSparkleTrailEffectsByRound[currentStage].postSparkleTrailEffect.SetActive(true);
			yield return new TIWaitForSeconds(postSparkleTrailEffectsByRound[currentStage].LENGTH_OF_EFFECT);
			postSparkleTrailEffectsByRound[currentStage].postSparkleTrailEffect.SetActive(false);
		}

		if (postSparkleTrailEffectsByRound.Length > currentStage && pickAnimator != null)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickAnimator, postSparkleTrailEffectsByRound[currentStage].animationName));
		}
	}
	
	protected override IEnumerator animateScore(long startScore, long endScore)
	{
		TICoroutine animateScoreRoutine = null;
		bool playedWinAmountRollupSound = false;

		foreach (LabelWrapper amountText in currentWinAmountTextsWrapper)
		{
			if (amountText != null)
			{
				animateScoreRoutine = StartCoroutine(SlotUtils.rollup(startScore, endScore, amountText, !playedWinAmountRollupSound));
				playedWinAmountRollupSound = true;
			}
		}

		playedWinAmountRollupSound = false;
		foreach (LabelWrapperComponent amountText in currentWinAmountTextWrappers)
		{
			if (amountText != null)
			{
				animateScoreRoutine = StartCoroutine(SlotUtils.rollup(startScore, endScore, amountText, !playedWinAmountRollupSound));
				playedWinAmountRollupSound = true;
			}
		}
		
		if (animationDefinitionsByRound.Length > currentStage && animationDefinitionsByRound[currentStage].animations.Length > 0)
		{
			foreach (AnimationDefinition animDef in animationDefinitionsByRound[currentStage].animations)
			{
				if (animDef.playType == (AnimationDefinition.PlayType.DuringRollup))
				{
					animDef.animator.Play(animDef.ANIM_NAME);
				}
			}
		}

		playSceneSounds(SoundDefinition.PlayType.DuringRollup);

		if (animateScoreRoutine != null)
		{
			yield return animateScoreRoutine;
			// Introduced a slight delay here so the click of the button doesn't immediately force the rollup to stop.
			yield return new WaitForSeconds(0.1f);
		}
		
		if (animationDefinitionsByRound.Length > currentStage && animationDefinitionsByRound[currentStage].animations.Length > 0)
		{
			foreach (AnimationDefinition animDef in animationDefinitionsByRound[currentStage].animations)
			{
				if (animDef.playType == (AnimationDefinition.PlayType.DuringRollup))
				{
					animDef.animator.Play(animDef.ANIM_END_NAME);
				}
			}
		}
	}
	
/*==========================================================================================================*\
* 	Common Reveals
\*==========================================================================================================*/	

	// Play Pickem Picked Sound
	
	public void playPickemPickedSound()
	{
		Audio.play(getSoundMappingByRoundDefaultTo0(PICKEM_PICKED_PREFIX));
	}
	
	// Reveal Picked/Unpicked Credits
	
	public IEnumerator revealPickedCredits(PickGameButtonData pick, long credits)
	{
		pick.setText(
			CreditsEconomy.convertCredits(credits),
			PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
			
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);

		string soundKey = getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX);
		string voKey = getSoundMappingByRound(PICKEM_PICK_CREDITS_VO_MAPPING_PREFIX);
		
		if (soundOverridesByRound != null && currentStage < soundOverridesByRound.Length)
		{
			PickSoundOverrides soundOverrides = soundOverridesByRound[currentStage];
			
			if (soundOverrides != null)
			{
				string soundOverride = soundOverrides.CREDITS_SOUND_NAME;
				if (!string.IsNullOrEmpty(soundOverride))
				{
					soundKey = soundOverride;
				}
				
				string voOverride = soundOverrides.CREDITS_VO_NAME;
				if (!string.IsNullOrEmpty(voOverride))
				{
					voKey = voOverride;
				}
			}
		}
		
		Audio.play(
			soundKey,
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
		
		Audio.play(
			voKey,
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_CREDITS_VO_SOUND_DELAY);
		
		playSceneAnimations(AnimationDefinition.PlayType.Credits);
		playSceneSounds(SoundDefinition.PlayType.Credits);
		
		yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
	}
	
	public void revealUnpickedCredits(PickGameButtonData pick, long credits)
	{
		pick.setText(
			CreditsEconomy.convertCredits(credits),
			PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_CREDITS_ANIM_NAME);
		playNotChosenAudio(true, currentStage);
	}
	
	// Reveal Picked/Unpicked Multiplier
	
	// The locKey is something like {0}x or +{0}x.
	// If the multiplier is always +1X and it's built-in to the prefab, then you don't have to set the locKey or multiplier.
	// If the multiplier animation reveals the multiplied credits, too, then pass those, too, but:
	// Hopefully you have a separate CHANGE_MULTIPLIER_TO_CREDITS animation for that.
	public IEnumerator revealPickedMultiplier(PickGameButtonData pick, string locKey = "", long multiplier = 0, long credits = 0)
	{
		if (!string.IsNullOrEmpty(locKey) && multiplier > 0)
		{
			pick.multiplierLabel.text = Localize.text(locKey, multiplier);
		}
		
		if (credits != 0)
		{
			pick.setText(
				CreditsEconomy.convertCredits(credits),
				PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		}
					
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_MULTIPLIER_ANIM_NAME);
		
		Audio.play(
			getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_MAPPING_PREFIX),
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_SOUND_DELAY);
		
		Audio.play(
			getSoundMappingByRound(PICKEM_PICK_MULTIPLIER_VO_MAPPING_PREFIX),
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_MULTIPLIER_VO_SOUND_DELAY);
		
		yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
	}
		
	public void revealUnpickedMultiplier(PickGameButtonData pick, string locKey = "", long multiplier = 0)
	{
		if (!string.IsNullOrEmpty(locKey) && multiplier > 0)
		{
			pick.multiplierLabel.text = Localize.text(locKey, multiplier);
		}
		
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_MULTIPLIER_ANIM_NAME);
		playNotChosenAudio(true, currentStage);
	}

	// Reveal Picked/Unpicked Special 1
		
	public IEnumerator revealPickedSpecial1(PickGameButtonData pick, long credits = 0)
	{
		if (credits > 0)
		{
			pick.setText(
				CommonText.formatNumber(credits),
				PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		}
		
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);

		string soundKey = getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX);
		string voKey = getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX);
		
		if (soundOverridesByRound != null && currentStage < soundOverridesByRound.Length)
		{
			PickSoundOverrides soundOverrides = soundOverridesByRound[currentStage];
			
			if (soundOverrides != null)
			{
				string soundOverride = soundOverrides.SPECIAL1_SOUND_NAME;
				if (!string.IsNullOrEmpty(soundOverride))
				{
					soundKey = soundOverride;
				}
				
				string voOverride = soundOverrides.SPECIAL1_VO_NAME;
				if (!string.IsNullOrEmpty(voOverride))
				{
					voKey = voOverride;
				}
			}
		}
		
		Audio.play(
			soundKey,
			1.0f, 0.0f, 
			soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
		
		Audio.play(
			voKey,
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
		
		playSceneSounds(SoundDefinition.PlayType.Special1);
		
		yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
	}
	
	public void revealUnpickedSpecial1(PickGameButtonData pick, long credits = 0)
	{
		if (credits > 0)
		{
			pick.setText(
				CommonText.formatNumber(credits),
				PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		}
		
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME);
		playNotChosenAudio(true, currentStage);
	}
	
	// Reveal Picked/Unpicked Bad End
	
	public IEnumerator revealPickedBadEnd(PickGameButtonData pick, long credits = 0)
	{
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);
		
		if (credits > 0)
		{
			pick.setText(CreditsEconomy.convertCredits(credits), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		}
		
		Audio.play(
			getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX),
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
		
		Audio.play(
			getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX),
			1.0f, 0.0f,
			soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);
		
		yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
	}
	
	public void revealUnpickedBadEnd(PickGameButtonData pick, long credits = 0)
	{
		if (credits > 0)
		{
			pick.setText(CreditsEconomy.convertCredits(credits), PickGameButtonDataFlags.Number | PickGameButtonDataFlags.GrayNumber);
		}
		
		pick.animator.Play(animationNamesByRound[currentStage].REVEAL_UNPICKED_END_ANIM_NAME);
		playNotChosenAudio(true, currentStage);
	}

	// Function to handle anything that needs to happen before BonusGamePresenter calls finalCleanup
	public override IEnumerator handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules()
	{
		if (onBonusGamePresenterFinalCleanupAnimatedObjectToTurnOn != null && onBonusGamePresenterFinalCleanupAnimatedObjectToTurnOn.Count > 0)
		{
			for (int i = 0; i < onBonusGamePresenterFinalCleanupAnimatedObjectToTurnOn.Count; i++)
			{
				onBonusGamePresenterFinalCleanupAnimatedObjectToTurnOn[i].SetActive(true);
			}
		}

		if (onBonusGamePresenterFinalCleanupAnimationList != null && onBonusGamePresenterFinalCleanupAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onBonusGamePresenterFinalCleanupAnimationList));
		}
		else
		{
			yield break;
		}
	}
}

// use this enum to avoid constantly casting to figure out what outcome type this game uses
public enum BonusOutcomeTypeEnum
{
	Undefined 							= -1,
	NewBaseBonusGameOutcomeType 		= 0,
	PickemOutcomeType 					= 1,
	WheelOutcomeType 					= 2
}

public enum PickemStageType
{
	None = -1,
	CreditsWithJackpotAdvanceAndBadEnd,
	SinglePickCreditsOrMultiplier,
	CreditsOrFightWithLoseOrMultiplier,
	SinglePickAdvance,
	CreditsOrAdvanceOrJackpotEnd,
	RetreatOrEnd,
	MultiplierAdvanceOrIncreaseWithCreditsOnEnd,
	CreditsWithAdvanceAndBadEnd, // eg mm01 Pickem
	FindToAdvanceWithCreditsAndBadEnd, // eg mtm01 Pickem
	PickNumPicks, // eg elvira04 Pickem
	CreditsAndMultipliers, // eg elvira04 Pickem,
	CreditsThenMultiplierThenTotal, // eg superman01 Pickem.  Requires GenericPickemGamGO and PickingRoundGOs!
	FindJackpotWithCreditsAndBadEnd, // eg gen22 Pickem.  Requires GenericPickemGameGO and PickingRoundGOs!
	SinglePickAdvanceWithCreditsOrCollectAllOrBadEnd,   // eg pb02 pickem 
	JackpotLadder // eg batman01
}

public enum NextStageRule
{
	NextStage,               // Continue to the next stage.
	LastStage,               // This is the last stage.
	NextStagePlusCurrentPick // The next stage depends on the current pick.
}

[System.Serializable]
public class PickButtonTemplateDefinition
{
	public string PATH_TO_BUTTON;
	public string PATH_TO_ANIMATOR;
	public string PATH_TO_REVEALNUMBERLABEL;
	public string PATH_TO_REVEALNUMBERLABELWRAPPER;
	public string PATH_TO_REVEALNUMBEROUTLINE;
	public string PATH_TO_GRAYREVEALNUMBER;
	public string PATH_TO_GRAYREVEALNUMBERLABELWRAPPER;
	public string PATH_TO_GRAYREVEALNUMBEROUTLINE;
	public string PATH_TO_MULTIPLIERLABEL;
	public string PATH_TO_MULTIPLIEROUTLINELABEL;
	public string PATH_TO_EXTRA_LABEL;
	public string PATH_TO_EXTRA_OUTLINE_LABEL;
	public string PATH_TO_IMAGE_REVEAL;
	public string[] PATHS_TO_MULTIPLE_IMAGE_REVEALS;
	public string PATH_TO_MATERIAL;
	public int round;
	public Vector3 boxColliderSize;
	public Vector3 boxColliderCenter;
	
	public List<GameObject> objectsToApplyTemplateTo;
}

[System.Serializable]
public class MultiplierAdvanceOrIncreaseWithCreditsOnEndDefinition
{
	public List<MultiplierCell> multiplierCells;
	public string PAYTABLE_POOL_NAME;

	public string ADVANCE_WIN_BOX_ANIMATION_NAME;
	public string INCREASE_WIN_BOX_ANIMATION_NAME;
	public string MULTIPLIER_INCREASE_INACTIVE_ANIMATION_NAME;
	public string MULTIPLIER_ADVANCE_ANIMATION_NAME;
	public string MULTIPLIER_ACTIVATE_ANIMATION_NAME;
	public string MULTIPLIER_INCREASE_ACTIVE_ANIMATION_NAME;
	public string MULTIPLIER_DEACTIVATE_ANIMATION_NAME;
	public string WINBOX_CELEBRATION_ANIMATION_NAME;
	public string CELL_BURST_ANIMATION_NAME;
	
	public float CELL_BURST_ADVANCE_DELAY;
	public float MULTIPLIER_INCREASE_DELAY;
	public float MULTIPLIER_INCREASE_ANIMATION_DELAY;
	public float MULTIPLIER_ADVANCE_ANIMATION_DELAY;
	public float ADVANCE_REVEAL_SPARKLE_TRAIL_DELAY;
	
	public float MULTIPLIER_INCREASE_REVEAL_CELEBRATION_DELAY;
	public float MULTIPLIER_ADVANCE_REVEAL_CELEBRATION_DELAY;
	
	public GameObject winBox;
	public GameObject advanceWinBox;
	public GameObject increaseWinBox;
	
	public bool usesDynamicCellBursts;
	
	public GameObject bigDynamicCellBurst;
	public GameObject dynamicCellBurst;
	
	[System.NonSerialized] public int verticalMultiplierIndex = 0;
	[System.NonSerialized] public int horizontalMultiplierIndex = 1;
	[System.NonSerialized] public RoundPicks currentRoundPick;
	[System.NonSerialized] public BasePick currentPick;
	[System.NonSerialized] public Dictionary<int, List<int>> paytableMultipliers;
}

[System.Serializable]
public class FightRoundDefinition
{
	public float SPECIAL1_ANIM_WAIT_TIME;
	public float FIGHT_TWEEN_TIME;
	public float FIGHT_POST_TWEEN_WAIT_TIME;
	public float FIGHT_POST_FIGHT_WAIT_TIME1;
	public float FIGHT_POST_FIGHT_WAIT_TIME2;
	public GameObject fighterAnimatorParent;
	public Animator fighterAnimator;
	public string FIGHTER_GOOD_ANIM_NAME;
	public string FIGHTER_BAD_ANIM_NAME;
	public Vector3 FIGHTER_START_POS_OFFSET;	
	public Vector3 FIGHTER_END_POS_OFFSET;	
	public FightLocation[] fightLocations;
}

[System.Serializable]
public class PickemCheat
{
	public bool shouldBadEnd = false;
	public bool shouldCredits = false;
	public bool shouldMultiplier = false;
	public bool shouldJackpotAdvance = false;
}

[System.Serializable]
public class FindDefinition
{
	public int numToFind = 0;
	public string findGroupId = "";
	public string endGroupId = "";
	
	// Count with a label (code not implemented yet).
	public LabelWrapperComponent countLabel;
	public int startCount;
	public bool shouldCountUp;
	
	// Show/hide each count game object one at a time.
	public GameObject[] countGos;
	
	// Animate the count.
	// Change the animator state names to "count1", "count2", "count3", etc.
	public Animator countAnimator;
	
	// Flag this false if you want to play an animation as the reveal is happening
	public bool shouldBadRevealBlock = true;
	public bool shouldGoodRevealBlock = true;
	
	// If not using a count animator
	public string individualAnimationName = "populate";
}

[System.Serializable]
public class PickNumPicksDefinition
{
	// If the server doesn't send the possible number of picks,
	// then we have to manually list them (not implemented).
	public List<int> possibleNumPicks;
}

[System.Serializable]
public class FightLocation
{
	public string FIGHT_LOCATION_GOOD_ANIM_NAME;
    public string FIGHT_LOCATION_BAD_ANIM_NAME;
    public string FIGHT_LOCATION_PRE_FIGHT_ANIM_NAME;
	public Animator fightLocationAnimator;
	public float GOOD_ANIM_DELAY;
	public float BAD_ANIM_DELAY;
}

[System.Serializable]
public class AnimationDefinitions
{
	public AnimationDefinition[]  animations;			
}

[System.Serializable]
public class SoundDefinition // sound definition
{
	public enum PlayType
	{
		Bad,
		Good,
		Jackpot,
		Credits,
		PostCreditsReveal,
		PreCreditsReveal,
		PostGoodReveal,
		PreGoodReveal,
		PostBadReveal,
		PreBadReveal,
		PostJackpotReveal,
		PreJackpotReveal,
		Idle,
		Reveal,
		NonPickReveal,
		PickMe,
		Pick, // any pick
		Advance,
		Special1,
		DuringRollup,
		OnIncrementMultiplier,
		Multiplier,
		StringMatched,
		IsStringMatched,
		Transition // Transition from this round to the next round
		// PLEASE ADD NEW ENTRIES TO ANIMATIONDEFINITION PLAYTYPE, TOO!
		// They should really just be one enumeration.
	}
	
	public PlayType	playType;
	public string soundName;
	public string musicName;
	public string matchName;
	
	[HideInInspector] public int numTries = 0; // Number of times the game tried to play this sound by calling playSceneSounds.
	[HideInInspector] public int numPlays = 0; // Number of times playSceneSounds actually played this sound.
	
	// Note!  Unity does not always assign the default values, so initStage fixes-up the default values.
	// Also note!  Maybe all of these settings should be in SCAT instead of tuning them in the prefabs.
	
	public int counter = 1; // 1 means play now, otherwise play later.
	public int maxCounter = 0; // Set maxCounter to randomize the start counter between counter and maxCounter.
	
	public int frequency = 1; // Play this sound every nth time (1 means play it every time).
	public int maxFrequency = 0; // Set the max frequency to randomize the frequency between frequency and maxFrequency.
	
	public int percent = 100; // Every nth time, this is the percent chance that it really plays the sound.
	
	public bool isCollection = false; // Is it a SCAT collection?  (You only need to set this for resetting and cycling).
	public bool shouldResetCollection = false; // When this round starts, should it reset the sequence?
	public bool shouldCycleCollection = true; // Should it cycle through the sequence, or should it play each sound once and stop?
	public bool isLooping = false; //Is this a sound that loops constantly?

	public int onlyOnNthPick = 0; // Only play this sound if it happened on the nth pick (first pick is n=1) (0 is ignore this setting).

	public int onlyOnNthTry = 0; // Only play this sound on the nth try.
	public int startPlayingOnNthTry = 0; // Start playing this sound on the nth try.
	public int keepPlayingUntilNthTry = 0; // Keep playing this sound until the nth try.
	
	public float delayBeforePlay = 0.0f;
}

[System.Serializable]
public class SoundDefinitions
{
	public SoundDefinition[]  sounds;			
}

[System.Serializable]
public class AnimationDefinition // animation definition
{
	// Additional rule to determine whether or not to play the animation.
	public enum PlayRule
	{
		None,
		PreviousPickEquals
	};
	
	public PlayRule playRule = PlayRule.None;
	public int ruleArgument = -1; // (eg only play this animation if the previous pick equals this argument).
	
	public enum PlayType
	{
		Bad,
		Good,
		Jackpot,
		Credits,
		PostCreditsReveal,
		PreCreditsReveal,
		PostGoodReveal,
		PreGoodReveal,
		PostBadReveal,
		PreBadReveal,
		PostJackpotReveal,
		PreJackpotReveal,
		Idle,
		Reveal,
		NonPickReveal,
		PickMe,
		Pick, // any pick
		Advance,
		Special1,
		DuringRollup,
		OnIncrementMultiplier,
		Multiplier,
		StringMatched,
		IsStringMatched,
		Transition // Transition from this round to the next round.	
		// PLEASE ADD NEW ENTRIES TO SOUNDDEFINITION PLAYTYPE, TOO!
		// They should really just be one enumeration.
	}
	
	public PlayType	playType;
	public long		optionalParameter;					
	public Animator animator;
	public string 	ANIM_NAME;
	public string   ANIM_END_NAME;
	public float 	ANIM_DELAY;	
	public float 	ANIM_LENGTH_OVERRRIDE = -1.0f;
	public bool		shouldBlockUntilAnimationFinished;
	public bool		useSetActive; // Activate the game object, play the animation, then deactivate it.
	public bool     shouldActivateAndStayActive; // Activate the game object, play the anim, do not deactivate (eg looping idle anim).
	public bool		useSuffixVariable;
	public string	soundName = "";
	public float    soundDelay = 0.0f;
	
	public enum PlaySource
	{
		AnimName,       // Use the ANIM_NAME as usual
		PreviousPick,   // Use the last pick to index into the ANIM_NAME_ARRAY.  Note, most pickem stage types don't set the previous pick yet.
	};
	public PlaySource playSource;
	
	public string[] ANIM_NAME_ARRAY;
}
				
[System.Serializable]
public class RoundAnimationNames
{
	public string PICKME_ANIM_NAME_OVERRIDE;
	public string FADE_OUT_ANIM_NAME;
	
	/***************************************************************************************************
	* INSPECTOR VARIABLES FOR ANIMATION NAMES OF PICKED REVEALS, KEYED BY ROUND
	***************************************************************************************************/	
	public string REVEAL_PICKED_CREDITS_ANIM_NAME;
	public string REVEAL_PICKED_SPECIAL1_ANIM_NAME;
	public string REVEAL_PICKED_SPECIAL2_ANIM_NAME;
	public string REVEAL_PICKED_SPECIAL3_ANIM_NAME;
	public string REVEAL_PICKED_END_ANIM_NAME;
	public string REVEAL_PICKED_MULTIPLIER_ANIM_NAME;
	public string REVEAL_PICKED_MULTIPLIER_ADVANCE_ANIM_NAME;
	public string REVEAL_PICKED_MULTIPLIER_INCREASE_ANIM_NAME;

	/***************************************************************************************************
	* INSPECTOR VARIABLES FOR ANIMATION NAMES OF UNPICKED REVEALS, KEYED BY ROUND
	***************************************************************************************************/	
	public string REVEAL_UNPICKED_CREDITS_ANIM_NAME;
	public string REVEAL_UNPICKED_SPECIAL1_ANIM_NAME;
	public string REVEAL_UNPICKED_SPECIAL2_ANIM_NAME;
	public string REVEAL_UNPICKED_SPECIAL3_ANIM_NAME;
	public string REVEAL_UNPICKED_END_ANIM_NAME;
	public string REVEAL_UNPICKED_MULTIPLIER_ANIM_NAME;
	public string REVEAL_UNPICKED_MULTIPLIER_ADVANCE_ANIM_NAME;
	public string REVEAL_UNPICKED_MULTIPLIER_INCREASE_ANIM_NAME;

	/***************************************************************************************************
	* INSPECTOR VARIABLES FOR ANIMATION NAMES OF FADE REVEALS, KEYED BY ROUND
	***************************************************************************************************/	
	public string FADE_UNPICKED_CREDITS_ANIM_NAME;
	public string FADE_UNPICKED_SPECIAL1_ANIM_NAME;
	public string FADE_UNPICKED_END_ANIM_NAME;
	public string FADE_PICKED_CREDITS_ANIM_NAME;
	public string FADE_PICKED_SPECIAL1_ANIM_NAME;
	public string FADE_PICKED_END_ANIM_NAME;
	
	/***************************************************************************************************
	* OTHER ANIMATIONS
	***************************************************************************************************/	
	public string CHANGE_MULTIPLIER_TO_CREDITS_ANIM_NAME;
}

[System.Serializable]
public class RevealDefinition
{
	public GameObject instancedRevealEffect;
	public string INSTANCED_REVEAL_EFFECT_ANIMATION_NAME;
	public string INSTANCED_END_REVEAL_EFFECT_ANIMATION_NAME;
	public bool SHOULD_USE_POSITION_OFFSET = true; // If the effect is already at the right position, then don't use the position offset.
	public Vector3 POSITION_OFFSET; // Offset from the sparkle trail end position.
}

[System.Serializable]
public class SparkleTrailDefinition
{
	public bool shouldActivate = true; // Should it activate and deactivate?  Or do the animations show and hide it?
	public GameObject instancedSparkleTrailByRound; // linked in inspector from bonus game prefab hierarchy, don't need to instantiate
	public string INSTANCED_SPARKLE_TRAIL_ANIMATION_NAME_BY_ROUND; 
	public string INSTANCED_SPARKLE_TRAIL_END_ANIMATION_NAME_BY_ROUND; 
	public GameObject startPos;
	public GameObject endPos;
	public bool ignoreZValue = false; // True means use sparkle's z-value, false means use start and end positions' z-values.
	public bool shouldUseMultiplierTextLocationForStart;
	public bool shouldUseValueTextLocationForEnd;
	public bool shouldClearParticlesWhenTurnedOff;
	public float SPARKLE_TRAIL_DUR = 2.0f;
	public float SPARKLE_TRAIL_WAIT_1;
	public float SPARKLE_TRAIL_WAIT_2;
	public float POST_TRAIL_PRE_ADD_WAIT;
	
	public iTween.EaseType easeType = iTween.EaseType.easeInCubic;
}

[System.Serializable]
public class PostSparkleTrailEffectsDefinition
{
	public GameObject postSparkleTrailEffect;
	public float LENGTH_OF_EFFECT;
	public string animationName;
}

[System.Serializable]
public class PickSoundOverrides
{
	// shouldResetXSound flags:
	// If it's a collection that plays in order,
	// then you can reset the collection at the beginning of the round to make sure it starts over from the beginning.

	// Credits.
	
	public bool SHOULD_RESET_CREDITS_SOUND = false;
	public string CREDITS_SOUND_NAME = "";
	
	public bool SHOULD_RESET_CREDITS_VO = false;
	public string CREDITS_VO_NAME = "";
	
	// Special 1.
	
	public bool SHOULD_RESET_SPECIAL1_SOUND = false;
	public string SPECIAL1_SOUND_NAME = "";
	
	public bool SHOULD_RESET_SPECIAL1_VO = false;
	public string SPECIAL1_VO_NAME = "";
}

[System.Serializable]
public class SoundDelaysDefinition
{
	public float INTRO_VO_SOUND_DELAY;
	public float PICK_PICKED_SOUND_DELAY;
	public float REVEAL_SPECIAL1_SOUND_DELAY;
	public float REVEAL_SPECIAL2_SOUND_DELAY;
	public float REVEAL_SPECIAL1_VO_SOUND_DELAY;
	public float REVEAL_SPECIAL2_VO_SOUND_DELAY;
	public float REVEAL_CREDITS_SOUND_DELAY;
	public float REVEAL_CREDITS_VO_SOUND_DELAY;
	public float REVEAL_MULTIPLIER_SOUND_DELAY;
	public float REVEAL_MULTIPLIER_VO_SOUND_DELAY;
	public float REVEAL_BAD_SOUND_DELAY;
	public float REVEAL_BAD_VO_SOUND_DELAY;
	public float MULTIPLIER_SPARKLE_TRAIL_TRAVEL_SOUND_DELAY;
	public float MULTIPLIER_SPARKLE_TRAIL_ARRIVE_SOUND_DELAY;
	public float REVEAL_BAD_SOUND_ROUND_AND_INDEX_DELAY;
}

[System.Serializable]
public class RoundGameObjects
{
	public GameObject[] objectsToDeactivateThisRound;
	public GameObject[] objectsToActivateThisRound;
}

[System.Serializable]
public class LabelsToLocalize2
{
	public LabelToLocalize[] labelDefs;
}

[System.Serializable]
public class LabelToLocalize
{
	public UILabel label;	// To be removed when prefabs are updated.
	public LabelWrapperComponent labelWrapperComponent;
	public string localizationKey;
	public bool toUpper;
	
	public LabelWrapper labelWrapper
	{
		get
		{
			if (_labelWrapper == null)
			{
				if (labelWrapperComponent != null)
				{
					_labelWrapper = labelWrapperComponent.labelWrapper;
				}
				else
				{
					_labelWrapper = new LabelWrapper(label);
				}
			}
			return _labelWrapper;
		}
	}
	private LabelWrapper _labelWrapper = null;	
}

[System.Serializable]
public class WingInformation
{
	public enum WingChallengeStage
	{
		None = -1,
		First,
		Secondary,
		Third,
		Fourth
	}
	public WingChallengeStage challengeStage;
}

[System.Serializable]
public class PickItemStartBehavior
{
	public bool shouldFloatItems;
	public float floatDistanceYLowerBound;
	public float floatDistanceYUpperBound;
	public float floatPunchTimeLowerBound;
	public float floatPunchTimeUpperBound;
	public iTween.EaseType easeTypeFirstFourth;
	public iTween.EaseType easeTypeMiddleHalf;
	public iTween.EaseType easetypeLastFourth;
}

[System.Serializable]
public class RemainingPicksRevealBehavior
{
	public bool skipRevealOnGoodOutcome;
	public bool skipRevealOnBadOutcome;
	public bool deactivateRemainingPicksBeforeRevealOnGoodOutcome;
}

[System.Serializable]
public class MultiplierCell
{
	public GameObject container;
	public Animator animator;
	public Animator cellBurst;
	
	public UILabel multiplierLabel;				// To be removed when prefabs are updated.
	public UILabel multiplierGlowLabel;			// To be removed when prefabs are updated.
	public UILabel multiplierBlueStrokeLabel;	// To be removed when prefabs are updated.
	public UILabel multiplierBlackStrokeLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierLabelWrapperComponent;
	public LabelWrapperComponent multiplierGlowLabelWrapperComponent;
	public LabelWrapperComponent multiplierBlueStrokeLabelWrapperComponent;
	public LabelWrapperComponent multiplierBlackStrokeLabelWrapperComponent;

	public LabelWrapper multiplierLabelWrapper
	{
		get
		{
			if (_multiplierLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierLabelWrapper = multiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierLabelWrapper = new LabelWrapper(multiplierLabel);
				}
			}
			return _multiplierLabelWrapper;
		}
	}
	private LabelWrapper _multiplierLabelWrapper = null;

	public LabelWrapper multiplierGlowLabelWrapper
	{
		get
		{
			if (_multiplierGlowLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierGlowLabelWrapper = multiplierGlowLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierGlowLabelWrapper = new LabelWrapper(multiplierGlowLabel);
				}
			}
			return _multiplierGlowLabelWrapper;
		}
	}
	private LabelWrapper _multiplierGlowLabelWrapper = null;

	public LabelWrapper multiplierBlueStrokeLabelWrapper
	{
		get
		{
			if (_multiplierBlueStrokeLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierBlueStrokeLabelWrapper = multiplierBlueStrokeLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierBlueStrokeLabelWrapper = new LabelWrapper(multiplierBlueStrokeLabel);
				}
			}
			return _multiplierBlueStrokeLabelWrapper;
		}
	}
	private LabelWrapper _multiplierBlueStrokeLabelWrapper = null;

	public LabelWrapper multiplierBlackStrokeLabelWrapper
	{
		get
		{
			if (_multiplierBlackStrokeLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierBlackStrokeLabelWrapper = multiplierBlackStrokeLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierBlackStrokeLabelWrapper = new LabelWrapper(multiplierBlackStrokeLabel);
				}
			}
			return _multiplierBlackStrokeLabelWrapper;
		}
	}
	private LabelWrapper _multiplierBlackStrokeLabelWrapper = null;
	
	public void setValue(int value)
	{
		string str = Localize.text("{0}X", value);

		multiplierLabelWrapper.text = str;
		if (multiplierGlowLabelWrapper != null)
		{
			multiplierGlowLabelWrapper.text = str;
		}
		multiplierBlueStrokeLabelWrapper.text = str;
		multiplierBlackStrokeLabelWrapper.text = str;
	}
};

[System.Serializable]
public class RevealDelaysDefintion
{
	public float WAIT_TO_ROLLUP = 0.0f;
	public float WAIT_TO_CHANGE_MULTIPLIER_TO_CREDITS = 0.0f;
	
	public float WAIT_TO_REVEAL_PICK_DUR = 0.0f;
	public float WAIT_TO_REVEAL_DUR = 0.2f;
	public float WAIT_TO_END_GAME_DUR = 1.0f;
	public float WAIT_TO_ADVANCE_DUR = 1.0f;
}

public class FadeAnimationDefinition
{
	public Animator objectAnimator;
	public string fadeAnimationName;
}
