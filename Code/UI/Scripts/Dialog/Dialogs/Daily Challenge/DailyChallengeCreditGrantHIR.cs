using UnityEngine;
using TMPro;

public class DailyChallengeCreditGrantHIR : DailyChallengeCreditGrant
{
	public const string BACKGROUND_PATH = "daily_challenge/Daily_Challenge_Credit_Grant_BG.png";

	public override void init()
	{
		base.init();
		bodyMessageLabel.text = Localize.text("daily_challenge_credit_grant_desc_{0}_{1}", DailyChallenge.challengeProgressTarget, DailyChallenge.gameName);
	}
}