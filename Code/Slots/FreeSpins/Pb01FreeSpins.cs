using UnityEngine;
using System.Collections;

public class Pb01FreeSpins : FreeSpinGame 
{
	[SerializeField] private ReelGameBackground backgroundScript;		// Need this to force the wings to show up correctly
	// Sound names
	private const string SUMMARY_VO = "FreespinSummaryVOPbride";

	public override void initFreespins()
	{
		backgroundScript.forceShowFreeSpinWings();
		base.initFreespins();
	}

	protected override void gameEnded()
	{
		Audio.play(SUMMARY_VO);
		base.gameEnded();
	}
}
