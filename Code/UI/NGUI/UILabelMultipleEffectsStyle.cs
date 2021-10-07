using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Defines a style that can be applied to UILabel objects using the UILabelStyler script.
*/

public class UILabelMultipleEffectsStyle : UILabelStyle
{
	public UILabel.Effect effect2 = UILabel.Effect.None;
	public float effectDistanceX2 = 0;
	public float effectDistanceY2 = 0;
	public Color effectColor2 = Color.black;
}
