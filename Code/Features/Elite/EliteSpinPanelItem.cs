using UnityEngine;
using System.Collections;
using Com.Rewardables;
using Com.Scheduler;
using TMPro;

public class EliteSpinPanelItem : MonoBehaviour
{
	[SerializeField] private UIMeterNGUI meter;
	[SerializeField] private TextMeshPro meterLabel;
	[SerializeField] private TextMeshPro headerLabel;
	[SerializeField] private Animator animator;

	private enum SpinPanelState
	{
		NOT_QUALIFIED,
		QUALIFIED,
		COMPLETED
	}

	private bool hasShownCap = false; // set to true when the user has seen their points capped
	private long previousWager; // last wager the user set
	private bool showPointsAdded;
	private int currentSpins;
	private SpinPanelState state;

	// =============================
	// CONST
	// =============================
	private string ELITE_BET = "elite_bet";
	private string ELITE_EARNED_POINTS = "elite_earned_points_{0}";
	private string ELITE_QUALIFY = "elite_qualify";

	// animations
	private string QUALIFIED = "introQualified";
	private string NOT_QUALIFIED = "introNotQualified";
	private string COMPLETED = "introCompleted";

	private const string POINTS_LOC = "elite_points_meter_{0}_{1}";
	private const string SPINS_LOC = "elite_spins_meter_{0}_{1}";

	private const int SHOW_PANEL_TRIGGER_AMOUNT = 100;

	public bool isShowing
	{
		get
		{
			return !animator.GetCurrentAnimatorStateInfo(0).IsName("off");
		}
	}
	

	void Awake()
	{
		RewardablesManager.addEventHandler(onRewardReceived);
		Server.registerEventDelegate(EliteManager.ELITE_PROGRESS_EVENT, onSpin, true);
		previousWager = SpinPanel.instance.currentWager;
		currentSpins = EliteManager.spinsTowardReward;
		reset();
		StartCoroutine(showPanelRoutine(1.0f));
	}

	void OnDestroy()
	{
		RewardablesManager.removeEventHandler(onRewardReceived);
		Server.unregisterEventDelegate(EliteManager.ELITE_PROGRESS_EVENT, onSpin, true);
		reset();
	}

	/// <summary>
	/// Update the toaster when user receives points rewardables
	/// </summary>
	/// <param name="rewardable"></param>
	private void onRewardReceived(Rewardable rewardable)
	{
		RewardElitePassPoints elitePoints = rewardable as RewardElitePassPoints;

		if (elitePoints != null)
		{
			showPointsAdded = true;
		}
	}

	/// <summary>
	/// Called from the spin panel when the wager changes
	/// </summary>
	/// <param name="wager"></param>
	public bool onBetChanged(long wager, float delay = 0.0f)
	{
		if (SpinPanel.instance == null)
		{
			return false;
		}

		if (animator == null)
		{
			return false;
		}

		if (!gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}

		if (EliteManager.isQualifyingBet(wager) && !EliteManager.isQualifyingBet(previousWager))
		{
			updateOnBetChanged(SpinPanelState.QUALIFIED, wager, delay);
			return true;
		}
		
		if (!EliteManager.isQualifyingBet(wager) && EliteManager.isQualifyingBet(previousWager))
		{
			updateOnBetChanged(SpinPanelState.NOT_QUALIFIED, wager, delay);
			return true;
		}

		return false;
	}

	private void updateOnBetChanged(SpinPanelState newState, long wager, float delay = 1.0f)
	{
		reset();
		updateMeter();
		previousWager = wager;
		state = newState;
		StartCoroutine(showPanelRoutine(delay));
	}

	public void onSpin(JSON data)
	{
		currentSpins = data.getInt("spin_count", 0);
		if (currentSpins % SHOW_PANEL_TRIGGER_AMOUNT == 0)
		{
			reset();
			StartCoroutine(showPanelRoutine(1.0f));
		}
	}

	/// <summary>
	/// Updates the meter to the current progress
	/// </summary>
	private void updateMeter()
	{
		if (ExperimentWrapper.ElitePass.showSpinsInToaster && !EliteManager.hasReachedSpinCap)
		{
			if (showPointsAdded)
			{
				meterLabel.text = Localize.text(SPINS_LOC, EliteManager.spinRewardThreshold, EliteManager.spinRewardThreshold);
				meter.setState(1,1);
			}
			else
			{
				meterLabel.text = Localize.text(SPINS_LOC, currentSpins, EliteManager.spinRewardThreshold);
				meter.setState(currentSpins, EliteManager.spinRewardThreshold);
			}
		}
		else
		{
			meterLabel.text = Localize.text(POINTS_LOC, EliteManager.points, EliteManager.targetPoints);
			meter.setState(EliteManager.points, EliteManager.targetPoints);
		}

		if (showPointsAdded)
		{
			state = SpinPanelState.COMPLETED;
			headerLabel.text = Localize.text(ELITE_EARNED_POINTS, CommonText.formatNumber(EliteManager.points));
			showPointsAdded = false;
		}
		else if (EliteManager.isQualifyingBet(SpinPanel.instance.currentWager))
		{
			state = SpinPanelState.QUALIFIED;
			headerLabel.text = Localize.text(ELITE_QUALIFY);
		}
		else
		{
			state = SpinPanelState.NOT_QUALIFIED;
			headerLabel.text = Localize.text(ELITE_BET);
		}
	}

	/// <summary>
	/// Stops all coroutines running on this gameobject
	/// </summary>
	public void reset()
	{
		StopAllCoroutines();
	}

	public IEnumerator showPanelRoutine(float delay)
	{
		yield return new WaitForSeconds(delay);
		updateMeter();
		onShowToaster();
	}

	private void onShowToaster(Dict args = null)
	{
		if (animator == null)
		{
			return;
		}
		
		Audio.play("ToastElite");

		if (state == SpinPanelState.QUALIFIED)
		{
			animator.Play(QUALIFIED);
		}
		else if(state == SpinPanelState.NOT_QUALIFIED)
		{
			animator.Play(NOT_QUALIFIED);
		}
		else if(state == SpinPanelState.COMPLETED)
		{
			animator.Play(COMPLETED);
		}
	}
}