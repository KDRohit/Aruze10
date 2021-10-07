using Com.HitItRich.EUE;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelUpSummaryScreen : MonoBehaviour
{
	[SerializeField] private GameObject multiplierRoot;
	[SerializeField] private TextMeshPro xpMultLabel;
	[SerializeField] private TextMeshPro multDescLabel;
	[SerializeField] private TextMeshPro levelLabel;
	[SerializeField] private TextMeshPro creditsAwardLabel;
	[SerializeField] private TextMeshPro vipPointsLabel;
	[SerializeField] private ButtonHandler closeButton;
	[SerializeField] private ButtonHandler collectButton;
	[SerializeField] private GameObject nextLevelTeaserParent;
	[SerializeField] private TextMeshPro nextLevelLabel;
	[SerializeField] private TextMeshPro ctaButtonLabel;
	[SerializeField] private GameObject starTrailTarget;
	[SerializeField] private GameObject richParent;
	
	private bool showNextLevelTeaser = false;

	public UIAnchor[] anchors;
	public Animator summaryAnimator;

	private LevelUpOverlay overlayParent = null;
	private LevelUpSummaryDialog dialogParent = null;

	private bool isAnimatingOut = false;
	private int level;
	private long awardAmount;
	private GameObject richCharacterPrefab;
	
	private const string EVEN_LEVEL_LOC_KEY = "xp_level_bonus_even";
	private const string ODD_LEVEL_LOC_KEY = "xp_level_bonus_odd";
	private const string XP_MULT_LOC_KEY = "{0}X";
	private const string LEVEL_UP_TEASER_SOUND = "AddTeaserLevelup";

	public void init(int newLevel, long creditsAmount, int vipPointsAmount, bool _showNextLevelTeaser = false, LevelUpOverlay parent = null, LevelUpSummaryDialog dialog = null)
	{
		level = newLevel;
		awardAmount = creditsAmount;
		showNextLevelTeaser = _showNextLevelTeaser;
		if (parent != null)
		{
			closeButton.gameObject.SetActive(false);
		}
		else if (dialog != null)
		{
			ctaButtonLabel.text = "Great";
			closeButton.registerEventDelegate(closeClicked);
		}
		collectButton.registerEventDelegate(closeClicked);

		levelLabel.text = newLevel.ToString();
		creditsAwardLabel.text = CreditsEconomy.convertCredits(creditsAmount);
		vipPointsLabel.text = vipPointsAmount > 1 ? string.Format("+{0} VIP Points", vipPointsAmount) : string.Format("+{0} VIP Point", vipPointsAmount);
		overlayParent = parent;
		dialogParent = dialog;
		if (showNextLevelTeaser)
		{
			nextLevelTeaserParent.SetActive(true);
			nextLevelLabel.text = (newLevel + 1).ToString();
		}

		bool levelUpBonusActive = LevelUpBonus.isBonusActive && LevelUpBonus.doesLevelMatch(newLevel);
		multiplierRoot.SetActive(levelUpBonusActive);
		if (levelUpBonusActive)
		{
			xpMultLabel.text = Localize.text(XP_MULT_LOC_KEY, LevelUpBonus.multiplier);
			multDescLabel.text = newLevel % 2 == 0 ? Localize.text(EVEN_LEVEL_LOC_KEY) : Localize.text(ODD_LEVEL_LOC_KEY);
		}
		
		if (ExperimentWrapper.EueFtue.isInExperiment)
		{
			if (level == EUEManager.RICH_TIP_LEVEL)
			{
				AssetBundleManager.load(EUEManager.CHARACTER_ITEM_PREFAB_PATH, onLoadEUECharacter, onLoadAssetFailure, isSkippingMapping:true, fileExtension:".prefab");
				StatsManager.Instance.LogCount("game_actions", "machine_ftue", "step", "", "level_up_" + EUEManager.RICH_TIP_LEVEL, "view");
			}
			else if (level == ExperimentWrapper.EueFtue.maxLevel)
			{
				StatsManager.Instance.LogCount("game_actions", "machine_ftue", "step", "", "level_up_" + ExperimentWrapper.EueFtue.maxLevel, "view");
			}
		}

		//hide feature ui during level up
		if (SpinPanel.instance != null)
		{
			SpinPanel.instance.showFeatureUI(false);	
		}
	}

	private void onLoadEUECharacter(string assetPath, object loadedObj, Dict data = null)
	{
		if (loadedObj != null)
		{
			//now load the animator
			richCharacterPrefab = loadedObj as GameObject;
			AssetBundleManager.load(EUEManager.LEVEL_UP_ANIMATION_PATH, onLoadEUEAnimation, onLoadAssetFailure, isSkippingMapping:true, fileExtension:".controller");	
		}
		else
		{
			Debug.LogError(string.Format("invalid resource: {0}", assetPath));
		}
	}
	
	private void onLoadEUEAnimation(string assetPath, object loadedObj, Dict data = null)
	{
		RuntimeAnimatorController controller = loadedObj as RuntimeAnimatorController;
		if (controller == null)
		{
			Debug.LogError("Could not load animation");
			return;
		}

		StatsManager.Instance.LogCount("game_actions", "machine_ftue", "", "", "level_up_2", "view");
		
		GameObject obj = CommonGameObject.instantiate(richCharacterPrefab, richParent.transform) as GameObject;
		if (obj != null)
		{
			//set to same layer as container
			CommonGameObject.setLayerRecursively(obj, this.gameObject.layer);
			
			//disable dialog script (as we don't use it here)
			EueFtueRichDialog dialog = obj.GetComponent<EueFtueRichDialog>();
			if (dialog != null)
			{
				Destroy(dialog);
			}
			
			//play animation on character animator
			EUECharacterItem character = obj.GetComponentInChildren<EUECharacterItem>();
			if (character != null)
			{
				Audio.play(EueFtueRichDialog.LEVEL_UP_SOUND);
				character.setText("ftue_level_up");
				character.animator.runtimeAnimatorController = controller;
				character.animator.Play("Intro");
			}
		}
	}
	
	private static void onLoadAssetFailure(string assetPath, Dict data = null)
	{
		Debug.LogError(string.Format("Failed to load asset at {0}", assetPath));
	}

	public float animateOut()
	{
		bool isIncreasingInflation = SlotsPlayer.instance.currentBuyPageInflationPercentIncrease > 0;
		StartCoroutine(playSummaryAnimation(isIncreasingInflation));
		return isIncreasingInflation ? LevelUpSummaryDialog.specialOutroAnimLength : LevelUpSummaryDialog.normalOutroAnimLength;
	}

	private IEnumerator playSummaryAnimation(bool isIncreasingInflation)
	{
		if (!isAnimatingOut)
		{
			string animName = isIncreasingInflation ? "special outro" : "normal outro";
			isAnimatingOut = true;
			yield return CommonAnimation.playAnimAndWait(summaryAnimator, animName);
		}
	}

	public void closeClicked(Dict args = null)
	{
		if (ExperimentWrapper.EueFtue.isInExperiment)
		{
			if (level == EUEManager.RICH_TIP_LEVEL)
			{
				StatsManager.Instance.LogCount("game_actions", "machine_ftue", "step", "", "level_up_" + EUEManager.RICH_TIP_LEVEL, "click");
			}
			else if (level == ExperimentWrapper.EueFtue.maxLevel)
			{
				StatsManager.Instance.LogCount("game_actions", "machine_ftue", "step", "", "level_up_" + ExperimentWrapper.EueFtue.maxLevel, "click");
			}
		}
		
		if (args != null)
		{
			if(args.containsKey(D.OPTION))
			{
				// Log skip, since the collect button actually starts out as a skip
				StatsManager.Instance.LogCount("dialog", "level_up", level.ToString(), "", "skip", "click", awardAmount);
			}
			else
			{
				StatsManager.Instance.LogCount("dialog", "level_up", level.ToString(), "", "collect", "click", awardAmount);
			}
		}
		
		closeButton.SetActive(false);
		collectButton.gameObject.SetActive(false);
		if (overlayParent != null)
		{
			Vector3 overlayCoinTransform = Overlay.instance.topV2.coinAnchor.position;
			starTrailTarget.transform.position = new Vector3(overlayCoinTransform.x, overlayCoinTransform.y, starTrailTarget.transform.position.z);
			overlayParent.close(); 
			
			//Overlay outro animation will re-enable feature ui on it's outro so there is no overlap, no need to manually force it here
		}
		else
		{
			if (dialogParent != null)
			{
				Dialog.close();
			}
			
			//overlay outro code won't run here so just show the feature ui again manually
			if (SpinPanel.instance != null)
			{
				SpinPanel.instance.showFeatureUI(true);	
			}	
		}

		// We need to set the overlay state back
		if (Overlay.instance != null && Overlay.instance.topV2 != null && Overlay.instance.topV2.xpUI != null)
		{
			Overlay.instance.topV2.xpUI.checkEventStates();
		}
		
		// Launch the eue termination dialog if they're at the final level
		if (ExperimentWrapper.EueFtue.isInExperiment && level == ExperimentWrapper.EueFtue.maxLevel)
		{
			EUEManager.showEndPresentation();
		}
	}
	
	public void playSound(string clipName)
	{
		if (clipName != LEVEL_UP_TEASER_SOUND || showNextLevelTeaser)
		{
			Audio.play(clipName);
		}
	}
}
