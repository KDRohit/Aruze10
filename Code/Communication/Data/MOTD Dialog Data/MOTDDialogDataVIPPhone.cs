using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MOTDDialogDataVIPPhone : MOTDDialogData {

	private const string TS_KEY = "reward_for_vip_phone_num";

	public override bool shouldShow
	{
		get
		{
			return (VIPPhoneCollectDialog.isActive &&
				!((Data.login.getJSON("player").getJSON("my_timestamps") == null) ? false :	
					Data.login.getJSON("player").getJSON("my_timestamps").hasKey(TS_KEY)));
		}
	}

	public override bool show()
	{
		return VIPPhoneCollectDialog.showDialog(keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}

