using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if ZYNGA_TRAMP
// This class holds the visualizer information for LADI (AutomatedPlayerCompanion).
// The Game Info Visualizer displays real-time information about the currently active game.
// If LADI is not running, the display should either not run or display an appropriate error.
// Most stats are updated directly by LADI when LADI receives an event from TRAMP. 
// Some information is directly taken from other components such as DateTime and SlotPlayer.

public class AutomatedCompanionVisualizer : TICoroutineMonoBehaviour, IResetGame
{
	private const int WINDOW_ID = 999;
	private const string WINDOW_TITLE = "LADI Active Game Info Visualizer";
	private const string NO_ACTIVE_GAME = "No active game";
	private static Rect windowRect = new Rect(0.0f, 0.0f, 300.0f, 50.0f);

	private static GUIStyle blackWindowStyle;
	private static GUIStyle blackTextField;
	private static GUIStyle headerStyle;

	private bool hideVisualizer;
	private bool stylesInitialized = false;

	public static AutomatedCompanionVisualizer instance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}
			else
			{
				_instance = new GameObject("LADI Active Game Info Visualizer").AddComponent<AutomatedCompanionVisualizer>();
				DontDestroyOnLoad(_instance.gameObject);
				return _instance;
			}
		}
		private set
		{
			_instance = value;
		}
	}
	private static AutomatedCompanionVisualizer _instance;

	// Update on game load.
	private string curGameKey;
	private string curGameName;
	private System.DateTime timeStarted;

	// Updated on spin start startGameLog(AutomatedBaseGameInfo gameInfo)
	private int spins;
	private string curBetAmount;
	private string totalAmountBet;

	// Updated on spin finished
	private double rtp;
	// These may not be available unless debug server messages are turned on.
	private string totalRewardAmount;

	// For debugging.
	private string startingCredits;
	private string endingCredits;

	private AutomatedGameIteration activeGame;

	private bool doDraw = true;

	public void show()
	{
		doDraw = true;
	}

	public void hide()
	{
		doDraw = false;
	}

	void Awake()
	{
		// Reset all the label values.
		resetValues();
		stylesInitialized = false;
	}

	// Called by TRAMP on game load
	public void gameLoad()
	{
		if (AutomatedPlayerCompanion.instance != null && AutomatedPlayerCompanion.instance.activeGame != null)
		{
			activeGame = AutomatedPlayerCompanion.instance.activeGame;
			curGameKey = activeGame.commonGame.gameKey;
			curGameName = activeGame.commonGame.gameName;
			timeStarted = activeGame.stats.timeStarted;
		}
		else
		{
			Debug.LogErrorFormat("LADI or active game is null!");
		}
	}

	// Called by TRAMP on spin requested
	public void spinRequested()
	{
		if (activeGame != null)
		{
			spins = activeGame.stats.spinsDone;
			curBetAmount = CreditsEconomy.convertCredits(ReelGame.activeGame.betAmount); // maybe we need to directly get the bet amount?
			totalAmountBet = CreditsEconomy.convertCredits(activeGame.stats.totalAmountBet);
		}
		else
		{
			Debug.LogErrorFormat("No currently active game");
		}
	}

	public static void resetStaticClassData()
	{
		// This needs to be here but I don't totally know what it's supposed to reset.
	}

	// Called by TRAMP on spin finished
	public void spinFinished()
	{
		if (activeGame != null)
		{
			rtp = activeGame.stats.rtp;
			totalRewardAmount = CreditsEconomy.convertCredits(activeGame.stats.coinsReturned);

			startingCredits = CreditsEconomy.convertCredits(activeGame.stats.startingPlayerCredits);
			endingCredits = CreditsEconomy.convertCredits(SlotsPlayer.creditAmount);
		}
		else
		{
			Debug.LogErrorFormat("No currently active game");
		}
	}

	// Called by TRAMP on game end.
	// Resets the displayed values.
	public void resetValues()
	{
		curGameKey = NO_ACTIVE_GAME;
		curGameName = NO_ACTIVE_GAME;
		spins = 0;
		curBetAmount = NO_ACTIVE_GAME;
		totalAmountBet = NO_ACTIVE_GAME;
		rtp = 0.0D;
		totalRewardAmount = NO_ACTIVE_GAME;
	}

    // Sets up the various UI styles.
	public static void initializeStyles()
	{


		Texture2D blackBackground = Texture2D.blackTexture;
		blackBackground.Resize(512, 512);
		// Set the style for the whole control panel window.
		blackWindowStyle = new GUIStyle(GUI.skin.box);
		blackWindowStyle.normal.background = blackBackground;

		blackTextField = new GUIStyle(GUI.skin.label);
		blackTextField.wordWrap = true;
		blackTextField.clipping = TextClipping.Overflow;
		blackTextField.normal.background = blackBackground;

		headerStyle = new GUIStyle(blackTextField);
		headerStyle.fontSize = 14;

	}

	void OnGUI()
	{
		if (!doDraw)
		{
			return;
		}
		if (!stylesInitialized)
		{
			initializeStyles();
		}

		if (instance.hideVisualizer)
		{
			windowRect.height = 50.0f;
		}
		else
		{
			windowRect.height = 300.0f;
		}
		windowRect = GUILayout.Window(WINDOW_ID, windowRect, drawMenu, string.Empty, blackWindowStyle);
	}

	public static void drawMenu(int id)
	{
		GUILayout.Label(WINDOW_TITLE, headerStyle);
		instance.hideVisualizer = GUILayout.Toggle(instance.hideVisualizer, "Hide GUI");
		if (!instance.hideVisualizer)
		{
			if (AutomatedPlayer.instance != null)
			{
				GUILayout.Label(string.Format("TRAMP State: {0}", AutomatedPlayer.instance.getGameMode().ToString()), blackTextField);
			}
			else
			{
				GUILayout.Label("TRAMP not running");
			}
			GUILayout.Label(string.Format(" \nGame Key: {1} \nGame Name: {2} \nSpins: {3} \n",
				AutomatedPlayer.instance.getGameMode(),
				instance.curGameKey,
				instance.curGameName,
				instance.spins), 
				blackTextField);
			GUILayout.Label(string.Format("Return To Player (RTP): {0:P2} \nCurrent Wager: {1} \nTotal Wager: {2} \nTotal Coins Rewarded: {3} \n",
				instance.rtp * 100.0D,
				instance.curBetAmount,
				instance.totalAmountBet,
				instance.totalRewardAmount), 
				blackTextField);
			double timeElapsed = 0.0D;
			double timePerSpin = 0.0D;
			if (instance.spins > 0)
			{
				timeElapsed = (System.DateTime.Now - instance.timeStarted).TotalSeconds;
				timePerSpin = timeElapsed / instance.spins;
			}
			GUILayout.Label(string.Format("Time Elapsed: {0:N2} s \nAvg Time Per Spin: {1:N2} s \n", timeElapsed, timePerSpin), blackTextField);
			GUILayout.Label(string.Format("Starting Credits: {0} \nEnding Credits: {1} \n", instance.startingCredits, instance.endingCredits), blackTextField);
		}
		GUI.DragWindow();
	}
}
#endif
