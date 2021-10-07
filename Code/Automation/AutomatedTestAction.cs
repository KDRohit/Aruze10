using System.Collections;
using System.Collections.Generic;
using System.Text; // String builder
using UnityEngine;

#if ZYNGA_TRAMP
[System.Serializable]
public class AutomatedTestAction
{
	public const string SPIN_ACTION = "spin";
	public const string DESYNC_CHECK_ACTION = "desync_check";
	public const string FEATURE_MINI_GAME_ACTION = "feature_mini_game_check";
	public const string AUTOSPIN_10 = "autospin_10";
	public const string AUTOSPIN_25 = "autospin_25";
	public const string AUTOSPIN_50 = "autospin_50";
	public const string AUTOSPIN_100 = "autospin_100";
	public const string FORCE_GIFTED_GAME_ACTION = "force_gifted_game";
	public const string PLAY_GIFTED_GAME_ACTION = "play_gifted_game";
	public enum Action
	{
		NONE = 0,
		SPIN = 1,
		KEY_PRESS = 2,
		DESYNC_CHECK = 3,
		FEATURE_MINI_GAME_CHECK = 4,
		AUTOSPIN_10 = 5,
		AUTOSPIN_25 = 6,
		AUTOSPIN_50 = 7,
		AUTOSPIN_100 = 8,
		FORCE_GIFTED_GAME = 9,
		PLAY_GIFTED_GAME = 10
	}

	public Action action = Action.NONE;
	public string actionName = "";
	// Tracks if this is the last spin for this type of action, 
	// for instance desync needs to spin one past a spin that produces a payout on the reels
	public bool isLastSpin = true; 

	public AutomatedTestAction(string passedActionName)
	{
		actionName = passedActionName;
		action = AutomatedTestAction.actionNameToEnum(passedActionName);

		switch (action)
		{
			case Action.FEATURE_MINI_GAME_CHECK:
			case Action.DESYNC_CHECK:
				isLastSpin = false;
				break;
			default:
				isLastSpin = true;
				break;
		}
	}

	public static Action actionNameToEnum(string passedActionName)
	{
		Action action = Action.NONE;
		switch (passedActionName)
		{
			case SPIN_ACTION:
				action = Action.SPIN;
				break;
			case DESYNC_CHECK_ACTION:
				action = Action.DESYNC_CHECK;
				// desync needs to spin till one spin past a spin that produced a payout on the reels
				break;
			case FEATURE_MINI_GAME_ACTION:
				action = Action.FEATURE_MINI_GAME_CHECK;
				break;
			case AUTOSPIN_10:
				action = Action.AUTOSPIN_10;
				break;
			case AUTOSPIN_25:
				action = Action.AUTOSPIN_25;
				break;
			case AUTOSPIN_50:
				action = Action.AUTOSPIN_50;
				break;
			case AUTOSPIN_100:
				action = Action.AUTOSPIN_100;
				break;
			case FORCE_GIFTED_GAME_ACTION:
				action = Action.FORCE_GIFTED_GAME;
				break;
			case PLAY_GIFTED_GAME_ACTION:
				action = Action.PLAY_GIFTED_GAME;
				break;
			default:
				action = Action.KEY_PRESS;
				break;
		}
		return action;
	}

	public static int getSpinCountFromAction(string passedActionName)
	{
		int spinCount = 1;

		switch (passedActionName)
		{
			case AutomatedTestAction.AUTOSPIN_10:
				spinCount = 10;
				break;
			case AutomatedTestAction.AUTOSPIN_25:
				spinCount = 25;
				break;
			case AutomatedTestAction.AUTOSPIN_50:
				spinCount = 50;
				break;
			case AutomatedTestAction.AUTOSPIN_100:
				spinCount = 100;
				break;
		}

		return spinCount;	
	}
}
#endif
