using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class VIPNewIconRevamp : VIPNewIcon
{	
	public UISprite iconFrame;

	public override void setLevel(int level)
	{
		setLevel(VIPLevel.find(level));
	}

	public override void setLevel(VIPLevel vipLevel)
	{
		if (vipLevel != null)
		{
			if (levelIcon != null)
			{
				string keyName = vipLevel.keyName;
				Regex reg = new Regex(@"vip_new_(\d+)_");
				keyName = reg.Replace(keyName, "");
				levelIcon.spriteName = keyName;

				if (iconFrame != null)
				{					
					iconFrame.spriteName = keyName + "_ring";
				}
			}
		}
	}
}