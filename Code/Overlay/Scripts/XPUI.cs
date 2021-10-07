using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls display and behavior of the XP meter and button on the overlay.
*/

public class XPUI : MonoBehaviour
{
	private const float MOVE_XP_MULTIPLIER_OUT_OF_THE_WAY = 2400.0f;

	// TBD rename these once we are 100% lobby v2
	public enum State
	{
		DEFAULT,				// The default state of the xp bar.
		SPECIAL, 				// For everything, except max level
		MAX_LEVEL,				// When the user is at the maximum level
		NONE					// Should never be the active state.
	}

	// We only want one of these and the rest will be synced up to it/use it.
	public UISprite xpMeter; 	// This should be setup to use a transparent meter sizer that all other meter versions match the width of.
	public Transform xpMeterBG; // This should be setup to use the normal black xp bar bg that is usually displayed.
	public ParticleSystem xpMeterEdgeEmitter;
	public UISprite levelMeterEdgeGlow;
	public TextMeshPro levelLabel;
	public Material specialStrokeMaterial;
	public Material defaultStrokeMaterial;
	public TextMeshPro[] specialStrokeTexts;
	public ClickHandler xpClickHandler;
	public UIAnchor xpClickAnchor;
	public UIAnchor spriteMaskAnchor;

	public GameObject passiveLevelUpFXTemplate;
	public GameObject powerupEffects;
	public GameObject powerupEffectsGlow;
	public ScaleGroup scaleGroup = new ScaleGroup();
	public UIAnchor leftAnchor;

	// This is just for inspector linking, we populate a dictionary at Awake().
	// Thus I am going to make it private so no one accidently uses it outside of the class.
	[SerializeField] private XPUIState[] linkedStates; 
	public XPUIState currentState;

	public GameObject starObject;

	public GameObject levelUpFxContainer;
	public GameObject passiveLevelUpContainer;

	private int lastPercent = 0;
	private float lastProgress = 0;
	private float edgeEmitterShutOffDelay = 0;
	private float xpMeterInset = 0.0f;	// The different in width of the xp meter compared to the frame it's in.
	private int lastLevelUpSpinCount = 0;
	protected float newXPMeterWidth = -1;
	private float lastLevelUpFlare = 0;	// Helps space out the flare if more than one level up happens at the same time.
	private Dictionary<State, XPUIState> allStates = new Dictionary<State, XPUIState>(); // All the possible states that are setup.

	[HideInInspector] public bool isWaitingForClickProcessing = false;

	public bool force3x;
	public bool force2x;
	public bool forceOdd;
	public bool forceBonus;
	public bool forceMax;

	public event System.Action xpUpdated;

	public static XPUI instance = null;

	private const float EDGE_GLOW_DURATION = .75f;
	private const int METER_STAR_SIZE = 85;

	protected virtual float tweenDuration
	{
		get { return 1.0f; }
	}
	
	protected virtual iTween.EaseType tweenEaseType
	{
		get { return iTween.EaseType.linear; }
	}
	
	protected virtual float edgeFadeDuration
	{
		get { return 0.0f; }
	}

	void Awake()
	{
		instance = this;
		UIStretch meterStretch = xpMeter.GetComponent<UIStretch>();
		if (meterStretch != null)
		{
			xpMeterInset = meterStretch.pixelOffset.x;
			// If there is a UIStretch component on the xpMeter, then we should remove it.
			Destroy(meterStretch);
		}
		
		foreach (XPUIState uiState in linkedStates)
		{
			// Adding all the states that are linked to the prefab.
			allStates.Add(uiState.state, uiState);
			uiState.init();
		}
		setState(State.DEFAULT);
		powerupEffects.SetActive(false);

		checkEventStates();
		checkPowerupActive();
 
		// Set width to 0 initially so the meters grow when starting up instead of shrinking.
		CommonTransform.setWidth(xpMeter.transform, 0);

		levelMeterEdgeGlow.alpha = 0.0f;

		updateXP();

		registerXpMultiplierEvents();

		xpClickHandler.registerEventDelegate(onClickHandler);
	}

	private void onClickHandler(Dict dict = null)
	{
		xpClickHandler.unregisterEventDelegate(onClickHandler);
		ExperienceLevelData nextLevelData = ExperienceLevelData.find(SlotsPlayer.instance.socialMember.experienceLevel + 1);
		if (nextLevelData != null)
		{
			Dict args = Dict.create(D.KEY, LevelUpUserExperienceToaster.PRESENTATION_TYPE.INFO);
			ToasterManager.addToaster(ToasterType.LEVEL_UP, args, null, 0.0f, onToasterClosed);
		}
	}

