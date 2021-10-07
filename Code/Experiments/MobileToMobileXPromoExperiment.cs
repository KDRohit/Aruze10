using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileToMobileXPromoExperiment : EosExperiment
{

	public string recipient { get; private set; }		
	public bool shouldOOCAutoPop { get; private set; }
	public int OOCMaxViews { get; private set; }
	
	public bool shouldRTLAutoPop { get; private set; }
	public int RTLMaxViews { get; private set; }
	public int autoPopCooldown { get; private set; }
	public bool enablePlay { get; private set; }

	public int dialogMaxViewToSwap { get; private set; }
	public int autoSwapRotationTimes { get; private set; }
	public string[] dialogArtCampaigns;

	public  string getArtCampaign()
	{
		if (!isInExperiment || null == dialogArtCampaigns || dialogArtCampaigns.Length == 0)
		{
			return "";
		}
		int artIndex = CustomPlayerData.getInt(CustomPlayerData.XPROMO_ART_CHANGE_COUNT , 0);
		artIndex = artIndex % dialogArtCampaigns.Length;
		return dialogArtCampaigns[artIndex];
	}

	public string[] getAllArtInCampaign()
	{
		if (!isInExperiment)
		{
			return null;
		}

		return dialogArtCampaigns;
		
	}

	public string getDialogArt()
	{
		string campaign = getArtCampaign();
		string game = "";
		if (!string.IsNullOrEmpty(campaign))
		{
			int index = campaign.LastIndexOf("_");
			if (index >= 0)
			{
				game = campaign.Substring(0, index);
			}	
		}
		
		if (string.IsNullOrEmpty(game))
		{
			return "";
		}

		return "xpromo/" + game + "/" + campaign + "_DialogBG.png";
	}

	public string getPromoGame()
	{
		string campaign = getArtCampaign();
		return getPromoGameFromArt(campaign);

	}

	public string getPromoGameFromArt(string campaign)
	{
		if (string.IsNullOrEmpty(campaign))
		{
			return "";
		}

		int index = campaign.LastIndexOf("_");
		if (index < 0)
		{
			return "";
		}
		return campaign.Substring(0, index);
	}

	public string playUrl
	{
		get 
		{ 
			string game = getPromoGame();
			return getPlayUrl(game);
		}
	}

	private static string getPlayUrl(string gameKey)
	{
		if (string.IsNullOrEmpty(gameKey))
		{
			return "";
		}
		return Data.liveData.safeGetString("XPROMO_URL_" + gameKey.ToUpper() + "_PLAY", "");
	}

	public string installUrl
	{
		get
		{
			string game = getPromoGame();
			string artCampaign = getArtCampaign();
			return getInstallUrl(artCampaign, game);
		}
	}

	private static string getInstallUrl(string artCampaign, string gameKey)
	{
		string campaignSpecificLiveDataKey = "XPROMO_URL_" + artCampaign.ToUpper();
		string campaignInstallUrl = Data.liveData.safeGetString(campaignSpecificLiveDataKey, "");
		if (string.IsNullOrEmpty(campaignInstallUrl))
		{
			// If we failed to get a campaign specific install url, just try to find a SKU one.

			if (string.IsNullOrEmpty(gameKey))
			{
				return "";
			}
			return Data.liveData.safeGetString("XPROMO_URL_" + gameKey.ToUpper(), "");
		}
		return campaignInstallUrl;
	}

	public MobileToMobileXPromoExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		shouldOOCAutoPop = getEosVarWithDefault(data, "ooc_auto_pop", false);
		shouldRTLAutoPop = getEosVarWithDefault(data, "rtl_auto_pop", false);
		OOCMaxViews = getEosVarWithDefault(data, "ooc_max_view", 0);
		RTLMaxViews = getEosVarWithDefault(data, "rtl_max_view", 0);
		autoPopCooldown = getEosVarWithDefault(data, "auto_pop_cooldown_min", 0) * Common.SECONDS_PER_MINUTE;
		autoSwapRotationTimes = getEosVarWithDefault(data, "auto_swap_rotation_times", 1);
		dialogMaxViewToSwap = getEosVarWithDefault(data, "dialog_max_view_to_swap", 0);
		recipient = getEosVarWithDefault(data, "recipient", "");
		enablePlay = getEosVarWithDefault(data, "enable_play", false);
		if (!string.IsNullOrEmpty(recipient))
		{
			dialogArtCampaigns = recipient.Split(',');	
		}

		// Disable campaigns that aren't setup right.
		string game = "";
		string campaign = "";

		List<string> artCampaigns = new List<string>(dialogArtCampaigns);
		for (int i = artCampaigns.Count - 1; i >= 0 ; i--)
		{
			campaign = artCampaigns[i];
			game = getPromoGameFromArt(campaign);
			if (string.IsNullOrEmpty(getInstallUrl(campaign, game)) ||
			    string.IsNullOrEmpty(getPlayUrl(game)))
			{
				artCampaigns.RemoveAt(i);
				Debug.LogWarningFormat("ExperimentWrapper.cs -- xPromo setup -- campaign setup failed, missing liveData urls for campaign: {0}", campaign);
			}
		}

		dialogArtCampaigns = artCampaigns.ToArray();
	}

	public override bool isInExperiment
	{
		get
		{
			return base.isInExperiment && !string.IsNullOrEmpty(recipient);
		}
	}

	public override void reset()
	{
		base.reset();
	
		recipient  = "";
		shouldOOCAutoPop = false;
		shouldRTLAutoPop = false;
		OOCMaxViews = 0;
		RTLMaxViews = 0;
		autoPopCooldown = 0;
		autoSwapRotationTimes = 1;
		dialogMaxViewToSwap = 0;
		enablePlay = false;
		dialogArtCampaigns = null;
	}
}
