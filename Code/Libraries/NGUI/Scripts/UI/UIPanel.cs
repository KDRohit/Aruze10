//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI Panel is responsible for collecting, sorting and updating widgets in addition to generating widgets' geometry.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Panel")]
public class UIPanel : TICoroutineMonoBehaviour
{
	public enum DebugInfo
	{
		None,
		Gizmos,
		Geometry,
	}

	public delegate void OnChangeDelegate ();

	/// <summary>
	/// Notification triggered when something changes within the panel.
	/// </summary>

	public OnChangeDelegate onChange;

	/// <summary>
	/// Defaults to 'false' so that older UIs work as expected.
	/// </summary>

	public bool sortByDepth = false;

	/// <summary>
	/// Whether this panel will show up in the panel tool (set this to 'false' for dynamically created temporary panels)
	/// </summary>

	public bool showInPanelTool = true;

	/// <summary>
	/// Whether normals and tangents will be generated for all meshes
	/// </summary>
	
	public bool generateNormals = false;

	/// <summary>
	/// Whether the panel will create an additional pass to write to depth.
	/// Turning this on will double the number of draw calls, but will reduce fillrate.
	/// In order to make the most out of this feature, move your widgets on the Z and minimize the amount of visible transparency.
	/// </summary>

	public bool depthPass = false;

	/// <summary>
	/// Whether widgets drawn by this panel are static (won't move). This will improve performance.
	/// </summary>

	public bool widgetsAreStatic = false;

	/// <summary>
	/// Whether widgets will be culled while the panel is being dragged.
	/// Having this on improves performance, but turning it off will reduce garbage collection.
	/// </summary>

	public bool cullWhileDragging = false;

	/// <summary>
	/// Matrix that will transform the specified world coordinates to relative-to-panel coordinates.
	/// </summary>

	[HideInInspector] public Matrix4x4 worldToLocal = Matrix4x4.identity;

	// Panel's alpha (affects the alpha of all widgets)
	[HideInInspector][SerializeField] float mAlpha = 1f;

	// Whether generated geometry is shown or hidden
	[HideInInspector][SerializeField] DebugInfo mDebugInfo = DebugInfo.Gizmos;

	// Clipping rectangle
	[HideInInspector][SerializeField] UIDrawCall.Clipping mClipping = UIDrawCall.Clipping.None;
	[HideInInspector][SerializeField] Vector4 mClipRange = Vector4.zero;
	[HideInInspector][SerializeField] Vector2 mClipSoftness = new Vector2(40f, 40f);

	// List of all widgets managed by this panel
	BetterList<UIWidget> mWidgets = new BetterList<UIWidget>();

	// Widgets using these materials will be rebuilt next frame
	BetterList<Material> mChanged = new BetterList<Material>();

	// List of UI Screens created on hidden and invisible game objects
	BetterList<UIDrawCall> mDrawCalls = new BetterList<UIDrawCall>();

#if ENABLE_DRAW_OPTIMIZATION
	// Cache for UIDrawCall in order to reduce memory allocations
	BetterList<UIDrawCall> mDrawCallsHidden = new BetterList<UIDrawCall>();
#else
	// Cached in order to reduce memory allocations
	BetterList<Vector3> mVerts = new BetterList<Vector3>();
	BetterList<Vector3> mNorms = new BetterList<Vector3>();
	BetterList<Vector4> mTans = new BetterList<Vector4>();
	BetterList<Vector2> mUvs = new BetterList<Vector2>();
	BetterList<Color32> mCols = new BetterList<Color32>();
#endif

	GameObject mGo;
	Transform mTrans;
	Camera mCam;
	int mLayer = -1;
	bool mDepthChanged = false;

	float mCullTime = 0f;
	float mUpdateTime = 0f;
	float mMatrixTime = 0f;

	// Values used for visibility checks
	static float[] mTemp = new float[4];
	Vector2 mMin = Vector2.zero;
	Vector2 mMax = Vector2.zero;

