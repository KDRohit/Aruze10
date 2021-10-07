using System.Collections.Generic;

/* 
 * Server action class for handling all the network actions
 */

public class IncentivizedPushNotificationAction : ServerAction 
{
	// Grant incentive key (no response data)
	private const string GRANT_INCENTIVE = "claim_pn_soft_prompt_incentive";

#if !ZYNGA_PRODUCTION
	private const string DEV_RESET_PUSH_NOTIF_INCENTIVE = "reset_pn_soft_prompt_incentive_eligibility";
#endif

	private static Dictionary<string, string[]> _propertiesLookup = null;
	
	private IncentivizedPushNotificationAction(ActionPriority priority, string type) : base(priority, type) {}

	public static void grantIncentive() 
	{
		new IncentivizedPushNotificationAction(ActionPriority.IMMEDIATE, GRANT_INCENTIVE);
		processPendingActions(true);
	}

#if !ZYNGA_PRODUCTION

	public static void devResetPushNotifIncentive()
	{
		new IncentivizedPushNotificationAction(ActionPriority.IMMEDIATE, DEV_RESET_PUSH_NOTIF_INCENTIVE);
		processPendingActions(true);
	}
#endif

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
#if !ZYNGA_PRODUCTION
				_propertiesLookup.Add(DEV_RESET_PUSH_NOTIF_INCENTIVE, new string[] {});
#endif
				_propertiesLookup.Add(GRANT_INCENTIVE, new string[] {});
			}
			return _propertiesLookup;
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		_propertiesLookup = null;
	}

	
}
