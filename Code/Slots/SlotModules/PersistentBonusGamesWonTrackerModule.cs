//
// Track number of bonus game won as they progress to some other super bonus game
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : August 10, 2020
// Games : orig002
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentBonusGamesWonTrackerModule : SlotModule
{
#region serialized member variables

	[Tooltip("Bonus Tracker pips with animations for turning on and off")]
	[SerializeField] private List<BonusTrackerPip> bonusTrackerPips;

	[Tooltip("Add a delay to allow bonus tracker pips to complete their animation before the bonus game starts")]
	[SerializeField] private float bonusTrackerAnimationCompleteDelay;

	[Tooltip("Wait for bonus tracker to populate at the start of the game")]
	[SerializeField] private bool blockUntilTrackerIsAnimatedAtStart;

	[Tooltip("Add a delay to allow bonus tracker pips to complete their animation when the game first starts")]
	[SerializeField] private float bonusTrackerAnimationCompleteAtStartDelay;

#endregion

#region private member variables

	private List<TICoroutine> allCoroutines;

	private int currentMeterLevel
	{
		get => _currentMeterLevel;
		set
		{
			if (value >= 0 && value < maxMeterLevel)
			{
				_currentMeterLevel = value;
			}
			else if (value >= maxMeterLevel)
			{
				_currentMeterLevel = maxMeterLevel - 1;
			}
		}
	}
	private int _currentMeterLevel;

	private int maxMeterLevel;

	private ReevaluationPersistentMeterRewardBonusGame reevaluationPersistentMeterRewardBonusGame;

#endregion

#region slotmodule overrides

	protected override void OnEnable()
	{
		// Handle playing or restoring animations here when needed so we
		// don't have the visual feature playing default animator states.
		if (currentMeterLevel > 0)
		{
			setBonusTrackerToLevel(currentMeterLevel);
		}
	}

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		allCoroutines = new List<TICoroutine>();

		ModiferExportsPersistentMeterRewardBonusGame modiferExportsPersistentMeterRewardBonusGame = getPersistentMeterRewardFromModifierExports();

		if (modiferExportsPersistentMeterRewardBonusGame == null)
		{
			yield break;
		}

		initializePipState(modiferExportsPersistentMeterRewardBonusGame);
		maxMeterLevel = modiferExportsPersistentMeterRewardBonusGame.maxMeter;
		setBonusTrackerToLevel(modiferExportsPersistentMeterRewardBonusGame.currentMeter);

		if (blockUntilTrackerIsAnimatedAtStart)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
		}

		if (bonusTrackerAnimationCompleteAtStartDelay > 0.0f)
		{
			yield return new WaitForSeconds(bonusTrackerAnimationCompleteAtStartDelay);
		}
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		if (reevaluationPersistentMeterRewardBonusGame.meterReset >= 0)
		{
			setBonusTrackerToLevel(reevaluationPersistentMeterRewardBonusGame.meterReset);
		}
		else
		{
			setBonusTrackerToLevel(reevaluationPersistentMeterRewardBonusGame.currentMeter);
		}

		yield break;
	}

	public override bool needsToExecuteOnShowSlotBaseGame()
	{
		return true;
	}

	public override void executeOnShowSlotBaseGame()
	{
		// this is used when the game is mostly hidden by a bigwin effect or full screen dialog
		setBonusTrackerToLevel(currentMeterLevel);
	}

	public override bool needsToExecuteOnPreBonusGameCreated()
	{
		return reelGame.outcome.hasReevaluations();
	}

	// Play bonus effects and any extra bonus celebration animations here.
	public override IEnumerator executeOnPreBonusGameCreated()
	{
		JSON[] arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();

		foreach (JSON reevalJson in arrayReevaluations)
		{
			string reevalType = reevalJson.getString("type", "");
			if (reevalType != "persistent_meter_reward_milestones" && reevalType != "persistent_meter_reward_milestones_when_mutator_not_trigger")
			{
				continue;
			}
			
			reevaluationPersistentMeterRewardBonusGame = new ReevaluationPersistentMeterRewardBonusGame(reevalJson);
			setBonusTrackerToLevel(reevaluationPersistentMeterRewardBonusGame.currentMeter);
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));

		if (bonusTrackerAnimationCompleteDelay > 0.0f)
		{
			yield return new WaitForSeconds(bonusTrackerAnimationCompleteDelay);
		}
	}

#endregion

