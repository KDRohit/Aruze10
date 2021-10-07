using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarterPackExperiment : EosExperiment
{

	public string creditPackageName { get; private set; }
	public int bonusPercent { get; private set; }
	public int strikethroughAmount { get; private set; }
	public int countdownTimer { get; private set; }
	public int featureCooldown { get; private set; }
	public string packageOfferString { get; private set; }
	private PurchasablePackage creditPackage;

	// HIR only
	public string artPackage { get; private set; } // Specific Art Template to show on the client

	public StarterPackExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data) 
	{
		bonusPercent = getEosVarWithDefault(data, "starter_pack_coins_bonus_percent", 0);
		strikethroughAmount = getEosVarWithDefault(data, "starter_pack_strikethrough_amount", 0);
		countdownTimer = getEosVarWithDefault(data, "starter_pack_countdown_timer", 0);
		featureCooldown = getEosVarWithDefault(data, "starter_pack_feature_cooldown", 0);
		creditPackageName = getEosVarWithDefault(data, "coin_package", "");
		creditPackage = PurchasablePackage.find(creditPackageName);
		artPackage = getEosVarWithDefault(data, "dialog_art_package", "");
		packageOfferString = getEosVarWithDefault(data, "package_offer_string", "");
	}

	public override bool isInExperiment
	{
		get
			{
				if (base.isInExperiment && SlotsPlayer.instance != null && !SlotsPlayer.instance.isPayerMobile) // The user should never be allowed to purchase a starter pack twice.
				{
					// The timer in starter pack no longer determines the availability of starter pack dialog for HIR.
					return creditPackage != null && artPackage != "";
				}
				else
				{
					return false;
				}
			}
	}

	public override void reset()
	{
		base.reset();
		bonusPercent = 0;
		strikethroughAmount = 0;
		countdownTimer = 0;
		featureCooldown = 0;
		creditPackageName = "";
		creditPackage = null;
		artPackage = "";
		packageOfferString = "";
	}
}
