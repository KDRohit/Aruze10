using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Handles display of dialog for telling a player that a touched game is locked.
*/

public class PreviewExpiredDialogHIR : PreviewExpiredDialog
{
	public Transform lockBurst1;
	public Transform lockBurst2;
	
	public override void init()
	{
		base.init();
		
		if (GameState.game != null)
		{
			levelLabel.text = CommonText.formatNumber(GameState.game.unlockLevel);
		}
	}

	protected override void Update()
	{
		base.Update();
		
		// Spin the starbursts.
		float rotate = 5f * Time.deltaTime;
		lockBurst1.transform.Rotate(0, 0, rotate);
		lockBurst2.transform.Rotate(0, 0, -rotate);
	}
}
