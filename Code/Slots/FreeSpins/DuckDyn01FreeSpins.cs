using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Duck dyn01 free spins. Has effect for duck mutation
/// </summary>
public class DuckDyn01FreeSpins : FreeSpinGame
{
	public DuckDyn01.DuckInfo duckInfo;
	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}

	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		this.StartCoroutine(DuckDyn01.doDuckWilds(this, duckInfo, base.reelsStoppedCallback));
	}
}
