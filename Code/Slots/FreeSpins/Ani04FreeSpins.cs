using UnityEngine;
using System.Collections;

/**
Free Spin Game for ani04 African Thunder

Original Author: Scott Lepthien
*/
public class Ani04FreeSpins : FreeSpinGame 
{
	[SerializeField] private WildOverlayTransformModule wildOverlayTransformModule = null;	// Module that controls the wild overlay effects for this game
	[SerializeField] private float FREESPIN_SUMMARY_VO_DELAY = 1.0f;

	// Constant Variables
	private const string FREE_SPIN_SUMMARY_VO_KEY = "freespin_summary_vo";

	public override void initFreespins()
	{
		BonusGamePresenter.instance.useMultiplier = false;
		mutationManager = new MutationManager(true);
		base.initFreespins();
	}

	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		// Grab the symbol and activate its wild overlay if its the targeted symbol from the mutation
		SymbolAnimator newSymbolAnimator;

		string serverName = SlotSymbol.getServerNameFromName(name);
		if (wildOverlayTransformModule.activeWilds.Contains(serverName))
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

	protected override void gameEnded()
	{
		Audio.play(Audio.soundMap(FREE_SPIN_SUMMARY_VO_KEY), 1.0f, 0.0f, FREESPIN_SUMMARY_VO_DELAY);
		base.gameEnded();
	}
}
