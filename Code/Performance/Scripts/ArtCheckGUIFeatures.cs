using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if !UNITY_EDITOR

// Dummy script for stripping this out of device builds
public class ArtCheckGUIFeatures : MonoBehaviour
{

}

#else

public class ArtCheckGUIFeatures : MonoBehaviour
{
	private const int WINDOW_ID = 777;
	
	private const int TOO_MUCH_VERTEX = 32 * 1024;
	private const int TOO_MUCH_MESH = 256;
	private const int TOO_MUCH_ANIMATION = 256 * 1024;
	private const int TOO_MUCH_PARTICLE = 512;
	private const int TOO_MUCH_COLLIDER = 64;
	private const int TOO_MANY_CAMERAS = 0;

	private const int MAX_TEXTURE_DIMENSION = 1024;
	
#if UNITY_IPHONE
	private const int TOO_MUCH_MEMORY = 6 * 1024 * 1024;
#elif UNITY_ANDROID
	private const int TOO_MUCH_MEMORY = 12 * 1024 * 1024;
#else
	private const int TOO_MUCH_MEMORY = 2 * 1024 * 1024;
#endif

	//Rough MB estimates based on existing features.
	private const int SMALL_SIZE = 2;
	private const int MEDIUM_SIZE = 4;
	private const int LARGE_SIZE = 8;
	private const int EXTRA_LARGE_SIZE = 12;
	
	/// This is a lookup of shader properties that have textures, please add to this as necessary.
	private string[] TEXTURE_PROPERTIES =
	{
		"_MainTex",
		"_BumpMap",
		"_SpecMap",
		"_BlendTex",
		"_ReflectTex",
		"_RefractTex",
		"_Details",
		"_ScrollingTex",
		"_Mask"
	};
	
	/// This is a lookup of texture names that should be ignored, please add to this as necessary.
	private Dictionary<string, bool> IGNORE_TEXTURES
	{
		get
		{
			if (_IGNORE_TEXTURES == null)
			{
				_IGNORE_TEXTURES = new Dictionary<string, bool>();
				_IGNORE_TEXTURES.Add("Generic Hi", true);
				_IGNORE_TEXTURES.Add("Generic Low", true);
				_IGNORE_TEXTURES.Add("Generic Uncompressed Hi", true);
				_IGNORE_TEXTURES.Add("Generic Uncompressed Low", true);
				_IGNORE_TEXTURES.Add("Dialog Hi", true);
				_IGNORE_TEXTURES.Add("Dialog Low", true);
				_IGNORE_TEXTURES.Add("Fonts 1 Hi", true);
				_IGNORE_TEXTURES.Add("Fonts 2 Hi", true);
				_IGNORE_TEXTURES.Add("Fonts 1 Low", true);
				_IGNORE_TEXTURES.Add("Fonts 2 Low", true);
				_IGNORE_TEXTURES.Add("Dialog Hi (RGB)", true);
				_IGNORE_TEXTURES.Add("Generic Hi (RGB)", true);
				_IGNORE_TEXTURES.Add("monofonto numbers SDF Atlas", true);
				_IGNORE_TEXTURES.Add("OpenSans-Bold SDF Atlas", true);

			}
			return _IGNORE_TEXTURES;
		}
	}

	private List<string> IGNORED_FONTS = new List<string>
	{
		"OpenSans-Bold SDF (TMPro.TMP_FontAsset)",
		"monofonto numbers SDF (TMPro.TMP_FontAsset)",
		"ARIALExtraFonts SDF (TMPro.TMP_FontAsset)",
		"OpenSans-Bold SDF Outline (TMPro.TMP_FontAsset)",
		"PollerOne SDF (TMPro.TMP_FontAsset)",
		"Teko-Bold SDF (TMPro.TMP_FontAsset)"
	};
	private Dictionary<string, bool> _IGNORE_TEXTURES = null;
	
	private static Rect windowSmallRect = new Rect(20, 20, 260, 80);
	private static Rect windowBigRect = new Rect(20, 20, 600, 600);
	private static Rect dragRect = new Rect(0, 0, 10000, 10000);
	private static Vector2 scrollPosition = Vector2.zero;
	private static ParticleSystem.Particle[] particleStash = new ParticleSystem.Particle[10000];
	
