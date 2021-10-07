using UnityEngine;
using System.Collections;
using TMPro;
using TMProExtensions;

class LevelUpBonusDialog : DialogBase
{
	public Renderer backgroundRenderer = null;
	public TextMeshPro titleLabel = null;
	public TextMeshPro levelLabel = null;
	public TextMeshPro patternBonusLabel = null;
	public TextMeshPro multiplierBonusLabel = null;
	public TextMeshPro originalCreditsLabel = null;
	public TextMeshPro totalCreditsLabel = null;
	public TextMeshPro vipPointsLabel = null;

	private const string BG_IMAGE_PATH = "level_up/Level_Up_Bonus_Dialog_BG.jpg";
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		int vipPoints = (int)dialogArgs.getWithDefault(D.VIP_POINTS, 0);
		long totalCredits = (long)dialogArgs.getWithDefault(D.TOTAL_CREDITS, 0);
		long bonusCredits = (long)dialogArgs.getWithDefault(D.BONUS_CREDITS, 0);
		int level = (int)dialogArgs.getWithDefault(D.NEW_LEVEL, 0);
		
		string patternKey = LevelUpBonus.patternKey;
		titleLabel.text = Localize.textUpper(patternKey + "_level_up");
		patternBonusLabel.text = Localize.textUpper(patternKey + "_level_bonus");

		multiplierBonusLabel.text = Localize.textUpper("{0}X_level_bonus", LevelUpBonus.multiplier);
		originalCreditsLabel.text = Localize.textUpper("purchased_details{0}", CreditsEconomy.convertCredits(bonusCredits));
		totalCreditsLabel.text = CreditsEconomy.convertCredits(totalCredits);
		vipPointsLabel.text = CommonText.formatNumber(vipPoints);
		levelLabel.text = Localize.textUpper("level_{0}_rewards", level);

		StatsManager.Instance.LogCount("dialog", "level_up", "bonus_level_up_coins", "", "collect", "view");
	}

	public void Update()
	{
	    AndroidUtil.checkBackButton(collectClicked);
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();

		if (!LevelUpBonus.isBonusActive)
		{
			Debug.LogErrorFormat("LevelUpBonusDialog.cs -- onFadeInCompelte -- Showing the dialog while the event is not active. Autoclosing.");
			Dialog.close();
		}
	}
	public void collectClicked()
	{
		StatsManager.Instance.LogCount("dialog", "level_up", "bonus_level_up_coins", "", "collect", "click");
		Dialog.close();
	}
	
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	protected override void playOpenSound ()
	{
		Audio.play("LevelUpHighlight1");
	}
	
	public static void showDialog(long totalCredits, long bonusCredits, int vipPoints, int level)
	{
		if (LevelUpBonus.isBonusActive)
		{
			// MCC Adding in a check to make sure we never show this dialog if the bonus is not active.
			Dialog.instance.showDialogAfterDownloadingTextures("level_up_bonus", BG_IMAGE_PATH,
				Dict.create(
					D.TOTAL_CREDITS, totalCredits,
					D.BONUS_CREDITS, bonusCredits,
					D.VIP_POINTS, vipPoints,
					D.NEW_LEVEL, level
				)
			);
		}
		else
		{
			Debug.LogErrorFormat("LevelUpBonusDialog.cs -- showDialog -- we got a level up bonus amount from the server, but we don't think that it is active on the client. Not showing the dialog.");
			
		}

	}
}
