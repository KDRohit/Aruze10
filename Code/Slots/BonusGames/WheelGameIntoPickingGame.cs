using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Generic base class for building a wheel game that has a picking game after you spin
*/
public class WheelGameIntoPickingGame : WheelGame 
{
	[SerializeField] protected PickingGameUsingPickemOutcome pickingGame = null;	// The pickem game that will be transitioned to after the wheel part is finished
	[SerializeField] protected GameObject objToHideOnPickingGameShow = null;		// Lets you set an object to hide when the picking game is shown, like the wheel
	protected PickemOutcome pickemOutcome = null;				// The outcome for the pickem game

	/// Transition to the picking game, which will need to have init() called on it
	protected virtual void transitionToPickingGame()
	{
		if (objToHideOnPickingGameShow != null)
		{
			objToHideOnPickingGameShow.SetActive(false);
		}

		pickingGame.gameObject.SetActive(true);
		pickingGame.init(pickemOutcome);
	} 

	/// Need to override so we can send the game into the PickingGame portion
	protected override void onWheelSpinComplete()
	{
		StartCoroutine(onWheelSpinCompleteCoroutine());
	}

	/// Coroutine version of onWheelSpinComplete callback so we can handle timing stuff
	protected virtual IEnumerator onWheelSpinCompleteCoroutine()
	{
		long _payout = wheelPick.wins[wheelPick.winIndex].credits;
		
		if (_payout > 0)
		{
			StartCoroutine(rollupAndEnd());
		}
		else
		{
			yield return StartCoroutine(showWinSliceAnimation());

			SlotOutcome pickemGame = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame);
			pickemOutcome = new PickemOutcome(pickemGame);
			
			transitionToPickingGame();
		}
	}
}
