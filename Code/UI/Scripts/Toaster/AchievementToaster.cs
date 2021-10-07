using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class AchievementToaster : Toaster 
{
	public TextMeshPro messageLabel;
	public TextMeshPro pointsLabel;

	[SerializeField] private ClickHandler showTrophyHandler;

	private List<Achievement> achievementList;
	private int currentIndex = 0;

	private const string TROPHY_INTRO_NAME = "intro";
	private const string TROPHY_OUTRO_NAME = "outro";
	private const string HOLD_ANIMATION_NAME = "hold";
	private const string MESSAGE_LOC_FORMAT = "you_earned_a_{0}_trophy";
	private const string POINTS_LOC_FORMAT = "plus_{0}_points";
	private const float TEXT_SWAP_LENGTH = 2.5f;

	// Sound names
	private const string POINTS_EARNED_AUDIO = "PointsEarnedAlertNetworkAchievements";
	private const string TROPHY_EARNED_AUDIO = "TrophyEarnedAlertNetworkAchievements";
	private const string TOASTER_INTRO_SOUND = "ToastInNetworkAchievements";	

	private const int MAX_ON_SCREEN_SECONDS = 10;

	private bool isSetupProperly = false;
	
	public override void init(ProtoToaster proto)
	{
		Audio.play(TOASTER_INTRO_SOUND);
	    achievementList = (List<Achievement>)proto.args.getWithDefault(D.VALUES, new List<Achievement>());
		currentIndex = 0;
		if (achievementList.Count <= 0)
		{
			Debug.LogErrorFormat("AchievementToaster.cs -- init -- trying to init with no achievements, this is bad.");
		    close();
		}
		else
		{
			// Set initial values.
			messageLabel.text = Localize.text(MESSAGE_LOC_FORMAT, achievementList[0].name);
			pointsLabel.text = Localize.text(POINTS_LOC_FORMAT, achievementList[0].score);
			isSetupProperly = true;
		}
		showTrophyHandler.registerEventDelegate(onClick);
		base.init(proto);
	}

    protected override void introAnimation()
	{
		base.introAnimation();
		runTimer = new GameTimer((achievementList.Count + 0.5f) * lifetime ); // Make sure it always runs longer just in case.
	}

	protected override void introFinished()
	{
		base.introFinished();
		StartCoroutine(textSwapRoutine());
	}

	
	public override string introAnimationName
	{
		get
		{
			return TROPHY_INTRO_NAME;
		}
	}

	public override string outroAnimationName
	{
		get
		{
			return TROPHY_OUTRO_NAME;
		}
	}	

	private void onClick(Dict args = null)
	{
		if (achievementList != null && achievementList.Count > currentIndex)
		{
			Achievement achievement = achievementList[currentIndex];
			NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, achievement);
		}
		else if (isSetupProperly)
		{
			// Only throw this log if we somehow manages to click on this in the split second before it gets cleaned up.
			// Should only happend in TRAMP.
			Debug.LogErrorFormat("AchievementToaster.cs -- onClick -- achievement was null at index: {0}", currentIndex);
		}
	}
	
	public IEnumerator textSwapRoutine()
	{
		int startTime = GameTimer.currentTime;
		currentIndex = 1;
		Achievement achievement;
		while (currentIndex < achievementList.Count)
		{
			if (GameTimer.currentTime - startTime > MAX_ON_SCREEN_SECONDS)
			{
				// Time out here.
				runTimer = null;
				yield break;
			}
			achievement = achievementList[currentIndex];
			if (achievement != null)
			{
				if (currentIndex == 1)
				{
					Audio.play(TROPHY_EARNED_AUDIO);
				}

				yield return new WaitForSeconds(TEXT_SWAP_LENGTH); // Wait while achievemnt name is showing.
				messageLabel.text = Localize.text(MESSAGE_LOC_FORMAT, achievement.name); // Now swap the name
				if (currentIndex == 1)
				{
					Audio.play(POINTS_EARNED_AUDIO);
				}

				yield return new WaitForSeconds(TEXT_SWAP_LENGTH); // Wait while the score is showing.
				pointsLabel.text = Localize.text(POINTS_LOC_FORMAT, achievement.score); // Now swap the score.
				// Play coroutine and wait for it to finish.
			}
			else
			{
				Debug.LogErrorFormat("AchievementToaster.cs -- textSwapRoutine -- trying to init with a null achievment, wtf?!");
			    runTimer = null; // Make the run timer expire.
			    break;
			}
			currentIndex++;
		}
		yield return new WaitForSeconds(TEXT_SWAP_LENGTH * 2); // Wait once more while the last text is showing.
		runTimer = null;
	}
	
}
