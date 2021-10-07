using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Crusher pick bonus for the t101 slot
*/
public class Crusher : ChallengeGame 
{
	private enum CrusherJackpotEnum
	{
		ZyngaCoin = 0,
		Kyle,
		Sarah,
		Arnold,
		Terminator
	}

	private const float BAR_TWEEN_TIME = 1.0f;			// Time it will take for the bar to tween in and out for the win celebration
	private const float COIN_TWEEN_TIME = 0.65f;		// Time it will take for the coin to reach it's target
	private const float CRUSHER_TWEEN_TIME = 0.5f;		// Time the crusher takes to descend
	private const float TIME_BETWEEN_REVEALS = 0.5f;	// Time between each reveal

	private static readonly int[] CRUSHER_Y_COORDS = { 700, 500, 200 };	// Set positions that the crusher uses to descend by
	private static readonly string[] REVEAL_SPRITE_NAMES = { "T1common_pickcoin_M", "T1commonDialogue_kyle_M", "T1commonDialogue_sarah_M", "T1commonDialogue_arnold_M", "T1commonDialogue_t1000_M" }; // Sprite names for the reveals
	private static readonly string[] ROBOT_SPRITE_NAMES = { "T1common_terminator1_M", "T1common_terminator2_M", "T1common_terminator3_M" }; // Sprite names for the different crushed robot states

	[SerializeField] private GameObject[] buttonSelections;				// List of all the buttons in this pickem
	private List<GameObject> enabledButtons = new List<GameObject>();	// List to track which of the buttons are still enabled to be clicked/revealed to
	[SerializeField] private GameObject[] buttonPickMeAnimations;		// Animations to attract the player to pick this object

	[SerializeField] private GameObject[] terminatorPips;				// Visual display for how many terminator pips are found
	[SerializeField] private GameObject[] arnoldPips;					// Visual display for how many arnold pips are found
	[SerializeField] private GameObject[] sarahPips;					// Visual display for how many sarah pips are found
	[SerializeField] private GameObject[] kylePips;						// Visual display for how many kyle pips are found
	[SerializeField] private GameObject[] zyngaPips;					// Visual display for how many zynga pips are found
	[SerializeField] private UILabel[] progressiveLabels;				// Labels that tell the values of collecting a set of portraits -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent[] progressiveLabelsWrapperComponent;				// Labels that tell the values of collecting a set of portraits

