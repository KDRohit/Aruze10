using UnityEngine;
using System.Collections;

public static class AutomationJSONKeys
{

	public const string DEFAULT_KEY = "[LEFT BLANK]";

	// AutomatedPlayerCompanion Keys
	public const string SESSION_ID_KEY = "SessionId";
	public const string TIME_SCALE_KEY = "TimeScale";
	public const string IS_TEST_MEMORY_KEY = "IsTestMemory";
	public const string SHOULD_TEST_MINI_GAMES_KEY = "ShouldTestMiniGames";
	public const string TEST_FILE_KEY = "TestFile";
	public const string BRANCH_NAME_KEY = "BranchName";
	public const string TOTAL_GREEN_STATUS_TESTS_KEY = "TotalGreenStatusTests";
	public const string TOTAL_YELLOW_STATUS_TESTS_KEY = "TotalYellowStatusTests";
	public const string TOTAL_RED_STATUS_TESTS_KEY = "TotalRedStatusTests";
	public const string GAMEKEY_EXCEPTION_COUNTS_KEY = "GamekeyExceptionCounts";
	public const string IS_TEST_PLAN_ABORTED_KEY = "IsTestPlanAborted";
	public const string TIME_STARTED_KEY = "TimeStarted";
	public const string TIME_ENDED_KEY = "TimeEnded";
	public const string AUTOMATED_GAMES_KEY = "AutomatedGames";
	public const string LAST_WARNING_KEY = "LastWarning";
	public const string LAST_ERROR_KEY = "LastError";
	public const string LAST_EXCEPTION_KEY = "LastException";
	public const string GAMES_PASSED_KEY = "GamesPassed";
	public const string GAMES_EXCEPTIONS_KEY = "GamesWithExceptions";
	public const string GAME_CURRENTLY_TESTING_KEY = "GameCurrentlyTesting";
	public const string TIME_ELAPSED_KEY = "ElapsedTestTime";
	public const string TOTAL_GAMES_TESTED_KEY = "TotalGamesTested";
	public const string GAMES_TO_TEST_KEY = "GamesLeftToTest";
	public const string NUM_OF_VISUAL_BUGS_KEY = "NumOfVisualBugs";
	public const string RANDOM_TESTING = "RandomTesting";
	public const string REPEAT_TESTING = "RepeatTesting";
	public const string PULL_WHEN_DONE = "PullWhenDone";
	public const string COLLIDER_VISUALIZATION = "ColliderVisualization";
	public const string GAME_TO_TEST_KEY_KEY = "Key";
	public const string GAME_TO_TEST_ITERATION_KEY = "Game";
	public const string INSTANCES_KEY = "Instances";

	// AutomatedGameIteration keys 
	public const string ITERATION_NUM_KEY = "IterationNumber";
	public const string STATS_KEY = "Stats";
	public const string GAME_LOGS_KEY = "GameLogs";
	public const string VISUAL_BUGS_KEY = "VisualBugs";
	public const string ACTIONS_TESTED_KEY = "ActionsTested";
	public const string ACTIONS_REMAINING_KEY = "ActionsRemaining";
	public const string ACTION_NAME_KEY = "ActionName";
	public const string ACTION_COUNT_KEY = "ActionCount";

	// Automated Companion Log Keys
	public const string LOG_TYPE_KEY = "LogType";
	public const string LOG_MESSAGE_KEY = "LogMsg";
	public const string STACK_TRACE_KEY = "StackTrace";
	public const string TIMESTAMP_KEY = "Timestamp";
	public const string LOGNUM_KEY = "LogNum";
	public const string ACTIVE_ACTION_KEY = "ActiveAction";
	public const string SPIN_NUMBER_KEY = "SpinNumber";
	public const string PREV_OUTCOME = "PrevOutcome";
	public const string OUTCOME = "Outcome";

	// AutomatedGame Keys
	public const string GAME_KEY_KEY = "GameKey";
	public const string GAME_NAME_KEY = "GameName";
	public const string GAME_ITERATIONS_KEY = "GameIterations";

	// AutomatedGameStats Keys
	public const string LOG_KEY = "NumberOfLogs";
	public const string EXCEPTION_KEY = "NumberOfExceptions";
	public const string ERROR_KEY = "NumberOfErrors";
	public const string WARNINGS_KEY = "NumberOfWarnings";
	public const string SPINS_DONE_KEY = "SpinsDone";
	public const string FORCED_SPINS_DONE_KEY = "ForcedSpinsDone";
	public const string NORMAL_SPINS_DONE_KEY = "NormalSpinsDone";
	public const string FAST_SPINS_DONE_KEY = "FastSpinsDone";
	public const string AUTOSPINS_REQUESTED_KEY = "AutospinsRequested";
	public const string AUTOSPINS_RECEIVED_KEY = "AutospinsReceived";
	public const string AUTOSPINS_FINISHED_KEY = "AutospinsFinished";
	public const string MAX_MEMORY_KEY = "MaxMemory";
	public const string MEAN_MEMORY_KEY = "MeanMemory";
	public const string MEMORY_SAMPLE_KEY = "MemorySample";
	public const string SPIN_TIMER_START_KEY = "SpinTimerStart";
	public const string MAX_SPIN_TIME_KEY = "MaxSpinTime";
	public const string MEAN_SPIN_TIME_KEY = "MeanSpinTime";
	public const string SPIN_TIME_SAMPLE_COUNT_KEY = "SpinTimeSampleCount";
	public const string STARTING_PLAYER_CREDS_KEY = "StartingPlayerCredits";
	public const string ENDING_PLAYER_CREDS_KEY = "EndingPlayerCredits";
	public const string TOTAL_BET_KEY = "TotalBet";
	public const string TOTAL_REWARD_KEY = "TotalReward";
	public const string BONUS_GAMES_ENTERED_KEY = "BonusGamesEntered";
}
