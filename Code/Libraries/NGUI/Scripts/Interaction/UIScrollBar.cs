//#define UNITY_WEBGL
//#define LOCAL_TESTING_SCROLLBAR
//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright � 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Scroll bar functionality.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/Scroll Bar")]
[RequireComponent(typeof(UIPanel))]
public class UIScrollBar : TICoroutineMonoBehaviour
{
	public enum Direction
	{
		Horizontal,
		Vertical,
	};

	public delegate void OnScrollBarChange (UIScrollBar sb);
	public delegate void OnDragFinished ();

	[HideInInspector][SerializeField] Color mColorOverride;
	[HideInInspector][SerializeField] UISprite mBG;
	[HideInInspector][SerializeField] UISprite mFG;
	[HideInInspector][SerializeField] UISprite mArrow0;
	[HideInInspector][SerializeField] UISprite mArrow1;
	[HideInInspector][SerializeField] Direction mDir = Direction.Horizontal;
	[HideInInspector][SerializeField] UIPanel mScrollBarPanel;
	[HideInInspector][SerializeField] bool mInverted = false;
	[HideInInspector][SerializeField] float mScroll = 0f;
	[HideInInspector][SerializeField] float mBarSize = 1f;
	[HideInInspector][SerializeField] float mHoverAlpha = 0.5f;
	[HideInInspector][SerializeField] float mDefaultAlpha = 0.3f;
	[HideInInspector][SerializeField] bool mCenterAlign = false;


	private Transform mTrans;
	private bool mIsDirty = false;
	private Camera mCam;
	private Vector2 mScreenPos = Vector2.zero;
	private bool isHovered; //used in WebGL mode
	private bool pressed = false;
	[System.NonSerialized] TweenAlpha alphaTween;
	[System.NonSerialized] float runtimeDefaultAlpha;
	[System.NonSerialized] float runtimeHoverAlpha;

	protected bool mStarted = false;
	protected bool mHighlighted = false;

	public bool alwaysShowArrows;
	public bool alwaysShowScrollbar;
	// MCC -- Adding here to allow us to manually control some sizes/pivots
	public bool shouldControlPivot = false;
	const float mobileScrollBarAlpha = 0.3f;

	/// <summary>
	/// Delegate triggered when the scroll bar has changed visibly.
	/// </summary>
	public OnScrollBarChange onChange;

	/// <summary>
	/// Delegate triggered when the scroll bar stops being dragged.
	/// Useful for things like centering on the closest valid object, for example.
	/// </summary>
	public OnDragFinished onDragFinished;

	/// <summary>
	/// Set to true when the user presses or drags the scroll bar
	/// </summary>
	public bool isSelected { get; private set; }

	/// <summary>
	/// Cached for speed.
	/// </summary>
	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Camera used to draw the scroll bar.
	/// </summary>
	public Camera cachedCamera { get { if (mCam == null) mCam = NGUITools.FindCameraForLayer(gameObject.layer); return mCam; } }

	/// <summary>
	/// The size of the foreground bar in percent (0-1 range).
	/// </summary>
	public float barSize
	{
		get
		{
			return mBarSize;
		}
		set
		{
			float val = Mathf.Clamp01(value);

			if (!Mathf.Approximately(mBarSize, val))
			{
				mBarSize = val;
				mIsDirty = true;
				if (onChange != null) 
				{
					onChange(this);
				}
			}
		}
	}


	///<summary>
	/// Color override for all sprites associated with the scrollbar
	/// </summary>
	public Color overrideColor 
	{
		get 
		{ 
			return mColorOverride; 
		}
		set 
		{ 
			if (mColorOverride != value) 
			{ 
				mColorOverride = value; 

				mColorOverride = new Color(value.r, value.g, value.b, 1);

				mFG.color = mColorOverride;
				mBG.color = mColorOverride;
				mArrow0.color = mColorOverride;
				mArrow1.color = mColorOverride;
				mIsDirty = true; 
			} 
		} 
	}
	