	// Used for SetAlphaRecursive()
	UIPanel[] mChildPanels;

#if UNITY_EDITOR
	// Screen size, saved for gizmos, since Screen.width and Screen.height returns the Scene view's dimensions in OnDrawGizmos.
	Vector2 mScreenSize = Vector2.one;
#endif

	/// <summary>
	/// Cached for speed. Can't simply return 'mGo' set in Awake because this function may be called on a prefab.
	/// </summary>

	public GameObject cachedGameObject { get { if (mGo == null) mGo = gameObject; return mGo; } }

	/// <summary>
	/// Cached for speed. Can't simply return 'mTrans' set in Awake because this function may be called on a prefab.
	/// </summary>

	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Panel's alpha affects everything drawn by the panel.
	/// </summary>

	public float alpha
	{
		get
		{
			return mAlpha;
		}
		set
		{
			float val = Mathf.Clamp01(value);

			if (mAlpha != val)
			{
				mAlpha = val;

				for (int i = 0; i < mDrawCalls.size; ++i)
				{
					UIDrawCall dc = mDrawCalls[i];
					MarkMaterialAsChanged(dc.material, false);
				}

				for (int i = 0; i < mWidgets.size; ++i)
					mWidgets[i].MarkAsChangedLite();
			}
		}
	}

	/// <summary>
	/// Recursively set the alpha for this panel and all of its children.
	/// </summary>

	public void SetAlphaRecursive (float val, bool rebuildList)
	{
		if (rebuildList || mChildPanels == null)
			mChildPanels = GetComponentsInChildren<UIPanel>(true);
		for (int i = 0, imax = mChildPanels.Length; i < imax; ++i)
			mChildPanels[i].alpha = val;
	}

	/// <summary>
	/// Whether the panel's generated geometry will be hidden or not.
	/// </summary>

	public DebugInfo debugInfo
	{
		get
		{
			return mDebugInfo;
		}
		set
		{
			if (mDebugInfo != value)
			{
				mDebugInfo = value;
				BetterList<UIDrawCall> list = drawCalls;
				HideFlags flags = (mDebugInfo == DebugInfo.Geometry) ? HideFlags.DontSave | HideFlags.NotEditable : HideFlags.HideAndDontSave;

				for (int i = 0, imax = list.size; i < imax;  ++i)
				{
					UIDrawCall dc = list[i];
					GameObject go = dc.gameObject;
					NGUITools.SetActiveSelf(go, false);
					go.hideFlags = flags;
					NGUITools.SetActiveSelf(go, true);
				}
			}
		}
	}

	/// <summary>
	/// Clipping method used by all draw calls.
	/// </summary>

	public UIDrawCall.Clipping clipping
	{
		get
		{
			return mClipping;
		}
		set
		{
			if (mClipping != value)
			{
				mClipping = value;
				mMatrixTime = 0f;
				UpdateDrawcalls();
			}
		}
	}

	/// <summary>
	/// Clipping position (XY) and size (ZW).
	/// </summary>

	public Vector4 clipRange
	{
		get
		{
			return mClipRange;
		}
		set
		{
			if (mClipRange != value)
			{
				mCullTime = (mCullTime == 0f) ? 0.001f : Time.realtimeSinceStartup + 0.15f;
				mClipRange = value;
				mMatrixTime = 0f;
				UpdateDrawcalls();
			}
		}
	}

	/// <summary>
	/// Clipping softness is used if the clipped style is set to "Soft".
	/// </summary>

	public Vector2 clipSoftness { get { return mClipSoftness; } set { if (mClipSoftness != value) { mClipSoftness = value; UpdateDrawcalls(); } } }

	/// <summary>
	/// Widgets managed by this panel.
	/// </summary>

	public BetterList<UIWidget> widgets { get { return mWidgets; } }

	/// <summary>
	/// Retrieve the list of all active draw calls, removing inactive ones in the process.
	/// </summary>

	public BetterList<UIDrawCall> drawCalls
	{
		get
		{
			for (int i = mDrawCalls.size; i > 0; )
			{
				UIDrawCall dc = mDrawCalls[--i];
				if (dc == null) mDrawCalls.RemoveAt(i);
			}
			return mDrawCalls;
		}
	}

