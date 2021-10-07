using UnityEngine;
using System.Collections;
using TMPro;

public class GameOverlayFeatureDisplayProgressive : GameOverlayFeatureDisplay
{
	[SerializeField] private TextMeshPro jackpotTMPro;
	[SerializeField] private GameObject giantJackpotLogo;
	[SerializeField] private GameObject normalJackpotLogo;

	public override void init()
	{
		if (!GameState.game.isMultiProgressive &&
			GameState.game.progressiveJackpots != null &&
			GameState.game.progressiveJackpots.Count > 0)
		{
			GameState.game.progressiveJackpots[0].registerLabel(jackpotTMPro);
		}
		giantJackpotLogo.SetActive(GameState.game.isGiantProgressive);
		normalJackpotLogo.SetActive(!GameState.game.isGiantProgressive);
		base.init();
	}

	public override bool shouldShow
	{
		get
		{
			return SpinPanel.instance != null &&
				SpinPanel.instance.shouldShowJackpot &&
				GameState.game != null &&
				!GameState.game.isMultiProgressive &&
				!SpinPanel.instance.isShowingCollectionOverlay;
		}
	}
}
