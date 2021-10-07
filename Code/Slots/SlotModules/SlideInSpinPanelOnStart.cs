using UnityEngine;
using System.Collections;

/**
 *
 * Starts the game after a certian delay
 */ 
public class SlideInSpinPanelOnStart : SlotModule 
{
	[SerializeField] private float TIME_BEFORE_SLIDE = 0.0f;
	[SerializeField] private float TIME_SLIDE_SPIN_PANEL = 0.0f;
	[SerializeField] private float TIME_AFTER_SLIDE = 0.0f;
	[SerializeField] private bool isHidingAllSpinPanelsOnSlotGameStarted = false;

// executeOnSlotGameStartedNoCoroutine() section
// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		if (isHidingAllSpinPanelsOnSlotGameStarted)
		{
			SpinPanel.instance.hidePanels();
		}
	
		SpinPanel.Type type = reelGame.isFreeSpinGame()? SpinPanel.Type.FREE_SPINS : SpinPanel.Type.NORMAL;
		SpinPanel.instance.setSpinPanelPosition(
			type,
			SpinPanel.SpinPanelSlideOutDirEnum.Down,
			false
		);
	}

	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		yield return new TIWaitForSeconds(TIME_BEFORE_SLIDE);
		SpinPanel.Type type = reelGame.isFreeSpinGame()? SpinPanel.Type.FREE_SPINS : SpinPanel.Type.NORMAL;
		// Make sure the panel is shown in case it was hidden when we moved it off screen
		SpinPanel.instance.showPanel(type);
		yield return StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom(
			type,
			SpinPanel.SpinPanelSlideOutDirEnum.Down,
			TIME_SLIDE_SPIN_PANEL,
			false
		));
		yield return new TIWaitForSeconds(TIME_AFTER_SLIDE);
	}
}
