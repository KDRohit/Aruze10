using UnityEngine;
using System.Collections;

/**
Attach to a game object with a UILabelStyler if you want there to be a different font applied for small devices as opposed to iPads
 */
[RequireComponent(typeof(UILabelStyler))]
public class SmallDeviceFontSwap : MonoBehaviour {

	public UILabelStyle alternateFontStyle;
	public int newMaxHeight = -1;
	public int newMaxWidth = -1;
	public int newLineSpacing = 0;
	
	/// The swap occurs just once when the prefab is created to scale the font properly.  Additionally, you can use the optional
	/// newMaxHeight and newMaxWidth to adjust labels that are being styled for the new size.
	void Awake()
	{
		//Swap the prefab of the UILabelStyler when we awaken with the prefab stored here
		if (alternateFontStyle != null && MobileUIUtil.isSmallMobile)
		{
			UILabelStyler styleToChange = this.gameObject.GetComponent<UILabelStyler>();
			styleToChange.style = alternateFontStyle;
			UILabel labelToChange = styleToChange.label;
			
			// if we can't get the the label from the style then we'll just get the one that this font swap script is attached to
			if (null == labelToChange)
			{
				labelToChange = this.gameObject.GetComponent<UILabel>();
			}

			if (newMaxHeight > 0)
			{
				labelToChange.lineHeight = newMaxHeight;
			}
			if (newMaxWidth > 0)
			{
				labelToChange.lineWidth = newMaxWidth;
			}
			if (newLineSpacing != 0)
			{
				labelToChange.lineSpacing = newLineSpacing;
			}
			styleToChange.updateStyle(); // When we are on an iphone, and not on an editor, this is not actually called after we alter the style on its own.
										 // So we do it here either way before destroying itself.

		}
		Destroy (this);
	}
}
