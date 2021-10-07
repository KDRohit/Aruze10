using UnityEngine;
using System.Collections;
using System;


/**
Early Access Dialog
**/

public class EarlyAccessDialogHIR : EarlyAccessDialog
{	
    public FacebookFriendInfo playerBox;
    public VIPNewIcon vipIcon;
    public UITexture gameTexture;

	public override void init()
	{
		playerBox.member = SlotsPlayer.instance.socialMember;    
		vipIcon.setLevel(SlotsPlayer.instance.vipNewLevel);
		
		downloadedTextureToUITexture(gameTexture, 0);
        
        base.init();
	}
		
}
