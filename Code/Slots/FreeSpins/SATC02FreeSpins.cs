using UnityEngine;
using System.Collections;

/* Free spin class for SATC02, clone of duckdyn01 free spins */
public class SATC02FreeSpins : FreeSpinGame 
{
	public Satc02.DiamondInfo diamondInfo; // info for mutation prefab
	public GameObject revealPrefab;
	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}

	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		this.StartCoroutine(Satc02.doDiamondWilds(this, diamondInfo, base.reelsStoppedCallback, revealPrefab));
	}
}