	/// <summary>
	/// Returns whether the specified rectangle is visible by the panel. The coordinates must be in world space.
	/// </summary>

	bool IsVisible (Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		UpdateTransformMatrix();

		// Transform the specified points from world space to local space
		a = worldToLocal.MultiplyPoint3x4(a);
		b = worldToLocal.MultiplyPoint3x4(b);
		c = worldToLocal.MultiplyPoint3x4(c);
		d = worldToLocal.MultiplyPoint3x4(d);

		mTemp[0] = a.x;
		mTemp[1] = b.x;
		mTemp[2] = c.x;
		mTemp[3] = d.x;

		float minX = Mathf.Min(mTemp);
		if (minX > mMax.x) return false;
		
        float maxX = Mathf.Max(mTemp);
        if (maxX < mMin.x) return false;
		
		mTemp[0] = a.y;
		mTemp[1] = b.y;
		mTemp[2] = c.y;
		mTemp[3] = d.y;

		float maxY = Mathf.Max(mTemp);
		if (maxY < mMin.y) return false;
		
        float minY = Mathf.Min(mTemp);
        if (minY > mMax.y) return false;

		return true;
	}

	/// <summary>
	/// Returns whether the specified world position is within the panel's bounds determined by the clipping rect.
	/// </summary>

	public bool IsVisible (Vector3 worldPos)
	{
		if (mAlpha < 0.001f) return false;
		if (mClipping == UIDrawCall.Clipping.None) return true;
		UpdateTransformMatrix();

		Vector3 pos = worldToLocal.MultiplyPoint3x4(worldPos);
		if (pos.x < mMin.x) return false;
		if (pos.y < mMin.y) return false;
		if (pos.x > mMax.x) return false;
		if (pos.y > mMax.y) return false;
		return true;
	}

	/// <summary>
	/// Returns whether the specified widget is visible by the panel.
	/// </summary>

	public bool IsVisible (UIWidget w)
	{
		if (mAlpha < 0.001f) return false;
		if (!w.enabled || !NGUITools.GetActive(w.cachedGameObject) || w.alpha < 0.001f) return false;

		// No clipping? No point in checking.
		if (mClipping == UIDrawCall.Clipping.None) return true;

		Vector2 size = w.relativeSize;
		Vector2 a = Vector2.Scale(w.pivotOffset, size);
		Vector2 b = a;

		a.x += size.x;
		a.y -= size.y;

		// Transform coordinates into world space
		Transform wt = w.cachedTransform;
		Vector3 v0 = wt.TransformPoint(a);
		Vector3 v1 = wt.TransformPoint(new Vector2(a.x, b.y));
		Vector3 v2 = wt.TransformPoint(new Vector2(b.x, a.y));
		Vector3 v3 = wt.TransformPoint(b);
		return IsVisible(v0, v1, v2, v3);
	}

	/// <summary>
	/// Helper function that marks the specified material as having changed so its mesh is rebuilt next frame.
	/// </summary>

	public void MarkMaterialAsChanged (Material mat, bool sort)
	{
		if (mat != null)
		{
			if (sort) mDepthChanged = true;
			if (!mChanged.Contains(mat))
				mChanged.Add(mat);
		}
	}

	/// <summary>
	/// Add the specified widget to the managed list.
	/// </summary>

	public void AddWidget (UIWidget w)
	{
		if (w != null)
		{
#if UNITY_EDITOR
			if (w.cachedTransform.parent != null)
			{
				UIWidget parentWidget = NGUITools.FindInParents<UIWidget>(w.cachedTransform.parent.gameObject);

				if (parentWidget != null)
				{
					w.cachedTransform.parent = parentWidget.cachedTransform.parent;
					Debug.LogError("You should never nest widgets! Parent them to a common game object instead. Forcefully changing the parent.", w);

					// If the error above gets triggered, it means that you parented one widget to another.
					// If left unchecked, this may lead to odd behavior in the UI. Consider restructuring your UI.
					// For example, if you were trying to do this:

					// Widget #1
					//  |
					//  +- Widget #2

					// You can do this instead, fixing the problem:

					// GameObject (scale 1, 1, 1)
					//  |
					//  +- Widget #1
					//  |
					//  +- Widget #2
				}
			}
#endif

			if (!mWidgets.Contains(w))
			{
				mWidgets.Add(w);

				if (!mChanged.Contains(w.material))
					mChanged.Add(w.material);

				mDepthChanged = true;
			}
		}
	}

