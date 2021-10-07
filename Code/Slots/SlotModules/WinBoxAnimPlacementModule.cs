using UnityEngine;
using System.Collections;

/**
Ensures that a winbox anim is placed in the correct place, since the spin panel UI is dynamic we need to make sure we line up our game effect with it
*/
public class WinBoxAnimPlacementModule : SlotModule 
{
	[SerializeField] private GameObject winBoxAnim = null;

	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// position the animation where the winnings section of the spin panel is
		SpinPanel spinPanel = SpinPanel.instance;
		if (spinPanel != null)
		{
			SpinPanel.Type spinPanelType = reelGame.isFreeSpinGame() ? SpinPanel.Type.FREE_SPINS : SpinPanel.Type.NORMAL;
			Vector3 winningsPos = spinPanel.getWinningsObjectTransform(spinPanelType).position;
			winBoxAnim.transform.position = new Vector3(winningsPos.x, winningsPos.y, winBoxAnim.transform.position.z);
		}

		yield break;
	}
}
