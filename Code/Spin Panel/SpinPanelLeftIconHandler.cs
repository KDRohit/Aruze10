using System;
using System.Collections;
using TMPro;
using QuestForTheChest;
using UnityEngine;

public class SpinPanelLeftIconHandler : MonoBehaviour
{
	// Robust Challenges Icon animation parameters.
	private const float CYCLE_TIME = 10.0f;
	private const float SHAKE_DURATION = 0.4f;
	private const float SCALE_AMOUNT = 1.6f;
	private const float BEFORE_SURFACE_BOX = 1.5f;
	private const float SHAKE_DEGREE = 45f;
	private const int COUNT_DOWN = 300;
	private const float CHALLENGE_COMPLETE_DISPLAY_TIME = 5.0f;
	
	[SerializeField] private bool crossFade = false;
	[SerializeField] private float displayTime = 5.0f;
	[SerializeField] private GameObject collectionsPanelParent;
	[SerializeField] private GameObject qfcMessageBoxParent;
	[SerializeField] private GameObject qfcPanelButtonParent;
	[SerializeField] protected GameObject xInYChallengeParent;
	[SerializeField] private TextMeshPro robustChallengesSlideOnMessageLabel;
	[SerializeField] private GameObject robustChallengesDefaultMessageBG;
	[SerializeField] private GameObject robustChallengesResetMessageBG;
	[SerializeField] private GameObject robustChallengesParent;
	[SerializeField] private Animator challengesMessageAnimator;
	[SerializeField] private TextMeshPro messageLabel;
	[SerializeField] private Animator messageBoxAnimator;
	
	public RobustChallengesInGameCounter robustChallengesInGameCounter { get; private set; }
	public XInYSpinPanelIcon xInYSpinPanelIcon { get; private set; }
	public TextMeshPro robustChallengesMessegeBoxLabel { get; private set; }
	
	public QFCSpinPanelMessageBox qfcSpinPanelMessageBox { get; private set; }
	public QFCSpinPanelButton qfcButton { get; private set; }
	
	private bool shouldDisplayQfcButton;
	private bool shouldDisplayRobustChallengeButton;
	
	private GameTimer iconCooldownTimer = null;
	private bool animateChecklist = false;
	private string animationMessage = "";
	private bool isLoadingRobustPrefab = false;
	private int bundleFails = 0;
	private bool robustPresentationComplete = false;
	private bool isWaitingForChallengePresentation = false;
	private float fadeTimer;

	private bool multipleButtonsEnabled
	{
		get
		{
			int count = 0;
			
			if (shouldDisplayQfcButton)
			{
				++count;
			}

			if (shouldDisplayRobustChallengeButton)
			{
				++count;
			}

			return count > 1;
		}
	}
	
	public static RobustCampaign robustCampaign
	{
		get
		{
			return CampaignDirector.robust;
		}
	}
	
	private void swapSpinPanelIcon(string message, bool animate, float time)
	{
		iconCooldownTimer = new GameTimer(time);
		animateChecklist = animate;
		animationMessage = message;
	}
	
