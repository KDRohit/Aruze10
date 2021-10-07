using UnityEngine;
using System.Collections;
using System;
using TMPro;

public class StarterDialogHIR : StarterDialog
{
	public const string IMAGE_PATH = "starter_pack/StarterPack_BG_{0}.png";
	public const string PHYLUM_NAME = "escalate";
	public TextMeshPro expireWordLabel;

	protected override void Update()
	{
		base.Update();
		if (saleTimer == null || saleTimer.isExpired)
		{
			timerLabel.gameObject.SetActive(false);
			expireWordLabel.gameObject.SetActive(false);
		}
	}

	protected override void setCreditPackageData()
	{
		base.setCreditPackageData();
		if (ExperimentWrapper.StarterPackEos.artPackage == "design2")
		{
			packCreditsLabel.text = Localize.textUpper("purchased_details{0}", packCreditsLabel.text);
		}
	}

	new public static void resetStaticClassData()
	{
	}
} 