		///<summary>
	/// Color override for all sprites associated with the scrollbar
	/// </summary>
	public float defaultAlpha 
	{
		get 
		{ 
			return mDefaultAlpha; 
		}
		set 
		{ 
			if (!Mathf.Approximately(mDefaultAlpha, value))
			{ 
				mDefaultAlpha = value; 
				mIsDirty = true; 
				mScrollBarPanel.alpha = value;
				//alpha = value;
			} 
		}
	}
	
		///<summary>
	/// Color override for all sprites associated with the scrollbar
	/// </summary>
	public float hoverAlpha 
	{ 
		get 
		{ 
			return mHoverAlpha; 
		} 
		set 
		{
			if (!Mathf.Approximately(mHoverAlpha, value)) 
			{ 
				mHoverAlpha = value; 
				mIsDirty = true; 
			} 
		} 
	}

	public float alpha
	{
		get
		{
			return mScrollBarPanel.alpha;
		}
		set
		{
			mScrollBarPanel.alpha = value;	
		}
	}

	public UIPanel scrollBarPanel
	{
		get{ if(mScrollBarPanel == null){mScrollBarPanel = GetComponent<UIPanel>();} return mScrollBarPanel;}
		set{ mScrollBarPanel = value;}
	}


	/// <summary>
	/// Sprite used for the background.
	/// </summary>

	public UISprite background { get { return mBG; } set { if (mBG != value) { mBG = value; mIsDirty = true; } } }

	/// <summary>
	/// Sprite used for the foreground.
	/// </summary>

	public UISprite foreground { get { return mFG; } set { if (mFG != value) { mFG = value; mIsDirty = true; } } }

	/// <summary>
	/// Sprite used for the arrow at 0 position.
	/// </summary>
	public UISprite arrow0 { get { return mArrow0; } set { if (mArrow0 != value) { mArrow0 = value; mIsDirty = true; } } }

	/// <summary>
	/// Sprite used for the arrow at 1 position.
	/// </summary>
	public UISprite arrow1 { get { return mArrow1; } set { if (mArrow1 != value) { mArrow1 = value; mIsDirty = true; } } }

	/// <summary>
	/// The scroll bar's direction.
	/// </summary>

	public Direction direction
	{
		get
		{
			return mDir;
		}
		set
		{
			if (mDir != value)
			{
				mDir = value;
				mIsDirty = true;

				// Since the direction is changing, see if we need to swap width with height (for convenience)
				if (mBG != null)
				{
					Transform t = mBG.cachedTransform;
					Vector3 scale = t.localScale;

					if ((mDir == Direction.Vertical   && scale.x > scale.y) ||
					    (mDir == Direction.Horizontal && scale.x < scale.y))
					{
						float x = scale.x;
						scale.x = scale.y;
						scale.y = x;
						t.localScale = scale;
						ForceUpdate();

						// Update the colliders as well
						if (mBG.GetComponent<Collider>() != null) NGUITools.AddWidgetCollider(mBG.gameObject);
						if (mFG.GetComponent<Collider>() != null) NGUITools.AddWidgetCollider(mFG.gameObject);
					}
				}
			}
		}
	}

	/// <summary>
	/// Whether the movement direction is flipped.
	/// </summary>

	public bool inverted { get { return mInverted; } set { if (mInverted != value) { mInverted = value; mIsDirty = true; } } }

	/// <summary>
	/// use of offset when positioning horizontally to account for center align
	/// </summary>
	/// <returns></returns>
	public bool centerAlign  { get { return mCenterAlign; } set { mCenterAlign = value; } }

	/// <summary>
	/// Modifiable value for the scroll bar, 0-1 range.
	/// </summary>

	public float scrollValue
	{
		get
		{
			return mScroll;
		}
		set
		{
			float val = Mathf.Clamp01(value);

			if (!Mathf.Approximately(mScroll, val))
			{
				mScroll = val;
				mIsDirty = true;
				if (onChange != null) 
				{
					onChange(this);
				}
			}
		}
	}