	/// <summary>
	/// Remove the specified widget from the managed list.
	/// </summary>

	public void RemoveWidget (UIWidget w)
	{
		if (w != null)
		{
			if (w != null && mWidgets.Remove(w) && w.material != null)
				mChanged.Add(w.material);
		}
	}

	/// <summary>
	/// Get or create a UIScreen responsible for drawing the widgets using the specified material.
	/// </summary>

	UIDrawCall GetDrawCall (Material mat, bool createIfMissing)
	{
		for (int i = 0, imax = drawCalls.size; i < imax; ++i)
		{
			UIDrawCall dc = drawCalls.buffer[i];
			if (dc.material == mat)
				return dc;
		}

		UIDrawCall sc = null;
		#if ENABLE_DRAW_OPTIMIZATION
		for (int i = 0, imax = mDrawCallsHidden.size; i < imax; ++i)
		{
			UIDrawCall dc = mDrawCallsHidden.buffer[i];
			if (dc.material == mat) 
			{
				sc =  dc;
				break;
			}
		}
		
		if( sc != null )
		{
			sc.gameObject.layer = cachedGameObject.layer;
			mDrawCallsHidden.Remove(sc);
			mDrawCalls.Add(sc);
			
			return sc;
		}

#endif

		if (createIfMissing)
		{
#if UNITY_EDITOR
			// If we're in the editor, create the game object with hide flags set right away
			GameObject go = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags("_UIDrawCall [" + mat.name + "]",
				(mDebugInfo == DebugInfo.Geometry) ? HideFlags.DontSave | HideFlags.NotEditable : HideFlags.HideAndDontSave);
#else
			GameObject go = new GameObject("_UIDrawCall [" + mat.name + "]");
			//go.hideFlags = HideFlags.DontSave;
			DontDestroyOnLoad(go);
#endif
			go.layer = cachedGameObject.layer;
			sc = go.AddComponent<UIDrawCall>();
			sc.material = mat;
			mDrawCalls.Add(sc);
		}
		return sc;
	}

	/// <summary>
	/// Cache components.
	/// </summary>

	void Awake ()
	{
		mGo = gameObject;
		mTrans = transform;
	}

	/// <summary>
	/// Layer is used to ensure that if it changes, widgets get moved as well.
	/// </summary>

	void Start ()
	{
		mLayer = mGo.layer;
		UICamera uic = UICamera.FindCameraForLayer(mLayer);
		mCam = (uic != null) ? uic.cachedCamera : NGUITools.FindCameraForLayer(mLayer);
		mUpdateTime = Time.realtimeSinceStartup;
	}

	/// <summary>
	/// Mark all widgets as having been changed so the draw calls get re-created.
	/// </summary>

	protected override void OnEnable ()
	{
		base.OnEnable();

		for (int i = 0; i < mWidgets.size; )
		{
			UIWidget w = mWidgets.buffer[i];

			if (w != null)
			{
				MarkMaterialAsChanged(w.material, true);
				++i;
			}
			else mWidgets.RemoveAt(i);
		}
	}

	/// <summary>
	/// Destroy all draw calls we've created when this script gets disabled.
	/// </summary>

	protected override void OnDisable ()
	{
		base.OnDisable();
			
		for (int i = mDrawCalls.size; i > 0; )
		{
			UIDrawCall dc = mDrawCalls.buffer[--i];
#if ENABLE_DRAW_OPTIMIZATION
			if(dc != null) 
			{			
				dc.gameObject.SetActive(false);
				mDrawCallsHidden.Add(dc);
			}
			mDrawCalls.Remove(dc);
#else
			if (dc != null) NGUITools.DestroyImmediate(dc.gameObject);
#endif
		}
		mDrawCalls.Release();
		mChanged.Release();
	}

