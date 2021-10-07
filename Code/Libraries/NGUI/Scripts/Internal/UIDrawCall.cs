//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This is an internally-created script used by the UI system. You shouldn't be attaching it manually.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Internal/Draw Call")]
public class UIDrawCall : TICoroutineMonoBehaviour
{
	public enum Clipping
	{
		None,
		HardClip,	// Obsolete. Used to use clip() but it's not supported by some devices.
		AlphaClip,	// Adjust the alpha, compatible with all devices
		SoftClip,	// Alpha-based clipping with a softened edge
	}

	Transform		mTrans;			// Cached transform
	Material		mSharedMat;		// Material used by this screen
	Mesh			mMesh0;			// First generated mesh
	Mesh			mMesh1;			// Second generated mesh
	MeshFilter		mFilter;		// Mesh filter for this draw call
	MeshRenderer	mRen;			// Mesh renderer for this screen
	Clipping		mClipping;		// Clipping mode
	Vector4			mClipRange;		// Clipping, if used
	Vector2			mClipSoft;		// Clipping softness
	Material		mMat;			// Instantiated material
	Material		mDepthMat;		// Depth-writing material, created if necessary
	int[]			mIndices;		// Cached indices

	bool mUseDepth = false;
	bool mReset = true;
	bool mEven = true;
	int mDepth = 0;

	public const int MAX_VERTS_PER_MESH = 32764; // This is the first number divisible by 4 that is less than 2**15, it is magic.
	
#if ENABLE_DRAW_OPTIMIZATION	
	public static int[] sIndicesBuffer = null;
	static int sMaxNumberOfVerts = MAX_VERTS_PER_MESH;
	static int sIntSize = sizeof( int );

	int vertsOldSize = 0;
	// Cached in order to reduce memory allocations
	// market as public so for fast access
	public BetterList<Vector3> mVerts = new BetterList<Vector3>();
	public BetterList<Vector3> mNorms = new BetterList<Vector3>();
	public BetterList<Vector4> mTans = new BetterList<Vector4>();
	public BetterList<Vector2> mUvs = new BetterList<Vector2>();
	public BetterList<Color32> mCols = new BetterList<Color32>();

	public void Awake()
	{
		if( sIndicesBuffer == null )
		{
			int count = sMaxNumberOfVerts;
			int indexCount = (count >> 1) * 3;
			sIndicesBuffer = new int[indexCount];
			int index = 0;

			for (int i = 0; i < count; i += 4)
			{
				sIndicesBuffer[index++] = i;
				sIndicesBuffer[index++] = i + 1;
				sIndicesBuffer[index++] = i + 2;

				sIndicesBuffer[index++] = i + 2;
				sIndicesBuffer[index++] = i + 3;
				sIndicesBuffer[index++] = i;
			}			
		}
		
	}

#endif

	/// <summary>
	/// Whether an additional pass will be created to render the geometry to the depth buffer first.
	/// </summary>

	public bool depthPass { get { return mUseDepth; } set { if (mUseDepth != value) { mUseDepth = value; mReset = true; } } }

