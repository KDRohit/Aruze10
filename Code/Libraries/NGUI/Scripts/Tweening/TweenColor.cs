//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Tween the object's color.
/// </summary>

[AddComponentMenu("NGUI/Tween/Color")]
public class TweenColor : UITweener
{
	public Color from = Color.white;
	public Color to = Color.white;

	Transform mTrans;
	UIWidget mWidget;
	Material[] mMat;			// Todd Gillissie / Zynga: made this an array.
	Light mLight;
	TMPro.TextMeshPro mTMPro;	// Added by Todd Gillissie / Zynga.

	/// <summary>
	/// Current color.
	/// </summary>

	public Color color
	{
		get
		{
			if (mTMPro != null) return mTMPro.color;	// Added by Todd Gillissie / Zynga. Must be before mMat check.
			if (mWidget != null) return mWidget.color;
			if (mLight != null) return mLight.color;
			if (mMat != null) return mMat[0].color;
			return Color.black;
		}
		set
		{
			// Sometimes, somehow, components change after Awake(),
			// so set all components anytime the color is set.
			setColorComponents();
			
			if (mTMPro != null)
			{
				// Added by Todd Gillissie / Zynga.
				// Do this before checking for material, since TextMeshPro objects also have a material but we don't want to change that.
				mTMPro.color = value;
			}
			else
			{
				if (mWidget != null) mWidget.color = value;
//				if (mMat != null) mMat.color = value;	// Todd Gillissie / Zynga: Color an array of materials instead of just one.
				if (mMat != null)
				{
					CommonMaterial.colorMaterials(mMat, value);
				}

				if (mLight != null)
				{
					mLight.color = value;
					mLight.enabled = (value.r + value.g + value.b) > 0.01f;
				}
			}
		}
	}

	/// <summary>
	/// Find all needed components.
	/// </summary>

	void Awake ()
	{
		setColorComponents();
	}
	
	private void setColorComponents()
	{
		mTMPro = GetComponent<TMPro.TextMeshPro>();	// Added by Todd Gillissie / Zynga.
		mWidget = GetComponentInChildren<UIWidget>();
		Renderer ren = GetComponent<Renderer>();
		if (ren != null) mMat = ren.materials;		// Todd Gillissie / Zynga: use the materials array.
		mLight = GetComponent<Light>();
	}

	/// <summary>
	/// Interpolate and update the color.
	/// </summary>

	protected override void OnUpdate(float factor, bool isFinished) { color = Color.Lerp(from, to, factor); }

	/// <summary>
	/// Start the tweening operation.
	/// </summary>

	static public TweenColor Begin (GameObject go, float duration, Color color)
	{
		TweenColor comp = UITweener.Begin<TweenColor>(go, duration);
		comp.from = comp.color;
		comp.to = color;

		if (duration <= 0f)
		{
			comp.Sample(1f, true);
			comp.enabled = false;
		}
		return comp;
	}
}