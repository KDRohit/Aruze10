using UnityEngine;
using System.Collections;

/**
This script gets attached to a game object to be animated in some way, then it destroys itself once it has completed.
The game object may contain a NGUI UIWidget, in which case the color value may be animated as well.
This script exists because of the animation limitations of the built-in NGUI tweening features.
*/

public class AwesomeTweenScript : TICoroutineMonoBehaviour
{
	public enum CurveType
	{
		LINEAR,
		EASE_IN,
		EASE_OUT,
		EASE_IN_OUT,
		BOUNCE,
		LINEAR_BUMP,
		EASE_IN_BUMP,
		BUMP
	}
	
	public CurveType curveType = CurveType.EASE_IN_OUT;	///< Defines what kind of animation curve to use.
	public float curveLength = .35f;					///< How long the slide curve takes to complete.

	// Possible animation goal options. Only set the ones you want to animate and leave the others alone.
	public float endX = float.NaN;						///< NaN means to leave this coord alone.
	public float endY = float.NaN;						///< NaN means to leave this coord alone.
	public Vector3 endScale = Vector3.down;				///< Vector3.down means leave the scale value alone.
	public Color endColor
	{
		get { return _endColor; }
		
		set
		{
			_endColor = value;
			animateColor = true;
		}
	}
	private Color _endColor = Color.white;
	
	[HideInInspector] public bool markWidgetsAsChanged = false;
	[HideInInspector] public GenericDelegate finishFunction;	///< Optional function to call after the slide has finished.
	
	private float startTime;
	private UIWidget widget;
	private float startX = 0;
	private float startY = 0;
	private Color startColor = Color.white;
	private Vector3 startScale = Vector3.one;
	private bool animateColor = false;		///< Is automatically set to true if endColor property is set and there is a UIWidget.

	new public Collider collider
	{
		get
		{
			if (!checkedCollider)
			{
				_collider = GetComponent<Collider>();
			}
			return _collider;
		}
	}
	private Collider _collider = null;
	private bool checkedCollider = false;

	void Start()
	{
		widget = gameObject.GetComponent<UIWidget>();

		if (widget == null)
		{
			// Can only animate color if there is a widget.
			animateColor = false;
		}

		if (!float.IsNaN(endX))
		{
			startX = transform.localPosition.x;
		}
		
		if (!float.IsNaN(endY))
		{
			startY = transform.localPosition.y;
		}

		if (endScale != Vector3.down)
		{
			startScale = transform.localScale;
		}

		if (animateColor)
		{
			startColor = widget.color;
		}

		if (collider != null)
		{
			// Disable the collider while sliding.
			collider.enabled = false;
		}

		startTime = Time.realtimeSinceStartup;
	}
	
	void Update()
	{
		float elapsed = Time.realtimeSinceStartup - startTime;
		
		Vector3 pos = transform.localPosition;

		float curvePoint = Mathf.Min(1, elapsed / curveLength);
		
		if (!float.IsNaN(endX))
		{
			pos.x = (int)animValue(startX, endX, curvePoint);
		}
		
		if (!float.IsNaN(endY))
		{
			pos.y = (int)animValue(startY, endY, curvePoint);
		}

		if (endScale != Vector3.down)
		{
			float x = animValue(startScale.x, endScale.x, curvePoint);
			float y = animValue(startScale.y, endScale.y, curvePoint);
			float z = animValue(startScale.z, endScale.z, curvePoint);
			
			transform.localScale = new Vector3(x, y, z);
		}

		if (animateColor)
		{
			float r = animValue(startColor.r, endColor.r, curvePoint);
			float g = animValue(startColor.g, endColor.g, curvePoint);
			float b = animValue(startColor.b, endColor.b, curvePoint);
			float a = animValue(startColor.a, endColor.a, curvePoint);
			
			widget.color = new Color(r, g, b, a);
		}
		
		transform.localPosition = pos;
		
		if (markWidgetsAsChanged)
		{
			foreach (UIWidget widget in gameObject.GetComponentsInChildren<UIWidget>())
			{
				widget.MarkAsChanged();
			}
		}
		
		if (elapsed >= curveLength)
		{
			// We're done here.
			if (collider != null && endColor.a > 0)
			{
				// Re-enable the collider if not ending at 0 alpha.
				collider.enabled = true;
			}

			if (finishFunction != null)
			{
				finishFunction();
				finishFunction = null;
			}
			
			Destroy(this);
		}
	}
	
	void OnDestroy()
	{
		// Make sure that, if the object is destroyed early and we
		//  have not yet called the finishFunction, to call it now.
		if (finishFunction != null)
		{
			finishFunction();
			finishFunction = null;
		}
	}
	
	private float animValue(float start, float end, float point)
	{
		float value = 0;
		
		switch (curveType)
		{
			case CurveType.LINEAR:
				value = Mathf.Lerp(start, end, point);
				break;

			case CurveType.EASE_IN:
				value = Mathz.Coserp(start, end, point);
				break;

			case CurveType.EASE_OUT:
				value = Mathz.Sinerp(start, end, point);
				break;

			case CurveType.EASE_IN_OUT:
				value = Mathz.Hermite(start, end, point);
				break;
				
			case CurveType.BOUNCE:
				value = Mathz.Berp(start, end, point);
				break;

			case CurveType.LINEAR_BUMP:
				value = Mathz.LerpBump(start, end, point);
				break;

			case CurveType.EASE_IN_BUMP:
				value = Mathz.HermiteBump(start, end, point);
				break;

			case CurveType.BUMP:
				value = Mathz.SinerpBump(start, end, point);
				break;
		}
		
		return value;
	}
}
