//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script can be used to stretch objects relative to the screen's width and height.
/// The most obvious use would be to create a full-screen background by attaching it to a sprite.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Stretch")]
public class UIStretch : TICoroutineMonoBehaviour
{
	public enum Style
	{
		None,
		Horizontal,
		Vertical,
		Both,
		BasedOnHeight,
		FillKeepingRatio, // Fits the image so that it entirely fills the specified container keeping its ratio
		FitInternalKeepingRatio // Fits the image/item inside of the specified container keeping its ratio
	}

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
	/// Stretching style.
	/// </summary>

	public Style style = Style.None;

	/// <summary>
	/// Relative-to-target size.
	/// </summary>

	public Vector2 relativeSize = Vector2.one;

	/// <summary>
	/// The size that the item/image should start out initially.
	/// Used for FillKeepingRatio, and FitInternalKeepingRatio.
	/// Contributed by Dylan Ryan.
	/// </summary>

	public Vector2 initialSize = Vector2.one;

	// Added by Zynga/Todd - Adjust the size by pixels (after relativeSize is applied).
	public Vector2 pixelOffset = Vector2.zero;
	public List<UIAnchor> dependentAnchors;
	public List<UIStretch> dependentStretches;

	Transform mTrans;
	UIRoot mRoot;
	Animation mAnim;
	Rect mRect;

	void Awake ()
	{
		mAnim = GetComponent<Animation>();
		mRect = new Rect();
		mTrans = transform;
	}

	void Start ()
	{
		// Zynga - Todd: Use our own function for finding the appropriate camera to use.
//		if (uiCamera == null) uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
		if (uiCamera == null) uiCamera = NGUIExt.getObjectCamera(gameObject);
		
		mRoot = NGUITools.FindInParents<UIRoot>(gameObject);
		Update();
	}

	void Update ()
	{
		if (mAnim != null && mAnim.isPlaying) return;

		if (style != Style.None)
		{
			float adjustment = 1f;

			if (panelContainer != null)
			{
				if (panelContainer.clipping == UIDrawCall.Clipping.None)
				{
					// Panel has no clipping -- just use the screen's dimensions
					mRect.xMin = -Screen.width * 0.5f;
					mRect.yMin = -Screen.height * 0.5f;
					mRect.xMax = -mRect.xMin;
					mRect.yMax = -mRect.yMin;
				}
				else
				{
					// Panel has clipping -- use it as the rect
					Vector4 pos = panelContainer.clipRange;
					mRect.x = pos.x - (pos.z * 0.5f);
					mRect.y = pos.y - (pos.w * 0.5f);
					mRect.width = pos.z;
					mRect.height = pos.w;
				}
			}
			else if (widgetContainer != null)
			{
				// Widget is used -- use its bounds as the container's bounds
				Transform t = widgetContainer.cachedTransform;
				Vector3 ls = t.localScale;
				Vector3 lp = t.localPosition;

				Vector3 size = widgetContainer.relativeSize;
				Vector3 offset = widgetContainer.pivotOffset;
				offset.y -= 1f;

				offset.x *= (widgetContainer.relativeSize.x * ls.x);
				offset.y *= (widgetContainer.relativeSize.y * ls.y);

				mRect.x = lp.x + offset.x;
				mRect.y = lp.y + offset.y;

				mRect.width = size.x * ls.x;
				mRect.height = size.y * ls.y;
			}
			else if (uiCamera != null)
			{
				mRect = uiCamera.pixelRect;
				if (mRoot != null) adjustment = mRoot.pixelSizeAdjustment;
			}
			else return;

			float rectWidth = mRect.width;
			float rectHeight = mRect.height;

			if (adjustment != 1f && rectHeight > 1f)
			{
				float scale = mRoot.activeHeight / rectHeight;
				rectWidth *= scale;
				rectHeight *= scale;
			}

			Vector3 localScale = mTrans.localScale;

			if (style == Style.BasedOnHeight)
			{
				localScale.x = relativeSize.x * rectHeight;
				localScale.y = relativeSize.y * rectHeight;
			}
			else if (style == Style.FillKeepingRatio)
			{
				// Contributed by Dylan Ryan
				float screenRatio = rectWidth / rectHeight;
				float imageRatio = initialSize.x / initialSize.y;

				if (imageRatio < screenRatio)
				{
					// Fit horizontally
					float scale = rectWidth / initialSize.x;
					localScale.x = rectWidth;
					localScale.y = initialSize.y * scale;
				}
				else
				{
					// Fit vertically
					float scale = rectHeight / initialSize.y;
					localScale.x = initialSize.x * scale;
					localScale.y = rectHeight;
				}
			}
			else if (style == Style.FitInternalKeepingRatio)
			{
				// Contributed by Dylan Ryan
				float screenRatio = rectWidth / rectHeight;
				float imageRatio = initialSize.x / initialSize.y;

				if (imageRatio > screenRatio)
				{
					// Fit horizontally
					float scale = rectWidth / initialSize.x;
					localScale.x = rectWidth;
					localScale.y = initialSize.y * scale;
				}
				else
				{
					// Fit vertically
					float scale = rectHeight / initialSize.y;
					localScale.x = initialSize.x * scale;
					localScale.y = rectHeight;
				}
			}
			else
			{
				if (style == Style.Both || style == Style.Horizontal) localScale.x = relativeSize.x * rectWidth;
				if (style == Style.Both || style == Style.Vertical) localScale.y = relativeSize.y * rectHeight;
			}
			
			// Added by Zynga/Todd.
			if (style == Style.Both || style == Style.Horizontal) localScale.x += pixelOffset.x;
			if (style == Style.Both || style == Style.Vertical) localScale.y += pixelOffset.y;

			if (localScale.x < 0.0f)
			{
				localScale.x = 0.0f;
			}

			if (localScale.y < 0.0f)
			{
				localScale.y = 0.0f;
			}
			// End added by Zynga/Todd.

			if (mTrans.localScale != localScale) mTrans.localScale = localScale;
			// Zynga/Todd - Don't destroy, so we can re-enable if resolution changes.


			if (dependentStretches != null && dependentStretches.Count > 0)
			{
				for (int i = 0; i < dependentStretches.Count; i++)
				{
					if (dependentStretches[i] != null)
					{
						dependentStretches[i].enabled = true;
					}
				}
			}

			if (dependentAnchors != null && dependentAnchors.Count > 0)
			{
				for (int i = 0; i < dependentAnchors.Count; i++)
				{
					if (dependentAnchors[i] != null)
					{
						dependentAnchors[i].enabled = true;
					}
				}
			}

			if (Application.isPlaying) enabled = false;
		}
	}
}
