using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class IncentivizedUpdate
{
	private static bool LOCAL_TESTING = false; // set to true to test the against a specific version
	private const string LOCAl_TESTING_VERSION = "1.7.6610";

	public static void init()
	{
		if (LOCAL_TESTING)
		{
			CustomPlayerData.setValue(CustomPlayerData.COLLECTED_UPDATE_REWARD, false);
		}

		if (isValidToSurface && meetsClientRequirements)
		{
			displayDialog();
		}
	}
	
	private static void displayDialog()
	{
		if (isValidToSurface)
		{
			if (meetsClientRequirements)
			{
				GenericDialog.showDialog
				(
					Dict.create
					(
						D.TITLE, 	Localize.text("incentivized_app_title"),
						D.MESSAGE, 	Localize.text("incentivized_app_body_{0}", CreditsEconomy.convertCredits((long)ExperimentWrapper.IncentivizedUpdate.coins)),
						D.OPTION1, 	Localize.textUpper("remind_me_later"),
						D.OPTION2, 	Localize.textUpper("upgrade_now"),
						D.REASON, 	"upgrade-client",
						D.CALLBACK, new DialogBase.AnswerDelegate(onUpgradeClient)
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
			}
			else
			{
				RewardAction.validateReward(ExperimentWrapper.IncentivizedUpdate.iLink);
				CustomPlayerData.setValue(CustomPlayerData.COLLECTED_UPDATE_REWARD, true);
			}
		}
	}

	private static void onUpgradeClient(Dict args = null)
	{
#if !UNITY_EDITOR
		Common.openUrlWebGLCompatible(Glb.clientAppstoreURL);
#endif
	}

	private static bool meetsClientRequirements
	{
		get
		{
			string glbVersion = string.Join("", LOCAL_TESTING ? LOCAl_TESTING_VERSION.Split('.') : Glb.clientVersion.Split('.'));
			string minClient = string.Join("", ExperimentWrapper.IncentivizedUpdate.minClient.Split('.'));

#if UNITY_EDITOR
			return false;
#else
			return int.Parse(glbVersion) <= int.Parse(minClient);
#endif
		}
	}

	public static bool isValidToSurface
	{
		get
		{
			// do not enable this for webgl, even if you're whitelisted (because QA will bug this later)
#if !UNITY_WEBGL			
			return ExperimentWrapper.IncentivizedUpdate.isInExperiment && !hasCollected;
#else
			return false;
#endif			
		}
	}

	private static bool hasCollected
	{
		get
		{
			return CustomPlayerData.getBool(CustomPlayerData.COLLECTED_UPDATE_REWARD, true);
		}
	}
}