	public List<LabelWrapper> progressiveLabelsWrapper
	{
		get
		{
			if (_progressiveLabelsWrapper == null)
			{
				_progressiveLabelsWrapper = new List<LabelWrapper>();

				if (progressiveLabelsWrapperComponent != null && progressiveLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in progressiveLabelsWrapperComponent)
					{
						_progressiveLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in progressiveLabels)
					{
						_progressiveLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _progressiveLabelsWrapper;
		}
	}
	private List<LabelWrapper> _progressiveLabelsWrapper = null;	
	
	
	[SerializeField] private GameObject winPopup;						// The win background that appears when the portrait and value reach the middle of the screen
	[SerializeField] private Animation winboxLightningAnimation;		// Lighitng effect that appears behind the win box during the rollup

	[SerializeField] private GameObject crusher;						// GameObject of the Crusher that descends to crush the robot
	[SerializeField] private UISprite robotJunk;						// Sprite for the robot that is being crushed
	[SerializeField] private Animation crusherLightningAnimation;		// Lighting effect that appears around the crusher when it descends
	private int robotStateIndex = 0;									// Tracks what stage the visual robot is in

	[SerializeField] private UILabel winLabel;							// Label that displays the amount the player won -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent winLabelWrapperComponent;							// Label that displays the amount the player won

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	
	[SerializeField] private GameObject instructionLabel;				// Text for instructions on what to do in the game
	[SerializeField] private GameObject coinSpinSparkleTrail;			// Sparkle effect that tweens to the acquired pip

	[SerializeField] private GameObject terminatorValueBar = null;		// The bar for the value which is won if you find the terminator symbols
	[SerializeField] private GameObject arnoldValueBar = null;			// The bar for the value which is won if you find the arnold symbols
	[SerializeField] private GameObject sarahValueBar = null;			// The bar for the value which is won if you find the sarah symbols
	[SerializeField] private GameObject kyleValueBar = null;			// The bar for the value which is won if you find the kyle symbols
	[SerializeField] private GameObject zyngaValueBar = null;			// The bar for the value which is won if you find the coin symbols

	[SerializeField] private GameObject crusherAreaSkull = null;		// Skull that sits in the crusher area
	[SerializeField] private GameObject terminatorWinSkull = null;		// Skull that shows up for the terminator win, hidden and appears when the crusher skull tweens to where it is
	
	private PickemOutcome _outcome;											// Stores the outcome for this bonus game
	private CrusherGameInfo[] crusherGameInfo = new CrusherGameInfo[5];	// Stores info about the different progressive elements of this bonus game
	private SkippableWait revealWait = new SkippableWait();

	private static readonly string PAYTABLE_NAME = "t1_common_t2";
	/*
	From the backend the Progressives come in as:
		"P5" : Terminator Skeleton Head : highest progressive 
		"P4" : Arnold
		"P3" : Sara Connor
		"P2" : Kyle Reese : lowest progressive
		"P1" : Spooky Coin  : coin bonus
	 */
	public override void init() 
	{
		_outcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		
		JSON[] progressivePools = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.

		if(progressivePools != null && progressivePools.Length > 0)
		{
			crusherGameInfo[(int)CrusherJackpotEnum.Terminator] = 
				new CrusherGameInfo("P5", 3, 0, progressivePools[0].getString("key_name", ""), "terminator", terminatorPips, terminatorValueBar, 0); 
			crusherGameInfo[(int)CrusherJackpotEnum.Arnold] = 
				new CrusherGameInfo("P4", 2, 0, progressivePools[1].getString("key_name", ""), "arnold", arnoldPips, arnoldValueBar, 0);
			crusherGameInfo[(int)CrusherJackpotEnum.Sarah] = 
				new CrusherGameInfo("P3", 2, 0, progressivePools[2].getString("key_name", ""), "sarah", sarahPips, sarahValueBar, 0);
			crusherGameInfo[(int)CrusherJackpotEnum.Kyle] = 
				new CrusherGameInfo("P2", 2, 0, progressivePools[3].getString("key_name", ""), "kyle", kylePips, kyleValueBar, 0);
			crusherGameInfo[(int)CrusherJackpotEnum.ZyngaCoin] = 
				new CrusherGameInfo("P1", 2, 0, progressivePools[4].getString("key_name", ""), "zynga", zyngaPips, zyngaValueBar, 0);
			
			for (int i = 0; i < progressiveLabelsWrapper.Count; i++)
			{
				long val = SlotsPlayer.instance.progressivePools.getPoolCredits(crusherGameInfo[i].progressiveName, SlotBaseGame.instance.multiplier, false);
				progressiveLabelsWrapper[i].text = CommonText.formatNumber(val);
				
				crusherGameInfo[i].progressiveAmount = val;
			}

		}
		else
		{
			crusherGameInfo[(int)CrusherJackpotEnum.Terminator] = 
				new CrusherGameInfo("P5", 3, 0, "", "terminator", terminatorPips, terminatorValueBar, 
				                    BonusGamePaytable.getBasePayoutCreditsForPaytable("pickem", PAYTABLE_NAME, "P5") * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier);
			crusherGameInfo[(int)CrusherJackpotEnum.Arnold] = 
				new CrusherGameInfo("P4", 2, 0, "", "arnold", arnoldPips, arnoldValueBar, 
				                    BonusGamePaytable.getBasePayoutCreditsForPaytable("pickem", PAYTABLE_NAME, "P4") * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier);
			crusherGameInfo[(int)CrusherJackpotEnum.Sarah] = 
				new CrusherGameInfo("P3", 2, 0, "", "sarah", sarahPips, sarahValueBar, 
				                    BonusGamePaytable.getBasePayoutCreditsForPaytable("pickem", PAYTABLE_NAME, "P3") * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier);
			crusherGameInfo[(int)CrusherJackpotEnum.Kyle] = 
				new CrusherGameInfo("P2", 2, 0, "", "kyle", kylePips, kyleValueBar, 
				                    BonusGamePaytable.getBasePayoutCreditsForPaytable("pickem", PAYTABLE_NAME, "P2") * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier);
			crusherGameInfo[(int)CrusherJackpotEnum.ZyngaCoin] = 
				new CrusherGameInfo("P1", 2, 0, "", "zynga", zyngaPips, zyngaValueBar, 
				                    BonusGamePaytable.getBasePayoutCreditsForPaytable("pickem", PAYTABLE_NAME, "P1") * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier);
			
			for (int i = 0; i < progressiveLabelsWrapper.Count; i++)
			{
				progressiveLabelsWrapper[i].text = CreditsEconomy.convertCredits(crusherGameInfo[i].basePayoutCredits);		
				crusherGameInfo[i].progressiveAmount = crusherGameInfo[i].basePayoutCredits;
			}
		}
		BonusGamePresenter.instance.useMultiplier = false;
		
		Audio.switchMusicKey(Audio.soundMap("progressive_idle"));
		Audio.stopMusic();

		foreach (GameObject go in buttonPickMeAnimations)
		{
			go.GetComponent<Animation>()["T1_CommonBonus_Pickme_sheen_ani"].normalizedTime = Random.Range(0f, 1f);
		}

		// store out all buttons as enabled buttons
		for (int i = 0; i < buttonSelections.Length; i++)
		{
			enabledButtons.Add(buttonSelections[i]);
		}

		_didInit = true;
	}
	
	/**
	Change the crusher position with effects and update the robot visual
	*/
	private IEnumerator changeRobotCrusher()
	{
		if (robotStateIndex < ROBOT_SPRITE_NAMES.Length)
		{
			crusherLightningAnimation.gameObject.SetActive(true);
			crusherLightningAnimation.Play();

			float time = CRUSHER_TWEEN_TIME;
			
			PlayingAudio playing = null;
			playing = Audio.play("MetalCrusherPt1");
			
			if (playing != null)
			{
				time = playing.audioInfo.clip.length;
			}
			
			Vector3 endPos = crusher.transform.localPosition;
			endPos.y = CRUSHER_Y_COORDS[robotStateIndex];
			Hashtable tween = iTween.Hash("position", endPos, "isLocal", true, "time", time, "easetype", iTween.EaseType.linear);
			yield return new TITweenYieldInstruction(iTween.MoveTo(crusher, tween));
			
			crusherLightningAnimation.Stop();
			crusherLightningAnimation.gameObject.SetActive(false);
			
			robotJunk.spriteName = ROBOT_SPRITE_NAMES[robotStateIndex];
			robotStateIndex++;

			if (robotStateIndex == ROBOT_SPRITE_NAMES.Length)
			{
				playing = Audio.play("MetalCrusherPt2");
				
				if (playing != null)
				{
					yield return new WaitForSeconds(playing.audioInfo.clip.length);
				}
			}
		}
	}

	/**
	Callback triggered when an unrevealed pick button is pressed
	*/
	public void onPickSelected(GameObject button)
	{
		StartCoroutine(handlePick(button));
	}
	
	/**
	Coroutine to handle the animations that follow a pick selection
	*/
	public IEnumerator handlePick(GameObject button)
	{
		//NGUITools.SetActive(button, false);

		// disable input until we finish handling this pick
		for (int i = 0; i < enabledButtons.Count; i++)
		{
			enabledButtons[i].GetComponent<Collider>().enabled = false;
		}

		// take the selected button out of the enabled list now that it is disabled
		enabledButtons.Remove(button);
	
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button);

		// Turn off the flash pickme animation
		NGUITools.SetActive(buttonPickMeAnimations[index], false);

		PickemPick pick = _outcome.getNextEntry();
		
		Audio.play("PickMeTerminator");

		GameObject coin = CommonGameObject.instantiate(this.coinSpinSparkleTrail, Vector3.zero, Quaternion.identity) as GameObject;
		coin.transform.parent = button.transform.parent.transform;
		
		//This is here because for some reason Tween does not like when you set the set the gameobject position to Vector3.zero and change parents  
		TweenPosition.Begin(coin, 0.0f, Vector3.zero);

		//[Hans] - This makes sure the button has a collider and then disables that collider so the button can't be choosen a second time.
		if (button.GetComponent<Collider>() != null)
		{
			button.GetComponent<Collider>().enabled = false;
		}

		CrusherJackpotEnum jackpotIndex = CrusherJackpotEnum.ZyngaCoin;
		GameObject currentPip = null;
		switch(pick.pick)
		{
		case "P1":
			jackpotIndex = CrusherJackpotEnum.ZyngaCoin;	
			button.GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)jackpotIndex];
			currentPip = zyngaPips[crusherGameInfo[(int)jackpotIndex].count];
			coin.transform.parent = zyngaPips[crusherGameInfo[(int)jackpotIndex].count].transform.parent.transform;
			crusherGameInfo[(int)jackpotIndex].count++;
			break;
		case "P2":
			jackpotIndex = CrusherJackpotEnum.Kyle;	
			button.GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)jackpotIndex];
			currentPip = kylePips[crusherGameInfo[(int)jackpotIndex].count];
			coin.transform.parent = kylePips[crusherGameInfo[(int)jackpotIndex].count].transform.parent.transform;
			crusherGameInfo[(int)jackpotIndex].count++;
			break;
		case "P3":
			jackpotIndex = CrusherJackpotEnum.Sarah;	
			button.GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)jackpotIndex];
			currentPip = sarahPips[crusherGameInfo[(int)jackpotIndex].count];
			coin.transform.parent = sarahPips[crusherGameInfo[(int)jackpotIndex].count].transform.parent.transform;
			crusherGameInfo[(int)jackpotIndex].count++;
			break;
		case "P4":
			jackpotIndex = CrusherJackpotEnum.Arnold;	
			button.GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)jackpotIndex];
			currentPip = arnoldPips[crusherGameInfo[(int)jackpotIndex].count];
			coin.transform.parent = arnoldPips[crusherGameInfo[(int)jackpotIndex].count].transform.parent.transform;
			crusherGameInfo[(int)jackpotIndex].count++;
			break;
		case "P5":
			jackpotIndex = CrusherJackpotEnum.Terminator;	
			button.GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)jackpotIndex];
			currentPip = terminatorPips[crusherGameInfo[(int)jackpotIndex].count];
			coin.transform.parent = terminatorPips[crusherGameInfo[(int)jackpotIndex].count].transform.parent.transform;			
			crusherGameInfo[(int)jackpotIndex].count++;
			break;
		}

		if (currentPip != null)
		{
			Hashtable tween = iTween.Hash("position", currentPip.transform.localPosition, "isLocal", true, "time", COIN_TWEEN_TIME, "easetype", iTween.EaseType.linear);
			yield return new TITweenYieldInstruction(iTween.MoveTo(coin, tween));

			Audio.play("RevealProgressiveAdvance");
			yield return StartCoroutine(turnOffCoin(0.4f, coin));

			// reveal the little character portrait
			currentPip.GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)jackpotIndex];

			if (jackpotIndex == CrusherJackpotEnum.Terminator)
			{
				yield return StartCoroutine(changeRobotCrusher());
			}
		}

		if (crusherGameInfo[(int)jackpotIndex].count >= crusherGameInfo[(int)jackpotIndex].threshold)
		{
			BonusGamePresenter.instance.currentPayout = crusherGameInfo[(int)jackpotIndex].progressiveAmount;
			
			switch (jackpotIndex)
			{
			case CrusherJackpotEnum.ZyngaCoin:
				Audio.play("RevealProgressiveT1");
				break;
			case CrusherJackpotEnum.Kyle:
				Audio.play("KRComeWithMeIfYouWannaLive");
				break;
			case CrusherJackpotEnum.Sarah:
				Audio.play("S1AreYouSayingFromTheFuture");
				break;
			case CrusherJackpotEnum.Arnold:
				Audio.play("A1IllBeBack");
				break;
			case CrusherJackpotEnum.Terminator:
				Audio.play("RevealProgressiveAdvance");
				break;
			}

			yield return StartCoroutine(rollup(jackpotIndex));
		}
		else
		{
			// re-enable input since game isn't over
			for (int i = 0; i < enabledButtons.Count; i++)
			{
				enabledButtons[i].GetComponent<Collider>().enabled = true;
			}
		}
	}
	
	/**
	Handle the rollup which includes a win effect animation
	that moves the winning portrait and value to the middle
	of the screen
	*/
	private IEnumerator rollup(CrusherJackpotEnum jackpotIndex)
	{
		instructionLabel.SetActive(false);

		foreach (GameObject go in buttonPickMeAnimations)
		{
			NGUITools.SetActive(go, false);///< Turn off the remaining pickme animations as we reveal the distractors			
		}

		GameObject selectedBar = crusherGameInfo[(int)jackpotIndex].valueBar;
		Vector3 barLocalStartPos = selectedBar.transform.localPosition;

		// move the bar forward
		selectedBar.transform.localPosition = new Vector3(barLocalStartPos.x, barLocalStartPos.y, -300);

		// if this is the terminator win we need to move the skull over to where the portrait can pop in
		Vector3 originalSkullPos = crusherAreaSkull.transform.localPosition;
		if (jackpotIndex == CrusherJackpotEnum.Terminator)
		{
			Hashtable skullTween = iTween.Hash("position", terminatorWinSkull.transform.localPosition, "isLocal", true, "time", BAR_TWEEN_TIME, "oncompletetarget", gameObject, "oncomplete", "onSkullArrived", "easetype", iTween.EaseType.linear);
			iTween.MoveTo(crusherAreaSkull, skullTween);
		}

		// start the scaling up of the bar before the movement starts
		iTween.ScaleTo(selectedBar, iTween.Hash("scale", new Vector3(2,2,1), "time", BAR_TWEEN_TIME, "islocal", true, "easetype", iTween.EaseType.linear));

		// move the correct bar to the center of the screen
		Vector3 barMoveTarget = winPopup.transform.position;
		barMoveTarget.z = selectedBar.transform.position.z;
		Hashtable barTweenOver = iTween.Hash("position", barMoveTarget, "isLocal", false, "time", BAR_TWEEN_TIME, "easetype", iTween.EaseType.linear);
		yield return new TITweenYieldInstruction(iTween.MoveTo(selectedBar, barTweenOver));

		// next reveal the win background and effects now that the bar has reached its position
		this.winPopup.SetActive(true);
		winboxLightningAnimation.gameObject.SetActive(true);
		winboxLightningAnimation.Play();

		// wait on the rollup (which can be skipped)
		yield return StartCoroutine(SlotUtils.rollup(0, BonusGamePresenter.instance.currentPayout, winLabelWrapper));
		yield return new TIWaitForSeconds(0.5f);

		// turn off the win background and effects
		this.winPopup.SetActive(false);
		winboxLightningAnimation.gameObject.SetActive(false);

		// if this is the terminator need to move the skull back to it's original position
		if (jackpotIndex == CrusherJackpotEnum.Terminator)
		{
			// hide the win skull and re-show the crusher skull
			terminatorWinSkull.SetActive(false);
			crusherAreaSkull.SetActive(true);

			// move the crusher skull back to its spot above the value
			Hashtable skullTween = iTween.Hash("position", originalSkullPos, "isLocal", true, "time", BAR_TWEEN_TIME, "easetype", iTween.EaseType.linear);
			iTween.MoveTo(crusherAreaSkull, skullTween);
		}

		 // start the scaling back down to default size
		iTween.ScaleTo(selectedBar, iTween.Hash("scale", Vector3.one, "time", BAR_TWEEN_TIME, "islocal", true, "easetype", iTween.EaseType.linear));

		// move the bar back to it's original position
		Hashtable barTweenBack = iTween.Hash("position", crusherGameInfo[(int)jackpotIndex].originalBarPos, "isLocal", false, "time", BAR_TWEEN_TIME, "easetype", iTween.EaseType.linear);
		yield return new TITweenYieldInstruction(iTween.MoveTo(selectedBar, barTweenBack));

		// move the bar back down so it is sitting on the level it started at
		selectedBar.transform.localPosition = barLocalStartPos;

		// reveal the remaining picks
		yield return StartCoroutine(revealRemainingPicks());
	}

	/**
	Handle changing the visuals when the skull has finished sliding 
	over to the side of the jackpot value on a terminator win
	*/
	private void onSkullArrived()
	{
		// hide crusher skull
		crusherAreaSkull.SetActive(false);
		// show the jackpot skull
		terminatorWinSkull.SetActive(true);
	}

	/**
	Reveal the remaining unchosen picks to the user
	*/
	private IEnumerator revealRemainingPicks()
	{
		PickemPick pick;

		for (int i = 0; i < enabledButtons.Count; i++)
		{
			pick = _outcome.getNextReveal();
			if (pick != null)
			{
				switch (pick.pick)
				{
					case "P1":
						enabledButtons[i].GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)CrusherJackpotEnum.ZyngaCoin];
						break;
					case "P2":
						enabledButtons[i].GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)CrusherJackpotEnum.Kyle];
						break;
					case "P3":
						enabledButtons[i].GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)CrusherJackpotEnum.Sarah];
						break;
					case "P4":
						enabledButtons[i].GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)CrusherJackpotEnum.Arnold];
						break;
					case "P5":
						enabledButtons[i].GetComponent<UISprite>().spriteName = REVEAL_SPRITE_NAMES[(int)CrusherJackpotEnum.Terminator];
						break;
				}
				
				if (!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap("reveal_not_chosen"));
				}
				enabledButtons[i].GetComponent<UISprite>().color = Color.gray;

				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}

		yield return new TIWaitForSeconds(0.5f);
		BonusGamePresenter.instance.gameEnded();
	}

	/**
	Turns off the coin animation after the passed in delay
	*/
	private IEnumerator turnOffCoin(float waitTime, GameObject coin)
	{
		yield return new TIWaitForSeconds(waitTime);
		Destroy(coin);
	}
}

/**
Class to store info about parts of the crusher game
*/
public class CrusherGameInfo
{
	public string key;
	public int threshold;
	public int count;
	public string progressiveName;
	public string label;
	public GameObject[] pipReference;
	public long progressiveAmount;
	public GameObject valueBar = null;
	public Vector3 originalBarPos = Vector3.zero;
	public long basePayoutCredits;		//now used instead of progressive pools
	
	public CrusherGameInfo(string key, int threshold, int count, string progressiveName, string label, GameObject[] pipReference, GameObject valueBar, long basePayoutCredits)
	{
		this.key = key;
		this.threshold = threshold;
		this.count = count;
		this.progressiveName = progressiveName;
		this.label = label;
		this.pipReference = pipReference;
		this.valueBar = valueBar;
		this.originalBarPos = valueBar.transform.position;
		this.basePayoutCredits = basePayoutCredits;
	}
}

