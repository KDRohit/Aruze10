//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// This script can be used to anchor an object to the side or corner of the screen, panel, or a widget.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Anchor")]
public class UIAnchor : TICoroutineMonoBehaviour
{
	public enum Side
	{
		BottomLeft,
		Left,
		TopLeft,
		Top,
		TopRight,
		Right,
		BottomRight,
		Bottom,
		Center,
	}

	bool mNeedsHalfPixelOffset = false;

	/// <summary>
	/// Camera used to determine the anchor bounds. Set automatically if none was specified.
	/// </summary>

	public Camera uiCamera = null;

	/// <summary>
	/// Widget used to determine the container's bounds. Overwrites the camera-based anchoring if the value was specified.
	/// </summary>

	public UIWidget widgetContainer = null;

	/// <summary>
	/// Panel used to determine the container's bounds. Overwrites the widget-based anchoring if the value was specified.
	/// </summary>

	public UIPanel panelContainer = null;

	/// <summary>
	/// Side or corner to anchor to.
	/// </summary>

	public Side side = Side.Center;

	/// <summary>
	/// Whether a half-pixel offset will be applied on windows machines. Most of the time you'll want to leave this as 'true'.
	/// This value is only used if the widget and panel containers were not specified.
	/// </summary>

	public bool halfPixelOffset = true;

	/// <summary>
	/// Relative offset value, if any. For example "0.25" with 'side' set to Left, means 25% from the left side.
	/// </summary>

	public Vector2 relativeOffset = Vector2.zero;
	
	/// <summary>
	/// Pixel offset value if any. For example "10" in x will move the widget 10 pixels to the right 
	/// while "-10" in x is 10 pixels to the left based on the pixel values set in UIRoot.
	/// </summary>
	
	public Vector2 pixelOffset = Vector2.zero;

	public bool adjustPixelOffsetByPixelFactor = false; ///< Added by Zynga Todd. Doubles the pixel offset on iPad retina, since twice as many pixels are needed.

	public List<UIAnchor> dependentAnchors;

	public bool allowPortrait = false; //< Added by Zynga. Scott. Needed to allow for anchoring in portrait mode for custom spin panels used by bonus games that run in portrait mode

	Transform _mTrans;
	Transform mTrans
	{
		get
		{
			if (_mTrans == null)
			{
				_mTrans = transform;
			}
			return _mTrans;
		}
		set
		{
			_mTrans = value;
		}
	}
	
	Animation mAnim;
	Rect mRect = new Rect();
	UIRoot mRoot;
	bool mHasInitialized = false;

	void Awake ()
	{
		mTrans = transform;
		mAnim = GetComponent<Animation>();
	}

	/// <summary>
	/// Automatically find the camera responsible for drawing the widgets under this object.
	/// </summary>

	void Start ()
	{
		if (!mHasInitialized)
		{
			initialize();
			Update();
		}
	}

	private void initialize()
	{
		mRoot = NGUITools.FindInParents<UIRoot>(gameObject);
			
		// Zynga - Todd: Use our own function for finding the appropriate camera to use.
		//if (uiCamera == null) uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
		if (uiCamera == null) uiCamera = NGUIExt.getObjectCamera(gameObject);
		mHasInitialized = true;
	}

	/// Added by Zynga/Todd. Returns the effective pixel offset, which may be adjusted by pixel factor.
	private Vector2 effectivePixelOffset
	{
		get
		{
			if (!adjustPixelOffsetByPixelFactor)
			{
				return pixelOffset;
			}

			Vector2 offset = pixelOffset;
			// Since iPad retina is pixel factor of 1, but we typically set up the scene using non-retina iPad res,
			// double the pixel factor that we divide by here, so it affects iPad retina but not non-retina.
			offset.x *= (NGUIExt.pixelFactor * 2f);
			offset.y *= (NGUIExt.pixelFactor * 2f);
			return offset;
		}
	}


	/// <summary>
	/// Anchor the object to the appropriate point.
	/// </summary>

	void Update ()
	{
		reposition();
	}

