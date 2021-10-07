using UnityEngine;

[ExecuteInEditMode]
public class GradientTint : MonoBehaviour
{
	[SerializeField] private UISprite sprite;
	[SerializeField] Gradient gradient;
	[Tooltip("When True, the affected sprite's current tint color will be overridden by this script.\n\nThis toggle is overridden by any linked GradientTintControllers.")]
	public bool useTint;

	[Header("Sample Color (Editor Only)")]
	[Range(0, 1)]
	[Tooltip("This value is overridden by any linked GradientTintControllers.")]
	[SerializeField] private float sampleValue;
	[SerializeField] private Color colorAtValue;
	private Color tintColor;

	private void OnValidate()
	{
		if (!Application.isPlaying)
		{
			if (sprite != null)
			{
				changeColorToSampleValue();
			}
		}
	}

	private void updateColor()
	{
		if (!useTint || sprite == null)
		{
			return;
		}

		if (useTint)
		{
			sprite.color = tintColor;
		}
	}

	//This is used for the in-editor functionality only
	private void changeColorToSampleValue()
	{
		colorAtValue = gradient.Evaluate(sampleValue);
		tintColor = colorAtValue;
		updateColor();
	}

	public void changeColorToValue(float value)
	{
		tintColor = gradient.Evaluate(value);
		updateColor();
	}
}
