
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
// WARNING THIS CLASS SLOWS DOWN DEVIES SOMETHING TERRIBLE.
using UnityEngine;
using System.Collections;

// Handles rendering the slotReels on the blur camera while the reels are spinning.
public class BlurEffectModule : SlotModule 
{

	[SerializeField] private Camera blurCamera;
	[SerializeField] private Shader motionBlurShader; // For whatever reason we need to link this here because 
	private MotionBlur motionBlur = null;

	private const bool INCLUDE_CRAPPY_DEVICES = false;
	
	public void Start()
	{
		if (blurCamera != null && (!MobileUIUtil.isCrappyDevice || INCLUDE_CRAPPY_DEVICES))
		{
			// Attach the blur script to the camera.
			motionBlur = blurCamera.gameObject.AddComponent<MotionBlur>() as MotionBlur;
			motionBlur.shader = motionBlurShader;
			motionBlur.enabled = false;
		}
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return motionBlur != null;
	}

	public override IEnumerator executeOnPreSpin()
	{
		motionBlur.enabled = true;
		yield return null;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return motionBlur != null;
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		motionBlur.enabled = false;
		yield return null;
	}

}