	public void reposition()
	{
		if (!mHasInitialized)
		{
			initialize();
		}
		// Zynga - Jon: If the resolution is 0 for width or height, then we have no context, so bail out.
		// This can happen on Android when the app is switched to the background.
		// Also bail out if in portrait mode, to cover iOS 6/7 bug.
		if (Screen.width == 0 || Screen.height == 0 || (Screen.height > Screen.width && !allowPortrait))
		{
			return;
		}
	
		if (mAnim != null && mAnim.enabled && mAnim.isPlaying) return;

		bool useCamera = false;

		if (!System.Object.ReferenceEquals(panelContainer,null))
		{
			if (panelContainer.clipping == UIDrawCall.Clipping.None)
			{
				// Panel has no clipping -- just use the screen's dimensions
				float ratio = (mRoot != null) ? (float)mRoot.activeHeight / Screen.height * 0.5f : 0.5f;
				mRect.xMin = -Screen.width * ratio;
				mRect.yMin = -Screen.height * ratio;
				mRect.xMax = -mRect.xMin;
				mRect.yMax = -mRect.yMin;
			}
			else
			{
				// Panel has clipping -- use it as the mRect
				Vector4 pos = panelContainer.clipRange;
				mRect.x = pos.x - (pos.z * 0.5f);
				mRect.y = pos.y - (pos.w * 0.5f);
				mRect.width = pos.z;
				mRect.height = pos.w;
			}
		}
		else if (!System.Object.ReferenceEquals(widgetContainer,null))
		{
			// Widget is used -- use its bounds as the container's bounds
			Transform t = widgetContainer.cachedTransform;
			Vector3 ls = t.localScale;
			Vector3 lp = t.localPosition;

			Vector3 size = widgetContainer.relativeSize;
			Vector2 offset = widgetContainer.pivotOffset;
			offset.y -= 1f;

			offset.x *= (widgetContainer.relativeSize.x * ls.x);
			offset.y *= (widgetContainer.relativeSize.y * ls.y);

			mRect.x = lp.x + offset.x;
			mRect.y = lp.y + offset.y;

			mRect.width = size.x * ls.x;
			mRect.height = size.y * ls.y;
		}
		// Zynga/Todd - Added pixelHeight and pixelWidth validation.
		else if (uiCamera != null && uiCamera.pixelHeight >= 1.0f && uiCamera.pixelWidth >= 1.0f)
		{
			useCamera = true;
			mRect = uiCamera.pixelRect;
		}
		else 
		{
			return;
		}

		float cx = (mRect.xMin + mRect.xMax) * 0.5f;
		float cy = (mRect.yMin + mRect.yMax) * 0.5f;
		
		Vector3 v;
		if(side != Side.Center)
		{
			if(side == Side.Right || side == Side.TopRight || side == Side.BottomRight)
				v.x = mRect.xMax;
			else if(side == Side.Top || side == Side.Center || side == Side.Bottom)
				v.x = cx;
			else
				v.x = mRect.xMin;

			if(side == Side.Top || side == Side.TopRight || side == Side.TopLeft)
				v.y = mRect.yMax;
			else if(side == Side.Left || side == Side.Center || side == Side.Right)
				v.y = cy;
			else
				v.y = mRect.yMin;
		} 
		else
		{
			v.x = cx;
			v.y = cy;
		}

		float width = mRect.width;
		float height = mRect.height;

		v.x += relativeOffset.x * width;
		v.y += relativeOffset.y * height;
		v.z  = 0;

		if (useCamera)
		{
			if (uiCamera.orthographic)
			{
				v.x = Mathf.Round(v.x);
				v.y = Mathf.Round(v.y);

				// Start Zynga/Todd change. Implement pixel offset adjustment by pixel factor.
				Vector2 offset = effectivePixelOffset;
				v.x += offset.x;
				v.y += offset.y;
//				v.x += pixelOffset.x;	// Original line
//				v.y += pixelOffset.y;	// Original line
				// End Zynga change.

				if (halfPixelOffset && mNeedsHalfPixelOffset)
				{
					v.x -= 0.5f;
					v.y += 0.5f;
				}
			}
			v.z = uiCamera.WorldToScreenPoint(mTrans.position).z;
			v = uiCamera.ScreenToWorldPoint(v);
		}
		else
		{
			v.x = Mathf.Round(v.x);
			v.y = Mathf.Round(v.y);

			// Start Zynga/Todd change. Implement pixel offset adjustment by pixel factor.
			Vector2 offset = effectivePixelOffset;
			v.x += offset.x;
			v.y += offset.y;
//			v.x += pixelOffset.x;	// Original line
//			v.y += pixelOffset.y;	// Original line
			// End Zynga change.

			if (!System.Object.ReferenceEquals(panelContainer,null))
			{
				v = panelContainer.cachedTransform.TransformPoint(v);
			}
			else if (!System.Object.ReferenceEquals(widgetContainer,null))
			{
				Transform t = widgetContainer.cachedTransform.parent;
				if (!System.Object.ReferenceEquals(t,null)) v = t.TransformPoint(v);
			}
			v.z = mTrans.position.z;
		}

		// Wrapped in an 'if' so the scene doesn't get marked as 'edited' every frame

		if (mTrans.position != v)
		{
			mTrans.position = v;
		}

		if (dependentAnchors != null)
		{
			for (int i = dependentAnchors.Count - 1; i >= 0; i--)
			{
				if (dependentAnchors[i] != null)
				{
					dependentAnchors[i].enabled = true;
					dependentAnchors[i].reposition();
				}
				else
				{
					dependentAnchors.RemoveAt(i);
				}
			}
		}

		// Zynga/Todd - Don't destroy, so we can re-enable if resolution changes.
		if (Application.isPlaying) enabled = false;
	}
}