	/// <summary>
	/// Destroy all the hidden draw calls we've created when this script gets destroyed.
	/// </summary>

#if ENABLE_DRAW_OPTIMIZATION
	void OnDestroy ()
	{
		for (int i = mDrawCallsHidden.size; i > 0; )
		{
			UIDrawCall dc = mDrawCallsHidden.buffer[--i];

			mDrawCallsHidden.Remove(dc);

			if (dc != null) NGUITools.DestroyImmediate(dc.gameObject);	
		}

	}
#endif

	/// <summary>
	/// Update the world-to-local transform matrix as well as clipping bounds.
	/// </summary>

	void UpdateTransformMatrix ()
	{
		if (mUpdateTime == 0f || mMatrixTime != mUpdateTime)
		{
			mMatrixTime = mUpdateTime;
			worldToLocal = cachedTransform.worldToLocalMatrix;

			if (mClipping != UIDrawCall.Clipping.None)
			{
				Vector2 size = new Vector2(mClipRange.z, mClipRange.w);

				if (size.x == 0f) size.x = (mCam == null) ? Screen.width  : mCam.pixelWidth;
				if (size.y == 0f) size.y = (mCam == null) ? Screen.height : mCam.pixelHeight;

				size *= 0.5f;

				mMin.x = mClipRange.x - size.x;
				mMin.y = mClipRange.y - size.y;
				mMax.x = mClipRange.x + size.x;
				mMax.y = mClipRange.y + size.y;
			}
		}
	}

	/// <summary>
	/// Update the clipping rect in the shaders and draw calls' positions.
	/// </summary>

	public void UpdateDrawcalls ()
	{
		Vector4 range = Vector4.zero;

		if (mClipping != UIDrawCall.Clipping.None)
		{
			range = new Vector4(mClipRange.x, mClipRange.y, mClipRange.z * 0.5f, mClipRange.w * 0.5f);
		}

		if (range.z == 0f) range.z = Screen.width * 0.5f;
		if (range.w == 0f) range.w = Screen.height * 0.5f;

		Transform t = cachedTransform;
		UIDrawCall dc;
		Transform dt;

		for (int i = 0, imax = mDrawCalls.size; i < imax; ++i)
		{
			dc = mDrawCalls.buffer[i];
			dc.clipping = mClipping;
			dc.clipRange = range;
			dc.clipSoftness = mClipSoftness;
			dc.depthPass = depthPass && mClipping == UIDrawCall.Clipping.None;

			// Set the draw call's transform to match the panel's.
			// Note that parenting directly to the panel causes unity to crash as soon as you hit Play.
			dt = dc.transform;
			dt.position = t.position;
			dt.rotation = t.rotation;
			dt.localScale = t.lossyScale;
		}
	}

	/// <summary>
	/// Set the draw call's geometry responsible for the specified material.
	/// </summary>

