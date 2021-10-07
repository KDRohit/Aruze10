using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiProgressiveJackpotsFreespinsGame : SlotModule
{
	[SerializeField] private LabelWrapperComponent miniJackpotLabel;
	[SerializeField] private LabelWrapperComponent majorJackpotLabel;
	[SerializeField] private LabelWrapperComponent vipJackpotLabel;

	[SerializeField] private List<Animator> pearlAnimators = new List<Animator>();
	[SerializeField] private ParticleTrailController sparkleTrailToPearlsObject;
	[SerializeField] private ParticleTrailController sparkleTrailToSpinPanelObject;

	[SerializeField] private AnimationListController.AnimationInformationList miniJackpotCelebrationAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList majorJackpotCelebrationAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList vipJackpotCelebrationAnimations;

	[SerializeField] private AnimationListController.AnimationInformationList miniJackpotCelebrationAnimationsOff;
	[SerializeField] private AnimationListController.AnimationInformationList majorJackpotCelebrationAnimationsOff;
	[SerializeField] private AnimationListController.AnimationInformationList vipJackpotCelebrationAnimationsOff;

	[SerializeField] private string miniJackpotSymbolName = "";
	[SerializeField] private string majorJackpotSymbolName = "";

	[SerializeField] private float preRollupWait = 0.5f;
	[SerializeField] private float postRollupWait = 0.5f;
	[SerializeField] private float sparkleTrailStaggerTime = 0.25f;

	[SerializeField] private GameObject lockingEffectPrefab;

	private GameObjectCacher lockEffectCacher = null;
	private Dictionary<string, long> symbolToValue = new Dictionary<string, long>(); //Dictionary that stores the scatter symbols and their associated credit value
	private SlotReel[] reelArray;
	int[] reelIndicesToLock;
	private int currentPearlLandIndex = 0;
	private int currentPearlLockIndex = 0;
	private int reelsStillLocking = 0;
	private bool miniJackpotWon = false;
	private bool majorJackpotWon = false;
	private bool vipjackpotWon = false;
	private long scatterWinnings = 0L;
	private long totalScatterWinnings = 0L;
	private SlotSymbol miniJackpotSymbol = null;
	private SlotSymbol majorJackpotSymbol = null;
	private List<SlotSymbol> scatterSymbols = new List<SlotSymbol>();
	private List<GameObject> sparkleTrailToSpinBoxStarts = new List<GameObject>();


	//Sound and Animation constants
	private const string PEARL_ON_ANIMATION = "on";

	private const string SYMBOL_LOCK_SOUND_PREFIX = "VIPReelLock";
	private const string MINI_LOCK_SOUND = "VIPMiniWinInit";
	private const string MAJOR_LOCK_SOUND = "VIPMajorWinInit";

	private const string SPARKLE_TO_PEARL_ARRIVE = "VIPScatterArriveIncrementPip";

	private const string MINI_ROLLUP_SOUND = "VIPMiniWinRollup";
	private const string MINI_ROLLUP_TERM_SOUND = "VIPMiniWinRollupTerm";

	private const string MAJOR_ROLLUP_SOUND = "VIPMajorWinRollup";
	private const string MAJOR_ROLLUP_TERM_SOUND = "VIPMajorWinRollupTerm";

	private const string GRAND_ROLLUP_SOUND = "VIPJackpotRollup";
	private const string GRAND_ROLLUP_TERM_SOUND = "VIPJackpotRollupTerm";

	private const string SCATTER_ROLLUP_SOUND = "VIPRollupLoop";
	private const string SCATTER_ROLLUP_TERM_SOUND = "VIPRollupTerm";

	private const string MINI_JACKPOT_ROLLUP_FANFARE_SOUND = "VIPAwardMini";
	private const string MAJOR_JACKPOT_ROLLUP_FANFARE_SOUND = "VIPAwardMajor";
	private const string GRAND_JACKPOT_ROLLUP_FANFARE_SOUND = "VIPAwardJackpot";


	public override bool needsToExecuteOnSlotGameStartedNoCoroutine (JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine (JSON reelSetDataJson)
	{
		lockEffectCacher = new GameObjectCacher(this.gameObject, lockingEffectPrefab);
		reelArray = reelGame.engine.getAllSlotReels();
		ProgressiveJackpot.vipRevampMini.registerLabel(miniJackpotLabel.labelWrapper);
		ProgressiveJackpot.vipRevampMajor.registerLabel(majorJackpotLabel.labelWrapper);
		ProgressiveJackpot.vipRevampGrand.registerLabel(vipJackpotLabel.labelWrapper);

		//Set the reelstrips from the outcome
		string[] reelstrips = null;
		JSON[] mutations = FreeSpinGame.instance.freeSpinsOutcomes.entries[0].getArrayReevaluations();
		for (int i = 0; i < mutations.Length; i++)
		{
			if (mutations[i].getString("type", "") == "vip_revamp_mini_game")
			{
				reelstrips = mutations[i].getStringArray("reel_strips");
				setScatterValues(mutations[i].getJSON("symbol_values"));
				reelGame.numberOfFreespinsRemaining = mutations[i].getInt("num_lives", 0);
				reelGame.endlessMode = false;
				BonusSpinPanel.instance.spinCountLabel.text = reelGame.numberOfFreespinsRemaining.ToString();
				break;
			}
		}

		for (int i = 0; i < reelGame.engine.getAllVisibleSymbols().Count; i++)
		{
			if (reelGame.engine.getAllVisibleSymbols()[i].isScatterSymbol)
			{
				executeAfterSymbolSetup(reelGame.engine.getAllVisibleSymbols()[i]);
			}
		}
		if (reelstrips != null)
		{
			for (int i = 0; i < reelArray.Length; i++)
			{
				SlotReel currentReel = reelArray[i];
				ReelStrip newReelStrip = ReelStrip.find(reelstrips[currentReel.getRawReelID()]);
				if (newReelStrip != null)
				{
					reelArray[i].reelData.reelStrip = newReelStrip;
				}
			}
		}
	}

	public override bool needsToExecutePreReelsStopSpinning()
	{
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
		{
			MutationBase mutation = reelGame.mutationManager.mutations[i];

			if (mutation.type == "vip_revamp_mini_game")
			{
				reelIndicesToLock = (mutation as StandardMutation).reels;
			}
		}
		yield break;
	}
	

	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		if (reelIndicesToLock != null)
		{
			return true; 
		}
		return false;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		if (System.Array.IndexOf(reelIndicesToLock, reel.getRawReelID()) >= 0)
		{
			playSymbolLandSound(reel, currentPearlLandIndex);
			currentPearlLandIndex++;
		}
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback ()
	{
		if (reelIndicesToLock != null)
		{
			return true; 
		}
		return false;		
	}

	public override IEnumerator executeOnReelsStoppedCallback ()
	{
		for (int i = 0; i < reelArray.Length; i++)
		{
			SlotReel currentReel = reelArray[i];
			if (System.Array.IndexOf(reelIndicesToLock, currentReel.getRawReelID()) >= 0)
			{
				StartCoroutine(lockSlotReel(currentReel, currentPearlLockIndex));
				currentPearlLockIndex++;
				yield return new TIWaitForSeconds(sparkleTrailStaggerTime);
			}
		}

		while (reelsStillLocking > 0)
		{
			yield return null;
		}

		List<TICoroutine> runningCoroutines = new List<TICoroutine>();

		for (int i = 0; i < sparkleTrailToSpinBoxStarts.Count; i++)
		{
			runningCoroutines.Add(StartCoroutine(sparkleTrailToSpinPanelObject.animateParticleTrail(sparkleTrailToSpinBoxStarts[i].transform.position, BonusSpinPanel.instance.spinCountLabel.gameObject.transform.position, sparkleTrailToSpinPanelObject.gameObject.transform)));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
		reelGame.numberOfFreespinsRemaining+= sparkleTrailToSpinBoxStarts.Count;
		sparkleTrailToSpinBoxStarts.Clear();
		yield return new TIWaitForSeconds(preRollupWait);

		if (currentPearlLockIndex >= pearlAnimators.Count)
		{				
			vipjackpotWon = true;
			reelGame.numberOfFreespinsRemaining = 0; //Need to just end the game early if we've filled the board
		}

		yield return StartCoroutine(rollupAllWins());


		if (reelGame.numberOfFreespinsRemaining == 0) //Once we have no spins left, roll everything up
		{
			VIPTokenCollectionModule.scatterWinnings = totalScatterWinnings;
			BonusGamePresenter.instance.currentPayout = 0;
		}

	}

	private IEnumerator rollupAllWins()
	{
		if (vipjackpotWon)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(vipJackpotCelebrationAnimations));
			yield return StartCoroutine(rollupWinnings(VIPTokenCollectionModule.grandJpValue, GRAND_ROLLUP_SOUND, GRAND_ROLLUP_TERM_SOUND, GRAND_JACKPOT_ROLLUP_FANFARE_SOUND));
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(vipJackpotCelebrationAnimationsOff));

		}

		if (majorJackpotWon)
		{
			majorJackpotSymbol.animateOutcome();
			yield return StartCoroutine(sparkleTrailToSpinPanelObject.animateParticleTrail(majorJackpotSymbol.transform.position, majorJackpotLabel.gameObject.transform.position, sparkleTrailToSpinPanelObject.gameObject.transform));

			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(majorJackpotCelebrationAnimations));
			yield return StartCoroutine(rollupWinnings(VIPTokenCollectionModule.majorJpValue, MAJOR_ROLLUP_SOUND, MAJOR_ROLLUP_TERM_SOUND, MAJOR_JACKPOT_ROLLUP_FANFARE_SOUND));
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(majorJackpotCelebrationAnimationsOff));

		}

		if (miniJackpotWon)
		{
			miniJackpotSymbol.animateOutcome();
			yield return StartCoroutine(sparkleTrailToSpinPanelObject.animateParticleTrail(miniJackpotSymbol.transform.position, miniJackpotLabel.gameObject.transform.position, sparkleTrailToSpinPanelObject.gameObject.transform));

			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(miniJackpotCelebrationAnimations));
			yield return StartCoroutine(rollupWinnings(VIPTokenCollectionModule.miniJpValue, MINI_ROLLUP_SOUND, MINI_ROLLUP_TERM_SOUND, MINI_JACKPOT_ROLLUP_FANFARE_SOUND));
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(miniJackpotCelebrationAnimationsOff));
		}

		if (scatterWinnings > 0)
		{
			for (int i = 0; i < scatterSymbols.Count; i++)
			{
				scatterSymbols[i].animateOutcome();
			}
			yield return StartCoroutine(rollupWinnings(scatterWinnings, SCATTER_ROLLUP_SOUND, SCATTER_ROLLUP_TERM_SOUND));
			scatterSymbols.Clear();
		}

		totalScatterWinnings += scatterWinnings;
		scatterWinnings = 0L;
		vipjackpotWon = false;
		majorJackpotWon = false;
		miniJackpotWon = false;
	}


	private IEnumerator rollupWinnings(long creditsAwarded, string rollupLoopSound, string rollupTermSound, string rollupFanfareSound = "")
	{
		long currentWinnings = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += creditsAwarded;
		if (!rollupFanfareSound.IsNullOrWhiteSpace())
		{
			Audio.play(rollupFanfareSound);
		}
		yield return StartCoroutine(SlotUtils.rollup(	
			currentWinnings, 
			BonusGamePresenter.instance.currentPayout, 
			BonusSpinPanel.instance.winningsAmountLabel, 
			true, 
			2.0f,
			true,
			false,
			rollupLoopSound,
			rollupTermSound));

		yield return new TIWaitForSeconds(postRollupWait);
	}

	private void playSymbolLandSound(SlotReel reelToLock, int pearlLandIndex)
	{	
		SlotSymbol stickySymbol = reelToLock.visibleSymbols[0];
		string stickySymbolName = stickySymbol.serverName;
		if (!stickySymbol.isBlankSymbol)
		{
			string lockSound = SYMBOL_LOCK_SOUND_PREFIX + (pearlLandIndex + 1).ToString();

			if (stickySymbol.isBonusSymbol)
			{
				if (stickySymbol.serverName == miniJackpotSymbolName)
				{
					lockSound = MINI_LOCK_SOUND;
				}
				else if (stickySymbol.serverName == majorJackpotSymbolName)
				{
					lockSound = MAJOR_LOCK_SOUND;
				}
			}
			Audio.play(lockSound);			
		}
	}

	private IEnumerator lockSlotReel(SlotReel reelToLock, int pearlLockIndex)
	{
		reelsStillLocking++;
		SlotSymbol stickySymbol = reelToLock.visibleSymbols[0];
		string stickySymbolName = stickySymbol.serverName;
		if (!stickySymbol.isBlankSymbol)
		{
			if (stickySymbol.isBonusSymbol)
			{
				if (stickySymbol.serverName == miniJackpotSymbolName)
				{
					miniJackpotSymbol = stickySymbol;
					miniJackpotWon = true;
				}
				else if (stickySymbol.serverName == majorJackpotSymbolName)
				{
					majorJackpotSymbol = stickySymbol;
					majorJackpotWon = true;
				}
			}

			if (stickySymbol.isScatterSymbol)
			{
				scatterWinnings += symbolToValue[stickySymbolName];
				scatterSymbols.Add(stickySymbol);
			}
						
			GameObject lockEffect = lockEffectCacher.getInstance();
			lockEffect.transform.position = stickySymbol.transform.position;
			lockEffect.transform.parent = stickySymbol.transform.parent;
			lockEffect.SetActive(true);
			AudioListController.AudioInformation arriveSound = new AudioListController.AudioInformation(SPARKLE_TO_PEARL_ARRIVE + (pearlLockIndex + 1).ToString());

			sparkleTrailToPearlsObject.addArrivalSoundToList(arriveSound);
			yield return StartCoroutine(sparkleTrailToPearlsObject.animateParticleTrail(stickySymbol.gameObject.transform.position, pearlAnimators[pearlLockIndex].gameObject.transform.position, sparkleTrailToPearlsObject.gameObject.transform));
			sparkleTrailToPearlsObject.removeArrivalSound(arriveSound);

			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pearlAnimators[pearlLockIndex], PEARL_ON_ANIMATION));
			sparkleTrailToSpinBoxStarts.Add(pearlAnimators[pearlLockIndex].gameObject);
			reelToLock.isLocked = true;
			lockEffectCacher.releaseInstance(lockEffect);
		}
		reelsStillLocking--;
	}

	private void setScatterValues(JSON scatterData)
	{
		List<string> keyList = scatterData.getKeyList();
		for (int i = 0; i < keyList.Count; i++)
		{
			symbolToValue.Add(keyList[i], scatterData.getLong(keyList[i], 0L));
		}
	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		if (symbol.isScatterSymbol)
		{
			return true;
		}
		return false;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (symbolToValue.Count > 0)
		{
			//Only set the label on Scatter symbols that are in our dictionary. 
			//If its a Scatter symbol without a credits value then it's the Scatter that awards the jackpot.
			long symbolCreditValue = 0;
			if (symbolToValue.TryGetValue(symbol.serverName, out symbolCreditValue))
			{
				SymbolAnimator symbolAnimator = symbol.getAnimator();
				if (symbolAnimator != null)
				{
					LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();

					if (symbolLabel != null)
					{
						symbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(symbolCreditValue, 2, false);
					}
				}
			}
		}
	}
}