	public bool showDetails = false;
	public float totalMemorySize = 0.0f;
	public string currentRating = "";
	public int currentWarnings = 0;

	public int maxVertexCount = 0;
	public long maxMeshMemory = 0;
	public int maxMeshCount = 0;
	public int maxSkinnedMeshCount = 0;
	public int maxSkinnedVertexCount = 0;
	public int maxBoneCount = 0;
	public int maxClassicParticleEmitterCount = 0;
	public int maxClassicParticleCount = 0;
	public int maxParticleSystemCount = 0;
	public int maxParticleCount = 0;
	public int maxColliderCount = 0;
	public int maxMeshColliderCount = 0;
	public int maxMaterialCount = 0;
	public int maxTextureCount = 0;
	public int maxRenderTextureCount = 0;
	public long maxTextureMemory = 0;
	public int maxAnimationCount = 0;
	public long maxAnimationMemory = 0;
	public int maxUIPanelCount = 0;
	public int maxUIAnchorCount = 0;
	public int maxCameraCount = 0;
	public int maxAnimatorCount = 0;
	public int maxAnimationClipCount = 0;

	public List<UIAnchor> uiAnchorsToCheck = new List<UIAnchor>();
	public List<GameObject> extraneousObjects = new List<GameObject>();
	public List<ButtonHandler> buttonHandlerToCheck = new List<ButtonHandler>();
	public List<ImageButtonHandler> imageButtonHandlerToCheck = new List<ImageButtonHandler>();
	public List<GameObject> textsUsingUnknownFonts = new List<GameObject>();
	public List<GameObject> nonOrthographicTexts = new List<GameObject>();
	public List<GameObject> inCorrectMarginsTexts = new List<GameObject>();
	private Dictionary<string, List<GameObject>> textToGameObjects = new Dictionary<string, List<GameObject>>();

	private string textureMemoryLog = "";

	private bool ranOnceAlready = false;

	void Start()
	{
		// Make sure quality settings are what they should be for testing art
		QualitySettings.pixelLightCount = 2;
		QualitySettings.shadowProjection = ShadowProjection.CloseFit;
		QualitySettings.shadowCascades = 0;
		QualitySettings.shadowDistance = 0f;
		QualitySettings.skinWeights = SkinWeights.FourBones;
		QualitySettings.antiAliasing = 0;
		QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
		QualitySettings.masterTextureLimit = 0;
		
#if !UNITY_WEBGL
		QualitySettings.maxQueuedFrames = -1;
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;
#endif
		
		resetSample();
	}
	
	void Update()
	{		
		updateSample();
	}
	
	void OnGUI()
	{
		if (showDetails)
		{
			// Show the big version of the window
			windowBigRect = GUI.Window(WINDOW_ID, windowBigRect, drawWindow, "Art Check Panel");
			windowSmallRect.x = windowBigRect.x;
			windowSmallRect.y = windowBigRect.y;
		}
		else
		{
			// Show the small version of the window
			windowSmallRect = GUI.Window(WINDOW_ID, windowSmallRect, drawWindow, "Art Check Panel");
			windowBigRect.x = windowSmallRect.x;
			windowBigRect.y = windowSmallRect.y;
		}
	}
	
	/// Draws the art check gui window
	private void drawWindow(int id)
	{
		GUILayout.BeginVertical();
		
		string ratingText = string.Format("Current Memory Usage: {0} ({1})", totalMemorySize, currentRating);
		if  (currentWarnings > 0)
		{
			ratingText += string.Format(" [{0} warnings]", currentWarnings);
		}
		switch(currentRating)
		{
			case("SMALL"):
				GUI.contentColor = Color.green;
				break;
			case("MEDIUM"):
				GUI.contentColor = Color.yellow;
				break;	
			case("LARGE"):
				GUI.contentColor = new Color(1.0f, 0.65f, 0.0f, 1.0f); //Orange
				break;
			default:
				GUI.contentColor = Color.red;
				break;
		}
		GUILayout.Label(ratingText);

		GUI.contentColor = Color.white;

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Toggle Details"))
		{
			showDetails = !showDetails;
		}
		if (GUILayout.Button("Reset Sample"))
		{
			resetSample();
		}
		GUILayout.EndHorizontal();
		
		if (showDetails)
		{
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);
			GUILayout.BeginVertical();

			GUILayout.TextArea(getDetails());
			
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}
		
