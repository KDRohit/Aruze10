using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DailyTripleWheel : DailyBonusGame
{

	private const float DECELERATION_SPEED = -320.0f;

	public class TripleWheelPaytable
	{
		public string id;
		public int stop;
		public int credits;
		public int multiplier;
		
		public TripleWheelPaytable(int stop, JSON paytable)
		{
			this.stop = stop;
			
			if (paytable.getString("id", "") != "")
			{
				id = paytable.getString("id", "");
			}
			
			if (paytable.getInt("credits", -1) != -1)
			{
				credits = paytable.getInt("credits", -1);
			}
			
			if (paytable.getInt("multiplier", -1) != -1)
			{
				multiplier = paytable.getInt("multiplier", -1);
			}
		}
	}
	
	private JSON _payTable;
	
	private List<TripleWheelPaytable> leftWheelData;
	private List<TripleWheelPaytable> middleWheelData;
	private List<TripleWheelPaytable> rightWheelData;
	
	private int stopIndexLeft = 6;
	private int stopIndexMiddle = 6;
	private int stopIndexRight = 6;
	
	private int wheel1NumericalOutcome;
	private int wheel2NumericalOutcome;
	private int wheel3NumericalOutcome;
	
	private WheelSpinner _wheelSpinner1;
	private WheelSpinner _wheelSpinner2;
	private WheelSpinner _wheelSpinner3;
	
	private int[] _rotateAngleVect;
	
	public GameObject spinLabel;
	
	public GameObject wheelPrefab;
	public DtwText dtwTextPrefab;
	private List<DtwText> wheelTexts = new List<DtwText>();
	public GameObject ScoreBoxPrefab;
	
	public bool shouldSwipeToSpin = false;
	
	// These should be the gameobject parents, and not the wheels themselves.
	public GameObject wheelLeftAnchor;
	public GameObject wheelMiddleAnchor;
	public GameObject wheelRightAnchor;
	protected DtwWheel wheelLeft;
	protected DtwWheel wheelMiddle;
	protected DtwWheel wheelRight;
	protected FacebookFriendInfo friendLeft;
	protected FacebookFriendInfo friendMiddle;
	protected FacebookFriendInfo friendRight;
	protected DtwScoreBox boxLeft;
	protected DtwScoreBox boxMiddle;
	protected DtwScoreBox boxRight;
	protected GameObject pieSliceEffectLeft;
	protected GameObject pieSliceEffectMiddle;
	protected GameObject pieSliceEffectRight;
	private bool leftComplete, middleComplete, rightComplete = false;

	private void Awake()
	{
		if (!shouldSwipeToSpin)
		{
			spinLabel.SetActive(false);
		}
		
		GameObject wheelObject;
		GameObject scoreBoxObject;
		
		wheelObject = NGUITools.AddChild(wheelLeftAnchor,wheelPrefab);
		wheelLeft = wheelObject.GetComponent<DtwWheel>();
		wheelLeft.SetAsBonusWheel();
		friendLeft = wheelLeft.friendInfo;
		scoreBoxObject = NGUITools.AddChild(wheelObject,ScoreBoxPrefab);
		scoreBoxObject.transform.localPosition = ScoreBoxPrefab.transform.localPosition;
		boxLeft = scoreBoxObject.GetComponent<DtwScoreBox>();
		
		wheelObject = NGUITools.AddChild(wheelMiddleAnchor,wheelPrefab);
		wheelMiddle = wheelObject.GetComponent<DtwWheel>();
		wheelMiddle.SetAsBonusWheel();
		friendMiddle = wheelMiddle.friendInfo;
		scoreBoxObject = NGUITools.AddChild(wheelObject,ScoreBoxPrefab);
		scoreBoxObject.transform.localPosition = ScoreBoxPrefab.transform.localPosition;
		boxMiddle = scoreBoxObject.GetComponent<DtwScoreBox>();
		
		wheelObject = NGUITools.AddChild(wheelRightAnchor,wheelPrefab);
		wheelRight = wheelObject.GetComponent<DtwWheel>();
		friendRight = wheelRight.friendInfo;
		wheelRight.SetAsMultiplierWheel();
		scoreBoxObject = NGUITools.AddChild(wheelObject,ScoreBoxPrefab);
		scoreBoxObject.transform.localPosition = ScoreBoxPrefab.transform.localPosition;
		boxRight = scoreBoxObject.GetComponent<DtwScoreBox>();
	}
	
	public override void init (JSON data)
	{
		boxLeft.score.color = coinsScoreBox.scoreLabel.color;
		boxMiddle.score.color = coinsScoreBox.scoreLabel.color;
		boxRight.score.color = multiplierScoreBox.scoreLabel.color;
		
		_rotateAngleVect = new int[] { -360, -45, -90, -135, -180, -225, -270, -315 };
		string wheelStop1ID = "0";
		string wheelStop2ID = "0";
		string wheelStop3ID = "0";
		
		leftWheelData = new List<TripleWheelPaytable>();
		middleWheelData = new List<TripleWheelPaytable>();
		rightWheelData = new List<TripleWheelPaytable>();
		
		foreach (JSON outcomeJSON in data.getJsonArray("outcomes"))
		{
			if (outcomeJSON.getString("bonus_game_pay_table", "") != "")
			{
				_payTable = BonusGamePaytable.findPaytable("wheel", outcomeJSON.getString("bonus_game_pay_table", ""));
				if (_payTable.getJsonArray("rounds").Length != 0)
				{
					JSON[] rounds = _payTable.getJsonArray("rounds");
					int wheelRoundsLength = rounds.Length;
					for (int i = 0; i < wheelRoundsLength;i++)
					{
						int winsLength = rounds[i].getJsonArray("wins").Length;
						for (int j = 0; j < winsLength; j++)
						{
							JSON[] wins = rounds[i].getJsonArray("wins");
							switch (i)
							{
								case 0:
									leftWheelData.Add(new TripleWheelPaytable(j, wins[j]));
									break;
								case 1:
									middleWheelData.Add(new TripleWheelPaytable(j, wins[j]));
									break;
								case 2:
									rightWheelData.Add(new TripleWheelPaytable(j, wins[j]));
									break;
							}
						}
					}
				}
			}
			
			if (outcomeJSON.getString("round_1_stop_id", "") != "")
			{
				wheelStop1ID = outcomeJSON.getString("round_1_stop_id", "");
			}
			if (outcomeJSON.getString("round_2_stop_id", "") != "")
			{
				wheelStop2ID = outcomeJSON.getString("round_2_stop_id", "");
			}
			if (outcomeJSON.getString("round_3_stop_id", "") != "")
			{
				wheelStop3ID = outcomeJSON.getString("round_3_stop_id", "");
			}
			
		}
		
		int index = 0;
		
		foreach(TripleWheelPaytable paytable in leftWheelData)
		{
			if (paytable.id == wheelStop1ID)
			{
				stopIndexLeft = index;
				wheel1NumericalOutcome = paytable.credits;
			}
			
			index++;
		}
		
		if (shouldSwipeToSpin)
		{
			SwipeableWheel swipeableWheel = wheelLeft.wheel.AddComponent<SwipeableWheel>();
			swipeableWheel.init(wheelLeft.wheel,_rotateAngleVect[stopIndexLeft],
								delegate()
								{
				float duration = 0.125f;
				Vector3 scale = 1.2f * Vector3.one;
				StartCoroutine(scaleFace(friendLeft.gameObject,scale,duration));
			}, 
			onWheel1Complete, wheelLeft.wheelSprite);
		}
		index = 0;
		
		foreach(TripleWheelPaytable paytable in middleWheelData)
		{
			if (paytable.id == wheelStop2ID)
			{
				stopIndexMiddle = index;
				wheel2NumericalOutcome = paytable.credits;
			}
			
			index++;
		}
		
		if (shouldSwipeToSpin)
		{
			SwipeableWheel swipeableWheel = wheelMiddle.wheel.AddComponent<SwipeableWheel>();
			swipeableWheel.init(wheelMiddle.wheel,_rotateAngleVect[stopIndexMiddle],
								delegate()
								{
				float duration = 0.125f;
				Vector3 scale = 1.2f * Vector3.one;
				StartCoroutine(scaleFace(friendMiddle.gameObject,scale,duration));
			}, 
			onWheel2Complete, wheelLeft.wheelSprite);
		}
		index = 0;
		
		foreach(TripleWheelPaytable paytable in rightWheelData)
		{
			if (paytable.id == wheelStop3ID)
			{
				stopIndexRight = index;
				wheel3NumericalOutcome = paytable.multiplier + 1;
			}
			
			index++;
		}
		
		if (shouldSwipeToSpin)
		{
			SwipeableWheel swipeableWheel = wheelRight.wheel.AddComponent<SwipeableWheel>();
			swipeableWheel.init(wheelRight.wheel,_rotateAngleVect[stopIndexRight],
								delegate()
								{
				float duration = 0.125f;
				Vector3 scale = 1.2f * Vector3.one;
				StartCoroutine(scaleFace(friendRight.gameObject,scale,duration));
			}, 
			onWheel3Complete, wheelLeft.wheelSprite);
		}
		
		// Please do not commit this sort of thing into development
		//Debug.Log("WinOutcome 1 is:" + wheel1NumericalOutcome);
		//Debug.Log("WinOutcome 2 is:" + wheel2NumericalOutcome);
		//Debug.Log("Multiplier is:" + wheel3NumericalOutcome);
		
		//int winTotal = (wheel1NumericalOutcome + wheel2NumericalOutcome) * wheel3NumericalOutcome;
		//Debug.Log("Win Total is:" + winTotal);
		
		updateLeftWheelNumbers();
		updateMiddleWheelNumbers();
		updateRightWheelNumbers();
		makeWheelValuesUniformSize();
		updateFriends();
		
		if (friendLeft.member != null)
		{
			boxLeft.nameLabel.text = friendLeft.member.firstName;
		}
		else
		{
			boxLeft.nameBox.SetActive(false);
		}
		
		if (friendMiddle.member != null)
		{
			boxMiddle.nameLabel.text = friendMiddle.member.firstName;
		}
		else
		{
			boxMiddle.nameBox.SetActive(false);
		}
		
		if (friendRight.member != null)
		{
			boxRight.nameLabel.text = friendRight.member.firstName;
		}
		else
		{
			boxRight.nameBox.SetActive(false);
		}
		
		if (!shouldSwipeToSpin)
		{
			StartCoroutine(startSpin());
		}
	}
	
	//Makes the facebook faces in the center of the wheels grow to their scale over the durration, Left lasts the duration, middle duration*2, right diration *3
	//They are staggered like this because we start the spin after they shrink down.
	private IEnumerator growFaces(float scale,float duration)
	{
		iTween.ScaleTo(friendLeft.gameObject, iTween.Hash("scale", Vector3.one * scale, "time", duration, "easetype", iTween.EaseType.easeOutSine));
		iTween.ScaleTo(friendMiddle.gameObject, iTween.Hash("scale", Vector3.one * scale, "time", duration*2, "easetype", iTween.EaseType.easeOutSine));
		iTween.ScaleTo(friendRight.gameObject, iTween.Hash("scale", Vector3.one * scale, "time", duration*3, "easetype", iTween.EaseType.easeOutSine));
		yield return new WaitForSeconds(duration);
	}
	
	//Shrinks a game object back down to its oringial size over the duration.
	private IEnumerator scaleFace(GameObject go, Vector3 origScale, float duration)
	{
		iTween.ScaleTo(go, iTween.Hash("scale", origScale, "time", duration, "easetype", iTween.EaseType.easeInSine));
		yield return new WaitForSeconds(duration);
		
		iTween.ScaleTo(go, iTween.Hash("scale", Vector3.one, "time", duration, "easetype", iTween.EaseType.easeInSine));	
	}
	
	public override void Update()
	{
		if (_wheelSpinner1 != null)
		{
			_wheelSpinner1.updateWheel();
		}
		
		if (_wheelSpinner2 != null)
		{
			_wheelSpinner2.updateWheel();
		}
		
		if (_wheelSpinner3 != null)
		{
			_wheelSpinner3.updateWheel();
		}
	}
	
	//All the Logic that should be happening when spins are starting
	//This doesn't happen anymore because we moved to swipeToSpin
	private IEnumerator startSpin()
	{
		yield return new WaitForSeconds(0.5f);
		
		//Stuff for scaling.
		float duration = 0.125f;
		Vector3 scale = 1.2f * Vector3.one;
		Vector3 origScale = friendLeft.gameObject.transform.localScale; //All of the objects are the same size.
		//First we want to Scale the faces up
		//yield return StartCoroutine(growFaces(scale,duration));
		
		int slicesPerWheel = 8;
		int pointerLocation = 0;
		
		//Now we want to scale the faces back down, and then after that we want to spin
		yield return StartCoroutine(scaleFace(friendLeft.gameObject, scale, duration));
		StartCoroutine(scaleFace(friendLeft.gameObject, origScale, duration));
		spin(slicesPerWheel, stopIndexLeft, pointerLocation, 0.0f, wheelLeft.wheel);

		//Middle wheel (time minus the time it watied before)
		yield return StartCoroutine(scaleFace(friendMiddle.gameObject, scale, duration));
		StartCoroutine(scaleFace(friendMiddle.gameObject, origScale, duration));
		spin(slicesPerWheel, stopIndexMiddle, pointerLocation, 1.0f, wheelMiddle.wheel);

		//Right wheel (time minus the time it waited before)
		yield return StartCoroutine(scaleFace(friendRight.gameObject, scale, duration));
		StartCoroutine(scaleFace(friendRight.gameObject, origScale, duration));
		spin(slicesPerWheel, stopIndexRight, pointerLocation, 2.0f, wheelRight.wheel);
	}
	
	public void spin(int numSlices, int sliceToStop, int pointerIndex, float spinDuration, GameObject wheelToTween)
	{		
		if (wheelToTween == wheelLeft.wheel)
		{
			_wheelSpinner1 = new WheelSpinner(wheelToTween, _rotateAngleVect[sliceToStop], onWheel1Complete, false, DECELERATION_SPEED);
			_wheelSpinner1.constantVelocitySeconds = spinDuration;
		}
		else if (wheelToTween == wheelMiddle.wheel)
		{
			_wheelSpinner2 = new WheelSpinner(wheelToTween, _rotateAngleVect[sliceToStop], onWheel2Complete, false, DECELERATION_SPEED);
			_wheelSpinner2.constantVelocitySeconds = spinDuration;
		}
		else
		{
			_wheelSpinner3 = new WheelSpinner(wheelToTween, _rotateAngleVect[sliceToStop], onWheel3Complete, false, DECELERATION_SPEED);
			_wheelSpinner3.constantVelocitySeconds = spinDuration;
		}
	}
	
	private void onWheel1Complete()
	{
		if (wheelLeft.pieSlicePrefab != null)
		{
			pieSliceEffectLeft = NGUITools.AddChild(wheelLeft.pieSliceAnchor, wheelLeft.pieSlicePrefab);
			ParticleSystem pieSliceEffect = pieSliceEffectLeft.GetComponentInChildren<ParticleSystem>();
			pieSliceEffect.Play();
		}
		
		leftComplete = true;
		amountWon += wheel1NumericalOutcome;
		coinsScoreBox.setScore(CreditsEconomy.convertCredits(amountWon));
		boxLeft.score.text = CreditsEconomy.convertCredits(wheel1NumericalOutcome);
		Audio.play("SummaryReveal1");
		StartCoroutine(finishMinigame());
	}
	
	private void onWheel2Complete()
	{
		if (wheelMiddle.pieSlicePrefab != null)
		{
			pieSliceEffectMiddle = NGUITools.AddChild(wheelMiddle.pieSliceAnchor, wheelMiddle.pieSlicePrefab);
			ParticleSystem pieSliceEffect = pieSliceEffectMiddle.GetComponentInChildren<ParticleSystem>();
			pieSliceEffect.Play();
		}
		
		middleComplete = true;
		amountWon += wheel2NumericalOutcome;
		coinsScoreBox.setScore(CreditsEconomy.convertCredits(amountWon));
		boxMiddle.score.text = CreditsEconomy.convertCredits(wheel2NumericalOutcome);
		Audio.play("SummaryReveal1");
		StartCoroutine(finishMinigame());
	}
	
	private void onWheel3Complete()
	{
		Audio.stopSound(Audio.findPlayingAudio("wheel_decelerate"));
		if (wheelRight.pieSlicePrefab != null)
		{
			pieSliceEffectRight = NGUITools.AddChild(wheelRight.pieSliceAnchor, wheelRight.pieSlicePrefab);
			ParticleSystem pieSliceEffect = pieSliceEffectRight.GetComponentInChildren<ParticleSystem>();
			pieSliceEffect.Play();
		}
		
		rightComplete = true;
		boxRight.score.text = CommonText.formatNumber(wheel3NumericalOutcome);
		multiplierScoreBox.setScore(Localize.text("{0}X",wheel3NumericalOutcome.ToString()));
		Audio.play("SummaryReveal1");
		StartCoroutine(finishMinigame());
	}
	
	private IEnumerator finishMinigame()
	{
		if (leftComplete && rightComplete && middleComplete)
		{	
			yield return new WaitForSeconds(1f);
			
			GameObject.Destroy(pieSliceEffectLeft);
			GameObject.Destroy(pieSliceEffectMiddle);
			GameObject.Destroy(pieSliceEffectRight);
			
			amountWon *= wheel3NumericalOutcome;
			totalScoreBox.setScore(CreditsEconomy.convertCredits(amountWon));
			_isDone = true;
		}
	}
	
	// Rolls the text from the score label from the center of the box out
	private void rollOutScoreLabel(DbsScoreBox box){
		float duration = 1;
		float delay = 1;
		iTween.MoveTo(box.scoreLabel.gameObject, iTween.Hash("position", new Vector3(0, 350 ,0), "isLocal", true, "delay", delay, "time", duration));
	}
	
	//Returns the value of the offset between the highest value and the lowest value in terms of indexes, otherwise lays out the labels
	private int updateDailyBonusWheelNumbersNonMultiplier(DtwWheel wheelToUpdate, List<TripleWheelPaytable> paytable)
	{
		int paytableEntries = paytable.Count;
		
		//We need to index so that the wheel's white index is the one that is greatest in value
		int greatestIndex = 0;
		int highestCredits = 0;
		for (int j = 0; j < paytableEntries; j++)
		{
			if (paytable[j].credits > highestCredits)
			{
				greatestIndex = j;
				highestCredits = paytable[j].credits;
			}
		}
		//The difference between the white index and the greatest index is the number of locations we need to start at.
		//i + delta +/- length to stay in bounds
		int delta = greatestIndex - wheelToUpdate.whiteSliceIndex;

		for (int i=0; i<paytableEntries; i++ )
		{
			int position = i + delta;
			position = adjustAroundLength(position, paytableEntries);
			GameObject dtwTextObject = NGUITools.AddChild(wheelToUpdate.wheel, dtwTextPrefab.gameObject);
			DtwText dtwText = dtwTextObject.GetComponent<DtwText>();
			
			dtwText.setCredits(i, paytable[position].credits, wheelToUpdate.whiteSliceIndex);
			
			wheelTexts.Add(dtwText);
		}
		//In case it matters, return the delta, because the stop id also needs to change
		return delta;
	}
	
	private int adjustAroundLength(int valueToWrap, int maxValue)
	{
		if (valueToWrap >= maxValue)
		{
			valueToWrap -= maxValue;
		} else if (valueToWrap < 0)
		{
			valueToWrap += maxValue;
		}
		return valueToWrap;
	}
	
	private void updateLeftWheelNumbers()
	{
		int indexDelta = updateDailyBonusWheelNumbersNonMultiplier(wheelLeft, leftWheelData);
		stopIndexLeft -= indexDelta;
		stopIndexLeft = adjustAroundLength(stopIndexLeft, leftWheelData.Count);
	}
	
	private void updateMiddleWheelNumbers()
	{
		int indexDelta = updateDailyBonusWheelNumbersNonMultiplier(wheelMiddle, middleWheelData);
		stopIndexMiddle -= indexDelta;
		stopIndexMiddle = adjustAroundLength(stopIndexMiddle, middleWheelData.Count);
	}
	
	private void updateRightWheelNumbers()
	{
		int paytableEntries = rightWheelData.Count;
		int greatestIndex = 0;
		int highestMultiplier = 0;
		for (int j = 0; j < paytableEntries; j++)
		{
			if (rightWheelData[j].multiplier > highestMultiplier)
			{
				greatestIndex = j;
				highestMultiplier = rightWheelData[j].multiplier;
			}
		}
		//The difference between the white index and the greatest index is the number of locations we need to start at.
		//i + delta +/- length to stay in bounds
		int delta = greatestIndex - wheelRight.whiteSliceIndex;
		
		for (int i=0; i<paytableEntries; i++ )
		{
			int position = i + delta;
			position = adjustAroundLength(position, paytableEntries);
			GameObject dtwTextObject = NGUITools.AddChild(wheelRight.wheel, dtwTextPrefab.gameObject);
			DtwText dtwText = dtwTextObject.GetComponent<DtwText>();
			
			dtwText.setMultiplier(i, rightWheelData[position].multiplier+1, wheelRight.whiteSliceIndex);
			
			wheelTexts.Add(dtwText);
		}
		stopIndexRight -= delta;
		stopIndexRight = adjustAroundLength(stopIndexRight, rightWheelData.Count);
	}
	
	private void makeWheelValuesUniformSize()
	{
		int min = int.MaxValue;
		foreach (DtwText t in wheelTexts)
		{
			min = Mathf.Min(min, Mathf.RoundToInt(t.valueLabel.transform.localScale.x));
		}
		
		foreach (DtwText t in wheelTexts)
		{
			t.valueLabel.transform.localScale = new Vector3(min, min, 1);
		}
	}
	
	private void updateFriends()
	{
		List<SocialMember> friendList = new List<SocialMember>();
		SocialMember.getRandomFriends(3,friendList);
		
		SocialMember leftMember = null;
		if (friendList.Count > 0)
		{
			leftMember = friendList[0];
		}
		if (leftMember != null)
		{
			friendLeft.member = leftMember;
		}
		
		SocialMember middleMember = null;
		if (friendList.Count > 1)
		{
			middleMember = friendList[1];
		}
		if (middleMember != null)
		{
			friendMiddle.member = middleMember;
		}
		
		SocialMember rightMember = null;
		if (friendList.Count > 2)
		{
			rightMember = friendList[2];
		}
		if (rightMember != null)
		{
			friendRight.member = rightMember;
		}
	}
}