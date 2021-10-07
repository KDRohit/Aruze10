using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Defines a style that can be applied to UILabel objects using the UILabelStyler script.
*/

public class UILabelStyle : TICoroutineMonoBehaviour
{
	public UIFont font = null;
	public Color color = Color.white;
	public UILabel.Effect effect = UILabel.Effect.None;
	public float effectDistanceX = 0;
	public float effectDistanceY = 0;
	public Color effectColor = Color.black;
	public UILabel.ColorMode colorMode = UILabel.ColorMode.Solid;
	public Color endGradientColor = Color.white;
	public List<GradientStep> gradientSteps = new List<GradientStep>();
	public string notes;
}