	// Show or hide Robust Challenges in-game icon
	public void showRobustChallengesInGame(bool shouldShow)
	{
		shouldDisplayRobustChallengeButton = shouldShow;
		toggleRobustButton(shouldShow);
		if (shouldShow && !isLoadingRobustPrefab && robustCampaign != null)
		{
			Objective activeXinYObjective = null;
			if (robustCampaign.currentMission != null)
			{
				for (int i = 0; i < robustCampaign.currentMission.objectives.Count; ++i)
				{
					Objective obj = robustCampaign.currentMission.objectives[i];
					if (!obj.isComplete &&
						obj.type == XinYObjective.X_COINS_IN_Y &&
						(string.IsNullOrEmpty(obj.game) || obj.game == GameState.game.keyName))
					{
						activeXinYObjective = obj;
						break;
					}
				}
			}
			
			//Create Private robust challenges button object
			//Load it here
			//Set the label objects or just call the ones on the button prefab
			if(robustCampaign.currentMission != null &&
				(iconCooldownTimer == null || iconCooldownTimer.isExpired) &&
				!isWaitingForChallengePresentation &&
				activeXinYObjective != null)
			{
				if (robustChallengesInGameCounter != null && robustChallengesInGameCounter.gameObject != null)
				{
					Destroy(robustChallengesInGameCounter.gameObject);
					robustChallengesInGameCounter = null;
				}
				
				if (xInYSpinPanelIcon == null)
				{
					createCoinsOverSpinsIcon();
				}
				else
				{
					xInYSpinPanelIcon.updateChallengesProgress(robustCampaign);
				}
			}
			else if (robustChallengesInGameCounter == null)
			{
				if (xInYSpinPanelIcon != null && xInYSpinPanelIcon.gameObject != null)
				{
					Destroy(xInYSpinPanelIcon.gameObject);	
				}
				xInYSpinPanelIcon = null;
				createRobustChallengesCounter();
			}
		}
	}

	private void Awake()
	{
		if (QuestForTheChestFeature.instance != null && QuestForTheChestFeature.instance.isEnabled)
		{
			shouldDisplayQfcButton = true;
			AssetBundleManager.load(this, "Features/Quest for the Chest/Prefabs/Instanced Prefabs/Spin Panel/Quest for the Chest In Game Panel Icon", qfcPanelButtonLoadSuccess, qfcPanelButtonLoadFailed);
			AssetBundleManager.load(this, "Features/Quest for the Chest/Prefabs/Instanced Prefabs/Spin Panel/Quest for the Chest Spin Panel Message Box", qfcMessageBoxLoadSuccess, qfcMessageBoxLoadFailed);
		}
	}
	
