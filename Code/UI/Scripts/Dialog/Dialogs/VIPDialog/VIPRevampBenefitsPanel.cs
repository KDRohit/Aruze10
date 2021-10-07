using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;

public class VIPRevampBenefitsPanel : VIPBenefitsPanel
{
	// =============================
	// PUBLIC
	// =============================
	public TextMeshPro vipName;
	public UISprite specialEventAccess;
	public UISprite dedicatedAccountManager;
	public UISprite vipIconReflection;
	
	// =============================
	// CONST
	// =============================
	protected const int STARTING_X = -955;

	public override void setVIPLevel(VIPLevel level)
	{
 		if (level == null)
		{
 			Bugsnag.LeaveBreadcrumb( "VIPRevampBenefitsPanel: VIPLevel was not set" );
 			return;				
		}
		else if(level.name == null)
		{
 			Bugsnag.LeaveBreadcrumb( "VIPRevampBenefitsPanel: VIPLevel.name was not set" );
 			return;
 		}
 		else if (dedicatedAccountManager == null)
		{
 			Bugsnag.LeaveBreadcrumb( "VIPRevampBenefitsPanel: dedicated account manager sprite is null" );
 			return;
 		}
 		else if (specialEventAccess == null)
		{
 			Bugsnag.LeaveBreadcrumb( "VIPRevampBenefitsPanel: special event access sprite is null" );
 			return;
 		}
 		else if (vipName == null)
		{
 			Bugsnag.LeaveBreadcrumb( "VIPRevampBenefitsPanel: vip text name is null" );
 			return;
 		}
 
		base.setVIPLevel(level);

		vipName.text = level.name;
		SafeSet.gameObjectActive(dedicatedAccountManager.gameObject, level.dedicatedAccountManager);
		SafeSet.gameObjectActive(specialEventAccess.gameObject, level.invitationToSpecialEvents);

		if (vipIconReflection != null)
		{
			string keyName = level.keyName;
			Regex reg = new Regex(@"vip_new_(\d+)_");
			keyName = reg.Replace(keyName, "");
			vipIconReflection.spriteName = keyName + "_reflection";
			vipIconReflection.color = new Color(1f, 1f, 1f, 0.5f);
		}
	}
}
