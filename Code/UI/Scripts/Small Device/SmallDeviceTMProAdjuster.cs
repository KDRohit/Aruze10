using UnityEngine;
using System.Collections;
using TMPro;
using TMProExtensions;

/*
Attach to a game object with a TextMeshPro if you want there to be a different properties
applied for small devices as opposed to tablets.
*/

[RequireComponent(typeof(TextMeshPro))]
public class SmallDeviceTMProAdjuster : MonoBehaviour
{
	public Material alternateMaterial;
	public int newMaxHeight = 0;
	public int newMaxWidth = 0;
	public int newMaxFontSize = 0;
	public int newLineSpacing = 0;
	
	void Awake()
	{
		//Swap the prefab of the UILabelStyler when we awaken with the prefab stored here
		if (MobileUIUtil.isSmallMobile)
		{
			TextMeshPro tmPro = gameObject.GetComponent<TextMeshPro>();
			if (tmPro != null)
			{
				if (alternateMaterial != null)
				{
					tmPro.fontMaterial = alternateMaterial;
				}
				
				if (newMaxHeight > 0)
				{
					// TODO:UNITY2018:obsoleteTextContainer:confirm
					tmPro.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newMaxHeight);
				}
				
				if (newMaxWidth > 0)
				{
					// TODO:UNITY2018:obsoleteTextContainer:confirm
					tmPro.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newMaxWidth);
				}
				
				if (newMaxFontSize > 0)
				{
					tmPro.fontSizeMax = newMaxFontSize;
				}
				
				tmPro.lineSpacing = newLineSpacing;
				
				tmPro.UpdateMeshPadding();
			}
		}
		Destroy (this);
	}
}