#region helper methods

	// This affects how the pips turn on the for the first time. When the game first loads we just want play the
	// on animation when we are just setting up the bonus tracker state.
	private void initializePipState(ModiferExportsPersistentMeterRewardBonusGame modiferExportsPersistentMeterRewardBonusGame)
	{
		for (int i = 0; i < modiferExportsPersistentMeterRewardBonusGame.currentMeter; i++)
		{
			bonusTrackerPips[i].isPipOn = true;
		}
	}

	private ModiferExportsPersistentMeterRewardBonusGame getPersistentMeterRewardFromModifierExports()
	{
		ModiferExportsPersistentMeterRewardBonusGame modiferExportsPersistentMeterRewardBonusGame = null;

		if (reelGame.modifierExports == null)
		{
			return null;
		}

		// get the animation level from server data
		foreach (JSON exportJSON in reelGame.modifierExports)
		{
			string reevalType = exportJSON.getString("type", "");
			if (reevalType == "persistent_meter_reward_milestones" || reevalType == "persistent_meter_reward_milestones_when_mutator_not_trigger")
			{
				modiferExportsPersistentMeterRewardBonusGame = new ModiferExportsPersistentMeterRewardBonusGame(exportJSON);
			}
		}

		return modiferExportsPersistentMeterRewardBonusGame;
	}

	private void setBonusTrackerToLevel(int newMeterLevel)
	{
		if (newMeterLevel >= currentMeterLevel)
		{
			// increase the bonus tracker to the new level
			allCoroutines.Add(StartCoroutine(increaseBonusTrackerToLevel(newMeterLevel)));
		}
		else
		{
			allCoroutines.Add(StartCoroutine(decreaseBonusTrackerToLevel(newMeterLevel)));
		}

		currentMeterLevel = newMeterLevel;
	}

	private IEnumerator increaseBonusTrackerToLevel(int newMeterLevel)
	{
		for (int i = 0; i < newMeterLevel; i++)
		{
			allCoroutines.Add(StartCoroutine(bonusTrackerPips[i].turnPipOn()));
		}

		yield break;
	}

	private IEnumerator decreaseBonusTrackerToLevel(int newMeterLevel)
	{
		for (int i = maxMeterLevel - 1; i >= newMeterLevel; i--)
		{
			allCoroutines.Add(StartCoroutine(bonusTrackerPips[i].turnPipOff()));
		}

		yield break;
	}

	#endregion

#region data classes

	[System.Serializable]
	public class BonusTrackerPip
	{
		[Tooltip("animation to play when a pip is first acquired by winning a bonus game")]
		[SerializeField] private AnimationListController.AnimationInformationList pipAcquiredAnimation;

		[Tooltip("animation to play when a pip is turn back on after returning from a bonus game")]
		[SerializeField] private AnimationListController.AnimationInformationList pipOnAnimation;

		[Tooltip("animation to play when a pip is turned off")]
		[SerializeField] private AnimationListController.AnimationInformationList pipOffAnimation;

		[HideInInspector]
		public bool isPipOn;

		public IEnumerator turnPipOn()
		{
			if (!isPipOn)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(pipAcquiredAnimation));
			}
			else
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(pipOnAnimation));
			}

			isPipOn = true;
		}

		public IEnumerator turnPipOff()
		{
			if (isPipOn)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(pipOffAnimation));
				isPipOn = false;
			}
		}
	}

	public class ModiferExportsPersistentMeterRewardBonusGame
	{
		public string type; //persistent_meter_reward_bonus_game,
		public string meterKey; // meter_key : TBD
		public int currentMeter; // null?
		public int maxMeter; // 14

		public ModiferExportsPersistentMeterRewardBonusGame(JSON exportJson)
		{
			type = exportJson.getString("type", "");
			meterKey = exportJson.getString("meter_key", "");
			currentMeter = exportJson.getInt("current_meter", 0);
			maxMeter = exportJson.getInt("max_meter", 0);
		}
	}

	public class ReevaluationPersistentMeterRewardBonusGame : ReevaluationBase
	{
		public string meterKey;
		public int currentMeter;
		public int meterReset;

		public ReevaluationPersistentMeterRewardBonusGame(JSON reevalJSON) : base(reevalJSON)
		{
			meterKey = reevalJSON.getString("meter_key", "");
			currentMeter = reevalJSON.getInt("current_meter", 0);
			meterReset = reevalJSON.getInt("meter_reset", -1);
		}
	}

#endregion

}
