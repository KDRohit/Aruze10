//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for all UI components that should be derived from when creating new widget types.
/// </summary>

public abstract class UIWidget : TICoroutineMonoBehaviour
{
	/// <summary>
	/// List of all the active widgets currently present in the scene.
	/// </summary>

	static public BetterList<UIWidget> list = new BetterList<UIWidget>();

	public enum Pivot
	{
		TopLeft,
		Top,
		TopRight,
		Left,
		Center,
		Right,
		BottomLeft,
		Bottom,
		BottomRight,
	}

	[HideInInspector] public Vector2 pivotOffsetFromAtlas = new Vector2(.5f, .5f);	// Added by Zynga/Todd

	// Cached and saved values
	[HideInInspector][SerializeField] Color mColor = Color.white;
	[HideInInspector][SerializeField] Pivot mPivot = Pivot.Center;
	[HideInInspector][SerializeField] int mDepth = 0;

	protected GameObject mGo;
	protected Transform mTrans;
	protected UIPanel mPanel;

	//public for fast access
	[System.NonSerialized]
	public Material mMatCached = null;

	protected bool mChanged = true;
	protected bool mPlayMode = true;

	bool mStarted = false;
	Vector3 mDiffPos;
	Quaternion mDiffRot;
	Vector3 mDiffScale;
	Matrix4x4 mLocalToPanel;
	bool mVisibleByPanel = true;
	float mLastAlpha = 0f;

	// Widget's generated geometry
	UIGeometry mGeom = new UIGeometry();

	/// <summary>
	/// Whether the widget is visible.
	/// </summary>

	public bool isVisible { get { return mVisibleByPanel && finalAlpha > 0.001f; } }

	/// <summary>
	/// Color used by the widget.
	/// </summary>

	public Color color { get { return mColor; } set { if (!mColor.Equals(value)) { mColor = value; mChanged = true; } } }

	/// <summary>
	/// Widget's alpha -- a convenience method.
	/// </summary>

	public float alpha { get { return mColor.a; } set { Color c = mColor; c.a = value; color = c; } }

	/// <summary>
	/// Widget's final alpha, after taking the panel's alpha into account.
	/// </summary>

	public float finalAlpha 
	{ 
		get 
		{ 
			if (!System.Object.ReferenceEquals(mPanel,null) || CreatePanel()) 
				return mColor.a * mPanel.alpha;
			return mColor.a;	
		} 
	}

	/// <summary>
	/// Set or get the value that specifies where the widget's pivot point should be.
	/// </summary>

	public Pivot pivot
	{
		get
		{
			return mPivot;
		}
		set
		{
			if (mPivot != value)
			{
				Vector3 before = NGUIMath.CalculateWidgetCorners(this)[0];

				mPivot = value;
				mChanged = true;

				Vector3 after = NGUIMath.CalculateWidgetCorners(this)[0];

				Transform t = cachedTransform;
				Vector3 pos = t.position;
				float z = t.localPosition.z;
				pos.x += (before.x - after.x);
				pos.y += (before.y - after.y);
				cachedTransform.position = pos;

				pos = cachedTransform.localPosition;
				pos.x = Mathf.Round(pos.x);
				pos.y = Mathf.Round(pos.y);
				pos.z = z;
				cachedTransform.localPosition = pos;
			}
		}
	}
	
	/// <summary>
	/// Depth controls the rendering order -- lowest to highest.
	/// </summary>

	public int depth
	{
		get
		{
			return mDepth;
		}
		set
		{
			if (mDepth != value)
			{
				mDepth = value;
				if (mPanel != null) mPanel.MarkMaterialAsChanged(material, true);
			}
		}
	}

	/// <summary>
	/// Helper function that calculates the relative offset based on the current pivot.
	/// </summary>

