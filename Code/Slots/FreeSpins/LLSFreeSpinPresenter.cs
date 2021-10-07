using UnityEngine;
using System.Collections;

public class LLSFreeSpinPresenter : BonusGamePresenter {

	public override void init(bool isCheckingReelGameCarryOverValue)
	{
		// Placeholder init, just in case. We really only need the welcome/summary screen functionality, hence the emptiness, and inheriting BonusGamePresenter
			
		base.init(isCheckingReelGameCarryOverValue);
	}

	new public static void resetStaticClassData(){}
}
