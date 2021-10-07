using UnityEngine;
using System.Collections;

/**
Reparents the whole bonus game object starting from the BonusGamePresenter under the ReelGame ReelGameBackground object
useful for stuff like banner portal games that need to be attached to the reel game background
*/
public class ChallengeGameReparentUnderReelGameBackgroundScriptModule : ChallengeGameModule 
{
	// Enable round start action
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}
	
	// Copies the ReelGameBackground transform onto an object in this game on round start
	public override IEnumerator executeOnRoundStart()
	{
		if (ReelGame.activeGame != null && ReelGame.activeGame.reelGameBackground != null)
		{
			BonusGamePresenter.instance.transform.parent = ReelGame.activeGame.reelGameBackground.challengeGameAttachTransform;
			BonusGamePresenter.instance.transform.localPosition = Vector3.zero;
			BonusGamePresenter.instance.transform.localScale = Vector3.one;
		}
		else
		{
			Debug.LogError("ChallengeGameReparentUnderReelGameBackgroundScriptModule.executeOnRoundStart() - Couldn't grab ReelGameBackground script! ReelGame.activeGame = " + ReelGame.activeGame + "; ReelGame.activeGame.reelGameBackground = " + ReelGame.activeGame.reelGameBackground);
		}

		yield break;
	}
}