	public Vector2 pivotOffset
	{
		get
		{
			Vector2 v = Vector2.zero;
			Vector4 p = relativePadding;

			Pivot pv = pivot;

			if (pv == Pivot.Top || pv == Pivot.Center || pv == Pivot.Bottom) v.x = (p.x - p.z - 1f) * 0.5f;
			else if (pv == Pivot.TopRight || pv == Pivot.Right || pv == Pivot.BottomRight) v.x = -1f - p.z;
			else v.x = p.x;

			if (pv == Pivot.Left || pv == Pivot.Center || pv == Pivot.Right) v.y = (p.w - p.y + 1f) * 0.5f;
			else if (pv == Pivot.BottomLeft || pv == Pivot.Bottom || pv == Pivot.BottomRight) v.y = 1f + p.w;
			else v.y = -p.y;

			// Added by Zynga/Todd:
			v.x -= pivotOffsetFromAtlas.x - .5f;
			v.y += pivotOffsetFromAtlas.y - .5f;

			return v;
		}
	}

	/// <summary>
	/// Game object gets cached for speed. Can't simply return 'mGo' set in Awake because this function may be called on a prefab.
	/// </summary>

	public GameObject cachedGameObject { get { if (mGo == null) mGo = gameObject; return mGo; } }

	/// <summary>
	/// Transform gets cached for speed. Can't simply return 'mTrans' set in Awake because this function may be called on a prefab.
	/// </summary>

