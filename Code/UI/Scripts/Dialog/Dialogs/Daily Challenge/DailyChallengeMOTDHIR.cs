using UnityEngine;
using TMPro;
using System.Collections;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class DailyChallengeMOTDHIR : DailyChallengeMOTD
{
	public const string BACKGROUND_IMAGE_PATH = "daily_challenge/Daily_Challenge_MOTD_BG.jpg";
	public TextMeshPro timerLabel;
	public TextMeshPro buttonLabel;
	public Renderer backgroundRenderer;

	public override void init()
	{
		base.init();
		downloadedTextureToRenderer(backgroundRenderer, 1);
		bodyMessageLabel.text = Localize.text("daily_challenge_desc_{0}_{1}", DailyChallenge.challengeProgressTarget, DailyChallenge.gameName);

		if (GameState.isMainLobby)
		{
			buttonLabel.text = Localize.textUpper("play_now!");
		}
		else
		{
			buttonLabel.text = Localize.textUpper("ok");
		}
	}

	protected override void updateTimer()
	{
		timerLabel.text = DailyChallenge.timerRange.timeRemainingFormatted;
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}
}
