/// Place a // in front of either of the following #define lines to turn off code that
/// depends on those components (NGUI and ReelGame) - this is for art support on isolated
/// standalone projects.
#define ARTCHECK_HAS_NGUI
#define ARTCHECK_HAS_REELGAME

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if !UNITY_EDITOR

// Dummy script for stripping this out of device builds
public class ArtCheckGUI : MonoBehaviour
{

}

#else

public class ArtCheckGUI : MonoBehaviour
{
	private const int WINDOW_ID = 777;
	
	private const int TOO_MUCH_VERTEX = 32 * 1024;
	private const int TOO_MUCH_MESH = 256;
	private const int TOO_MUCH_ANIMATION = 256 * 1024;
	private const int TOO_MUCH_PARTICLE = 512;
	private const int TOO_MUCH_COLLIDER = 64;
	
#if UNITY_IPHONE
	private const int TOO_MUCH_MEMORY = 6 * 1024 * 1024;
#elif UNITY_ANDROID
	private const int TOO_MUCH_MEMORY = 12 * 1024 * 1024;
#else
	private const int TOO_MUCH_MEMORY = 2 * 1024 * 1024;
#endif
	
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
			}
			return _IGNORE_TEXTURES;
		}
	}
	private Dictionary<string, bool> _IGNORE_TEXTURES = null;
	
	private static Rect windowSmallRect = new Rect(20, 20, 260, 80);
	private static Rect windowBigRect = new Rect(20, 20, 600, 600);
	private static Rect dragRect = new Rect(0, 0, 10000, 10000);
	private static Vector2 scrollPosition = Vector2.zero;
	private static ParticleSystem.Particle[] particleStash = new ParticleSystem.Particle[10000];
	
	public bool showDetails = false;
	public float currentRating = 0;
	public int currentWarnings = 0;

	public int maxVertexCount = 0;
	public float maxMeshMemory = 0;
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
	public float maxTextureMemory = 0;
	public int maxAnimationCount = 0;
	public float maxAnimationMemory = 0;
	
	private string textureMemoryLog = "";

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
		
		string ratingText = string.Format("Current rating: {0}", currentRating);
		if  (currentWarnings > 0)
		{
			ratingText += string.Format(" [{0} warnings]", currentWarnings);
		}
		GUILayout.Label(ratingText);
		
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
		if (maxAnimationMemory + maxTextureMemory + maxMeshMemory > TOO_MUCH_MEMORY)
		{
			txt += "\n" + "WARNING: Too much memory use.";
		}
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
		
		Dictionary<Mesh, bool> meshUses = new Dictionary<Mesh, bool>();
		Dictionary<Texture, bool> textureUses = new Dictionary<Texture, bool>();
		Dictionary<AnimationClip, bool> animationUses = new Dictionary<AnimationClip, bool>();

		List<GameObject> gameObjects = new List<GameObject>();
		List<MeshFilter> meshFilters = new List<MeshFilter>();
		List<SkinnedMeshRenderer> skinnedMeshes = new List<SkinnedMeshRenderer>();
		List<Renderer> renderers = new List<Renderer>();
#if ARTCHECK_HAS_NGUI
		List<UIPanel> uiPanels = new List<UIPanel>();
		List<UITexture> uiTextures = new List<UITexture>();
#endif
#if ARTCHECK_HAS_REELGAME
		List<ReelGame> reelGames = new List<ReelGame>();
