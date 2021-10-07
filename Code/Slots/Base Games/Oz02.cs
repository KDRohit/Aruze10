using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Oz02 : SlotBaseGame
{
	public GameObject crystalBall;
	private string mutationTarget = "";
	private bool doWildReplacement = false;

	/// slotOutcomeEventCallback - after a spin occurs, Server calls this with the results.
	protected override void slotOutcomeEventCallback(JSON data)
	{
		if (isSpinTimedOut)
		{
			// Not matching base class because this class has many changes to it.
			return;
		}

		// cancel the spin timeout since we recieved a response from the server
		setIsCheckingSpinTimeout(false);

		base.setOutcome(data);

		if (mutationManager.mutations.Count > 0)
		{
			this.StartCoroutine(this.doWilds());
		}
		else
		{
			this.setEngineOutcome(_outcome);
		}

		// IF ANYONE ELSE DOES THIS I WILL HUNT YOU DOWN AND HURT YOU
		// Usually it is done in the base class!  But there is no call to 
		// the base class here so I have to jam the code in here too.
		// This is how we measure the timing between requesting a spin from
		// the server and receieving it
#if ZYNGA_TRAMP
		AutomatedPlayer.spinReceived();
#endif
	}

	/// Brings up the crystal ball and activates the wild overlay for the chosen symbol
	private IEnumerator doWilds()
	{
		// Create the crystal ball
		GameObject ball = CommonGameObject.instantiate(crystalBall) as GameObject;
		// Find the symbol material in the ball
		Transform ballT = ball.transform.Find("OZ02_Reel_CrystalBall_symbol_mesh");
		MeshRenderer ballRenderer = ballT.GetComponent<MeshRenderer>();
		PlayingAudio whirl = Audio.play("symbol_whirl");

		// Find the symbol that is being mad wild
		JSON mut = outcome.getMutations()[0];

		mutationTarget = mut.getString("replace_symbol", "");

		// Replace the ball's symbol texture with the symbol that is being changed
		SymbolInfo info = findSymbolInfo(mutationTarget);
		if (info != null)
		{
			ballRenderer.material.mainTexture = info.getTexture();
		}

		if (whirl != null)
		{
			Audio.play("roll_wild",1,0,whirl.endAfter);
		}
		
		// wait a bit for the crystal ball to run through some of its animations, start drawing the symbol with wild
		yield return new WaitForSeconds(4.25f);
		doWildReplacement = true;
		yield return new WaitForSeconds(3.0f);
		Audio.play("disperse_ball");
		this.setEngineOutcome(_outcome);
	}

	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		// Grab the symbol and activate its wild overlay if its the targeted symbol from the mutation
		SymbolAnimator newSymbolAnimator;

		string serverName = SlotSymbol.getServerNameFromName(name);
		if (doWildReplacement && serverName == mutationTarget)
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(serverName, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
			newSymbolAnimator.showWild();
		}
		else
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
		}
		
		return newSymbolAnimator;
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		mutationTarget = "";
		doWildReplacement = false;
	}
}
