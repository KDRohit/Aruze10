using UnityEngine;
using System.Collections;
using TMPro;

public class GameOverlayFeatureDisplayMultiprogressive : GameOverlayFeatureDisplay
{
	[SerializeField] private TextMeshPro[] multiProgressiveTMPros;
	public override void init()
	{
		if (shouldShow)
		{
			GameState.game.registerMultiProgressiveLabels(multiProgressiveTMPros, false);
		}
		base.init();
	}

	public override bool shouldShow
	{
		get
		{
			return SpinPanel.instance != null &&
				SpinPanel.instance.shouldShowJackpot &&
				GameState.game != null &&
				GameState.game.isMultiProgressive &&
				!SpinPanel.instance.isShowingCollectionOverlay;
		}
	}	
}