#endif
		List<Animation> animations = new List<Animation>();
		List<ParticleSystem> particleSystems = new List<ParticleSystem>();
		List<Collider> colliders = new List<Collider>();
		
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

				while (parent != null)
				{
					if (parent.GetComponent<IgnoreDuringArtCheck>() != null)
					{
						skip = true;
						break;
					}
					parent = parent.parent;
				}
				if (!skip)
				{
					gameObjects.Add(child.gameObject);
				}
			}
		}
		foreach (GameObject go in gameObjects)
		{
			meshFilters.AddRange(go.GetComponents<MeshFilter>());
			skinnedMeshes.AddRange(go.GetComponents<SkinnedMeshRenderer>());
			renderers.AddRange(go.GetComponents<Renderer>());
#if ARTCHECK_HAS_NGUI
			uiPanels.AddRange(go.GetComponents<UIPanel>());
			uiTextures.AddRange(go.GetComponents<UITexture>());
#endif
#if ARTCHECK_HAS_REELGAME
			reelGames.AddRange(go.GetComponents<ReelGame>());
#endif
			animations.AddRange(go.GetComponents<Animation>());
			particleSystems.AddRange(go.GetComponents<ParticleSystem>());
			colliders.AddRange(go.GetComponents<Collider>());
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
		
		foreach (Renderer foundRenderer in renderers)
		{
			foreach (Material material in foundRenderer.sharedMaterials ?? foundRenderer.materials)
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
								textureMemory += getTextureMemory(texture, ref textureMemoryLog);
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
		
#if ARTCHECK_HAS_NGUI
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
								textureMemory += getTextureMemory(texture, ref textureMemoryLog);
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
		
		foreach (UITexture uiTexture in uiTextures)
		{
			Texture texture = uiTexture.mainTexture;
							
			// Since this is a tool for artists, purposely ignore the memory used by the generic atlas to avoid confusion on their end
			if (texture != null && !textureUses.ContainsKey(texture) && !IGNORE_TEXTURES.ContainsKey(texture.name))
			{
				textureCount += 1;
				textureMemory += getTextureMemory(texture, ref textureMemoryLog);
				textureUses.Add(texture, true);
				
				if (texture is RenderTexture)
				{
					renderTextureCount += 1;
				}
			}
		}
#endif

#if ARTCHECK_HAS_REELGAME	
		foreach (ReelGame reelGame in reelGames)
		{
			List<Texture> texturesForReelGame = new List<Texture>();
			List<Material> materialsForReelGame = new List<Material>();
			List<GameObject> prefabsForReelGame = new List<GameObject>();
			
			texturesForReelGame.Add(reelGame.wildTexture);
			prefabsForReelGame.Add(reelGame.wildOverlayGameObject);
			
			if (reelGame.symbolTemplates != null)
			{
				
				foreach (SymbolInfo symbolTemplate in reelGame.symbolTemplates)
				{
					texturesForReelGame.Add(symbolTemplate.getTexture());
					texturesForReelGame.Add(symbolTemplate.wildTexture);
					prefabsForReelGame.Add(symbolTemplate.vfxPrefab);
					texturesForReelGame.Add(symbolTemplate.wildTexture);
					prefabsForReelGame.Add(symbolTemplate.wildOverlayGameObject);
					prefabsForReelGame.Add(symbolTemplate.symbolPrefab);
					prefabsForReelGame.Add(symbolTemplate.symbol3d);
				}
			}
			
			foreach (GameObject prefab in prefabsForReelGame)
			{
				if (prefab != null)
				{
					foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
					{
						foreach (Material material in renderer.sharedMaterials)
						{
							materialsForReelGame.Add(material);
						}
					}
				}
			}
			
			foreach (Material material in materialsForReelGame)
			{
				if (material != null)
				{
					texturesForReelGame.Add(material.mainTexture);
				}
			}
			
			foreach (Texture texture in texturesForReelGame)
			{	
				// Since this is a tool for artists, purposely ignore the memory used by the generic atlas to avoid confusion on their end
				if (texture != null && !textureUses.ContainsKey(texture) && !IGNORE_TEXTURES.ContainsKey(texture.name))
				{
					textureCount += 1;
					textureMemory += getTextureMemory(texture, ref textureMemoryLog);
					textureUses.Add(texture, true);
		
					if (texture is RenderTexture)
					{
						renderTextureCount += 1;
					}
				}
			}
		}
#endif
		
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
		
		maxVertexCount = Mathf.Max(maxVertexCount, vertexCount);
		maxMeshMemory = Mathf.Max(maxMeshMemory, meshMemory);
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
		maxTextureMemory = Mathf.Max(maxTextureMemory, textureMemory);
		maxAnimationCount = Mathf.Max(maxAnimationCount, animationCount);
		maxAnimationMemory = Mathf.Max(maxAnimationMemory, animationMemory);
		
		calculateScoring();
	}
	
	/// Calculates the score and warnings
	private void calculateScoring()
	{
		int warnings = 0;
		float warningWeight = 0;
		
		if (maxVertexCount >= TOO_MUCH_VERTEX)
		{
			warnings += 1;
			warningWeight += 0 + (maxVertexCount / TOO_MUCH_VERTEX);
		}
		if (maxMeshCount + (maxSkinnedMeshCount * 4) >= TOO_MUCH_MESH)
		{
			warnings += 1;
			warningWeight += 0 + ((maxMeshCount + (maxSkinnedMeshCount * 4)) / TOO_MUCH_MESH);
		}
		if (maxBoneCount * maxSkinnedVertexCount >= TOO_MUCH_ANIMATION)
		{
			warnings += 1;
			warningWeight += 0 + ((maxBoneCount * maxSkinnedVertexCount) / TOO_MUCH_ANIMATION);
		}
		if (maxClassicParticleCount + (maxParticleCount * 2) >= TOO_MUCH_PARTICLE)
		{
			warnings += 1;
			warningWeight += 0 + ((maxClassicParticleCount + (maxParticleCount * 2)) / TOO_MUCH_PARTICLE);
		}
		if (maxColliderCount >= TOO_MUCH_COLLIDER)
		{
			warnings += 1;
			warningWeight += 0;		// No penalty
		}
		if (maxAnimationMemory + maxTextureMemory + maxMeshMemory > TOO_MUCH_MEMORY)
		{
			warnings += 1;
			warningWeight += 1 + ((maxAnimationMemory + maxTextureMemory + maxMeshMemory) / TOO_MUCH_MEMORY);
		}
		if (maxRenderTextureCount > 0)
		{
			warnings += 1;
			warningWeight += 0;		// No penalty
		}
		if (maxMeshColliderCount > 0)
		{
			warnings += 1;
			warningWeight += 3;
		}
		
		long vertexScore = normalizeScore(maxVertexCount, 4 * 1024, TOO_MUCH_VERTEX);
		long animationScore = normalizeScore(maxBoneCount * maxSkinnedVertexCount, 64, TOO_MUCH_ANIMATION);
		long particleScore = normalizeScore(maxClassicParticleCount + (maxParticleCount * 2), 16, TOO_MUCH_PARTICLE);
		long memoryScore = normalizeScore(maxAnimationMemory + maxTextureMemory + maxMeshMemory, 512 * 1024, TOO_MUCH_MEMORY);

		float scoreMax = Mathf.Max(vertexScore, Mathf.Max(animationScore, Mathf.Max(particleScore, memoryScore)));
		long scoreAvg = vertexScore + animationScore + particleScore + memoryScore;
		float score = scoreMax * 8 + scoreAvg;
		
		if (warningWeight > 0)
		{
			// If you get any warnings, then your score suffers
			score = score * (warningWeight + 1);
		}
		
		currentWarnings = warnings;
		currentRating = score / 12;
	}
	
	/// Returns a normalized score for usage, long math is used because some numbers are too damn big.
	private int normalizeScore(float value, float minAcceptable, float maxAcceptable)
	{
		long range = (long)(maxAcceptable - minAcceptable);
		long rawScore = (100L * (long)(value - minAcceptable)) / range;
		return Mathf.Max((int)rawScore, 0);
	}

	public static long getTextureMemory(Texture texture)
	{
		string dummy = "";

		return getTextureMemory(texture, ref dummy);
	}

	public static long getTextureMemory(Texture texture, ref string messageLog, float maxSize = 0)
	{
		long memory = 0;
		
		if (texture is Texture2D)
		{
			int bitPerPixel = 0;
			Texture2D tex2d = (Texture2D)texture;
			switch (tex2d.format)
			{
				case TextureFormat.PVRTC_RGB4:
				case TextureFormat.PVRTC_RGBA4:
				case TextureFormat.ETC_RGB4:
					bitPerPixel = 4;
					break;
					
				case TextureFormat.Alpha8:
				case TextureFormat.ETC2_RGBA8:
					bitPerPixel = 8;
					break;
			
				case TextureFormat.RGB565:
				case TextureFormat.RGBA4444:
					bitPerPixel = 16;
					break;
				
				case TextureFormat.RGB24:
					bitPerPixel = 24;
					break;
			
				case TextureFormat.RGBA32:
				case TextureFormat.ARGB32:
					bitPerPixel = 32;
					break;
				
				default:
					messageLog += string.Format("Unsupported texture format {0} in {1}",  tex2d.format.ToString(), tex2d.name);
					break;
			}
			
			if (bitPerPixel > 0)
			{
				memory = (bitPerPixel * tex2d.width * tex2d.height) / 8;
				messageLog += string.Format("\n  {0} using calculated {1:0.000} MB", tex2d.name, ((double)memory / (double)(1024 * 1024)));
			}

			if (maxSize > 0 && (tex2d.width > maxSize || tex2d.height > maxSize))
			{
				messageLog += string.Format(" Texture Size is larger than expected " + maxSize);
			}
		}
		
		if (memory == 0)
		{
			memory = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
			messageLog += string.Format("\n  {0} using profiled {1:0.000} MB", texture.name, ((double)memory / (double)(1024 * 1024)));
		}
		
		if (memory == 0)
		{
			messageLog += string.Format("No memory detected for {0}", texture.name);
		}

		
		return memory;
	}
}

#endif