	void Fill (Material mat)
	{
		int highest = -100;

#if ENABLE_DRAW_OPTIMIZATION
		// -kvb start optimization
		UIDrawCall dc = GetDrawCall(mat, false);	
#endif		

		// Fill the buffers for the specified material
		for (int i = 0; i < mWidgets.size; )
		{
			UIWidget w = mWidgets.buffer[i];

			if (System.Object.ReferenceEquals(w,null))
			{
				mWidgets.RemoveAt(i);
				continue;
			}
			else if (System.Object.ReferenceEquals(w.material,mat) && w.isVisible)
			{
#if ENABLE_DRAW_OPTIMIZATION
				if (System.Object.ReferenceEquals(w.panel,this))
				{
					int depth = w.depth;
					if (depth > highest) highest = depth;
					if( dc == null )
					{
						dc = GetDrawCall( mat, true );
					}
					if (generateNormals) 
					{
						w.WriteToBuffers(dc.mVerts, dc.mUvs, dc.mCols, dc.mNorms, dc.mTans);
					}
					else
					{
						w.WriteToBuffers(dc.mVerts, dc.mUvs, dc.mCols, null, null);
					}
				}
				else
				{
					mWidgets.RemoveAt(i);
					continue;
				}
			}
			++i;
		}

		if (dc != null)
		{
			if (dc.mVerts.size > 0)
			{
				// Rebuild the draw call's mesh
				if(!dc.gameObject.activeInHierarchy)
				{
					dc.gameObject.SetActive(true);
				}

				dc.depthPass = depthPass && mClipping == UIDrawCall.Clipping.None;
				dc.depth = sortByDepth ? highest : 0;
				dc.Set(dc.mVerts, generateNormals ? dc.mNorms : null, generateNormals ? dc.mTans : null, dc.mUvs, dc.mCols);
				dc.mainTexture = mat.mainTexture;
			}
			else
			{
				// There is nothing to draw for this material -- eliminate the draw call
				mDrawCalls.Remove(dc);										
				dc.gameObject.SetActive(false);
				mDrawCallsHidden.Add( dc );
			}

			// Cleanup
			dc.mVerts.Clear();
			dc.mNorms.Clear();
			dc.mTans.Clear();
			dc.mUvs.Clear();
			dc.mCols.Clear();

		}

#else
				if (w.panel == this)
				{
					int depth = w.depth;
					if (depth > highest) highest = depth;
					if (generateNormals) w.WriteToBuffers(mVerts, mUvs, mCols, mNorms, mTans);
					else w.WriteToBuffers(mVerts, mUvs, mCols, null, null);
				}
				else
				{
					mWidgets.RemoveAt(i);
					continue;
				}
			}
			++i;
		}

		if (mVerts.size > 0)
		{
			// Rebuild the draw call's mesh
			UIDrawCall dc = GetDrawCall(mat, true);
			dc.depthPass = depthPass && mClipping == UIDrawCall.Clipping.None;
			dc.depth = sortByDepth ? highest : 0;
#if ZYNGA_TRAMP
			if (!AutomatedPlayer.isOver9000Vertices)
#endif
			if (mVerts.size > 9000)
			{
				Debug.LogErrorFormat("Number of widgets on UIPanel with too many vertices: {0}, UIPanel: {1}, verts: {2}",
					mWidgets.size,
					CommonGameObject.getObjectPath(gameObject),
					mVerts.size
				);
				Debug.LogError("Looking at the first 20 widgets on the UIPanel:");
				for (int i  = 0; i < mWidgets.size && i < 20; i++)
				{
					UIWidget w = mWidgets.buffer[i];
					UILabel label = w as UILabel;
					UISprite sprite = w as UISprite;
					UITexture tex = w as UITexture;
					
					if (label != null)
					{
						Debug.LogErrorFormat("UILabel: {0}", CommonGameObject.getObjectPath(w.gameObject));
					}
					if (sprite != null)
					{
						Debug.LogErrorFormat("UISprite: {0}, Sprite Name: {1}", CommonGameObject.getObjectPath(w.gameObject), sprite.spriteName);
					}
					if (tex != null)
					{
						Debug.LogErrorFormat("UITexture: {0}", CommonGameObject.getObjectPath(w.gameObject));
					}
				}
			}
			dc.Set(mVerts, generateNormals ? mNorms : null, generateNormals ? mTans : null, mUvs, mCols);
			dc.mainTexture = mat.mainTexture;
		}
		else
		{
			// There is nothing to draw for this material -- eliminate the draw call
			UIDrawCall dc = GetDrawCall(mat, false);

			if (dc != null)
			{
				mDrawCalls.Remove(dc);
				NGUITools.DestroyImmediate(dc.gameObject);
			}
		}

		// Cleanup
		mVerts.Clear();
		mNorms.Clear();
		mTans.Clear();
		mUvs.Clear();
		mCols.Clear();
