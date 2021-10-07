using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class DuckDyn03BonusGamePresenter : BonusGamePresenter
{
	private const string SUMMARY_VO = "WRChangeGoodSometimesBackToBasics";

	public override bool gameEnded()
	{
		Audio.play(SUMMARY_VO);
		return base.gameEnded();
	}

	new public static void resetStaticClassData(){}
}