using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BattleBarPip
{
	public AnimationListController.AnimationInformation turnOnAnimation;
	public AnimationListController.AnimationInformation turnOffAnimation;
}

//This module is for games that have an end condition based on marking off pips 
public class BattleMultiplierModule : SlotModule
{
	[Header("Battle Multiplier Mutation")]	
	private string battleMutationKey = "free_spin_battle_multiplier";
	//A list for each pip.  Since each pip needs to store the on and off version of itself
	public List<BattleBarPip> battlePips;
	//These will play on a bad pip or loss
	public AnimationListController.AnimationInformationList badBackgroundEffects;
	//These will play on a good pip
	public AnimationListController.AnimationInformationList goodBackgroundEffects;

	[Header("Jackpot Values")]
	public LabelWrapper jackpotLabel;
	public AnimationListController.AnimationInformation jackpotAnimationController;
	[SerializeField] private float JACKPOT_DRAIN_TIME = 2.0f;
	//Art wanted a a little bit of linger time
	[SerializeField] private float POST_JACKPOT_DRAIN_TIME = 0.5f;
	[SerializeField] private string ROLLUP_JACKPOT_SOUND;
	[SerializeField] private string ROLLUP_TERM_JACKPOT_SOUND;
	public ParticleTrailController jackpotParticleTrail;

	//The tracked mutation
	private StandardMutation battleMultiplierMutation;

	//keeps track of the strike that need to be turned on/off
	private int strikeCount = 0;	

	//Locally stores the game multiplier
	private long gameMultiplier;

	//Store the displayed jackpot
	private long displayedJackpot;	
	private bool setJackpot = true;

	public override void Awake()
	{
		base.Awake();

		if (GameState.giftedBonus != null)
		{
			gameMultiplier = GiftedSpinsVipMultiplier.playerMultiplier;
		}
		else
		{
			gameMultiplier = SlotBaseGame.instance.multiplier;
		}
	}
		
	//Set the Initial jackpot value form the mutation
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return setJackpot;
	}
	public override IEnumerator executePreReelsStopSpinning()
	{		 
		battleMultiplierMutation = null;
		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				if (baseMutation.type == battleMutationKey)
				{
					battleMultiplierMutation = baseMutation as StandardMutation;
				}
			}
		}
		yield return StartCoroutine(SlotUtils.rollup(displayedJackpot, (battleMultiplierMutation.freeSpinBattleMutliplierData.currentJackpot * gameMultiplier), jackpotLabel.tmPro, false));
		displayedJackpot = battleMultiplierMutation.freeSpinBattleMutliplierData.currentJackpot * gameMultiplier;
		setJackpot = false;
	}

	//Do the Good and Bad battle strikes
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		battleMultiplierMutation = null;

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{				
				if (baseMutation.type == battleMutationKey)
				{
					battleMultiplierMutation = baseMutation as StandardMutation;
				}
			}
		}

		return (battleMultiplierMutation != null);
	}
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		//Do Battle Multiplier
		if (battleMultiplierMutation.freeSpinBattleMutliplierData != null)
		{
			if (battleMultiplierMutation.freeSpinBattleMutliplierData.hit == "bad")
			{
				if (strikeCount < battlePips.Count)
				{				
					if (battlePips[strikeCount].turnOnAnimation != null)
					{
						//use IsBlockingModule if you want for the particle trail to wait for the background animations
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(badBackgroundEffects));
						yield return StartCoroutine(AnimationListController.playAnimationInformation(battlePips[strikeCount].turnOnAnimation));
					}
					strikeCount++;
				}
			}
			else if (battleMultiplierMutation.freeSpinBattleMutliplierData.hit == "good")
			{
				if (strikeCount > 0)
				{
					strikeCount--;					
					if (battlePips[strikeCount].turnOffAnimation != null)
					{
						//use IsBlockingModule if you want for the particle trail to wait for the background animations						
						yield return StartCoroutine(AnimationListController.playAnimationInformation(battlePips[strikeCount].turnOffAnimation));
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(goodBackgroundEffects));
						//this will increase the displayed jackpot on a good hit						
						yield return StartCoroutine(SlotUtils.rollup(displayedJackpot,																				 //start value
																	(battleMultiplierMutation.freeSpinBattleMutliplierData.currentJackpot * gameMultiplier),         //end value
																	jackpotLabel.tmPro,																				 //label to update													
																	true,																							 //play audio
																	JACKPOT_DRAIN_TIME,
																	true,
																	true,
																	ROLLUP_JACKPOT_SOUND,
																	ROLLUP_TERM_JACKPOT_SOUND));

						
						displayedJackpot = battleMultiplierMutation.freeSpinBattleMutliplierData.currentJackpot * gameMultiplier;
					}
				}
			}			
		}		
	}

	//Once the Final paylines are finished handle the jackpot drain and winnings rollup
	public override bool needsToExecuteAfterPaylines()
	{
		battleMultiplierMutation = null;

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				if (baseMutation.type == battleMutationKey)
				{
					battleMultiplierMutation = baseMutation as StandardMutation;
				}
			}
		}

		return (battleMultiplierMutation != null && battleMultiplierMutation.freeSpinBattleMutliplierData.battleResult == "loss");
	}
	public override IEnumerator executeAfterPaylinesCallback(bool winsShown)
	{
		if (battleMultiplierMutation.freeSpinBattleMutliplierData.battleResult == "loss")
		{
			reelGame.endlessMode = false;
			reelGame.numberOfFreespinsRemaining = 0;
			if (jackpotAnimationController != null)
			{
				//use IsBlockingModule if you want for the particle trail to wait for the jackpot animations
				yield return StartCoroutine(AnimationListController.playAnimationInformation(jackpotAnimationController));
				//Wait until the particle reaches the winnings position to do the rollup
				yield return StartCoroutine(jackpotParticleTrail.animateParticleTrail(SpinPanel.instance.bonusWinningsObjectTransform.position, jackpotParticleTrail.transform.parent));
				//Do the roll up of the winnings and the drain of the jackpot
				yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout,								//start value
															(BonusGamePresenter.instance.currentPayout + displayedJackpot),			//end value
															new LabelWrapper(BonusSpinPanel.instance.winningsAmountLabel),			//label to update
															drainJackpotValue,														//callback 
															true,																	//play audio
															JACKPOT_DRAIN_TIME														//time override
															));

				//Post roll up delay to give it a bit of a pause before the bonus summary
				yield return new TIWaitForSeconds(POST_JACKPOT_DRAIN_TIME);
			}
			BonusGamePresenter.instance.currentPayout += (battleMultiplierMutation.freeSpinBattleMutliplierData.currentJackpot * gameMultiplier);
		}
	}

	//Rollup callback function, will drain the jackpot
	private void drainJackpotValue(long value)
	{	
		jackpotLabel.text = CommonText.formatNumber(displayedJackpot - (value - BonusGamePresenter.instance.currentPayout));
	}
}
