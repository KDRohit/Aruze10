using UnityEngine;
using System.Collections;

public class StatsZIS
{
	/// <summary>
	/// Logs carousel click, leading to zis sign in
	/// </summary>
	public static void logCarousel()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "carousel_card",
			phylum: "event",
			klass: "zis_sign_in",
			family: "",
			genus: "click"
		);
	}

	/// <summary>
	/// Logs click for zis sign in from the settings dialog
	/// family can be "manage_account" or "zis_sign_in"
	/// </summary>
	public static void logSettingsZis(string family="zis_sign_in", string genus="click", string kingdom = "settings")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: kingdom,
			phylum: "",
			klass: "",
			family: family,
			genus: genus
		);
	}

	/// <summary>
	/// Logs view/close for zis sign in dialog, and sign in options for family. This will be
	/// sign_in_with_<insert family>, email, facebook, mobile, or apple
	/// </summary>
	/// <param name="state"></param>
	public static void logZisSignIn(string family, string genus)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "zis_sign_in",
			phylum: "",
			klass: "",
			family: family,
			genus: genus
		);
	}

	/// <summary>
	/// Logs sign in from email
	/// </summary>
	/// <param name="phylum">new_user, or existing_user</param>
	/// <param name="klass">email_from_apply, email_from_facebook, no_email</param>
	/// <param name="family">"", edit, awesome, send_confirmation</param>
	/// <param name="genus">view/click/close</param>
	public static void logZisSigninEmail(string phylum, string klass, string family="", string genus="")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "zis_sign_in_email",
			phylum: phylum,
			klass: klass,
			family: family,
			genus: genus
		);
	}

	/// <summary>
	/// Logs sign in from incentivized CTA
	/// </summary>
	/// <param name="phylum">"",facebook_account, facebook_email, sign_in_with_apple_account,sign_in_with_apple_email,email</param>
	/// <param name="klass">email_from_apply, email_from_facebook, no_email</param>
	/// <param name="family">"", CTA</param>
	/// <param name="genus">view/click/close</param>
	public static void logZisSigninIncentive(string phylum="", string family="", string genus="", string klass="")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "zis_sign_in_incentive",
			phylum: phylum,
			klass: klass,
			family: family,
			genus: genus
		);
	}

	/// <summary>
	/// Logs sign in from the restore account dialog
	/// </summary>
	/// <param name="phylum">"",facebook_account, facebook_email, sign_in_with_apple_account,sign_in_with_apple_email,email</param>
	/// <param name="klass">email_from_apply, email_from_facebook, no_email</param>
	/// <param name="family">"", CTA</param>
	/// <param name="genus">view/click/close</param>
	public static void logZisSigninRestoreAccount(string phylum="", string family="", string genus="")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "zis_sign_in_facebook",
			phylum: phylum,
			klass: "",
			family: family,
			genus: genus
		);
	}

	/// <summary>
	/// Logs the manage account dialog
	/// </summary>
	/// <param name="phylum">"", or edit
	/// <param name="family">mobile,email,facebook,apple,loyalty_lounge,logout</param>
	/// <param name="genus">view/click/close</param>
	public static void logZisManageAccount(string phylum="", string family="", string genus="")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "zis_manage_account",
			phylum: phylum,
			klass: "",
			family: family,
			genus: genus
		);
	}

	/// <summary>
	/// Logs sign out from zis
	/// </summary>
	/// <param name="family">keep_playing, sign_out </param>
	/// <param name="genus">view/click/close</param>
	public static void logZisSignOut(string phylum="", string family="", string genus="")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "zis_sign_out",
			phylum: phylum,
			klass: "",
			family: family,
			genus: genus
		);
	}

	/// <summary>
	/// Logs stats from the associated account dialog
	/// </summary>
	/// <param name="family">keep_playing, switch_to_this_account, contact_customer_service </param>
	/// <param name="genus">view/click/close</param>
	public static void logZisAssociatedAccount(string family="", string genus="")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "zis_associated_account",
			phylum: "",
			klass: "",
			family: family,
			genus: genus
		);
	}
}