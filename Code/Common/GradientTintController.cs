using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GradientTintController : MonoBehaviour
{
	[SerializeField] private List<GradientTint> gradientTints;
	[Range(0,1)] public float sampleValue;
	[Tooltip("When True, the affected sprites' current tint colors will be overridden by their Gradient Tint color values.")]
	public bool useGradientTint;

	private void OnValidate()
	{
		if (!Application.isPlaying)
		{
			changeColorToSampleValue();
		}
	}

	public void updateColor(float newValue, bool activateGradient)
	{
		useGradientTint = activateGradient;
		if (gradientTints.Count > 0)
		{
			foreach (GradientTint gradient in gradientTints)
			{
				gradient.useTint = useGradientTint;
				if (useGradientTint)
				{
					gradient.changeColorToValue(newValue);
				}
			}
		}
	}

	private void changeColorToSampleValue()
	{
		updateColor(sampleValue, useGradientTint);
	}
}