	/// <summary>
	/// Draw order used by the draw call.
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
				if (mMat != null && mSharedMat != null)
					mMat.renderQueue = mSharedMat.renderQueue + value;
			}
		}
	}

	/// <summary>
	/// Transform is cached for speed and efficiency.
	/// </summary>

	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Material used by this screen.
	/// </summary>

	public Material material { get { return mSharedMat; } set { mSharedMat = value; } }

	/// <summary>
	/// Texture used by the material.
	/// </summary>

	public Texture mainTexture { get { return mMat.mainTexture; } set { mMat.mainTexture = value; } }

	/// <summary>
	/// The number of triangles in this draw call.
	/// </summary>

	public int triangles
	{
		get
		{
			Mesh mesh = mEven ? mMesh0 : mMesh1;
			return (mesh != null) ? mesh.vertexCount >> 1 : 0;
		}
	}

	/// <summary>
	/// Whether the draw call is currently using a clipped shader.
	/// </summary>

	public bool isClipped { get { return mClipping != Clipping.None; } }

	/// <summary>
	/// Clipping used by the draw call
	/// </summary>

	public Clipping clipping { get { return mClipping; } set { if (mClipping != value) { mClipping = value; mReset = true; } } }

	/// <summary>
	/// Clip range set by the panel -- used with a shader that has the "_ClipRange" property.
	/// </summary>

	public Vector4 clipRange { get { return mClipRange; } set { mClipRange = value; } }

	/// <summary>
	/// Clipping softness factor, if soft clipping is used.
	/// </summary>

	public Vector2 clipSoftness { get { return mClipSoft; } set { mClipSoft = value; } }

	/// <summary>
	/// Returns a mesh for writing into. The mesh is double-buffered as it gets the best performance on iOS devices.
	/// http://forum.unity3d.com/threads/118723-Huge-performance-loss-in-Mesh.CreateVBO-for-dynamic-meshes-IOS
	/// </summary>

	Mesh GetMesh (ref bool rebuildIndices, int vertexCount)
	{
		mEven = !mEven;

		if (mEven)
		{
			if (mMesh0 == null)
			{
				mMesh0 = new Mesh();
				mMesh0.hideFlags = HideFlags.DontSave;
				mMesh0.name = "Mesh0 for " + mSharedMat.name;
				mMesh0.MarkDynamic();
				rebuildIndices = true;
			}
			else if (rebuildIndices || mMesh0.vertexCount != vertexCount)
			{
				rebuildIndices = true;
#if !ENABLE_DRAW_OPTIMIZATION
				mMesh0.Clear(); //-kvb mesh.clear is now called if vertices are updated also.
#endif
			}
			return mMesh0;
		}
		else if (mMesh1 == null)
		{
			mMesh1 = new Mesh();
			mMesh1.hideFlags = HideFlags.DontSave;
			mMesh1.name = "Mesh1 for " + mSharedMat.name;
			mMesh1.MarkDynamic();
			rebuildIndices = true;
		}
		else if (rebuildIndices || mMesh1.vertexCount != vertexCount)
		{
			rebuildIndices = true;
#if !ENABLE_DRAW_OPTIMIZATION	
			mMesh1.Clear(); //-kvb mesh.clear is now called if vertices are updated also.
#endif
		}
		return mMesh1;
	}

	/// <summary>
	/// Update the renderer's materials.
	/// </summary>

	void UpdateMaterials ()
	{
		bool useClipping = (mClipping != Clipping.None);

		// Create a temporary material
		if (mMat == null)
		{
			mMat = new Material(mSharedMat);
			mMat.hideFlags = HideFlags.DontSave;
			mMat.CopyPropertiesFromMaterial(mSharedMat);
			mMat.renderQueue = mSharedMat.renderQueue + mDepth;
		}

		// If clipping should be used, we need to find a replacement shader
		if (useClipping && mClipping != Clipping.None)
		{
			Shader shader = null;
			const string alpha	= " (AlphaClip)";
			const string soft	= " (SoftClip)";

			// Figure out the normal shader's name
			string shaderName = mSharedMat.shader.name;
			shaderName = shaderName.Replace(alpha, "");
			shaderName = shaderName.Replace(soft, "");

			// Try to find the new shader
			if (mClipping == Clipping.HardClip ||
				mClipping == Clipping.AlphaClip) shader = ShaderCache.find(shaderName + alpha);
			else if (mClipping == Clipping.SoftClip) shader = ShaderCache.find(shaderName + soft);

			// If there is a valid shader, assign it to the custom material
			if (shader != null)
			{
				mMat.shader = shader;
			}
			else
			{
				Debug.LogError(shaderName + " doesn't have a clipped shader version for " + mClipping);
				mClipping = Clipping.None;
			}
		}

		// If depth pass should be used, create the depth material
		if (mUseDepth)
		{
			var alphaTex = mSharedMat.HasProperty("_AlphaTex") ? mSharedMat.GetTexture("_AlphaTex") : null;
			bool usesAlphaSplit = (alphaTex != null);
			Shader shader = usesAlphaSplit ? ShaderCache.find("Unlit/Depth Cutout From Green") : ShaderCache.find("Unlit/Depth Cutout");

			// If the type of depth cutout shader has changed (between standard and alphasplit) then discard existing material
			if (mDepthMat != null && mDepthMat.shader != shader)
			{
				NGUITools.Destroy(mDepthMat);
				mDepthMat = null;
			}

			// Make new material with appropriate shader
			if (mDepthMat == null)
			{
				mDepthMat = new Material(shader);
				mDepthMat.hideFlags = HideFlags.DontSave;
			}
			mDepthMat.mainTexture = usesAlphaSplit ? alphaTex : mSharedMat.mainTexture;
		}
		else if (mDepthMat != null)
		{
			NGUITools.Destroy(mDepthMat);
			mDepthMat = null;
		}

		if (mDepthMat != null)
		{
			// If we're already using this material, do nothing
			if (mRen.sharedMaterials != null && mRen.sharedMaterials.Length == 2 && 
			    mRen.sharedMaterials[0] == mDepthMat && mRen.sharedMaterials[1] == mMat) return;

			// Set the double material
			mRen.sharedMaterials = new Material[] { mDepthMat, mMat };
		}
		else if (mRen.sharedMaterial != mMat)
		{
			mRen.sharedMaterials = new Material[] { mMat };
		}
	}

	/// <summary>
	/// Set the draw call's geometry.
	/// </summary>

	public void Set (BetterList<Vector3> verts, BetterList<Vector3> norms, BetterList<Vector4> tans, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		int count = verts.size;

		// Safety check to ensure we get valid values
		if (count > 0 && (count == uvs.size && count == cols.size) && (count % 4) == 0)
		{
			// Cache all components
			if (mFilter == null) mFilter = gameObject.GetComponent<MeshFilter>();
			if (mFilter == null) mFilter = gameObject.AddComponent<MeshFilter>();
			if (mRen == null) mRen = gameObject.GetComponent<MeshRenderer>();

			if (mRen == null)
			{
				mRen = gameObject.AddComponent<MeshRenderer>();
				UpdateMaterials();
			}
			else if (mMat != null && mMat.mainTexture != mSharedMat.mainTexture)
			{
				UpdateMaterials();
			}

			// This is for early detection of excessive UIPanel setups.
			// At over 9000 vertices, we're talking about a UI layout with
			// a minimum of over 2000 quads.
			if (verts.size > 9000 && gameObject != null)
			{
				string warningMessage = string.Format("Over 9000 vertices on one panel on gameObject {0}: {1} vertices", 
					CommonGameObject.getObjectPath(gameObject), verts.size);
#if ZYNGA_TRAMP
				if (!AutomatedPlayer.isOver9000Vertices)
				{
					Debug.LogWarning(warningMessage);
					AutomatedPlayer.isOver9000Vertices = true;
				}
#elif UNITY_EDITOR
				Debug.LogWarning(warningMessage);
				Debug.LogError("Breaking. Please get manager approval to allow NGUI items with over 9000 vertices.");
#if !ZYNGA_TRAMP
				if (Application.isEditor && Application.isPlaying)
				{
					Debug.Break();
				}
#endif
#else
				Debug.LogWarning(warningMessage);
#endif
			}
			
#if ENABLE_DRAW_OPTIMIZATION	
			// -kvb solstice changes start 
			if (verts.size < sMaxNumberOfVerts)
			{
				
				int indexCount = ( verts.Capacity() >> 1) * 3;
				bool rebuildIndices = (mIndices == null || mIndices.Length != indexCount );			
				
				// Populate the index buffer
				if (rebuildIndices)
				{
//					 It takes 6 indices to draw a quad of 4 vertices
					mIndices = new int[indexCount];				
//					Array.Copy( sIndicesBuffer, mIndices, indexCount);
					System.Buffer.BlockCopy(sIndicesBuffer, 0 * sIntSize, mIndices, 0 * sIntSize, indexCount * sIntSize);
				}
				
				// Set the mesh values
				Mesh mesh = GetMesh(ref rebuildIndices, verts.size);
				bool updateVerts = vertsOldSize != verts.size || mesh.vertices.Length != verts.Capacity();

				if( updateVerts || rebuildIndices )
				{
					mesh.Clear();
				}
				
				mesh.vertices = verts.ToArrayNoResize( true );	
				if (norms != null) mesh.normals = norms.ToArrayNoResize( true );
				if (tans != null) mesh.tangents = tans.ToArrayNoResize( true );
				mesh.uv = uvs.ToArrayNoResize( true );
				mesh.colors32 = cols.ToArrayNoResize( true );

				if ( updateVerts || rebuildIndices ) 
					mesh.triangles = mIndices;				
				mFilter.mesh = mesh;
				
				vertsOldSize = verts.size;
				
			// -kvb solstice changes end
#else
			if (verts.size < MAX_VERTS_PER_MESH)
			{
				int indexCount = (count >> 1) * 3;
				bool rebuildIndices = (mIndices == null || mIndices.Length != indexCount);

				// Populate the index buffer
				if (rebuildIndices)
				{
					// It takes 6 indices to draw a quad of 4 vertices
					mIndices = new int[indexCount];
					int index = 0;

					for (int i = 0; i < count; i += 4)
					{
						mIndices[index++] = i;
						mIndices[index++] = i + 1;
						mIndices[index++] = i + 2;

						mIndices[index++] = i + 2;
						mIndices[index++] = i + 3;
						mIndices[index++] = i;
					}
				}

				// Set the mesh values
				Mesh mesh = GetMesh(ref rebuildIndices, verts.size);
				mesh.vertices = verts.ToArray();
				if (norms != null) mesh.normals = norms.ToArray();
				if (tans != null) mesh.tangents = tans.ToArray();
				mesh.uv = uvs.ToArray();
				mesh.colors32 = cols.ToArray();
				if (rebuildIndices) mesh.triangles = mIndices;
				mesh.RecalculateBounds();
				mFilter.mesh = mesh;
#endif
			}
			else
			{
				if (mFilter.mesh != null) mFilter.mesh.Clear();
#if ZYNGA_TRAMP
				if (!AutomatedPlayer.isOver9000Vertices)
#endif
				Debug.LogError(string.Format("Too many vertices on one panel on gameObject {0}: {1} vertices", CommonGameObject.getObjectPath(gameObject), verts.size));
			}
		}
		else
		{
			if (mFilter.mesh != null) mFilter.mesh.Clear();
#if ZYNGA_TRAMP
			if (!AutomatedPlayer.isOver9000Vertices)
#endif
			Debug.LogError("UIWidgets must fill the buffer with 4 vertices per quad. Found " + count);
		}
	}

	/// <summary>
	/// This function is called when it's clear that the object will be rendered.
	/// We want to set the shader used by the material, creating a copy of the material in the process.
	/// We also want to update the material's properties before it's actually used.
	/// </summary>

	void OnWillRenderObject ()
	{
		if (mReset)
		{
			mReset = false;
			UpdateMaterials();
		}

		if (mMat != null && isClipped)
		{
			mMat.mainTextureOffset = new Vector2(-mClipRange.x / mClipRange.z, -mClipRange.y / mClipRange.w);
			mMat.mainTextureScale = new Vector2(1f / mClipRange.z, 1f / mClipRange.w);

			Vector2 sharpness = new Vector2(1000.0f, 1000.0f);
			if (mClipSoft.x > 0f) sharpness.x = mClipRange.z / mClipSoft.x;
			if (mClipSoft.y > 0f) sharpness.y = mClipRange.w / mClipSoft.y;
			mMat.SetVector("_ClipSharpness", sharpness);
		}
	}

	/// <summary>
	/// Cleanup.
	/// </summary>

	void OnDestroy ()
	{
		NGUITools.DestroyImmediate(mMesh0);
		NGUITools.DestroyImmediate(mMesh1);
		NGUITools.DestroyImmediate(mMat);
		NGUITools.DestroyImmediate(mDepthMat);
	}
}
