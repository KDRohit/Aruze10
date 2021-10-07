using UnityEngine;
using System.Collections;

/* Free spin class for Superman01, clone of SATC02 free spins */
public class Superman01FreeSpins : FreeSpinGame 
{
	public Superman01.SupermanInfo supermanInfo; // info for mutation prefab
	public GameObject revealPrefab;
	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}
	
	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		this.StartCoroutine(Superman01.doSupermanWilds(this, supermanInfo, base.reelsStoppedCallback, revealPrefab));
	}
}