		GUILayout.EndVertical();
		
		GUI.DragWindow(dragRect);
	}
	
	/// Gets a details string to display in the big window
	private string getDetails()
	{
		string txt = "";

		txt += string.Format("Runtime Version: '{0}' \n", Application.unityVersion);
		txt += SystemInfo.operatingSystem;
		txt += "\n" + SystemInfo.processorType + " (x" + SystemInfo.processorCount + ") " + SystemInfo.systemMemorySize + "MB";
		txt += "\n" + SystemInfo.graphicsDeviceVendor + " " + SystemInfo.graphicsDeviceName + " " + SystemInfo.graphicsMemorySize + "MB (shader level " + SystemInfo.graphicsShaderLevel + ")";
		txt += "\nScreen resolution: " + Screen.width + "x" + Screen.height;
		txt += "\n";
		txt += "\n" + string.Format("Mesh memory: {0:0.0} MB", (float)maxMeshMemory / (1024f * 1024f));
		txt += "\n" + string.Format("Animation memory: {0:0.0} MB", (float)maxAnimationMemory / (1024f * 1024f));
		txt += "\n" + string.Format("Texture memory: {0:0.0} MB", (float)maxTextureMemory / (1024f * 1024f));
		txt += "\n";
		txt += "\n" + string.Format("Total vertex count: {0}", maxVertexCount);
		txt += "\n" + string.Format("Static mesh count: {0}", maxMeshCount);
		txt += "\n" + string.Format("Skinned mesh count: {0}", maxSkinnedMeshCount);
		txt += "\n" + string.Format("Skinned vertex count: {0}", maxSkinnedVertexCount);
		txt += "\n";
		txt += "\n" + string.Format("Animation count: {0}", maxAnimationCount);
		txt += "\n" + string.Format("Material count: {0}", maxMaterialCount);
		txt += "\n" + string.Format("Texture count: {0}", maxTextureCount);
		txt += "\n" + string.Format("Bone count: {0}", maxBoneCount);
		txt += "\n" + string.Format("Collider count: {0}", maxColliderCount);
		txt += "\n";
		txt += "\n" + string.Format("Classic particle systems: {0}", maxClassicParticleEmitterCount);
		txt += "\n" + string.Format("Classic particle count: {0}", maxClassicParticleCount);
		txt += "\n" + string.Format("Shuriken particle systems: {0}", maxParticleSystemCount);
		txt += "\n" + string.Format("Shuriken particle count: {0}", maxParticleCount);
		txt += "\n";
		txt += "\n" + string.Format("Total draw calls: {0}", UnityEditor.UnityStats.batches);
		txt += "\n";
		txt += "\n" + string.Format("UI Panel Count: {0}", maxUIPanelCount);
		txt += "\n" + string.Format("UI Anchor Count: {0}", maxUIAnchorCount);
		txt += "\n";
		txt += "\n" + string.Format("Animators Count: {0}", maxAnimatorCount);
		txt += "\n" + string.Format("Animation Clips Count: {0}", maxAnimationClipCount);
		txt += "\n";

		txt += "\nUIAnchors with settings that need to be checked: ";
		foreach(UIAnchor anchor in uiAnchorsToCheck)
		{
			txt += "\n" + anchor;
		}

		txt += "\n";
		if (maxCameraCount > TOO_MANY_CAMERAS)
		{
			txt += "\n" + string.Format("WARNING Camera Count {0}: Be mindful of using extra cameras only when necessary.", maxCameraCount);
			txt += "\n";
		}

		if (extraneousObjects.Count > 0)
		{
			txt += "\nExtraneous Objects that can be deleted or consolidated with children: ";
			foreach(GameObject obj in extraneousObjects)
			{
				txt += "\n" + obj.name;
			}
			txt += "\n";
		}

		if (textsUsingUnknownFonts.Count > 0)
		{
			txt += "\nText Objects are using unknown fonts";
			foreach(GameObject obj in textsUsingUnknownFonts)
			{
				txt += "\n" + obj.name;
			}
			txt += "\n";
		}

		if (inCorrectMarginsTexts.Count > 0)
		{
			txt += "\nText Objects have non-Zero margins";
			foreach(GameObject obj in inCorrectMarginsTexts)
			{
				txt += "\n" + obj.name;
			}
			txt += "\n";
		}

		if (nonOrthographicTexts.Count > 0)
		{
			txt += "\nText Objects are not setup for Orthographic cameras. These might display incorrectly";
			foreach(GameObject obj in nonOrthographicTexts)
			{
				txt += "\n" + obj.name;
			}
			txt += "\n";
		}

		foreach(List<GameObject> textObjects in textToGameObjects.Values)
		{
			if (textObjects.Count > 1)
			{
				txt += "\n" + "These objects have duplicate texts. Consolidate if possible";
				foreach(GameObject textObject in textObjects)
				{
					txt += "\n" + textObject.name;
				}
				txt += "\n";
			}
		}

		
		if (maxVertexCount >= TOO_MUCH_VERTEX)
		{
			txt += "\n" + "WARNING: Too many vertices.";
		}
		if (maxMeshCount + (maxSkinnedMeshCount * 4) >= TOO_MUCH_MESH)
		{
			txt += "\n" + "WARNING: Too many meshes.";
		}
		if (maxBoneCount * maxSkinnedVertexCount >= TOO_MUCH_ANIMATION)
		{
			txt += "\n" + "WARNING: Bone animation is too complex.";
		}
		if (maxClassicParticleCount + (maxParticleCount * 2) >= TOO_MUCH_PARTICLE)
		{
			txt += "\n" + "WARNING: Too many particles.";
		}
		if (maxColliderCount >= TOO_MUCH_COLLIDER)
		{
			txt += "\n" + "WARNING: Lots of colliders, check that this is correct.";
		}

	/* Commenting this out for now since we don't have a max size we're going with on Features but are using T-Shirt size costing instead
		if (maxAnimationMemory + maxTextureMemory + maxMeshMemory > TOO_MUCH_MEMORY)
		{
			txt += "\n" + "WARNING: Too much memory use.";
		}
	*/
		if (maxRenderTextureCount > 0)
		{
			txt += "\n" + "WARNING: Use of restricted item RenderTexture detected!";
		}
		if (maxMeshColliderCount > 0)
		{
			txt += "\n" + "WARNING: Use of restricted item MeshCollider detected!";
		}
		
		txt += "\n\n" + textureMemoryLog;

		return txt;
	}
	
	/// Resets the profiling sample values to a clean slate
	private void resetSample()
	{
		maxVertexCount = 0;
		maxMeshMemory = 0;
		maxMeshCount = 0;
		maxSkinnedMeshCount = 0;
		maxSkinnedVertexCount = 0;
		maxBoneCount = 0;
		maxClassicParticleEmitterCount = 0;
		maxClassicParticleCount = 0;
		maxParticleSystemCount = 0;
		maxParticleCount = 0;
		maxColliderCount = 0;
		maxMeshColliderCount = 0;
		maxMaterialCount = 0;
		maxTextureCount = 0;
		maxRenderTextureCount = 0;
		maxTextureMemory = 0;
		maxAnimationCount = 0;
		maxAnimationMemory = 0;
		maxUIAnchorCount = 0;
		maxUIPanelCount = 0;
		maxAnimatorCount = 0;
		maxAnimationClipCount = 0;
		uiAnchorsToCheck.Clear();
		extraneousObjects.Clear();
		buttonHandlerToCheck.Clear();
		imageButtonHandlerToCheck.Clear();
		textToGameObjects.Clear();
		inCorrectMarginsTexts.Clear();
		nonOrthographicTexts.Clear();
		textsUsingUnknownFonts.Clear();
		ranOnceAlready = false;
	}
	
	/// Updates the sample by brute force collecting and tallying different items of memory note.
	/// This is purposely brute force in order to have clear logic and because we want to encourage
	/// the art this is testing
	private void updateSample()
	{
		textureMemoryLog = "Textures:";
		
		int vertexCount = 0;
		long meshMemory = 0;
		int meshCount = 0;
		int skinnedMeshCount = 0;
		int skinnedVertexCount = 0;
		int boneCount = 0;
		int classicParticleEmitterCount = 0;
		int classicParticleCount = 0;
		int particleSystemCount = 0;
		int particleCount = 0;
		int colliderCount = 0;
		int meshColliderCount = 0;
		int materialCount = 0;
		int textureCount = 0;
		int renderTextureCount = 0;
		long textureMemory = 0;
		int animationCount = 0;
		long animationMemory = 0;
		int uiPanelCount = 0;
		int uiAnchorCount = 0;
		int cameraCount = 0;
		int animationClipCount = 0;

		
		Dictionary<Mesh, bool> meshUses = new Dictionary<Mesh, bool>();
		Dictionary<Texture, bool> textureUses = new Dictionary<Texture, bool>();
		Dictionary<AnimationClip, bool> animationUses = new Dictionary<AnimationClip, bool>();

		List<GameObject> gameObjects = new List<GameObject>();
		List<MeshFilter> meshFilters = new List<MeshFilter>();
		List<SkinnedMeshRenderer> skinnedMeshes = new List<SkinnedMeshRenderer>();
		List<Renderer> renderers = new List<Renderer>();
		List<UIPanel> uiPanels = new List<UIPanel>();
		List<UITexture> uiTextures = new List<UITexture>();
		List<Animation> animations = new List<Animation>();
		List<ParticleSystem> particleSystems = new List<ParticleSystem>();
		List<Collider> colliders = new List<Collider>();
		List<UIAnchor> uiAnchors = new List<UIAnchor>();
		List<Camera> cameras = new List<Camera>();
		List<UISprite> sprites = new List<UISprite>();
		List<Animator> animators = new List<Animator>();
		List<ButtonHandler> buttonHandlers = new List<ButtonHandler>();
		List<ImageButtonHandler> imageButtonHandlers = new List <ImageButtonHandler>();
		List<TextMeshPro> tmProTexts = new List<TextMeshPro>();
		
		// Modern way of retrieving root objects
		GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

		// Gather up items to profile
		foreach (GameObject rootObject in rootObjects)
		{
			foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
			{
				// Look at the parent of each of these transforms to see if it should be added:
				Transform parent = child.parent;
				bool skip = false;

				if (child.gameObject.GetComponent<IgnoreDuringArtCheck>() != null)
				{
					skip = true;
					parent = null;
				}

				while (parent != null)
				{
					if (parent.GetComponent<IgnoreDuringArtCheck>() != null && parent.GetComponent<IgnoreDuringArtCheck>().skipChildren)
					{
						skip = true;
						break;
					}
					parent = parent.parent;
				}
				if (!skip)
				{
					gameObjects.Add(child.gameObject);
					if (!ranOnceAlready)
					{
						Component[] comps = child.gameObject.GetComponents<Component>(); //Components in current game object being added

						//We might not need this object in the prefab if we only have 1 component(Transform) && the object isn't a sizer or mover
						if (comps.Length <= 1 && 
							(!child.gameObject.name.Contains("sizer") || !child.gameObject.name.Contains("Sizer") ||
							!child.gameObject.name.Contains("mover") || !child.gameObject.name.Contains("Mover")))
						{
							if (child.gameObject.GetComponentsInChildren<Transform>(true).Length <= 2) //Has 1 or 0 children and children don't have more children
							{
								if (child.localScale == Vector3.one)
								{
									child.gameObject.name += " - POSSIBLE EXTRA OBJECT"; //Appeding this just in case there are objects in the prefab with the same name
									extraneousObjects.Add(child.gameObject);
								}
								else if (child.childCount == 0)
								{
									child.gameObject.name += " - POSSIBLE EXTRA OBJECT"; //Appending this just in case there are objects in the prefab with the same name
									extraneousObjects.Add(child.gameObject);
								}	
							}
						}
					}
				}
			}
		}

		foreach (GameObject go in gameObjects)
		{
			meshFilters.AddRange(go.GetComponents<MeshFilter>());
			skinnedMeshes.AddRange(go.GetComponents<SkinnedMeshRenderer>());
			renderers.AddRange(go.GetComponents<Renderer>());
			uiPanels.AddRange(go.GetComponents<UIPanel>());
			uiTextures.AddRange(go.GetComponents<UITexture>());
			animations.AddRange(go.GetComponents<Animation>());
			particleSystems.AddRange(go.GetComponents<ParticleSystem>());
			colliders.AddRange(go.GetComponents<Collider>());
			uiAnchors.AddRange(go.GetComponents<UIAnchor>());
			cameras.AddRange(go.GetComponents<Camera>());
			sprites.AddRange(go.GetComponents<UISprite>());
			animators.AddRange(go.GetComponents<Animator>());
			buttonHandlers.AddRange(go.GetComponents<ButtonHandler>());
			imageButtonHandlers.AddRange(go.GetComponents<ImageButtonHandler>());
			tmProTexts.AddRange(go.GetComponents<TextMeshPro>());
		}
		
		foreach (MeshFilter meshFilter in meshFilters)
		{
			if (meshFilter.gameObject != null)
			{
				meshCount += 1;
				
				if (meshFilter.sharedMesh != null && !meshUses.ContainsKey(meshFilter.sharedMesh))
				{
					vertexCount += meshFilter.sharedMesh.vertexCount;
					meshMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(meshFilter.sharedMesh);
					meshUses.Add(meshFilter.sharedMesh, true);
				}
				else if (meshFilter.mesh != null && !meshUses.ContainsKey(meshFilter.mesh))
				{
					vertexCount += meshFilter.mesh.vertexCount;
					meshMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(meshFilter.mesh);
					meshUses.Add(meshFilter.mesh, true);
				}
			}
		}
	
		foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
		{
			if (skinnedMesh.gameObject != null)
			{
				skinnedMeshCount += 1;
				boneCount += skinnedMesh.bones.Length;
				
				if (skinnedMesh.sharedMesh != null && !meshUses.ContainsKey(skinnedMesh.sharedMesh))
				{
					vertexCount += skinnedMesh.sharedMesh.vertexCount;
					skinnedVertexCount += skinnedMesh.sharedMesh.vertexCount;
					meshMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(skinnedMesh.sharedMesh);
					meshUses.Add(skinnedMesh.sharedMesh, true);
				}
			}
		}
		
		foreach (Renderer renderer in renderers)
		{
			foreach (Material material in renderer.sharedMaterials ?? renderer.materials)
			{
				if (material != null)
				{
					foreach (string textureProperty in TEXTURE_PROPERTIES)
					{
						if (material.HasProperty(textureProperty))
						{
							Texture texture = material.GetTexture(textureProperty) as Texture;
							if (texture != null && !textureUses.ContainsKey(texture) && !IGNORE_TEXTURES.ContainsKey(texture.name))
							{
								textureCount += 1;
								textureMemory += ArtCheckGUI.getTextureMemory(texture, ref textureMemoryLog, MAX_TEXTURE_DIMENSION);
								textureUses.Add(texture, true);
								
								if (texture is RenderTexture)
								{
									renderTextureCount += 1;
								}
							}
						}
					}
				}
			}
		}
		
		foreach (UIPanel uiPanel in uiPanels)
		{
			foreach (UIDrawCall drawCall in uiPanel.drawCalls)
			{			
				if (drawCall.material != null)
				{
					foreach (string textureProperty in TEXTURE_PROPERTIES)
					{
						if (drawCall.material.HasProperty(textureProperty))
						{
							Texture texture = drawCall.material.GetTexture(textureProperty) as Texture;
							
							// Since this is a tool for artists, purposely ignore the memory used by the generic atlas to avoid confusion on their end
							if (texture != null && !textureUses.ContainsKey(texture) && !IGNORE_TEXTURES.ContainsKey(texture.name))
							{
								textureCount += 1;
								textureMemory += ArtCheckGUI.getTextureMemory(texture, ref textureMemoryLog, MAX_TEXTURE_DIMENSION);
								textureUses.Add(texture, true);
								
								if (texture is RenderTexture)
								{
									renderTextureCount += 1;
								}
							}
						}
					}
				}
			}
		}
		uiPanelCount = uiPanels.Count;
		
		foreach (UITexture uiTexture in uiTextures)
		{
			Texture texture = uiTexture.mainTexture;
							
			// Since this is a tool for artists, purposely ignore the memory used by the generic atlas to avoid confusion on their end
			if (texture != null && !textureUses.ContainsKey(texture) && !IGNORE_TEXTURES.ContainsKey(texture.name))
			{
				textureCount += 1;
				textureMemory += ArtCheckGUI.getTextureMemory(texture, ref textureMemoryLog, MAX_TEXTURE_DIMENSION);
				textureUses.Add(texture, true);
				
				if (texture is RenderTexture)
				{
					renderTextureCount += 1;
				}
			}
		}
		foreach (Animation animation in animations)
		{
			if (animation.gameObject != null)
			{
				foreach (AnimationState animationState in animation)
				{
					if (animationState != null && animationState.clip != null)
					{
						if (!animationUses.ContainsKey(animationState.clip))
						{
							animationCount += 1;
							animationUses.Add(animationState.clip, true);
						}
					
						animationMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(animationState.clip);
					}
				}
			}
		}
	
		int instanceParticleCount;
		foreach (ParticleSystem particleSystem in particleSystems)
		{
			if (particleSystem.gameObject != null)
			{
				instanceParticleCount = particleSystem.GetParticles(particleStash);
				particleSystemCount += 1;
				particleCount += instanceParticleCount;
				vertexCount += instanceParticleCount * 4;
			}
		}

		foreach (Collider collider in colliders)
		{
			if (collider.gameObject != null)
			{
				colliderCount += 1;
				
				if (collider is MeshCollider)
				{
					meshColliderCount += 1;
				}
			}
		}

		if (!ranOnceAlready)
		{
			foreach(TextMeshPro text in tmProTexts)
			{
				//text.isOrthographic;
				//Check if the text is using a Font not already account for
				if (!IGNORED_FONTS.Contains(text.font.ToString()))
				{
					//Debug.Log(text.font);
					//Debug.Log(text.font.ToString());
					//Debug.LogWarning("TMPro Object is using an unknown font");
					textsUsingUnknownFonts.Add(text.gameObject);
				}

				if (!text.isOrthographic)
				{
					nonOrthographicTexts.Add(text.gameObject);
				}

				if (text.margin != Vector4.zero)
				{
					inCorrectMarginsTexts.Add(text.gameObject);
				}

				if(textToGameObjects.ContainsKey(text.text))
				{
					textToGameObjects[text.text].Add(text.gameObject);
				}
				else
				{
					textToGameObjects.Add(text.text, new List<GameObject>{text.gameObject});
				}
			}

			foreach(UIAnchor anchor in uiAnchors)
			{
				if (!anchor.isActiveAndEnabled)
				{
					anchor.gameObject.name += " ANCHOR CHECK";
					uiAnchorsToCheck.Add(anchor);
				}
			}

			foreach(Animator anim in animators)
			{
				AnimationClip[] clips = AnimationUtility.GetAnimationClips(anim.gameObject);
				animationMemory += checkForBrokenPropertiesAndGetMemoryUsage(anim.gameObject, clips);
				animationClipCount += clips.Length;
			}

			foreach(ButtonHandler handler in buttonHandlers)
			{
				if (handler.gameObject.GetComponent<UIButton>() == null)
				{
					buttonHandlerToCheck.Add(handler);
				}
			}

			foreach(ImageButtonHandler handler in imageButtonHandlers)
			{
				if (handler.gameObject.GetComponent<UIImageButton>() == null)
				{
					imageButtonHandlerToCheck.Add(handler);
				}
			}
		}

		uiAnchorCount = uiAnchors.Count;
		maxVertexCount = Mathf.Max(maxVertexCount, vertexCount);
		maxMeshMemory = (long)Mathf.Max(maxMeshMemory, meshMemory);
		maxMeshCount = Mathf.Max(maxMeshCount, meshCount);
		maxSkinnedMeshCount = Mathf.Max(maxSkinnedMeshCount, skinnedMeshCount);
		maxSkinnedVertexCount = Mathf.Max(maxSkinnedVertexCount, skinnedVertexCount);
		maxBoneCount = Mathf.Max(maxBoneCount, boneCount);
		maxClassicParticleEmitterCount = Mathf.Max(maxClassicParticleEmitterCount, classicParticleEmitterCount);
		maxClassicParticleCount = Mathf.Max(maxClassicParticleCount, classicParticleCount);
		maxParticleSystemCount = Mathf.Max(maxParticleSystemCount, particleSystemCount);
		maxParticleCount = Mathf.Max(maxParticleCount, particleCount);
		maxColliderCount = Mathf.Max(maxColliderCount, colliderCount);
		maxMeshColliderCount = Mathf.Max(maxMeshColliderCount, meshColliderCount);
		maxMaterialCount = Mathf.Max(maxMaterialCount, materialCount);
		maxTextureCount = Mathf.Max(maxTextureCount, textureCount);
		maxRenderTextureCount = Mathf.Max(maxRenderTextureCount, renderTextureCount);
		maxTextureMemory = (long)Mathf.Max(maxTextureMemory, textureMemory);
		maxAnimationCount = Mathf.Max(maxAnimationCount, animationCount);
		maxAnimationMemory = (long)Mathf.Max(maxAnimationMemory, animationMemory);
		maxUIPanelCount = Mathf.Max(maxUIPanelCount, uiPanelCount);
		maxUIAnchorCount = Mathf.Max(maxUIAnchorCount, uiAnchorCount);
		maxCameraCount = Mathf.Max(maxCameraCount, cameras.Count);
		maxAnimatorCount = Mathf.Max(maxAnimatorCount, animators.Count);
		maxAnimationClipCount = Mathf.Max(maxAnimationClipCount, animationClipCount);
		calculateScoring();
		ranOnceAlready = true;
	}
	
	/// Calculates the score and warnings
	private void calculateScoring()
	{
		totalMemorySize = (float)maxMeshMemory / (1024f * 1024f) + (float)maxAnimationMemory / (1024f * 1024f) + (float)maxTextureMemory / (1024f * 1024f);
		if (totalMemorySize < MEDIUM_SIZE)
		{
			currentRating = "SMALL";
		}
		else if(totalMemorySize >= MEDIUM_SIZE && totalMemorySize < LARGE_SIZE)
		{
			currentRating = "MEDIUM";
		}
		else if(totalMemorySize >= LARGE_SIZE && totalMemorySize < EXTRA_LARGE_SIZE)
		{
			currentRating = "LARGE";
		}
		else
		{
			currentRating = "X-LARGE";
		}
	}
	
	private long checkForBrokenPropertiesAndGetMemoryUsage(GameObject parent, AnimationClip[] clips)
	{
		//Gather up the children of the parent
		List<string> childrenPaths = new List<string>();
		long memoryusage = 0;
		foreach(Transform childT in parent.GetComponentsInChildren<Transform>(true))
		{
			if (childT.gameObject != parent)
			{
				string pathToObject = "";
				GameObject currentParent = childT.parent.gameObject;
				while (currentParent != parent)
				{
					pathToObject = pathToObject.Insert(0, currentParent.name + "/");
					currentParent = currentParent.transform.parent.gameObject;
				}
				pathToObject += childT.gameObject.name;
				childrenPaths.Add(pathToObject);
			}
		}

		foreach (AnimationClip clip in clips)
		{
			memoryusage += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(clip);
			EditorCurveBinding[] curves = AnimationUtility.GetObjectReferenceCurveBindings(clip);
			EditorCurveBinding[] curvez = AnimationUtility.GetCurveBindings(clip);
			foreach (EditorCurveBinding curve in curves)
			{
				if (!childrenPaths.Contains(curve.path))
				{
					//Debug.LogWarning(string.Format("OBJECT {0} NOT FOUND IN CLIP {1} ON ANIMATOR {2}. PATH TO OBJECT NEEDS TO BE UPDATED", curve.path, clip.name, anim.name));
				}
			}

			foreach(EditorCurveBinding curve in curvez)
			{
				if (!childrenPaths.Contains(curve.path))
				{
					//Debug.LogWarning(string.Format("OBJECT {0} NOT FOUND IN CLIP {1} ON ANIMATOR {2}. PATH TO OBJECT NEEDS TO BE UPDATED", curve.path, clip.name, anim.name));
				}
			}
		}
		return memoryusage;
	}
}

#endif
