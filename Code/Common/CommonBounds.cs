using UnityEngine;

/**
Common Bounding Box related utility functions

Author: kkralian
*/

public static class CommonBounds
{
	// Options for RenderTarget-based bounds determination
	const int RT_WIDTH = 64;
	const int RT_HEIGHT = 64;
	const int RT_LAYER = Layers.ID_BOUNDS_RENDERING; // Layer to isolate rendertargets
	const string RT_CAM_NAME = "BoundsRTCamera"; // Our singleton camera object name
	static Color RT_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.0f); // Clear to mid-gray with alpha of 0
	const float RT_DEVIATION = 0.03f;  // Minimum change to RT_COLOR components considered 'visibly significant'


	// Transforms an Axis Aligned Bounding Box (Bounds) by a Transform, from its local to world space.
	// NOTE: Rotating bounds will result in larger bounds, avoid repeated transforms.
	public static Bounds transformBounds(Transform transform, Bounds localBounds)
	{
		var center = transform.TransformPoint(localBounds.center);

		// transform the local extents' axes
		var extents = localBounds.extents;
		var axisX = transform.TransformVector(extents.x, 0, 0);
		var axisY = transform.TransformVector(0, extents.y, 0);
		var axisZ = transform.TransformVector(0, 0, extents.z);

		// sum their absolute value to get the world extents
		extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
		extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
		extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

		return new Bounds { center = center, extents = extents };
	}

	// Transforms an Axis Aligned Bounding Box (Bounds) by a Transform, from its world to local space.
	// NOTE: Rotating bounds will result in larger bounds, avoid repeated transforms.
	public static Bounds inverseTransformBounds(Transform transform, Bounds localBounds)
	{
		var center = transform.InverseTransformPoint(localBounds.center);

		// transform the local extents' axes
		var extents = localBounds.extents;
		var axisX = transform.InverseTransformVector(extents.x, 0, 0);
		var axisY = transform.InverseTransformVector(0, extents.y, 0);
		var axisZ = transform.InverseTransformVector(0, 0, extents.z);

		// sum their absolute value to get the world extents
		extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
		extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
		extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

		return new Bounds { center = center, extents = extents };
	}

	// Transforms an Axis Aligned Bounding Box (Bounds) by a Matrix4x4. 
	// NOTE: Rotating bounds will result in larger bounds, avoid repeated transforms.
	public static Bounds transformBounds(Matrix4x4 matrix, Bounds localBounds)
	{
		var center = matrix.MultiplyPoint3x4(localBounds.center);

		// transform the local extents' axes
		var extents = localBounds.extents;
		var axisX = matrix.MultiplyVector(new Vector3(extents.x, 0, 0));
		var axisY = matrix.MultiplyVector(new Vector3(0, extents.y, 0));
		var axisZ = matrix.MultiplyVector(new Vector3(0, 0, extents.z));

		// sum their absolute value to get the world extents
		extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
		extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
		extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

		return new Bounds { center = center, extents = extents };
	}


	// Calculates the skinnedMeshRenderer bounds, local to the SkinnedMeshRenderer
	// This also corrects the unity bug where it still applies world scale to the local results
	public static Bounds getSkinnedMeshRendererBounds(SkinnedMeshRenderer skinnedMeshRenderer)
	{
		var bakedMesh = new Mesh();
		skinnedMeshRenderer.BakeMesh( bakedMesh );
		bakedMesh.RecalculateBounds();
		var bounds = bakedMesh.bounds;

		// Unity's SkinnedMeshRenderer.BakeMesh should be in SkinnedMeshRenderer's local space, 
		// but it leaves world scale in the results (unity bug!); we fix it here...
		Vector3 scale = skinnedMeshRenderer.transform.lossyScale;
		Vector3 invScale = new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z);
		bounds.extents = Vector3.Scale(bounds.extents, invScale);
		bounds.center = Vector3.Scale(bounds.center, invScale);

		return bounds;
	}

	// Calculates the skinnedMeshRenderer bounds relative to any desired space (or null for worldspace)
	public static Bounds getSkinnedMeshRendererBoundsRelativeTo(SkinnedMeshRenderer skinnedMeshRenderer, Transform desiredSpace)
	{
		// Get bounds in whatever space we want by temporarily reparenting
		Transform origParent = skinnedMeshRenderer.transform.parent;
		skinnedMeshRenderer.transform.SetParent(desiredSpace, false);

		var bounds = getSkinnedMeshRendererBounds(skinnedMeshRenderer);

		skinnedMeshRenderer.transform.SetParent(origParent, false);
		return bounds;
	}

	// Calculates the skinnedMeshRenderer bounds in world space
	public static Bounds getSkinnedMeshRendererBoundsInWorldSpace(SkinnedMeshRenderer skinnedMeshRenderer)
	{
		return getSkinnedMeshRendererBoundsRelativeTo(skinnedMeshRenderer, null);
	}


	// Get's a MeshRenderer's bounds by rendering it, reading back the pixels, and ignoring the blank borders
	//   can pass in any transform for a desired space, or 'null' for worldspace results
	//   can pass in an optional out string to receive back a diagnostic 'ascii art' representation
	public static Bounds getRenderedMeshBoundsRelativeTo(MeshRenderer meshRenderer, Transform desiredSpace, out string asciiArt)
	{
		// Unity is very finicky with the order of operations in this function (pushing/restoring states)
		// All changes, including Unity upgrades, need to be thoroughly tested -KK

		asciiArt = "<NotYetRenderer>";
		var name = meshRenderer.name;

		// Get the mesh associated with this meshRenderer
		var meshFilter = meshRenderer.GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			Debug.LogWarning("BOUNDS: MeshRenderer has no matching meshFilter: " + name);
			return new Bounds();
		}

		var mesh = meshFilter.sharedMesh;
		if (mesh == null)
		{
			Debug.LogWarning("BOUNDS: MeshFilter has no mesh: " + name);
			return new Bounds();
		}

		if (!mesh.isReadable)
		{
			Debug.LogWarning("BOUNDS: Mesh is not readable: " + name);
			// code below still works, it just won't recalculate geometric bounds
		}

		// From MeshRenderer's space to the caller's desired space (or null for worldspace)
		var meshToDesiredMatrix = (desiredSpace == null) ? meshRenderer.transform.localToWorldMatrix :
			desiredSpace.worldToLocalMatrix * meshRenderer.transform.localToWorldMatrix;

		// Calculate geometric bounds if mesh is readable, else use object bounds
		var boundsInDesiredSpace = (mesh.isReadable && mesh.vertices.Length > 0) ? 
			GeometryUtility.CalculateBounds( mesh.vertices, meshToDesiredMatrix) :
			transformBounds( meshToDesiredMatrix, mesh.bounds );

		// Manually managed camera; Setup ortho camera to frame the geometric bounds
		var rtCamera = getBoundsRenderTargetCamera();
		rtCamera.transform.localPosition = boundsInDesiredSpace.center;
		rtCamera.orthographicSize = boundsInDesiredSpace.extents.y; 
		rtCamera.aspect = boundsInDesiredSpace.extents.x / boundsInDesiredSpace.extents.y;
		rtCamera.nearClipPlane = -boundsInDesiredSpace.extents.z;
		rtCamera.farClipPlane = boundsInDesiredSpace.extents.z + 1; // +1 to prevent depth of 0

		var origRT = RenderTexture.active;
		var origCamera = Camera.current;
		Camera.SetupCurrent(rtCamera);

		// Due to RenderMeshNow not working with MaterialPropertyBlocks, we have to render our mesh in-scene
		// Which means temporarily reparenting, overriding it's transform, setting a layer mask, etc.
		var origPos = meshRenderer.transform.localPosition;
		var origRot = meshRenderer.transform.localRotation;
		var origScale = meshRenderer.transform.localScale;
		var origLayer = meshRenderer.gameObject.layer;
		var origMeshParent = meshRenderer.transform.parent;

		// Temp Overrides
		meshRenderer.transform.SetParent(null, false);
		meshRenderer.gameObject.layer = RT_LAYER;
		setTransformFromMatrix(meshRenderer.transform, meshToDesiredMatrix); //ugh

		// Render this camera now
		GL.Clear(true, true, RT_COLOR); //force
		rtCamera.Render();

		// Restore 
		meshRenderer.transform.SetParent(origMeshParent, false);
		meshRenderer.transform.localPosition = origPos;
		meshRenderer.transform.localRotation = origRot;
		meshRenderer.transform.localScale = origScale;
		meshRenderer.gameObject.layer = origLayer;

		// Get the pixeldata
		var pixels = getPixelsFromRenderTarget();

		// restore original rendertarget
		RenderTexture.active = origRT;

		// Diagnostic drawing of our tempRenderTarget (only to Scene view)
		if (origCamera != null && origCamera.cameraType == CameraType.SceneView &&
			Event.current != null && Event.current.type == EventType.Repaint)
		{
			diagnosticBlitToScreen(rtCamera.targetTexture);
		}

		// Provide an AsciiArt version if the caller asked for it (for logging purposes)
		if (asciiArt != null)
		{
			//asciiArt = getAsciiArtFromColorArray(pixels, RT_WIDTH, RT_HEIGHT);
		}

		// Must restore scene cameras during GUI updates
		if (origCamera != null && origCamera.cameraType == CameraType.SceneView && Event.current != null)
		{
			Camera.SetupCurrent(origCamera);
		}

		// Constrain geometric bounds to the top and bottom most visible pixels 
		var constrainedBounds = constrainBoundsToVerticalVisiblePixels(boundsInDesiredSpace, pixels, RT_WIDTH, RT_HEIGHT);

		return constrainedBounds;
	}

	// version without an AsciiArt parameter
	public static Bounds getRenderedMeshBoundsRelativeTo(MeshRenderer meshRenderer, Transform desiredSpace)
	{
		string nullString = null;
		return getRenderedMeshBoundsRelativeTo(meshRenderer, desiredSpace, out nullString);
	}


	// Get's a SpriteRenderer's bounds by rendering it, reading back the pixels, and ignoring the blank borders
	//   can pass in any transform for a desired space, or 'null' for worldspace results
	public static Bounds getRenderedSpriteBoundsRelativeTo(SpriteRenderer spriteRenderer, Transform desiredSpace)
	{
		// Unity is very finicky with the order of operations in this function (pushing/restoring states)
		// All changes, including Unity upgrades, need to be thoroughly tested -KK
		
		var sprite = spriteRenderer.sprite;
		if (sprite == null)
		{
			Debug.LogWarning("BOUNDS: spriteRenderer has no sprite");
			return new Bounds();
		}

		var material = spriteRenderer.sharedMaterial;
		if (sprite == null)
		{
			Debug.LogWarning("BOUNDS: spriteRenderer has no material");
			return new Bounds();
		}

		// From SpriteRenderer's space to the caller's desired space (or null for worldspace)
		var spriteToDesiredMatrix = spriteRenderer.transform.localToWorldMatrix;
		if (desiredSpace != null)
		{
			spriteToDesiredMatrix = desiredSpace.worldToLocalMatrix * spriteToDesiredMatrix;
		}

		// When transforming local sprite bounds, we need to apply the x/y flip ourselves
		var spriteBoundsToDesiredMatrix = spriteToDesiredMatrix;
		if (spriteRenderer.flipX || spriteRenderer.flipY)
		{
			var eulerRotations = new Vector3(spriteRenderer.flipY ? 180 : 0, spriteRenderer.flipX ? 180 : 0, 0);
			var spriteFlipMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(eulerRotations), Vector3.one);
			spriteBoundsToDesiredMatrix = spriteToDesiredMatrix * spriteFlipMatrix;
		}

		var boundsInDesiredSpace = transformBounds(spriteBoundsToDesiredMatrix, spriteRenderer.sprite.bounds);

		// Manually managed camera; Setup ortho camera to frame the geometric bounds
		var rtCamera = getBoundsRenderTargetCamera();
		rtCamera.transform.localPosition = boundsInDesiredSpace.center;
		rtCamera.orthographicSize = boundsInDesiredSpace.extents.y; 
		rtCamera.aspect = boundsInDesiredSpace.extents.x / boundsInDesiredSpace.extents.y;
		rtCamera.nearClipPlane = -boundsInDesiredSpace.extents.z;
		rtCamera.farClipPlane = boundsInDesiredSpace.extents.z + 1; // +1 to prevent depth of 0

		var origRT = RenderTexture.active;
		var origCamera = Camera.current;
		Camera.SetupCurrent(rtCamera);

		// Due to RenderMeshNow not working with MaterialPropertyBlocks, we have to render our mesh in-scene
		// Which means temporarily reparenting, overriding it's transform, setting a layer mask, etc.
		var origPos = spriteRenderer.transform.localPosition;
		var origRot = spriteRenderer.transform.localRotation;
		var origScale = spriteRenderer.transform.localScale;
		var origLayer = spriteRenderer.gameObject.layer;
		var origSpriteParent = spriteRenderer.transform.parent;

		// Temp Overrides
		spriteRenderer.transform.SetParent(null, false);
		spriteRenderer.gameObject.layer = RT_LAYER;
		setTransformFromMatrix(spriteRenderer.transform, spriteToDesiredMatrix); //ugh

		// Render this camera now
		GL.Clear(true, true, RT_COLOR); //force
		rtCamera.Render();

		// Restore 
		spriteRenderer.transform.SetParent(origSpriteParent, false);
		spriteRenderer.transform.localPosition = origPos;
		spriteRenderer.transform.localRotation = origRot;
		spriteRenderer.transform.localScale = origScale;
		spriteRenderer.gameObject.layer = origLayer;

		// Get the pixeldata
		var pixels = getPixelsFromRenderTarget();

		// restore original rendertarget
		RenderTexture.active = origRT;

		// Diagnostic drawing of our tempRenderTarget (only to Scene view)
		if (origCamera != null && origCamera.cameraType == CameraType.SceneView &&
		    Event.current != null && Event.current.type == EventType.Repaint)
		{
			diagnosticBlitToScreen(rtCamera.targetTexture);
		}

		// Provide an AsciiArt version if the caller asked for it (for logging purposes)
		//asciiArt = getAsciiArtFromColorArray(pixels, RT_WIDTH, RT_HEIGHT);

		// Must restore scene cameras during GUI updates
		if (origCamera != null && origCamera.cameraType == CameraType.SceneView && Event.current != null)
		{
			Camera.SetupCurrent(origCamera);
		}

		// Constrain geometric bounds to the top and bottom most visible pixels 
		var constrainedBounds = constrainBoundsToVerticalVisiblePixels(boundsInDesiredSpace, pixels, RT_WIDTH, RT_HEIGHT);

		return constrainedBounds;
	}

	//............. helper utility functions below (private for now) ..............

	// Sets a transforms local position/rotation/scale by extracting those fields from a matrix
	// (Warning - cannot perfectly extract from skewed transforms)
	private static void setTransformFromMatrix(Transform transform, Matrix4x4 matrix)
	{
		transform.position = extractPositionFromMatrix(matrix);
		transform.rotation = extractRotationFromMatrix(matrix);
		transform.localScale = extractScaleFromMatrix(matrix);
	}

	private static Vector3 extractPositionFromMatrix(Matrix4x4 matrix)
	{
		Vector3 position = new Vector3(matrix.m03, matrix.m13, matrix.m23);
		return position;
	}

	private static Quaternion extractRotationFromMatrix(Matrix4x4 matrix)
	{
		Vector3 forward = new Vector3(matrix.m02, matrix.m12, matrix.m22);
		Vector3 upwards = new Vector3(matrix.m01, matrix.m11, matrix.m21);
		return Quaternion.LookRotation(forward, upwards);
	}

	private static Vector3 extractScaleFromMatrix(Matrix4x4 matrix)
	{
		Vector3 scale;
		scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
		scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
		scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
		return scale;
	}

	// Returns a (reusable) singleton camera for the purposes of rendertarget based bounds detection
	private static Camera getBoundsRenderTargetCamera()
	{
		if (sRenderTargetCamera == null || sResolveTexture == null)
		{
			Debug.Log("Creating a BoundsRenderTargetCamera & texture (should only happen once)");

			// Destroy any previous renderTarget camera object, component, and resolve texture (if they exist)
			// This generally shouldn't be needed in game, but keeps things robust, especially in editor
			GameObject cameraObject = GameObject.Find(RT_CAM_NAME);
			Object.DestroyImmediate(sRenderTargetCamera);
			Object.DestroyImmediate(cameraObject);
			Object.DestroyImmediate(sResolveTexture);
			sRenderTargetCamera = null;
			cameraObject = null;
			sResolveTexture = null; 

			// Create a new camera
			cameraObject = new GameObject(RT_CAM_NAME);
			sRenderTargetCamera = cameraObject.AddComponent<Camera>();
			sRenderTargetCamera.enabled = false; // don't auto-render, we'll manually use it
			sRenderTargetCamera.orthographic = true;
			sRenderTargetCamera.useOcclusionCulling = false;
			sRenderTargetCamera.cullingMask = 1 << RT_LAYER;
			sRenderTargetCamera.backgroundColor = RT_COLOR;
			sRenderTargetCamera.clearFlags = CameraClearFlags.SolidColor;
			sRenderTargetCamera.targetTexture = new RenderTexture(RT_WIDTH, RT_HEIGHT, 16, RenderTextureFormat.ARGB32);

			// Create a matching texture to resolve to (separate from the rendertarget texture)
			sResolveTexture = new Texture2D(RT_WIDTH, RT_HEIGHT, TextureFormat.ARGB32, false);

			// Preserve these objects across scene reloads
			if (Application.isPlaying)
			{
				Object.DontDestroyOnLoad(cameraObject); 
				Object.DontDestroyOnLoad(sResolveTexture);
			}
		}

		return sRenderTargetCamera;
	}

	private static Camera sRenderTargetCamera;  // singleton Camera
	private static Texture2D sResolveTexture;   // matching texture to resolve to


	// Returns the raw Color32 pixeldata from the (still active) BoundsRenderTargetCamera rendertarget
	private static Color32[] getPixelsFromRenderTarget()
	{
		Debug.Assert(RenderTexture.active != null, "No active RT!");

		// Must read from the active RenderTarget into a Texture2d, then get pixels from that
		sResolveTexture.ReadPixels(new Rect(0, 0, RT_WIDTH, RT_HEIGHT), 0, 0, false);
		Color32[] pixels = sResolveTexture.GetPixels32();
		return pixels;
	}


	// Constrains a BoundingBox (in an arbitrary space) to the top and bottom "visible" pixels of a 2D pixel array
	// This is used to tighten bounding boxes to a bitmap by ignoring black/transparent border areas.
	// If it can't detect any visible pixels, will return  a bounding box with a width/height/depth of 0
	private static Bounds constrainBoundsToVerticalVisiblePixels(Bounds bounds, Color32[] pixels, int width, int height)
	{
		int topVisibleRow = findTopVisibleRow(pixels, width, height);
		int bottomVisibleRow = findBottomVisibleRow(pixels, width, height);

		if (topVisibleRow < 0 || bottomVisibleRow < 0)
		{
			// no visible pixels? return a minimal bounds... 
			return new Bounds(bounds.center, Vector3.zero);
		}

		// Interpolators are inclusive of top & bottom most pixels
		float tBot = (float)(bottomVisibleRow ) / height; // [0.0, 1.0)
		float tTop = (float)(topVisibleRow + 1) / height; // (0.0, 1.0]

		// Scale caller-provided bounds top & bottom
		return new Bounds 
		{
			min = new Vector3( bounds.min.x, Mathf.Lerp( bounds.min.y, bounds.max.y, tBot), bounds.min.z),
			max = new Vector3( bounds.max.x, Mathf.Lerp( bounds.min.y, bounds.max.y, tTop), bounds.max.z)
		};
	}

	// Returns the bottommost "visible" row found in the 2D pixel array, or -1 if no visible pixels found
	// (Row # is in pixel space coords: 0 at the bottom)
	private static int findBottomVisibleRow(Color32[] pixels, int width, int height)
	{
		int totalPixels = width * height;

		// search bottom-up (in memory order) for first visible pixel 
		int index = 0;
		while (index < totalPixels && !isPixelSignificant(pixels[index]))
		{
			index++;
		}

		if (index >= totalPixels)
		{
			return -1; // No visible pixels detected
		}

		// Coords from index:  x = index % width,  y = index / width
		int bottomPixelY = index / width;

		return bottomPixelY;
	}

	// Returns the topmost "visible" row found in the 2D pixel array, or -1 if no visible pixels found
	// (Row # is in pixel space coords: 0 at the bottom)
	private static int findTopVisibleRow(Color32[] pixels, int width, int height)
	{
		int totalPixels = width * height;

		// search top-down (in reverse-memory order) for first visible pixel

		int index = totalPixels-1;
		while (index >= 0 && !isPixelSignificant(pixels[index]))
		{
			index--;
		}

		if (index <= 0)
		{
			return -1; // No visible pixels detected
		}

		// Coords from index:  x = index % width,  y = index / width
		int topPixelY = index / width;

		return topPixelY;
	}


	// pre-calculate min/max deviations from the clear color for pixel testing (clamped to [0,255] when converted to Color32)
	private static Color32 MIN_DEVIATION = (Color32)(RT_COLOR - new Color(RT_DEVIATION, RT_DEVIATION, RT_DEVIATION, RT_DEVIATION));
	private static Color32 MAX_DEVIATION = (Color32)(RT_COLOR + new Color(RT_DEVIATION, RT_DEVIATION, RT_DEVIATION, RT_DEVIATION));

	// Decides if a pixel is 'visibly significant' for purposes of rendered bounds determination
	private static bool isPixelSignificant(Color32 pixel)
	{
		// A rendered pixel is considered 'significant' if it's alphavalue is over a minimum threshold
		// Some shaders don't write destination alpha, so we also check for RGB deviation from the clear color

		// A color component must deviate from the clear color by a certain amount to be considered significant
		// The MIN_DEVIATION and MAX_DEVIATION color components have been pre-calculated above
		bool isSignificant =
			pixel.a < MIN_DEVIATION.a || pixel.a > MAX_DEVIATION.a ||
			pixel.r < MIN_DEVIATION.r || pixel.r > MAX_DEVIATION.r ||
			pixel.g < MIN_DEVIATION.g || pixel.g > MAX_DEVIATION.g ||
			pixel.b < MIN_DEVIATION.b || pixel.b > MAX_DEVIATION.b;

		return isSignificant;
	}


	// Creates a mesh from sprite geometry (could be a rectangle or a tightly fit mesh, both work)
	private static Mesh createMeshFromSprite(Sprite sprite)
	{
		// convert sprite's vertices from Vector2s to Vector3s (needed by Mesh)
		Vector2[] spriteVerts2d = sprite.vertices;
		Vector3[] spriteVerts3d = new Vector3[ spriteVerts2d.Length ];
		for (int i = 0; i < spriteVerts2d.Length; i++)
		{
			spriteVerts3d[i] = new Vector3(spriteVerts2d[i].x, spriteVerts2d[i].y, 0.0f);
		}

		// convert sprite indices from ushorts to ints (needed by Mesh)
		ushort[] triShortIndices = sprite.triangles;
		int[] triIntIndices = new int[ triShortIndices.Length ];
		for (int i = 0; i < triShortIndices.Length; i++)
		{
			triIntIndices[i] = (int)(triShortIndices[i]);
		}

		var newMesh = new Mesh();
		newMesh.name = "tempMesh from " + sprite.name;
		newMesh.vertices = spriteVerts3d;
		newMesh.uv = sprite.uv;
		newMesh.triangles = triIntIndices; //recalc's bounds

		return newMesh;
	}


	// Diagnostic function that returns a string of basic info (positions & uv's) about a mesh
	private static string getMeshInfoAsString(Mesh mesh)
	{
		string str = "MeshInfo for: " + mesh.name + "  (" + mesh.subMeshCount + " submeshes) : \n";

		var uvs = mesh.uv;
		var verts = mesh.vertices;
		for(int i=0; i < verts.Length; i++)
		{
			str += "  Pos = " + verts[i] + "   UV = " + uvs[i] + "\n";
		}

		return str;
	}


	// Diagnostic function to draw a texture onto the bottom right of screen
	private static void diagnosticBlitToScreen(Texture tex)
	{
		if (sBlitMaterial == null)
		{
			sBlitMaterial = new Material(ShaderCache.find("Hidden/BlitCopy"));
		}

		GL.PushMatrix();
		GL.LoadIdentity();
		GL.LoadProjectionMatrix(Matrix4x4.Ortho(-2, 2, +2, -2, -10, 10)); // invert Y so texture is rightside up
		Graphics.DrawTexture(new Rect(1,1,1,1), tex, sBlitMaterial);
		GL.PopMatrix();
	}

	// Material used for diagnostic blitting, uses a "BlitCopy" shader that copies alpha
	private static Material sBlitMaterial; 


	// Diagnostic function to convert a 2D pixel array into ascii-art we can print to console
	private static string getAsciiArtFromColorArray(Color32[] pixels, int width, int height)
	{
		var sb = new System.Text.StringBuilder(width * height);
		for (int y = 0; y < height; y++)
		{
			int index = (height - 1 - y) * width; //pixels arranged from bottom to top 
			for (int x = 0; x < width; x++)
			{
				// Is this pixel visibly significant?
				bool isSignificant = isPixelSignificant(pixels[index]);

				sb.Append( isSignificant ? '#' : '.' );
				index++;
			}
			sb.Append( '\n' );
		}
		return sb.ToString();
	}

}