	private void Awake()
	{
		mScrollBarPanel = GetComponent<UIPanel>();
#if UNITY_WEBGL || LOCAL_TESTING_SCROLLBAR
		runtimeHoverAlpha = mHoverAlpha;
		runtimeDefaultAlpha = mDefaultAlpha;
#else
		runtimeHoverAlpha = 0.3f;
		runtimeDefaultAlpha = alwaysShowScrollbar ? mobileScrollBarAlpha : 0;

#if UNITY_EDITOR
		Debug.Assert(arrow0 != null && arrow1 != null);
#endif
		arrow0.enabled = alwaysShowArrows ? true : false;
		arrow1.enabled = arrow0.enabled;

		mBG.alpha = 1;
		mFG.alpha = 1;
		arrow0.alpha = 1;
		arrow1.alpha = 1;
#endif
		alphaTween = TweenAlpha.Begin(this.gameObject, 0.2f, runtimeDefaultAlpha);
	}

	// MCC -- Added to support setting the value of the scroll bar without firing the changed event.
	// This is needed as we want to support both setting the scroll value to match the content being dragged,
	// as well as setting the content's position with the scroll bar.
	public void setScrollValue(float value)
	{
		float val = Mathf.Clamp01(value);
		bool sameValue = Mathf.Approximately(mScroll, val);

		if (!sameValue)
		{
			mScroll = val;
			mIsDirty = true;

			if (pressed)
			{
				CenterOnMouse();
			}
		}
		
		
#if !UNITY_WEBGL && !LOCAL_TESTING_SCROLLBAR
		//let any active tween finish before setting new hover
		//for mobile - which is the result of dragging the SlideControl
		if(alphaTween == null || (alphaTween != null && alphaTween.enabled) || alwaysShowScrollbar)
		{
			return;
		}

		//we had a value change from something other than dragging scroll bar
		//so we have to fade the thing either in or out

		//fade in
		if(scrollBarPanel.alpha < runtimeHoverAlpha && !sameValue)
		{
			//on mobile, value is changed, but we didn't scroll here via the scroll bar
			//control (and can't), so we highlight the bar
			alphaTween.from = runtimeDefaultAlpha;
			alphaTween.to = runtimeHoverAlpha;
			alphaTween.Reset();
			alphaTween.Play(true);
		}
		//fade out
		else if(scrollBarPanel.alpha > 0 && sameValue)
		{
			alphaTween.from = runtimeHoverAlpha;
			alphaTween.to = runtimeDefaultAlpha;
			alphaTween.Reset();
			alphaTween.Play(true);
		}
#endif
	}


	/// <summary>
	/// Move the scroll bar to be centered on the specified position.
	/// </summary>
	void CenterOnPos (Vector2 localPos)
	{
		if (mBG == null || mFG == null) 
		{
			return;
		}

		// Background's bounds
		Bounds bg = NGUIMath.CalculateRelativeInnerBounds(cachedTransform, mBG);
		Bounds fg = NGUIMath.CalculateRelativeInnerBounds(cachedTransform, mFG);

		if (mDir == Direction.Horizontal)
		{
			float size = bg.size.x - fg.size.x;
			float offset = size * 0.5f;
			float min = bg.center.x - offset;
			float val = (size > 0f) ? (localPos.x - min) / size : 0f;
			scrollValue = mInverted ? val : 1f - val ;
		}
		else
		{
			float size = bg.size.y - fg.size.y;
			float offset = size * 0.5f;
			float min = bg.center.y - offset;
			float val = (size > 0f) ? 1f - (localPos.y - min) / size : 0f;
			scrollValue = mInverted ? val : 1 - val;
		}

		Debug.Log("moving scrollbar: " + scrollValue.ToString());
	}

	/// <summary>
	/// Drag the scroll bar by the specified on-screen amount.
	/// </summary>
	void Reposition (Vector2 screenPos)
	{
		// Create a plane
		Transform trans = cachedTransform;
		Plane plane = new Plane(trans.rotation * Vector3.back, trans.position);

		// If the ray doesn't hit the plane, do nothing
		float dist;
		Ray ray = cachedCamera.ScreenPointToRay(screenPos);
		if (!plane.Raycast(ray, out dist)) 
		{
			return;
		}

		// Transform the point from world space to local space
		CenterOnPos(trans.InverseTransformPoint(ray.GetPoint(dist)));
	}

	void CenterOnMouse()
	{
		Vector2 pos = UICamera.lastTouchPosition;
		CenterOnPos(cachedTransform.InverseTransformPoint(cachedCamera.ScreenToWorldPoint(pos)));

	}