#endif
	}

	/// <summary>
	/// Main update function
	/// </summary>

	protected override void LateUpdate ()
	{
		base.LateUpdate();
					
		mUpdateTime += Time.unscaledDeltaTime;
		UpdateTransformMatrix();

		// Always move widgets to the panel's layer
		if (mLayer != cachedGameObject.layer)
		{
			mLayer = mGo.layer;
			UICamera uic = UICamera.FindCameraForLayer(mLayer);
			mCam = (uic != null) ? uic.cachedCamera : NGUITools.FindCameraForLayer(mLayer);
			SetChildLayer(cachedTransform, mLayer);
			for (int i = 0, imax = drawCalls.size; i < imax; ++i) mDrawCalls.buffer[i].gameObject.layer = mLayer;
		}

#if UNITY_EDITOR
		bool forceVisible = cullWhileDragging ? false : (clipping == UIDrawCall.Clipping.None) || (Application.isPlaying && mCullTime > mUpdateTime);
#else
		bool forceVisible = cullWhileDragging ? false : (clipping == UIDrawCall.Clipping.None) || (mCullTime > mUpdateTime);
#endif
		// Update all widgets
		for (int i = 0, imax = mWidgets.size; i < imax; ++i)
		{
			UIWidget w = mWidgets[i];

			// If the widget is visible, update it
			if (w.UpdateGeometry(this, forceVisible))
			{
				// We will need to refill this buffer
				Material mat = w.mMatCached;
				if(System.Object.ReferenceEquals(mat,null))
					mat = w.material;
				if (!mChanged.Contains(mat))
					 mChanged.Add(mat);
			}
		}

		// Inform the changed event listeners
		if (mChanged.size != 0 && onChange != null) onChange();

		// If the depth has changed, we need to re-sort the widgets
		if (mDepthChanged)
		{
			mDepthChanged = false;
			mWidgets.Sort(UIWidget.CompareFunc);
		}

		// Fill the draw calls for all of the changed materials
		for (int i = 0, imax = mChanged.size; i < imax; ++i) Fill(mChanged.buffer[i]);

		// Update the clipping rects
		UpdateDrawcalls();
		mChanged.Clear();
#if UNITY_EDITOR
		mScreenSize = new Vector2(Screen.width, Screen.height);
#endif
	}

	/// <summary>
	/// Immediately refresh the panel.
	/// </summary>

	public void Refresh ()
	{
		UIWidget[] wd = GetComponentsInChildren<UIWidget>();
		for (int i = 0, imax = wd.Length; i < imax; ++i) wd[i].Update();
		LateUpdate();
	}

#if UNITY_EDITOR

	// This is necessary because Screen.height inside OnDrawGizmos will return the size of the Scene window,
	// and we need the size of the game window in order to draw the bounds properly.
	int mScreenHeight = 720;
	void Update () { mScreenHeight = Screen.height; }

	/// <summary>
	/// Draw a visible pink outline for the clipped area.
	/// </summary>

	void OnDrawGizmos ()
	{
		if (mDebugInfo == DebugInfo.Gizmos)
		{
			bool clip = (mClipping != UIDrawCall.Clipping.None);
			Vector2 size = clip ? new Vector2(mClipRange.z, mClipRange.w) : Vector2.zero;

			GameObject go = UnityEditor.Selection.activeGameObject;
			bool selected = (go != null) && (NGUITools.FindInParents<UIPanel>(go) == this);

			if (selected || clip || (mCam != null && mCam.orthographic))
			{
				if (size.x == 0f) size.x = mScreenSize.x;
				if (size.y == 0f) size.y = mScreenSize.y;

				if (!clip)
				{
					UIRoot root = NGUITools.FindInParents<UIRoot>(cachedGameObject);
					if (root != null) size *= root.GetPixelSizeAdjustment(mScreenHeight);
				}

				Transform t = clip ? transform : (mCam != null ? mCam.transform : null);

				if (t != null)
				{
					Vector3 pos = new Vector2(mClipRange.x, mClipRange.y);

					Gizmos.matrix = t.localToWorldMatrix;

					if (go != cachedGameObject)
					{
						Gizmos.color = clip ? Color.magenta : new Color(0.5f, 0f, 0.5f);
						Gizmos.DrawWireCube(pos, size);

						// Make the panel selectable
						//Gizmos.color = Color.clear;
						//Gizmos.DrawCube(pos, size);
					}
					else
					{
						Gizmos.color = Color.green;
						Gizmos.DrawWireCube(pos, size);
					}
				}
			}
		}
	}