	private void qfcMessageBoxLoadSuccess(string assetPath, UnityEngine.Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			GameObject qfcBetMessageBoxObject = NGUITools.AddChild(qfcMessageBoxParent, obj as GameObject, true);
			qfcSpinPanelMessageBox = qfcBetMessageBoxObject.GetComponent<QFCSpinPanelMessageBox>();
			if (qfcSpinPanelMessageBox != null)
			{
				qfcSpinPanelMessageBox.init();
			}		
		}
	}

	private void qfcMessageBoxLoadFailed(string assetPath, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("Failed to load the Quest for the Chest Bet Message Box: " + assetPath);
	}
	
	private void qfcPanelButtonLoadSuccess(string assetPath, UnityEngine.Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			GameObject qfcPanelButtonObject = NGUITools.AddChild(qfcPanelButtonParent, obj as GameObject, true);
			qfcButton = qfcPanelButtonObject.GetComponent<QFCSpinPanelButton>();
			if (qfcButton != null)
			{
				qfcButton.init();
			}
		}
	}

	private void qfcPanelButtonLoadFailed(string assetPath, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("Failed to load the Quest for the Chest Panel Button: " + assetPath);
	}

	private void createCoinsOverSpinsIcon()
	{
		//load the individual challenge meter
		isLoadingRobustPrefab = true;
		string prefabPath = "Features/Robust Challenges/Prefabs/Coins Over Spins Challenge Meter";
		AssetBundleManager.load(this, prefabPath, robustChallengesObjectiveLoadSuccess, robustChallengesObjectiveLoadFailure);
	}

	private void createRobustChallengesCounter()
	{
		//load the objective counter
		isLoadingRobustPrefab = true;
		string prefabPath = "Features/Robust Challenges/Prefabs/Robust Challenge Spin Panel Button V2";
		AssetBundleManager.load(this, prefabPath, robustChallengesCounterLoadSuccess, robustChallengesCounterLoadFailure);
	}

	private void robustChallengesCounterLoadSuccess(string path, UnityEngine.Object obj, Dict args = null)
	{
		isLoadingRobustPrefab = false;
		if (robustChallengesParent != null)
		{
			GameObject prefab = obj as GameObject;
			GameObject go = NGUITools.AddChild(robustChallengesParent, prefab);
			robustChallengesInGameCounter = go.GetComponent<RobustChallengesInGameCounter>();
			if (robustChallengesInGameCounter != null)
			{
				robustChallengesInGameCounter.updateChallengesProgress(robustCampaign, animateChecklist);

				if (animateChecklist)
				{
					showRobustChallengesMessage(animationMessage, false);
				}
			}
		}
		else
		{
			Debug.LogWarning("SpinPanelHIR::robustChallengesCounterLoadSuccess - Attempted to create the challenges UI with no anchor. Is this a custom prefab?");
		}

		animateChecklist = false;
		animationMessage = "";
	}
	
	public void showRobustChallengesMessage(string message, bool isFailure)
	{
		StartCoroutine(playRobustChallengesMessageHelper(message, isFailure));
	}

	private void robustChallengesCounterLoadFailure(string path, Dict args = null)
	{
		isLoadingRobustPrefab = false;
		animateChecklist = false;
		animationMessage = "";
		bundleFails++;
		Debug.LogErrorFormat("SpinPanelHIR.cs -- RobustChallengesCounterLoadFailure -- failed to load prefab at path: {0}", path);
	}

	private void robustChallengesObjectiveLoadSuccess(string path, UnityEngine.Object obj, Dict args = null)
	{
		isLoadingRobustPrefab = false;
		if (xInYChallengeParent != null)
		{
			GameObject prefab = obj as GameObject;
			GameObject go = NGUITools.AddChild(xInYChallengeParent, prefab);
			xInYSpinPanelIcon = go.GetComponent<XInYSpinPanelIcon>();
			if (xInYSpinPanelIcon != null)
			{
				xInYSpinPanelIcon.updateChallengesProgress(robustCampaign);
			}
		}
		else
		{
			Debug.LogWarning("SpinPanelHIR::robustChallengesCounterLoadSuccess - Attempted to create the challenges UI with no anchor. Is this a custom prefab?");
		}
	}

	private void robustChallengesObjectiveLoadFailure(string path, Dict args = null)
	{
		isLoadingRobustPrefab = false;
		bundleFails++;
		Debug.LogErrorFormat("SpinPanelHIR.cs -- RobustChallengesCounterLoadFailure -- failed to load prefab at path: {0}", path);
	}

	private void Update()
	{
		
		// Update the robust challenge icon.
		if ((robustChallengesParent.activeSelf || xInYChallengeParent.activeSelf) && bundleFails <= AssetBundleManager.MAX_RETRY_COUNT)
		{
			if (((iconCooldownTimer == null || iconCooldownTimer.isExpired) && !RobustCampaign.hasActiveRobustCampaignInstance && !isWaitingForChallengePresentation) ||
				(ReelGame.activeGame != null && ReelGame.activeGame.hasFreespinsSpinsRemaining))
			{
				if (!RobustCampaign.hasActiveRobustCampaignInstance && !robustPresentationComplete)
				{
					isWaitingForChallengePresentation = true;
				}
				else
				{
					showRobustChallengesInGame(false);
				}
				return;
			}
			else if (!robustPresentationComplete && 
					 (iconCooldownTimer == null || iconCooldownTimer.isExpired) && 
					 !RobustCampaign.hasActiveRobustCampaignInstance)
			{
				robustPresentationComplete = true;
				return;
			}
			//swap back to clipboard icon if we're not an x in y challenge
			if (xInYSpinPanelIcon != null &&
				(robustCampaign.currentMission == null || 
				 (iconCooldownTimer != null && !iconCooldownTimer.isExpired) ||
				 (robustCampaign.currentMission != null &&
					(robustCampaign.currentMission.currentObjective == null || robustCampaign.currentMission.currentObjective.type != XinYObjective.X_COINS_IN_Y))))
			{
				showRobustChallengesInGame(true);
			}
			else if (iconCooldownTimer != null && iconCooldownTimer.isExpired)
			{
				iconCooldownTimer = null;
				showRobustChallengesInGame(true);
			}

			// Show the timer in last 5 mins.
			// Only showing the timer in the final minutes if there is actually time left and we haven't completed the campaign
			if (robustCampaign != null)
			{
				if (!robustCampaign.isComplete && robustCampaign.timerRange != null && !robustCampaign.timerRange.isExpired)
				{
					showRobustChallengesInGame(true);
					if (robustChallengesInGameCounter != null && robustCampaign.timerRange.timeRemaining < COUNT_DOWN)
					{
						robustChallengesInGameCounter.showTimeRemaining(robustCampaign);
					}
				}
				else if (!robustCampaign.isComplete)
				{
					showRobustChallengesInGame(false);
				}
			}
			else
			{
				showRobustChallengesInGame(false);
			}
		}

		if (crossFade)
		{
			fadeTimer += Time.deltaTime;
			if (multipleButtonsEnabled)
			{
				int mode = ((int)(fadeTimer / CYCLE_TIME)) % 2;
				switch (mode)
				{
					case 0:
						toggleQfcButton(false);
						toggleRobustButton(true);
						break;
				
					case 1:
						toggleQfcButton(true);
						toggleRobustButton(false);
						break;
				}   
			}
		}
	}
	
	private IEnumerator playRobustChallengesMessageHelper(string message, bool isFailure)
	{
		robustCampaign.playAudio("ToastGoalComplete");

		slideInChallengesMessage(message, isFailure);
		
		// Icon animation.
		if (robustChallengesInGameCounter == null)
		{
			if (xInYSpinPanelIcon != null && !isFailure)
			{
				swapSpinPanelIcon(message, true, CHALLENGE_COMPLETE_DISPLAY_TIME);
			}
			yield break;
		}

		yield return StartCoroutine(doRobustChallengeCounterAnimation(message, isFailure));


	}
	
	public void slideInChallengesMessage(string localizedMessage, bool useFailBackground)
	{
		isWaitingForChallengePresentation = false;
		if (challengesMessageAnimator != null && !challengesMessageAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.on"))
		{
			robustChallengesSlideOnMessageLabel.text = localizedMessage;
			robustChallengesDefaultMessageBG.SetActive(!useFailBackground);
			robustChallengesResetMessageBG.SetActive(useFailBackground);
			challengesMessageAnimator.Play("on");
		}
	}

	public void refreshUI(bool playXinYResetAnim)
	{
		if (robustChallengesInGameCounter != null)
		{
			robustChallengesInGameCounter.updateChallengesProgress(robustCampaign);
		}
		

		if (xInYSpinPanelIcon != null)
		{
			if (playXinYResetAnim)
			{
				xInYSpinPanelIcon.playResetAnimation();
			}
			xInYSpinPanelIcon.updateChallengesProgress(robustCampaign);
		}
	}

	public void resetUI(string campaignID)
	{
		if (robustCampaign != null && robustCampaign.campaignID == campaignID)
		{
			refreshUI(true);
		}
	}

	private IEnumerator doRobustChallengeCounterAnimation(string message, bool isFailure)
	{
		//if we completed a goal, show the checkmark
		if (!isFailure)
		{
			robustChallengesInGameCounter.robustChallengesButton.button.UpdateColor(true, true);

			// If the icon is in count down, don't show the check mark.
			if (robustCampaign != null && robustCampaign.timerRange != null && robustCampaign.timerRange.timeRemaining > COUNT_DOWN)
			{
				robustChallengesInGameCounter.toggleCheckmark(true);
			}
		}

		if (robustChallengesInGameCounter != null)
		{
			robustChallengesInGameCounter.updateChallengesProgress(CampaignDirector.robust, !isFailure);
			robustChallengesInGameCounter.enableUpdates(false);
			
			if (robustChallengesInGameCounter.counterAnimator != null)
			{
				robustChallengesInGameCounter.counterAnimator.Play("on");
			}
		}

		GameObject objectToShake = robustChallengesInGameCounter.robustChallengesButton.transform.parent.gameObject;
		if (objectToShake == null)
		{
			yield break;
		}
		
		float originalScale = objectToShake.transform.localScale.x;

		iTween.ScaleTo(objectToShake, iTween.Hash("x", SCALE_AMOUNT, "y", SCALE_AMOUNT, "time", SHAKE_DURATION, "islocal", true, "easetype", iTween.EaseType.easeInCubic));
		iTween.RotateTo(objectToShake, iTween.Hash("z", SHAKE_DEGREE, "time", SHAKE_DURATION, "islocal", true, "easetype", iTween.EaseType.easeInCubic));
		yield return new WaitForSeconds(SHAKE_DURATION);

		if (objectToShake == null)
		{
			yield break;
		}
			
		iTween.RotateTo(objectToShake, iTween.Hash("z", -SHAKE_DEGREE, "time", SHAKE_DURATION, "islocal", true, "easetype", iTween.EaseType.linear));
		yield return new WaitForSeconds(SHAKE_DURATION);

		if (objectToShake == null)
		{
			yield break;
		}
		iTween.ScaleTo(objectToShake, iTween.Hash("x", originalScale, "y", originalScale, "time", SHAKE_DURATION, "islocal", true, "easetype", iTween.EaseType.easeOutCubic));
		iTween.RotateTo(objectToShake, iTween.Hash("z", 0f, "time", SHAKE_DURATION, "islocal", true, "easetype", iTween.EaseType.easeOutCubic));

		// Black box animation.
		if (robustChallengesMessegeBoxLabel != null)
		{
			robustChallengesMessegeBoxLabel.text = message;
			robustChallengesMessegeBoxLabel.transform.parent.gameObject.SetActive(true);
			messageLabel.transform.parent.gameObject.SetActive(false);
			iTween.RotateFrom(robustChallengesMessegeBoxLabel.transform.parent.gameObject, iTween.Hash("x", -90f, "time", BEFORE_SURFACE_BOX, "islocal", true, "easetype", iTween.EaseType.easeOutQuad));
			yield return new WaitForSeconds(2 * BEFORE_SURFACE_BOX);
			
			iTween.RotateTo(robustChallengesMessegeBoxLabel.transform.parent.gameObject, iTween.Hash("x", 90f, "time", BEFORE_SURFACE_BOX, "islocal", true, "easetype", iTween.EaseType.easeOutQuad));
			yield return new WaitForSeconds(BEFORE_SURFACE_BOX);
			
			robustChallengesMessegeBoxLabel.transform.parent.gameObject.SetActive(false);
		}
		messageLabel.transform.parent.gameObject.SetActive(true);
		if (robustChallengesInGameCounter != null)
		{
			robustChallengesInGameCounter.toggleCheckmark(false);
			robustChallengesInGameCounter.enableUpdates(true);
			robustChallengesInGameCounter.updateChallengesProgress(CampaignDirector.robust);
		}
	}

	private void toggleQfcButton(bool enabled)
	{
		SafeSet.gameObjectActive(qfcPanelButtonParent, enabled);
	}

	private void toggleRobustButton(bool enabled)
	{
		SafeSet.gameObjectActive(robustChallengesParent, enabled);
		SafeSet.gameObjectActive(xInYChallengeParent, enabled);
	}
	
	public void hideCoinsOverSpinIcon()
	{
		if (xInYSpinPanelIcon != null)
		{
			//remove coins over spin icon
			Destroy(xInYSpinPanelIcon.gameObject);
			xInYSpinPanelIcon = null;
			
			// Show Robust Challenges icon if there are more challenges
			bool isRobustChallengesGame = RobustCampaign.hasActiveRobustCampaignInstance && CampaignDirector.robust.currentMission != null;
			showRobustChallengesInGame(isRobustChallengesGame);	
		}
	}
}
	