	/// <summary>
	/// Position the scroll bar to be under the current touch.
	/// </summary>
	void OnPressBackground (GameObject go, bool isPressed)
	{
		isSelected = true;
		mCam = UICamera.currentCamera;
		Reposition(UICamera.lastTouchPosition);
#if UNITY_EDITOR
		Debug.Log("Hitting OnPressBackground");
#endif
		if (!isPressed && onDragFinished != null) 
		{
			onDragFinished();
#if UNITY_EDITOR
			Debug.Log("Hitting OnPressBackground, onDragFinished");
#endif
		}
	}

	/// <summary>
	/// Position the scroll bar to be under the current touch.
	/// </summary>
	void OnDragBackground (GameObject go, Vector2 delta)
	{
#if UNITY_WEBGL || LOCAL_TESTING_SCROLLBAR
		isSelected = true;		
		mCam = UICamera.currentCamera;
		Reposition(UICamera.lastTouchPosition);
#if UNITY_EDITOR
		Debug.Log("Hitting OnDragBackground");
#endif
#endif
	}

	/// <summary>
	/// Save the position of the foreground on press.
	/// </summary>
	void OnPressForeground (GameObject go, bool isPressed)
	{
		if (isPressed)
		{
			isSelected = true;
			mCam = UICamera.currentCamera;
			Bounds b = NGUIMath.CalculateAbsoluteWidgetBounds(mFG.cachedTransform);
			mScreenPos = mCam.WorldToScreenPoint(b.center);
#if UNITY_EDITOR
			Debug.Log("Hitting OnPressForeground");
#endif
		}
		else if (onDragFinished != null) 
		{
			onDragFinished();
#if UNITY_EDITOR
			Debug.Log("Hitting OnPressForeground, onDragFinished");
#endif
		}
	}

	/// <summary>
	/// Drag the scroll bar in the specified direction.
	/// </summary>
	void OnDragForeground (GameObject go, Vector2 delta)
	{
		mCam = UICamera.currentCamera;
		isSelected = true;
		Bounds b = NGUIMath.CalculateAbsoluteWidgetBounds(mFG.cachedTransform);
		mScreenPos = mCam.WorldToScreenPoint(b.center);

		Reposition(UICamera.lastTouchPosition);
#if UNITY_EDITOR
		Debug.Log("Hitting OnDragForeground for ScrollBar: " +mScroll.ToString());
#endif
	}
	
	public virtual void OnPress (bool isPressed)
	{
#if UNITY_WEBGL || LOCAL_TESTING_SCROLLBAR
		if (enabled)
		{
			if (!mStarted) 
			{
				Start();
			}

			mCam = UICamera.currentCamera;
			Reposition(UICamera.lastTouchPosition);
			pressed = true;
#if UNITY_EDITOR
			Debug.Log("Hitting OnPress for ScrollBar at: " + UICamera.lastTouchPosition);
#endif

		}		
#endif
	}

	/// <summary>
	/// Register the event listeners.
	/// </summary>
	void Start ()
	{
#if UNITY_WEBGL || LOCAL_TESTING_SCROLLBAR
		if (background != null && background.GetComponent<Collider>() != null)
		{
			UIEventListener listener = UIEventListener.Get(background.gameObject);
			listener.onPress += OnPressBackground;
			listener.onDrag += OnDragBackground;
		}

		if (foreground != null && foreground.GetComponent<Collider>() != null)
		{
			UIEventListener listener = UIEventListener.Get(foreground.gameObject);
			listener.onPress += OnPressForeground;
			listener.onDrag += OnDragForeground;
		}
#endif


		ForceUpdate();

		mStarted = true;
	}

	/// <summary>
	/// Update the value of the scroll bar if necessary.
	/// </summary>

	void Update()
	{
		if (mIsDirty)
		{
			ForceUpdate();
		}

		if (!UICamera.isDragging)
		{
			isSelected = false;
		}

		if (pressed && !TouchInput.isTouchDown)
		{
			pressed = false;
		}

		//Prevent issues with the inspector trying to create tween values
#if UNITY_EDITOR 
		if(!UnityEditor.EditorApplication.isPlaying)
		{
			return;
		}
#endif

		//if (alphaTween != null && alphaTween.enabled)
		//{
		//	alpha = alphaTween.alpha;
		//}
	}