#endif

	/// <summary>
	/// Calculate the offset needed to be constrained within the panel's bounds.
	/// </summary>

	public Vector3 CalculateConstrainOffset (Vector2 min, Vector2 max)
	{
		float offsetX = clipRange.z * 0.5f;
		float offsetY = clipRange.w * 0.5f;

		Vector2 minRect = new Vector2(min.x, min.y);
		Vector2 maxRect = new Vector2(max.x, max.y);
		Vector2 minArea = new Vector2(clipRange.x - offsetX, clipRange.y - offsetY);
		Vector2 maxArea = new Vector2(clipRange.x + offsetX, clipRange.y + offsetY);

		if (clipping == UIDrawCall.Clipping.SoftClip)
		{
			minArea.x += clipSoftness.x;
			minArea.y += clipSoftness.y;
			maxArea.x -= clipSoftness.x;
			maxArea.y -= clipSoftness.y;
		}
		return NGUIMath.ConstrainRect(minRect, maxRect, minArea, maxArea);
	}

	/// <summary>
	/// Constrain the current target position to be within panel bounds.
	/// </summary>

	public bool ConstrainTargetToBounds (Transform target, ref Bounds targetBounds, bool immediate)
	{
		Vector3 offset = CalculateConstrainOffset(targetBounds.min, targetBounds.max);

		if (offset.magnitude > 0f)
		{
			if (immediate)
			{
				target.localPosition += offset;
				targetBounds.center += offset;
				SpringPosition sp = target.GetComponent<SpringPosition>();
				if (sp != null) sp.enabled = false;
			}
			else
			{
				SpringPosition sp = SpringPosition.Begin(target.gameObject, target.localPosition + offset, 13f);
				sp.ignoreTimeScale = true;
				sp.worldSpace = false;
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// Constrain the specified target to be within the panel's bounds.
	/// </summary>

	public bool ConstrainTargetToBounds (Transform target, bool immediate)
	{
		Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(cachedTransform, target);
		return ConstrainTargetToBounds(target, ref bounds, immediate);
	}

	/// <summary>
	/// Helper function that recursively sets all children with widgets' game objects layers to the specified value, stopping when it hits another UIPanel.
	/// </summary>

	static void SetChildLayer (Transform t, int layer)
	{
		for (int i = 0; i < t.childCount; ++i)
		{
			Transform child = t.GetChild(i);

			if (child.GetComponent<UIPanel>() == null)
			{
				if (child.GetComponent<UIWidget>() != null)
				{
					child.gameObject.layer = layer;
				}					
				SetChildLayer(child, layer);
			}
		}
	}

	/// <summary>
	/// Find the UIPanel responsible for handling the specified transform.
	/// </summary>

	static public UIPanel Find (Transform trans, bool createIfMissing)
	{
		Transform origin = trans;
		UIPanel panel = null;

		while (panel == null && trans != null)
		{
			panel = trans.GetComponent<UIPanel>();
			if (panel != null) break;
			if (trans.parent == null) break;
			trans = trans.parent;
		}

		if (createIfMissing && panel == null && trans != origin)
		{
			panel = trans.gameObject.AddComponent<UIPanel>();
//			panel.sortByDepth = true;	// Zynga/Todd - Don't set this true. We never want to use the depth sorting option.
			SetChildLayer(panel.cachedTransform, panel.cachedGameObject.layer);
		}
		return panel;
	}

	/// <summary>
	/// Find the UIPanel responsible for handling the specified transform, creating a new one if necessary.
	/// </summary>

	static public UIPanel Find (Transform trans) { return Find(trans, true); }
}
