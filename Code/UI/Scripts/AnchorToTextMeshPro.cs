using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Anchors any Transform to a TextMeshPro object, based on the current size of the displayed text (not the size of the text box).
To avoid transform local to world positions and the headaches related to that,
the targetLabel should be at the same heirachy level as the GameObject with this component.
*/

[ExecuteInEditMode]
public class AnchorToTextMeshPro : MonoBehaviour
{
	public TextMeshPro targetLabel = null;
	[FormerlySerializedAs("position")]
	public TMPro.TextContainerAnchors anchorPosition = TMPro.TextContainerAnchors.Middle;
	public Vector2int pixelOffset = Vector2int.zero;
	
	private TextMeshPro lastTargetLabel = null;
	private TMPro.TextContainerAnchors lastAnchorPosition = TMPro.TextContainerAnchors.Middle;
	private Vector2int lastPixelOffset = Vector2int.zero;
	private Vector2 lastTargetPosition = Vector2.zero;
	private string lastTextValue = "";
	private Vector2 lastTargetLabelPivot = Vector2.zero;
	
	private bool shouldRefresh
	{
		get
		{
			if (targetLabel == null)
			{
				return false;
			}

			return
				targetLabel != lastTargetLabel ||
				anchorPosition != lastAnchorPosition ||
				pixelOffset.x != lastPixelOffset.x ||
				pixelOffset.y != lastPixelOffset.y ||
				targetLabel.transform.localPosition.x != lastTargetPosition.x ||
				targetLabel.transform.localPosition.y != lastTargetPosition.y ||
				lastTextValue != targetLabel.text ||
				lastTargetLabelPivot.x != targetLabel.rectTransform.pivot.x ||
				lastTargetLabelPivot.y != targetLabel.rectTransform.pivot.y;
		}
	}
	
	void Awake()
	{
		refresh();
	}
	
	void Update()
	{
		if (shouldRefresh)
		{
			refresh();
		}
	}
	
	private void refresh()
	{
		if (targetLabel != null)
		{
			Transform targetTransform = targetLabel.transform;
			if (targetTransform.parent != transform.parent)
			{
				Debug.LogWarning("AnchorToTextMeshPro: Target Label should be at the same heirarchy level as the object with this component.", gameObject);
			}

			targetLabel.ForceMeshUpdate();	// Make sure bounds are accurate.
		
			Bounds bounds = targetLabel.bounds;
			Vector3 targetLocalPosition = targetTransform.localPosition;
			Vector2 localPosition = new Vector2(
				targetLocalPosition.x + bounds.center.x,
				targetLocalPosition.y + bounds.center.y
			);
			float extentsX = bounds.extents.x;
			float extentsY = bounds.extents.y;
		
			switch (anchorPosition)
			{
				case TMPro.TextContainerAnchors.Middle:
					// Don't need to do anything since we start in the middle with the bounds.
					break;
				case TMPro.TextContainerAnchors.TopLeft:
					localPosition.x -= extentsX;
					localPosition.y += extentsY;
					break;
				case TMPro.TextContainerAnchors.Top:
					localPosition.y += extentsY;
					break;
				case TMPro.TextContainerAnchors.TopRight:
					localPosition.x += extentsX;
					localPosition.y += extentsY;
					break;
				case TMPro.TextContainerAnchors.Left:
					localPosition.x -= extentsX;
					break;
				case TMPro.TextContainerAnchors.Right:
					localPosition.x += extentsX;
					break;
				case TMPro.TextContainerAnchors.BottomLeft:
					localPosition.x -= extentsX;
					localPosition.y -= extentsY;
					break;
				case TMPro.TextContainerAnchors.Bottom:
					localPosition.y -= extentsY;
					break;
				case TMPro.TextContainerAnchors.BottomRight:
					localPosition.x += extentsX;
					localPosition.y -= extentsY;
					break;
			}
	
			transform.localPosition = new Vector3(localPosition.x + pixelOffset.x, localPosition.y + pixelOffset.y, transform.localPosition.z);

			lastTargetPosition.x = targetLocalPosition.x;
			lastTargetPosition.y = targetLocalPosition.y;
			lastTextValue = targetLabel.text;
			lastTargetLabelPivot = targetLabel.rectTransform.pivot;
		}
		else
		{
			lastTargetPosition = Vector2.zero;
			lastTextValue = "";
			lastTargetLabelPivot = Vector2.zero;
		}
		
		lastTargetLabel = targetLabel;
		lastAnchorPosition = anchorPosition;
		lastPixelOffset.x = pixelOffset.x;
		lastPixelOffset.y = pixelOffset.y;
	}
}