	/// <summary>
	/// Update the value of the scroll bar.
	/// </summary>
	public void ForceUpdate ()
	{
		mIsDirty = false;

		if (mBG != null && mFG != null)
		{
			mBarSize = Mathf.Clamp01(mBarSize);
			mScroll = Mathf.Clamp01(mScroll);

			Vector4 bg = mBG.border;
			Vector4 fg = mFG.border;

			// Space available for the background
			Vector2 bgs = new Vector2(
				Mathf.Max(0f, mBG.cachedTransform.localScale.x - bg.x - bg.z),
				Mathf.Max(0f, mBG.cachedTransform.localScale.y - bg.y - bg.w));

			float val = mInverted ? mScroll : 1f - mScroll;

			if (mDir == Direction.Horizontal)
			{
				Vector2 fgs = new Vector2(bgs.x * mBarSize, bgs.y);

				//mFG.pivot = UIWidget.Pivot.Left;
				//mBG.pivot = UIWidget.Pivot.Left;
				//mBG.cachedTransform.localPosition = Vector3.zero;
				Bounds boundedBG = NGUIMath.CalculateRelativeInnerBounds(cachedTransform, mBG);
				Bounds boundedFG = NGUIMath.CalculateRelativeInnerBounds(cachedTransform, mFG);
				float offset = 0;
				if (mCenterAlign && boundedBG.size.x > 1 && boundedFG.size.x >1)  //values less than 0 don't work
				{
					offset = (boundedBG.size.x / 2.0f) - (boundedFG.size.x / 2.0f);
				}
				mFG.cachedTransform.localPosition = new Vector3((bg.x - fg.x + (bgs.x - fgs.x) * val) - offset, mFG.cachedTransform.localPosition.y, 0f);
				mFG.cachedTransform.localScale = new Vector3(fgs.x + fg.x + fg.z, fgs.y + fg.y + fg.w, 1f);
				if (val < 0.999f && val > 0.001f) 
				{
					mFG.MakePixelPerfect();
				}
			}
			else
			{
				Vector2 fgs = new Vector2(bgs.x, bgs.y * mBarSize);

				if (shouldControlPivot)
				{
					mFG.pivot = UIWidget.Pivot.Top;
					mBG.pivot = UIWidget.Pivot.Top;
				}

				mFG.cachedTransform.localPosition = new Vector3(mFG.cachedTransform.localPosition.x, -bg.y + fg.y - (bgs.y - fgs.y) * val, 0f);
				mFG.cachedTransform.localScale = new Vector3(fgs.x + fg.x + fg.z, fgs.y + fg.y + fg.w, 1f);

				if (val < 0.999f && val > 0.001f)
				{
					mFG.MakePixelPerfect();
				}
			}
		}
	}

	public void OnHover(bool isOver)
	{
#if UNITY_WEBGL || LOCAL_TESTING_SCROLLBAR
		if (isOver && isHovered)
		{
			return;
		}
		
		if (enabled)
		{
			isHovered = isOver;
			if (!mStarted)
			{
				Start();
			}

			//set alpha starting value for fade in/out
			if (alphaTween.alpha <= 0)
			{
				alphaTween.alpha = runtimeDefaultAlpha;
			}

			//setup the hover values, if we're over it, fade it in.
			//if we aren't over it, fade it out
			// hover = from default to hover
			// no hover = from hover to default
			alphaTween.from = isOver ? runtimeDefaultAlpha : runtimeHoverAlpha ;
			alphaTween.to = isOver ? runtimeHoverAlpha : runtimeDefaultAlpha;
			alphaTween.Reset();
			alphaTween.Play(true);
#if UNITY_EDITOR			
			Debug.Log("Fading Scrollbar from: " + alphaTween.from + " to: " + alphaTween.to);
#endif
		}
#endif
	}

	private void Reset()
	{
		mColorOverride = Color.white;
		mDefaultAlpha = 0.3f;
		mHoverAlpha = 0.5f;
	}
}
