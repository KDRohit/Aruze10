using System.Collections.Generic;
using UnityEngine;


public class ReelPerspectiveEffectModule : SlotModule
{
	[Tooltip("Scale in Y that a symbol will get on the reel extremes")]
	public float minSymbolScaleY = 0.71f; 					// This scale is the minimal scale a symbol will get applied as local scale when it is about to dissapear either on the top or bottom of the reel (only applies if you use perspective fx)

	[Tooltip("Scale in X that a symbol will get on the reel extremes")]
	public float minSymbolScaleX = 0.86f;					// Minimal scale to be applied on X axis

	[Tooltip("Specify the X Offsets for each reel in left (Element 0) to right order (Last Element)")]
	public List<float> reelSymbolHorizontalOffsets;			// Store one custom offset per reel to be applied to symbols in the horizontal direction, right now only used for faking perspective effect

	public override bool needsToExecuteOnSymbolPositionChanged()
	{
		return true;
	}

	public override void OnSymbolPositionChanged(float symbolNormalizedPosY, int reelIndex, SymbolAnimator symbolAnimator)
	{
		//Scale elements to symbol fake perspective
		symbolAnimator.transform.localScale = getPerspectiveTransformedSymbolScale(symbolNormalizedPosY);

		//Horizontal offset to fake reels perspective
		float offsetX = getPerspectiveTransformedHorizontalOffset(symbolNormalizedPosY,reelIndex); 
		symbolAnimator.positioning = new Vector3(offsetX, symbolAnimator.transform.localPosition.y, symbolAnimator.transform.localPosition.z);
	}

	public Vector3 getPerspectiveTransformedSymbolScale(float symbolNormalizedPosition)
	{
		float perspectiveScaleY = CommonEffects.triangleLERP(symbolNormalizedPosition, minSymbolScaleY, 1);
		float perspectiveScaleX = CommonEffects.triangleLERP(symbolNormalizedPosition, minSymbolScaleX, 1);
		return new Vector3 (perspectiveScaleX, perspectiveScaleY, 1);
	}

	public float getPerspectiveTransformedHorizontalOffset(float symbolNormalizedPosition, int reelIndex)
	{
		return CommonEffects.inverseTriangleLERP(symbolNormalizedPosition,0,reelSymbolHorizontalOffsets[reelIndex]);
	}
}


