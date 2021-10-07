using UnityEngine;
using System.Collections;

/**
 * This class handles the Flash Gordon base game. All it has to do is animate the flash gordon asset
 */
public class Com03 : SlotBaseGame 
{
	private CoroutineRepeater flashAnimController;						// Class to call the flash gordon idle animation(s) on a loop	
	private int animCount = 1;
	private bool canDoAnim = true;

	public Animation flashGordonAnim;									// Inspector varialbe for the actualy Flash Gordon GameObject Animation component

	private const float MIN_TIME_ANIM = 2.0f;
	private const float MAX_TIME_ANIM = 5.0f;

	protected override void Awake()
	{
		base.Awake();
		flashAnimController = new CoroutineRepeater(MIN_TIME_ANIM, MAX_TIME_ANIM, animCallback);
	}

	
	protected override void Update()
	{
		base.Update();
		if (canDoAnim)
		{
			flashAnimController.update();
		}
	}

	// play the next idle animation, eventually looping back around to the first one
	private IEnumerator animCallback()
	{
		canDoAnim = false;

		flashGordonAnim.Play("com03_Flash_idle_Animation" + ((animCount % 3) + 1));
		animCount++;
		yield return new TIWaitForSeconds(flashGordonAnim.clip.length);

		canDoAnim = true;
	}
}
