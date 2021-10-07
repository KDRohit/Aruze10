using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeCycleSalesExperiment : EosExperiment
{
	public string creditPackageName { get; private set; }
	public int bonusPercent { get; private set; }
	public int strikethroughAmount { get; private set; }
	public int countdownTimer { get; private set; }
	public int featureCooldown { get; private set; }
	public int dialogDaysCap { get; private set; }
	public string dialogImage { get; private set; }
	private PurchasablePackage creditPackage;

	public LifeCycleSalesExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		bonusPercent = getEosVarWithDefault(data, "coins_bonus_percent", 0);
		strikethroughAmount = getEosVarWithDefault(data, "dialog_strikethrough_amount", 0);
		countdownTimer = getEosVarWithDefault(data, "dialog_countdown_timer", 0);
		featureCooldown = getEosVarWithDefault(data, "dialog_feature_cooldown", 0);
		creditPackageName = getEosVarWithDefault(data, "coin_package", "");
		dialogDaysCap = getEosVarWithDefault(data, "dialog_days_cap", 0);
		dialogImage = getEosVarWithDefault(data, "lifecycle_dialog_background", "");
		creditPackage = PurchasablePackage.find(creditPackageName);
	}

	public override bool isInExperiment
	{
		get
		{
			return base.isInExperiment && SlotsPlayer.instance != null && creditPackage != null && dialogImage != "";
		}
	}

	public override void reset()
	{
		base.reset();
		creditPackage = null;
		creditPackageName = null;
		bonusPercent = 0;
		strikethroughAmount = 0;
		countdownTimer = 0;
		featureCooldown = 0;
		dialogDaysCap = 0;
		dialogImage = "";
	}
}