	public Transform cachedTransform { get { if (System.Object.ReferenceEquals(mTrans,null)) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Material used by the widget.
	/// </summary>

	public virtual Material material
	{
		get
		{
			return null;
		}
		set
		{
			throw new System.NotImplementedException(GetType() + " has no material setter");
		}
	}

	/// <summary>
	/// Texture used by the widget.
	/// </summary>

	public virtual Texture mainTexture
	{
		get
		{
			if(!System.Object.ReferenceEquals (mMatCached, null))
				return mMatCached.mainTexture;
			Material mat = material;
			return (mat != null) ? mat.mainTexture : null;
		}
		set
		{
			throw new System.NotImplementedException(GetType() + " has no mainTexture setter");
		}
	}

	/// <summary>
	/// Returns the UI panel responsible for this widget.
	/// </summary>

	public UIPanel panel { get { if (System.Object.ReferenceEquals(mPanel,null)) CreatePanel(); return mPanel; } set { mPanel = value; } }

	/// <summary>
	/// Raycast into the screen and return a list of widgets in order from closest to farthest away.
	/// This is a slow operation and will consider ALL widgets underneath the specified game object.
	/// </summary>

	static public BetterList<UIWidget> Raycast (GameObject root, Vector2 mousePos)
	{
		BetterList<UIWidget> list = new BetterList<UIWidget>();
		UICamera uiCam = UICamera.FindCameraForLayer(root.layer);

		if (uiCam != null)
		{
			Camera cam = uiCam.cachedCamera;
			UIWidget[] widgets = root.GetComponentsInChildren<UIWidget>();

			for (int i = 0; i < widgets.Length; ++i)
			{
				UIWidget w = widgets[i];

				Vector3[] corners = NGUIMath.CalculateWidgetCorners(w);
				if (NGUIMath.DistanceToRectangle(corners, mousePos, cam) == 0f)
					list.Add(w);
			}

			list.Sort(delegate(UIWidget w1, UIWidget w2) { return w2.mDepth.CompareTo(w1.mDepth); });
		}
		return list;
	}

	/// <summary>
	/// Static widget comparison function used for Z-sorting.
	/// </summary>

	static public int CompareFunc (UIWidget left, UIWidget right)
	{
		if (left.mDepth > right.mDepth) return 1;
		if (left.mDepth < right.mDepth) return -1;
		return 0;
	}

	/// <summary>
	/// Remove this widget from the panel.
	/// </summary>

	protected void RemoveFromPanel ()
	{
		if (mPanel != null)
		{
			mPanel.RemoveWidget(this);
			mPanel = null;
		}
	}

	/// <summary>
	/// Only sets the local flag, does not notify the panel.
	/// In most cases you will want to use MarkAsChanged() instead.
	/// </summary>

	public void MarkAsChangedLite () { mChanged = true; }

	/// <summary>
	/// Tell the panel responsible for the widget that something has changed and the buffers need to be rebuilt.
	/// </summary>

	public virtual void MarkAsChanged ()
	{
		mChanged = true;

		// If we're in the editor, update the panel right away so its geometry gets updated.
		if (mPanel != null && enabled && NGUITools.GetActive(gameObject) && !Application.isPlaying && (!System.Object.ReferenceEquals(mMatCached,null) || !System.Object.ReferenceEquals(material,null)))
		{
			mPanel.AddWidget(this);
			CheckLayer();
#if UNITY_EDITOR
			// Mark the panel as dirty so it gets updated
			UnityEditor.EditorUtility.SetDirty(mPanel.gameObject);
#endif
		}
	}

	/// <summary>
	/// Ensure we have a panel referencing this widget.
	/// </summary>

	public bool CreatePanel ()
	{
		bool created = false;
		if (System.Object.ReferenceEquals(mPanel,null) && enabled && NGUITools.GetActive(gameObject) && material != null)
		{
			mPanel = UIPanel.Find(cachedTransform, mStarted);

			if (!System.Object.ReferenceEquals(mPanel,null))
			{
				CheckLayer();
				mPanel.AddWidget(this);
				mChanged = true;
			}
		}
		return created;
	}

	/// <summary>
	/// Check to ensure that the widget resides on the same layer as its panel.
	/// </summary>

	public void CheckLayer ()
	{
		if (mPanel != null && mPanel.gameObject.layer != gameObject.layer)
		{
			//czablocki - 2/3/2021 Commenting this out because it spams logs that crash v4.8.4 of Bugsnag when its
			//Notify() hook handles them to leave a Breadcrumb
			// Debug.LogWarning("You can't place widgets on a layer different than the UIPanel that manages them.\n" +
			// 	"If you want to move widgets to a different layer, parent them to a new panel instead.\nOld layer: " +
			// 	gameObject.layer.ToString() + ", new layer: " + mPanel.gameObject.layer.ToString(), this);
			gameObject.layer = mPanel.gameObject.layer;
		}
	}

	/// <summary>
	/// For backwards compatibility. Use ParentHasChanged() instead.
	/// </summary>

	[System.Obsolete("Use ParentHasChanged() instead")]
	public void CheckParent () { ParentHasChanged(); }

	/// <summary>
	/// Checks to ensure that the widget is still parented to the right panel.
	/// </summary>

	public void ParentHasChanged ()
	{
		if (mPanel != null)
		{
			UIPanel p = UIPanel.Find(cachedTransform);

			// If widget is no longer parented to the same panel. Remove it and re-add it to a new one.
			if (mPanel != p)
			{
				RemoveFromPanel();
				CreatePanel();
			}
		}
	}

	/// <summary>
	/// Remember whether we're in play mode.
	/// </summary>

	protected virtual void Awake ()
	{
		mGo = gameObject;
		mPlayMode = Application.isPlaying;
	}

	/// <summary>
	/// Mark the widget and the panel as having been changed.
	/// </summary>

	protected override void OnEnable ()
	{
		base.OnEnable();
		
#if UNITY_EDITOR
		if (GetComponents<UIWidget>().Length > 1)
		{
			Debug.LogError("Can't have more than one widget on the same game object!", this);
			enabled = false;
		}
		else
#endif
		{
			list.Add(this);
			mChanged = true;
			mPanel = null;
		}
	}

	/// <summary>
	/// Set the depth, call the virtual start function, and sure we have a panel to work with.
	/// </summary>

	void Start ()
	{
		mStarted = true;
		OnStart();
		CreatePanel();
	}

	/// <summary>
	/// Ensure that we have a panel to work with. The reason the panel isn't added in OnEnable()
	/// is because OnEnable() is called right after Awake(), which is a problem when the widget
	/// is brought in on a prefab object as it happens before it gets parented.
	/// </summary>

	public virtual void Update ()
	{
		// Ensure we have a panel to work with by now
		if (System.Object.ReferenceEquals(mPanel,null)) CreatePanel();
#if UNITY_EDITOR
		else if (!Application.isPlaying) ParentHasChanged();
#endif
	}

	/// <summary>
	/// Clear references.
	/// </summary>

	protected override void OnDisable ()
	{
		base.OnDisable();
		
		list.Remove(this);
		RemoveFromPanel();
	}

	/// <summary>
	/// Unregister this widget.
	/// </summary>

	void OnDestroy () { RemoveFromPanel(); }

#if UNITY_EDITOR

	static int mHandles = -1;

	/// <summary>
	/// Whether widgets will show handles with the Move Tool, or just the View Tool.
	/// </summary>

	static public bool showHandlesWithMoveTool
	{
		get
		{
			if (mHandles == -1)
			{
				mHandles = UnityEditor.EditorPrefs.GetInt("NGUI Handles", 1);
			}
			return (mHandles == 1);
		}
		set
		{
			int val = value ? 1 : 0;

			if (mHandles != val)
			{
				mHandles = val;
				UnityEditor.EditorPrefs.SetInt("NGUI Handles", mHandles);
			}
		}
	}

	/// <summary>
	/// Whether the widget should have some form of handles shown.
	/// </summary>

	static public bool showHandles
	{
		get
		{
			if (showHandlesWithMoveTool)
			{
				return UnityEditor.Tools.current == UnityEditor.Tool.Move;
			}
			return UnityEditor.Tools.current == UnityEditor.Tool.View;
		}
	}

	/// <summary>
	/// Whether handles should be shown around the widget for easy scaling and resizing.
	/// </summary>

	public virtual bool showResizeHandles { get { return true; } }

	/// <summary>
	/// Draw some selectable gizmos.
	/// </summary>

	void OnDrawGizmos ()
	{
		if (isVisible && mPanel != null && mPanel.debugInfo == UIPanel.DebugInfo.Gizmos)
		{
			if (UnityEditor.Selection.activeGameObject == gameObject && showHandles) return;

			Color outline = new Color(1f, 1f, 1f, 0.2f);

			// Position should be offset by depth so that the selection works properly
			Vector3 pos = Vector3.zero;
			pos.z -= mDepth * 0.25f;

			Vector3 size = relativeSize;
			Vector2 offset = pivotOffset;
			Vector4 padding = relativePadding;

			float x0 = offset.x * size.x - padding.x;
			float y0 = offset.y * size.y + padding.y;

			float x1 = x0 + size.x + padding.x + padding.z;
			float y1 = y0 - size.y - padding.y - padding.w;

			pos.x = (x0 + x1) * 0.5f;
			pos.y = (y0 + y1) * 0.5f;

			size.x = (x1 - x0);
			size.y = (y1 - y0);

			// Draw the gizmo
			Gizmos.matrix = cachedTransform.localToWorldMatrix;
			Gizmos.color = (UnityEditor.Selection.activeGameObject == gameObject) ? Color.green : outline;
			Gizmos.DrawWireCube(pos, size);

			// Make the widget selectable
			size.z = 0.01f;
			Gizmos.color = Color.clear;
			Gizmos.DrawCube(pos, size);
		}
	}
#endif

	bool mForceVisible = false;
	Vector3 mOldV0;
	Vector3 mOldV1;

	/// <summary>
	/// Update the widget and fill its geometry if necessary. Returns whether something was changed.
	/// </summary>

	public bool UpdateGeometry (UIPanel p, bool forceVisible)
	{
		//if (material != null && p != null)
		if((!System.Object.ReferenceEquals(mMatCached,null) || !System.Object.ReferenceEquals(material,null)) && !System.Object.ReferenceEquals(p,null))
		{
			mPanel = p;
			bool hasMatrix = false;
			float final = finalAlpha;
			bool visibleByAlpha = (final > 0.001f);
			bool visibleByPanel = forceVisible || mVisibleByPanel;

			// Has transform moved?
			if (cachedTransform.hasChanged)
			{
				mTrans.hasChanged = false;
				
				// Check to see if the widget has moved relative to the panel that manages it
#if UNITY_EDITOR
				if (!mPanel.widgetsAreStatic || !Application.isPlaying)
#else
				if (!mPanel.widgetsAreStatic)
#endif
				{
					Vector2 size = relativeSize;
					Vector2 offset = pivotOffset;
					Vector4 padding = relativePadding;

					float x0 = offset.x * size.x - padding.x;
					float y0 = offset.y * size.y + padding.y;

					float x1 = x0 + size.x + padding.x + padding.z;
					float y1 = y0 - size.y - padding.y - padding.w;

					mLocalToPanel = p.worldToLocal * mTrans.localToWorldMatrix;
					hasMatrix = true;

					Vector3 v0 = new Vector3(x0, y0, 0f);
					Vector3 v1 = new Vector3(x1, y1, 0f);

					v0 = mLocalToPanel.MultiplyPoint3x4(v0);
					v1 = mLocalToPanel.MultiplyPoint3x4(v1);

					if (Vector3.SqrMagnitude(mOldV0 - v0) > 0.000001f || Vector3.SqrMagnitude(mOldV1 - v1) > 0.000001f)
					{
						mChanged = true;
						mOldV0 = v0;
						mOldV1 = v1;
					}
				}

				// Is the widget visible by the panel?
				if (visibleByAlpha || mForceVisible != forceVisible)
				{
					mForceVisible = forceVisible;
					visibleByPanel = forceVisible || mPanel.IsVisible(this);
				}
			}
			else if (visibleByAlpha && mForceVisible != forceVisible)
			{
				mForceVisible = forceVisible;
				visibleByPanel = mPanel.IsVisible(this);
			}

			// Is the visibility changing?
			if (mVisibleByPanel != visibleByPanel)
			{
				mVisibleByPanel = visibleByPanel;
				mChanged = true;
			}

			// Has the alpha changed?
			if (mVisibleByPanel && mLastAlpha != final) mChanged = true;
			mLastAlpha = final;

			if (mChanged)
			{
				mChanged = false;

				if (isVisible)
				{
					mGeom.Clear();
					OnFill(mGeom.verts, mGeom.uvs, mGeom.cols);

					// Want to see what's being filled? Uncomment this line.
					//Debug.Log("Fill " + name + " (" + Time.time + ")");

					if (mGeom.hasVertices)
					{
						Vector3 offset = pivotOffset;
						Vector2 scale = relativeSize;

						offset.x *= scale.x;
						offset.y *= scale.y;

						if (!hasMatrix) mLocalToPanel = p.worldToLocal * mTrans.localToWorldMatrix;

						mGeom.ApplyOffset(offset);
						mGeom.ApplyTransform(mLocalToPanel);
					}
					return true;
				}
				else if (mGeom.hasVertices)
				{
					mGeom.Clear();
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Append the local geometry buffers to the specified ones.
	/// </summary>

	public void WriteToBuffers (BetterList<Vector3> v, BetterList<Vector2> u, BetterList<Color32> c, BetterList<Vector3> n, BetterList<Vector4> t)
	{
		mGeom.WriteToBuffers(v, u, c, n, t);
	}

	/// <summary>
	/// Make the widget pixel-perfect.
	/// </summary>

	virtual public void MakePixelPerfect ()
	{
		Vector3 scale = cachedTransform.localScale;

		int width  = Mathf.RoundToInt(scale.x);
		int height = Mathf.RoundToInt(scale.y);

		scale.x = width;
		scale.y = height;
		scale.z = 1f;

		Vector3 pos = cachedTransform.localPosition;
		pos.z = Mathf.RoundToInt(pos.z);

		if (width % 2 == 1 && (pivot == Pivot.Top || pivot == Pivot.Center || pivot == Pivot.Bottom))
		{
			pos.x = Mathf.Floor(pos.x) + 0.5f;
		}
		else
		{
			pos.x = Mathf.Round(pos.x);
		}

		if (height % 2 == 1 && (pivot == Pivot.Left || pivot == Pivot.Center || pivot == Pivot.Right))
		{
			pos.y = Mathf.Ceil(pos.y) - 0.5f;
		}
		else
		{
			pos.y = Mathf.Round(pos.y);
		}

		cachedTransform.localPosition = pos;
		cachedTransform.localScale = scale;
	}

	/// <summary>
	/// Visible size of the widget in relative coordinates. In most cases this can remain at (1, 1).
	/// If you want to figure out the widget's size in pixels, scale this value by cachedTransform.localScale.
	/// </summary>

	virtual public Vector2 relativeSize { get { return Vector2.one; } }

	/// <summary>
	/// Extra padding around the sprite, in pixels.
	/// </summary>

	virtual public Vector4 relativePadding { get { return Vector4.zero; } }

	/// <summary>
	/// Dimensions of the sprite's border, if any.
	/// </summary>

	virtual public Vector4 border { get { return Vector4.zero; } }

	/// <summary>
	/// Whether this widget will automatically become pixel-perfect after resize operation finishes.
	/// </summary>

	virtual public bool pixelPerfectAfterResize { get { return false; } }

	/// <summary>
	/// Virtual Start() functionality for widgets.
	/// </summary>

	virtual protected void OnStart () { }

	/// <summary>
	/// Virtual function called by the UIPanel that fills the buffers.
	/// </summary>

	virtual public void OnFill(BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols) { }


	static protected Vector3[] mCorners = new Vector3[4];
    
    public Vector3[] worldCorners
	{
		get
		{
			Transform trans = cachedTransform;
        
			Vector3 scale = trans.localScale;
			float width  = scale.x;
			float height = scale.y;

			Vector2 offset = pivotOffset;

			float x0 = -offset.x * width;
			float y0 = -offset.y * height;
			float x1 = x0 + width;
			float y1 = y0 + height;

            Matrix4x4 t = trans.localToWorldMatrix;

            Vector3 v3;
            v3.x = x0;
            v3.y = y0;
            v3.z = 0;
            mCorners[0] = t.MultiplyPoint3x4(v3);
            v3.y = y1;
            mCorners[1] = t.MultiplyPoint3x4(v3);
            v3.x = x1;
            mCorners[2] = t.MultiplyPoint3x4(v3);
            v3.y = y0;
            mCorners[3] = t.MultiplyPoint3x4(v3);
         
			return mCorners;
		}
	}

	public Vector3[] GetSidesRelativeTo (ref Matrix4x4 worldToLocalMatrix)
	{
		Vector2 offset = pivotOffset;

		Transform trans = cachedTransform;
        
		Vector3 scale = trans.localScale;
		float width  = scale.x;
		float height = scale.y;

		float x0 = -offset.x * width;
		float y0 = -offset.y * height;
		float x1 = x0 + width;
		float y1 = y0 + height;
        
        float cx = (x0 + x1) * 0.5f;
        float cy = (y0 + y1) * 0.5f;

         
        Matrix4x4 t = worldToLocalMatrix * trans.localToWorldMatrix;

        Vector3 v3;
        v3.x = x0;
        v3.y = cy;
        v3.z = 0;
        mCorners[0] = t.MultiplyPoint3x4(v3);

        v3.x = x1;
        mCorners[2] = t.MultiplyPoint3x4(v3);   

        v3.x = cx;
        v3.y = y1;
        mCorners[1] = t.MultiplyPoint3x4(v3);  

        v3.y = y0;
        mCorners[3] = t.MultiplyPoint3x4(v3);  
		return mCorners;
	}    
    
	/// <summary>
	/// Get the sides of the rectangle relative to the specified transform.
	/// The order is left, top, right, bottom.
	/// </summary>
	public Vector3[] GetSides (Transform relativeTo)
	{
		Vector2 offset = pivotOffset;

		Transform trans = cachedTransform;
        
		Vector3 scale = trans.localScale;
		float width  = scale.x;
		float height = scale.y;


		float x0 = -offset.x * width;
		float y0 = -offset.y * height;
		float x1 = x0 + width;
		float y1 = y0 + height;
        
        float cx = (x0 + x1) * 0.5f;
        float cy = (y0 + y1) * 0.5f;

         
        Matrix4x4 t = (!System.Object.ReferenceEquals(relativeTo,null)) ? (relativeTo.worldToLocalMatrix * trans.localToWorldMatrix) : trans.localToWorldMatrix;

        Vector3 v3;
        v3.x = x0;
        v3.y = cy;
        v3.z = 0;
        mCorners[0] = t.MultiplyPoint3x4(v3);

        v3.x = x1;
        mCorners[2] = t.MultiplyPoint3x4(v3);   

        v3.x = cx;
        v3.y = y1;
        mCorners[1] = t.MultiplyPoint3x4(v3);  

        v3.y = y0;
        mCorners[3] = t.MultiplyPoint3x4(v3);  
		return mCorners;
	}
}