	public void onToasterClosed()
	{
		xpClickHandler.registerEventDelegate(onClickHandler);
	}

	private void registerXpMultiplierEvents()
	{
		// Remove first just in case.
		XPMultiplierEvent.instance.onEnabledEvent -= checkEventStates;
		XPMultiplierEvent.instance.onDisabledEvent -= checkEventStates;

		XPMultiplierEvent.instance.onEnabledEvent += checkEventStates;
		XPMultiplierEvent.instance.onDisabledEvent += checkEventStates;
		PowerupsManager.addEventHandler(onPowerupActivated);
	}

	private void onPowerupActivated(PowerupBase powerup)
	{
		checkPowerupActive();
	}

	private void checkPowerupActive()
	{
		if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_TRIPLE_XP_KEY) ||
		    PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_ODD_LEVELS_KEY) ||
			PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_EVEN_LEVELS_KEY) ||
		    PowerupsManager.hasActivePowerupByName(PowerupBase.LEVEL_LOTTO_TRIPLE_XP_KEY))
		{
			setState(State.SPECIAL);

			if (powerupEffects != null)
			{
				powerupEffects.SetActive(true);
			}

			if (powerupEffectsGlow != null)
			{
				powerupEffectsGlow.SetActive(true);

				UIAnchor powerupGlowAnchor = powerupEffectsGlow.GetComponent<UIAnchor>();

				if (powerupGlowAnchor != null)
				{
					powerupGlowAnchor.enabled = true;
				}
			}
		}
	}

	// Sets that state of the XPBar.
	public void setState(State state)
	{
		if (!allStates.ContainsKey(state))
		{
			Debug.LogError("Accessing a state that has not been set up.");
			return;
		}

		XPUIState newState = allStates[state];
		
		if (newState == currentState)
		{
			currentState.updateActiveState();
			return;
		}

		if (currentState != null)
		{
			currentState.setActive(false, newState); // Disable the current state if it exists.
		}
		// Set our current state variables
		currentState = newState;
		currentState.setActive(true);
		currentState.levelLabel.text = levelLabel.text; // In case the current state has an overide, set that too.

		scaleGroup.clear();
		scaleGroup.add(currentState.xpMeterBG.transform);
		scaleGroup.add(currentState.xpMeter.transform);
		scaleGroup.add(powerupEffectsGlow.transform);

		if (currentState.xpMeterGlow != null)
		{
			scaleGroup.add(currentState.xpMeterGlow);
		}

		xpMeter = currentState.xpMeter;
		updateMaterials();

		leftAnchor.enabled = true;
		currentState.xpMeterAnchor.enabled = true;
		xpClickAnchor.enabled = true;

		if (currentState.xpMeterGlowAnchor != null)
		{
			currentState.xpMeterGlowAnchor.enabled = true;

			UIStretch glowStretch = currentState.xpMeterGlowAnchor.gameObject.GetComponent<UIStretch>();

			if (glowStretch != null)
			{
				glowStretch.enabled = true;
			}
		}
	}

	private void updateMaterials()
	{
		for (int i = 0; i < specialStrokeTexts.Length; ++i)
		{
			TextMeshPro text = specialStrokeTexts[i];
			text.fontMaterial = currentState.state == State.SPECIAL ? specialStrokeMaterial : defaultStrokeMaterial;
		}
	}

	public void onItemAddedToOverlay(float itemWidth)
	{
		if (scaleGroup != null)
		{
			scaleGroup.adjustWidthBy(-itemWidth);
			leftAnchor.enabled = true;
			currentState.xpMeterAnchor.enabled = true;
			xpClickAnchor.enabled = true;
			spriteMaskAnchor.enabled = true;
		}
	}

	public void onItemRemovedFromOverlay(float itemWidth)
	{
		if (scaleGroup != null)
		{
			scaleGroup.adjustWidthBy(itemWidth);
			leftAnchor.enabled = true;
			currentState.xpMeterAnchor.enabled = true;
			xpClickAnchor.enabled = true;
			spriteMaskAnchor.enabled = true;
		}
	}

	// Goes through all the states and checks checks whether each is valid or not, and set the state accordingly.
	public void checkEventStates()
	{
		if
		(
			!(SlotsPlayer.instance.isMaxLevel || forceMax) &&
			(XPMultiplierEvent.instance.isEnabled ||
		    force3x ||
		    force2x ||
		    LevelUpBonus.isBonusActive ||
		    forceOdd ||
		    forceBonus)
		)
		{
			setState(State.SPECIAL);
		}
		else if (SlotsPlayer.instance.isMaxLevel || forceMax)
		{
			// Shows the MAX LEVEL notification if at the max level and xp multiplier feature isn't on.
			// It was strangely decided to have the xp multiplier override the display of MAX LEVEL,
			// even though no more xp can be gained. If this decision is changed, then move this to the top of the switch statement.
			setState(State.MAX_LEVEL);
			powerupEffects.SetActive(false);
			powerupEffectsGlow.SetActive(false);
		}
		else
		{
			setState(State.DEFAULT);
			powerupEffects.SetActive(false);
			powerupEffectsGlow.SetActive(false);
		}
	}
	
	// This is intentionally lower case since it gets updated by the OverlayTop.update() call.
	public void update()
	{
		if (currentState != null)
		{
			currentState.update();

#if DEBUG
			if (force3x || force2x||  forceMax || forceOdd || forceBonus)
			{
				checkEventStates();
			}
#endif

			if (currentState.state == State.SPECIAL)
			{
				// If showing for more than 3 spins, disable level_up state and swap back to the proper one.
				checkEventStates();
			}
		}
		
		handleLevelUp();
	}

	// Update the xp meter whenever xp is gained, and when the level up dialog is closed.
	public void updateXP()
	{
		if (SlotsPlayer.instance == null || SlotsPlayer.instance.xp == null || GameState.giftedBonus != null)
		{
			return;
		}

		if (xpUpdated != null)
		{
			xpUpdated();
		}
		// If a tween was already happening, cancel it first.
		iTween.Stop(xpMeter.gameObject);

		float oldWidth = newXPMeterWidth;

		if (newXPMeterWidth != -1 && oldWidth < newXPMeterWidth)
		{
			// Only jump to the old width if it's less, so we don't see the bar
			// jump up before going down if we need to fix XP from a multiplier issue.
			CommonTransform.setWidth(xpMeter.transform, oldWidth);
		}

		// The minimum width is > 0 because some Android devices apparently have
		// problems displaying sprites properly when the scale is 0 in at least one axis.
		newXPMeterWidth = Mathf.Max(2.0f, getTargetXpBarWidth());

		//Debug.Log(string.Format("New XP: {0}, width: {1}", SlotsPlayer.instance.xp.amount, newXPMeterWidth));

		float progress = getLevelProgress();
		int percent = (int)(progress * 100.0f);
		edgeEmitterShutOffDelay = 0.0f;

		if (newXPMeterWidth < oldWidth || SlotsPlayer.instance.isMaxLevel || lastProgress == progress)
		{
			// If resetting after a level up, or at max level, or at the same percent as last update, don't tween.
			// The progress thing is important because the width of the meter may change, forcing an update,
			// even though the progress percent hasn't changed.
			CommonTransform.setWidth(xpMeter.transform, newXPMeterWidth);
			finishXpTween();
		}
		else
		{
			if (percent == lastPercent && lastProgress != progress)
			{
				// bar will not actually resize and tween will end right away, yet we still want edge glow, so make glow last a little while
				edgeEmitterShutOffDelay = EDGE_GLOW_DURATION;
			}
			iTween.ScaleTo(xpMeter.gameObject, iTween.Hash("x", newXPMeterWidth, "time", tweenDuration, "onupdatetarget", gameObject, "onupdate", "updateXpTween", "oncompletetarget", gameObject, "oncomplete", "finishXpTween", "easetype", tweenEaseType));
			playEdgeEmitter();
		}

		lastPercent = percent;
		lastProgress = progress;
	}

	private void playEdgeEmitter()
	{
		float pos = xpMeter.transform.localPosition.x + xpMeter.transform.localScale.x - xpMeterEdgeEmitter.transform.localScale.x / 2f;
		CommonTransform.setX(levelMeterEdgeGlow.transform, pos);

		// Enable the sparkly emitter that follows the animated edge of the meter.
		xpMeterEdgeEmitter.Play();
		levelMeterEdgeGlow.alpha = 1.0f;
	}

	private IEnumerator stopEdgeEmitter(float delay)
	{
		yield return new WaitForSeconds(delay);

		xpMeterEdgeEmitter.Stop();
		
		if (edgeFadeDuration > 0.0f)
		{
			TweenAlpha.Begin(levelMeterEdgeGlow.gameObject, edgeFadeDuration, 0.0f);
		}
		else
		{
			levelMeterEdgeGlow.alpha = 0.0f;
		}
	}

	// Returns the width that the xp meter should be at the current xp amount.
	private float getTargetXpBarWidth()
	{
		// Get crazy with the nullchecks due to some weird NRE in this function. HIR-20819
		if (SlotsPlayer.instance == null)
		{
			Debug.LogError("XPUI.getTargetXpBarWidth(): SlotsPlayer.instance is null.");
			return 0.0f;
		}

		if (SlotsPlayer.instance.socialMember == null)
		{
			Debug.LogError("XPUI.getTargetXpBarWidth(): SlotsPlayer.instance.socialMember is null.");
			return 0.0f;
		}

		if (SlotsPlayer.instance.xp == null)
		{
			Debug.LogError("XPUI.getTargetXpBarWidth(): SlotsPlayer.instance.xp is null.");
			return 0.0f;
		}
		
		ExperienceLevelData currentLevel = ExperienceLevelData.find(SlotsPlayer.instance.socialMember.experienceLevel);
	
		if (currentLevel == null)
		{
			Debug.LogError("XPUI.getTargetXpBarWidth(): currentLevel is null for " + SlotsPlayer.instance.socialMember.experienceLevel);
			return 0.0f;
		}

		if (levelLabel == null)
		{
			Debug.LogError("XPUI.getTargetXpBarWidth(): levelLabel is null.");
			return 0.0f;
		}

		if (currentState == null)
		{
			Bugsnag.LeaveBreadcrumb("XPUI.getTargetXpBarWidth(): currentstate is null.");
			return 0.0f;
		}

		if (xpMeterBG == null)
		{
			Bugsnag.LeaveBreadcrumb("XPUI.getTargetXpBarWidth(): xpMeterBG is null.");
			return 0.0f;
		}
		
		if (!currentState.textCycler.isPlaying)
		{
			levelLabel.text = CommonText.formatNumber(currentLevel.level);
			currentState.levelLabel.text = levelLabel.text; // In case the current state has an overide, set that too.
		}
		
		if (SlotsPlayer.instance.isMaxLevel)
		{
			return fullMeterWidth;
		}

		float lvlProgress = getLevelProgress(currentLevel);

		if (Mathf.Round(lvlProgress * xpMeterBG.transform.localScale.x) < 0)
		{
			Debug.LogError("Level progress is below 0, which should never happen! Figure out the source of the issue!");
			return 0.0f;
		}
		else
		{
			return Mathf.Floor(lvlProgress * fullMeterWidth);
		}
	}
	
	protected float fullMeterWidth
	{
		get { return xpMeterBG.transform.localScale.x + xpMeterInset; }
	}
	
	private float getLevelProgress(ExperienceLevelData currentLevel = null)//
	{
		if (currentLevel == null)
		{
			currentLevel = ExperienceLevelData.find(SlotsPlayer.instance.socialMember.experienceLevel);
		}

		float lvlProgress = 1f;

		if (currentLevel.level < ExperienceLevelData.maxLevel)
		{
			ExperienceLevelData newlevel = ExperienceLevelData.find(currentLevel.level + 1);
			
			if (newlevel == null)
			{
				Debug.LogError("XPUI.getTargetXpBarWidth(): newlevel is null for " + (currentLevel.level + 1));
				return 0.0f;
			}

			lvlProgress = ((float)(SlotsPlayer.instance.xp.amount - currentLevel.requiredXp) / (newlevel.requiredXp - currentLevel.requiredXp));
			lvlProgress = Mathf.Min(lvlProgress, 1f);	// Just in case the player has enough xp for the next level, but hasn't leveled up yet.

			if (lvlProgress < 0)
			{
				lvlProgress = 0;
			}
		}
		return lvlProgress;
	}

	// Keep the xp meter edge in sync with the meter width.
	public void updateXpTween()
	{
		float pos = xpMeter.transform.localPosition.x + xpMeter.transform.localScale.x - xpMeterEdgeEmitter.transform.localScale.x / 2f;
		CommonTransform.setX(levelMeterEdgeGlow.transform, pos);
		CommonTransform.setX(xpMeterEdgeEmitter.transform, pos);

		float d = xpMeter.transform.localScale.x / newXPMeterWidth;
		float finalWidth = getTargetXpBarWidth();

		updateMeterSize(finalWidth * d);
	}

	public void finishXpTween()
	{
		if (gameObject != null && gameObject.activeSelf)
		{
			StartCoroutine(stopEdgeEmitter(edgeEmitterShutOffDelay));
			updateMeterSize();
		}
	}

	public void updateMeterSize(float value = -1)
	{
		float finalWidth = value < 0 ? getTargetXpBarWidth() : value;
		// Sync up all the meters.
		foreach(XPUIState state in allStates.Values)
		{
			if (state.xpMeter != null)
			{
				CommonTransform.setWidth(state.xpMeter.transform, finalWidth);
			}
		}
	}

	// Checks if there was a level up event and handles it.
	// Doing it here allows us to handle multi-levelups in a single dialog.
	public void handleLevelUp()
	{
		if (LevelUpDialog.eventNewLevel == 0)
		{
			// No level ups to handle.
			// Check for a select game unlock dialog.
			if (SelectGameUnlockDialog.readyToShowDialog)
			{
				SelectGameUnlockDialog.showQueuedDialog();
			}
			return;
		}

		int newLevel = LevelUpDialog.eventNewLevel;
		
		if (newLevel > SlotsPlayer.instance.socialMember.experienceLevel)
		{
			StatsManager.Instance.LogMileStone("level_up", newLevel);

			int bonusVipForLevel;
			long bonusCreditsForLevel;
			int oldLevel = SlotsPlayer.instance.socialMember.experienceLevel;
			LevelUpDialog.getLevelUpBonuses(oldLevel, newLevel, out bonusCreditsForLevel, out bonusVipForLevel);

			levelUpPassive(newLevel);

			if (Overlay.instance != null && Overlay.instance.topHIR != null)
			{
				Overlay.instance.topHIR.showLevelUpAnimation(bonusCreditsForLevel, bonusVipForLevel);
			}			
		}
		else
		{
			Debug.LogWarning(string.Format("LevelUpDialog - discarding duplicate leveled_up message for level {0}, already at {1}.", newLevel, SlotsPlayer.instance.socialMember.experienceLevel));
		}

		// Clear it to prevent using this function again for the same level.
		LevelUpDialog.eventNewLevel = 0;
	}

	// Do a passive level up. This would get called once per level up when in passive mode.
	public void levelUpPassive(int newLevel)
	{
		// Only show the "LEVEL UP!" text for 3 spins then hide it if the player doesn't touch it first.
		lastLevelUpSpinCount = GameExperience.totalSpinCount;
		
		int oldLevel = SlotsPlayer.instance.socialMember.experienceLevel;
		LevelUpDialog.passiveLevelUps += (newLevel - SlotsPlayer.instance.socialMember.experienceLevel);
		
		// Get bonus amounts for the levels attained since the old level (could be more than one level).
		long bonusCredits = 0;
		int bonusVIPPoints = 0;

		if (SlotsPlayer.instance.socialMember.experienceLevel < Glb.MAX_VOLTAGE_MIN_LEVEL && newLevel >= Glb.MAX_VOLTAGE_MIN_LEVEL)
		{
			MaxVoltageDialog.showDialog("max_voltage_unlock");
		}
		
		LevelUpDialog.getLevelUpBonuses(oldLevel, newLevel, out bonusCredits, out bonusVIPPoints);
		
		// Apply the new level immediately when passive.
		// These bonuses are NOT applied on the dialog when finally viewed.
		LevelUpDialog.applyLevelUp(oldLevel, newLevel, bonusCredits, bonusVIPPoints);

		if (!ExperimentWrapper.RepriceLevelUpSequence.isInExperiment || LevelUpUserExperienceFeature.instance.isEnabled)
		{
			StartCoroutine(levelUpPassiveFX());
		}
	}

	// Add some visual flare to the passive level up. It's not COMPLETELY passive!
	private IEnumerator levelUpPassiveFX()
	{
		// Enforce a half-second cooldown period between level up effects.
		while (Time.realtimeSinceStartup - lastLevelUpFlare < .5)
		{
			yield return null;
		}
		lastLevelUpFlare = Time.realtimeSinceStartup;
		
		GameObject go = CommonGameObject.instantiate(passiveLevelUpFXTemplate) as GameObject;
		
		// We can't parent to the xpMeter because that gets scaled,
		// and we want to keep the effect at the normal aspect ratio.
		go.transform.parent = passiveLevelUpContainer.transform;
		go.transform.localScale = Vector3.one;
		// Position in front of the xpMeter.
		go.transform.position = xpMeter.transform.position;
		go.transform.localPosition = Vector3.zero;
		CommonTransform.setZ(go.transform, go.transform.localPosition.z - 10.0f);
		
		PassiveLevelUpFX fx = go.GetComponent<PassiveLevelUpFX>();
		
		yield return StartCoroutine(fx.doEffect());
	}

	private void OnDestroy()
	{
		PowerupsManager.removeEventHandler(onPowerupActivated);
	}
}
