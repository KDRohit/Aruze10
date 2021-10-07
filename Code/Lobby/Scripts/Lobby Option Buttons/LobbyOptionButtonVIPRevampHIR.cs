using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;

public class LobbyOptionButtonVIPRevampHIR : LobbyOptionButtonVIPHIR
{
    public GameObject lockedState;
	public TextMeshPro lockLabel;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
	}

	public override void refresh()
	{
		base.refresh();
		if (option.type == LobbyOption.Type.GAME)
		{
			if (!option.game.isUnlocked)
			{
				lockedState.SetActive(true);
				lockLabel.text = option.game.vipLevel.name + " Required";
				imageTint = Color.grey;
			}
			else
			{
				lockedState.SetActive(false);
				imageTint = Color.white;
			}

			if (frame != null)
			{
				string keyName = option.game.vipLevel.keyName;
				Regex reg = new Regex(@"vip_new_(\d+)_");
				keyName = reg.Replace(keyName, "");
				frame.spriteName = keyName + "_frame Stretchy";				
			}
		}
	}
}