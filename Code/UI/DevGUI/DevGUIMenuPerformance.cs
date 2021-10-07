// #define COUNT_PARTICLES

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Object = UnityEngine.Object;

/*
A dev panel.
*/

public class DevGUIMenuPerformance : DevGUIMenu
{
	private static bool doRenderInfo = false;
	private static List<Camera> disabledCameras = null;
	private static int memoryWarningCount = 0;
	private static string fpsOverride = "";
	private static int originalWidth = -1;
	private static int originalHeight = -1;
	private static float cameraAspectRatio = 0.0f;

	// Below: a non-comprehensive list of testing other resolutions based on popular modern device aspect ratios and resolutions 
	// We round off to 2 decimal points since some resolutions (= width/height) do not precisly equal their aspect ratio
	private static Dictionary<float, List<string>> resolutionDict = new Dictionary<float,List<string>>
	{
		{CommonMath.round(4.0f/3.0f, 2), new List<string>{"960x720", "1280x960", "2048x1536", "2732x2048"}},
		{CommonMath.round(3.0f/2.0f, 2), new List<string>{"480x320", "960x640", "1920x1280", "2160x1440"}},
		{CommonMath.round(16.0f/9.0f, 2), new List<string>{"1280x720", "1334x750", "1920x1080", "2560x1440"}}	
	};

	private System.Text.StringBuilder statString = new System.Text.StringBuilder(512);
	private float lastTimeStatsRendered;
	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Extra Info: " + onOff[doRenderInfo ? 1 : 0]))
		{
			doRenderInfo = !doRenderInfo;
		}
		if (GUILayout.Button("Rendering: " + onOff[(disabledCameras == null) ? 1 : 0]))
		{
			toggleCameras();
		}

		if (GUILayout.Button("MemWarn"))
		{
			MemoryWarningHandler.forceMemoryWarning();
		}
		
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		
		if (GUILayout.Button("Log Biggest"))
		{
			// log a list of the largest textures...
			string biggestTextures = getTextureList();
			Debug.Log( biggestTextures );
		}
		
		if (GUILayout.Button("Log Usage Diff"))
		{
			// log new and deleted texture report
			string usageDiffReport = getTextureUseDiff();
			Debug.Log( usageDiffReport );
		}

		if (GUILayout.Button("Log Bundles"))
		{
			Debug.Log( AssetBundleManager.Instance.getLoadedBundlesStr() );
		}
		
		if (GUILayout.Button("Unload Bundles"))
        {
        	AssetBundleManager.Instance.unloadAllBundles();
        	Debug.Log( AssetBundleManager.Instance.getLoadedBundlesStr() );
        }
		
		if (GUILayout.Button("Log Tex Cache"))
		{
			DisplayAsset.outputSessionCacheStats();
		}
		
		if (GUILayout.Button("Clear Tex Cache"))
		{
			DisplayAsset.forceClearSessionCache();
			DisplayAsset.outputSessionCacheStats();
		}

		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();

		// Toggle the buffered symbol culling through: Off / On / AutoFlip (each frame)
		string cull = Glb.autoToggleSymbolCulling ? "FLIP" : Glb.enableSymbolCulling ? "ON" : "OFF";
		if (GUILayout.Button("SymCulling is: " + cull))
		{
			// CullingSystem must be enabled to change culling states (don't confuse user)
			if (Glb.enableSymbolCullingSystem)
			{
				if (Glb.autoToggleSymbolCulling)
				{
					Glb.autoToggleSymbolCulling = false;
					Glb.enableSymbolCulling = false;
					Debug.Log("Glb.autoToggleSymbolCulling = " + Glb.autoToggleSymbolCulling);
					Debug.Log("Glb.enableSymbolCulling = " + Glb.enableSymbolCulling);
				}
				else if (Glb.enableSymbolCulling == false)
				{
					Glb.enableSymbolCulling = true;
					Debug.Log("Glb.enableSymbolCulling = " + Glb.enableSymbolCulling);
				}
				else
				{
					Glb.autoToggleSymbolCulling = true;
					Debug.Log("Glb.autoToggleSymbolCulling = " + Glb.autoToggleSymbolCulling);
				}
			}
		}

#if !UNITY_WEBGL
		string targetFPS = Application.targetFrameRate == -1 ? "Unlimited": string.Format("{0} FPS", Application.targetFrameRate);
		GUILayout.Label(string.Format("FPS limit: {0}", targetFPS), GUILayout.MinWidth(250));

		if (GUILayout.Button("-5"))
		{
			if (Application.targetFrameRate == -1)
			{
				Application.targetFrameRate = 60;
			}
			else if (Application.targetFrameRate > 5)
			{
				Application.targetFrameRate = Mathf.Max(5, Application.targetFrameRate - 5);
			}
		}

		if (GUILayout.Button("+5"))
		{
			if (Application.targetFrameRate != -1)
			{
				if (Application.targetFrameRate < 60)
				{
					Application.targetFrameRate = Mathf.Min(60, Application.targetFrameRate + 5);
				}
				else
				{
					Application.targetFrameRate = -1;
				}
			}
		}

		if (GUILayout.Button("1"))
		{
			Application.targetFrameRate = 1;
		}

		GUILayout.EndHorizontal();
		
		GUILayout.Space(10);
		
		GUILayout.BeginHorizontal();
		
		cameraAspectRatio = cameraAspectRatio == 0.0f ? CommonMath.round(Camera.main.aspect, 2) : cameraAspectRatio;
		if (resolutionDict.ContainsKey(cameraAspectRatio))
		{
			if (GUILayout.Button("Default"))
			{
				UnityEngine.Screen.SetResolution(originalWidth, originalHeight, true);
			}
			for (int i = 0; i < resolutionDict[cameraAspectRatio].Count; ++i)
			{
				if (GUILayout.Button(resolutionDict[cameraAspectRatio][i]))
				{
					string[] dimensions = resolutionDict[cameraAspectRatio][i].Split('x');
					if (originalHeight == -1 || originalWidth == -1)
					{
						originalHeight = Screen.height;
						originalWidth = Screen.width;
					}
					int width = int.Parse(dimensions[0]);
					int height = int.Parse(dimensions[1]);
					UnityEngine.Screen.SetResolution(width, height, true);
				}
			}
		}
		else 
		{	
			GUILayout.Label(string.Format("Couldn't find resolutions for your device aspect ratio ({0} @ {1}x{2})",
							cameraAspectRatio, UnityEngine.Screen.width, UnityEngine.Screen.height));
		}
		
#endif

		GUILayout.EndHorizontal();
		
		GUILayout.Space(10);
		
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("isCrappyDevice: " + onOff[MobileUIUtil.isCrappyDevice ? 1 : 0]);
		GUILayout.Label("isSlowDevice: " + onOff[MobileUIUtil.isSlowDevice ? 1 : 0]);
		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Override FPS setting");
		fpsOverride = GUILayout.TextField(fpsOverride).Trim();

		if (GUILayout.Button("Save FPS Override"))
		{
			PlayerPrefsCache.SetInt("FPS_OVERRIDE", int.Parse(fpsOverride));
		}
		GUILayout.EndHorizontal();

		// TODO: most of this string doesnt vary frame-to-frame!
		float now = Time.realtimeSinceStartup;
		if (now - lastTimeStatsRendered >= 1.0)
		{
			lastTimeStatsRendered = now;
			
			statString.Length = 0;
			statString.AppendFormat("Client Version: '{0}'\n", Glb.clientVersion);
			statString.AppendFormat("Client Runtime Version: '{0}'\n", Application.unityVersion);
			statString.Append(SystemInfo.operatingSystem);
			statString.AppendFormat("Device Type: {0}", SystemInfo.deviceType);
			statString.AppendFormat("\n{0} (x{1}) {2}MB", SystemInfo.processorType, SystemInfo.processorCount, SystemInfo.systemMemorySize);
			statString.AppendFormat("\n{0} {1} {2}MB (shader level {3})",SystemInfo.graphicsDeviceVendor,SystemInfo.graphicsDeviceName,
																	     SystemInfo.graphicsMemorySize, SystemInfo.graphicsShaderLevel);
			statString.AppendFormat("\nScreen resolution: {0}x{1} @ {2}Hz", UnityEngine.Screen.currentResolution.width,UnityEngine.Screen.currentResolution.height, UnityEngine.Screen.currentResolution.refreshRate);
			statString.AppendFormat("\n\nHeap size: {0:0.0} MB", UnityEngine.Profiling.Profiler.usedHeapSizeLong / (1024f * 1024f));
			statString.AppendFormat("\nTotal allocated: {0:0.0} MB", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
			statString.AppendFormat("\nTotal reserved: {0:0.0} MB", UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f));
			statString.AppendLine($"\nTotal sys mem: {Zynga.Core.Platform.DeviceInfo.CurrentMemoryMB:0.0} MB/{Zynga.Core.Platform.DeviceInfo.DeviceMemoryMB:0.0} MB" );

			statString.AppendLine($"Asset Bundles Loaded: {AssetBundleManager.Instance.LoadedBundleCount}");
			statString.AppendLine($"Session Texture Cache: {DisplayAsset.SessionCacheCount}");
			statString.AppendFormat("\n{0}", textureStats());
			
	#if UNITY_WEBGL
			statString.AppendFormat("\n WebGl Total Memory size: {0} Bytes", WebGLFunctions.GetTotalMemorySize());
			statString.AppendFormat("\n WebGl Total Stack size: {0} Bytes", WebGLFunctions.GetTotalStackSize());
			statString.AppendFormat("\n WebGl Total Static Memory size: {0} Bytes", WebGLFunctions.GetStaticMemorySize());
			statString.AppendFormat("\n WebGl Total Dynamic size: {0} Bytes", WebGLFunctions.GetDynamicMemorySize());
	#endif
			
			statString.AppendFormat("\nMemory warnings: {0}", memoryWarningCount);
			
			if (doRenderInfo)
			{
				statString.AppendFormat("\n{0}\n\n{1}", getExtraInfo(), Server.getInfoString());
			}
		}
		
		GUILayout.TextArea(statString.ToString());
	}

	// get texture info..
	public string textureStats()
	{
		long memTextures = 0;
		Texture[] textures = Resources.FindObjectsOfTypeAll(typeof(Texture)) as Texture [];
		for (int i=0;i<textures.Length;i++)
		{
			Texture tex = textures[i];
			memTextures += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
		}
		string result = $"{textures.Length} textures: {memTextures / (1024f * 1024f):0.0} MB  ";
		return result;
	}

	private struct TextureInfo
	{
		public int id;
		public string name;
		public long memoryBytes;

		public TextureInfo(Texture tex)
		{
			id = tex.GetInstanceID();
			name = tex.name;
			memoryBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
		}
		
		public override string ToString()
		{
			return $"ID:{id,8}  {memoryBytes/1024,7:N0}kb   {name}";
		}
	}
	
	private Dictionary<int, TextureInfo> currentTextureIDToNameMap = new Dictionary<int, TextureInfo>();
	private List<TextureInfo> newTextures = new List<TextureInfo>();
	private List<TextureInfo> deletedTextures = new List<TextureInfo>();
	
	// Returns textual list of largest textures in memory
	public string getTextureList(int numberToShow = 125)
	{
		Texture[] allTextures = Resources.FindObjectsOfTypeAll(typeof(Texture)) as Texture[];
		IEnumerable<Texture> sortedTextures = allTextures
			.Where(tex => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex) >= 1024*150)
			.OrderByDescending(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong)
			.Take(numberToShow);

		long totalLargestTexMemoryBytes = 0;
		StringBuilder largestTexturesBuilder = new StringBuilder(256);
		foreach(Texture tex in sortedTextures)
		{
			long memorySizeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
			totalLargestTexMemoryBytes += memorySizeBytes;
			string memSizeSnippet = $"{memorySizeBytes/1024}kb";
			string dimensionsSnippet = $"({tex.width}x{tex.height})";
			largestTexturesBuilder.AppendLine($"{memSizeSnippet,8}  {dimensionsSnippet,13}  ID:{tex.GetInstanceID(),8}  {tex.name}");
		}

		System.Text.StringBuilder result = new System.Text.StringBuilder(1024);
		result.AppendLine($"{numberToShow} LARGEST TEXTURES of {allTextures.Length} - Total: {totalLargestTexMemoryBytes/1024:N0}kb");
		result.Append(largestTexturesBuilder);
		result.AppendLine($"Largest Total: {totalLargestTexMemoryBytes/1024:N0}kb");

		Dictionary<string, List<Texture>> duplicatedTextures = new Dictionary<string, List<Texture>>();
		foreach (Texture tex in allTextures)
		{
			if (duplicatedTextures.ContainsKey(tex.name))
			{
				duplicatedTextures[tex.name].Add(tex);
			}
			else
			{
				duplicatedTextures[tex.name] = new List<Texture> { tex };
			}
		}

		result.AppendLine("DUPLICATED TEXTURES:");
		foreach (KeyValuePair<string, List<Texture>> pair in duplicatedTextures)
		{
			if (pair.Value.Count > 1)
			{
				long totalSize = pair.Value.Sum(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong);
				string texName = string.IsNullOrEmpty(pair.Value[0].name) ? "unnamed" : pair.Value[0].name;
				result.AppendLine($" ({pair.Value.Count})\t{texName} {totalSize/1024}kb = {totalSize/pair.Value.Count/1024}kb avg");
			}
		}

		return result.ToString();
	}

	public string getTextureUseDiff()
	{
		StringBuilder result = new StringBuilder(512);
		
		Texture[] allTextures = Resources.FindObjectsOfTypeAll(typeof(Texture)) as Texture[];
		foreach (Texture tex in allTextures)
		{
			if (!currentTextureIDToNameMap.ContainsKey(tex.GetInstanceID()))
			{
				TextureInfo textureInfo = new TextureInfo(tex);
				currentTextureIDToNameMap[tex.GetInstanceID()] = textureInfo;
				newTextures.Add(textureInfo);
			}
		}
		
		IEnumerable<int> allTexIDs = allTextures.Select(tex => tex.GetInstanceID());
		foreach (KeyValuePair<int, TextureInfo> kvp in currentTextureIDToNameMap)
		{
			if (!allTexIDs.Contains(kvp.Key))
			{
				deletedTextures.Add(kvp.Value);
			}
		}
		
		StringBuilder newTextureList = new StringBuilder(512);
		long totalNewTextureMemBytes = 0;
		foreach (TextureInfo texInfo in newTextures)
		{
			totalNewTextureMemBytes += texInfo.memoryBytes;
			newTextureList.AppendLine(texInfo.ToString());
		}

		StringBuilder deletedTextureList = new StringBuilder(512);
		long totalDeletedTextureMemBytes = 0;
		foreach (TextureInfo texInfo in deletedTextures)
		{
			totalDeletedTextureMemBytes += texInfo.memoryBytes;
			currentTextureIDToNameMap.Remove(texInfo.id);
			deletedTextureList.AppendLine(texInfo.ToString());
		}

		long newKb = totalNewTextureMemBytes / 1024;
		long deletedKb = totalDeletedTextureMemBytes / 1024;
		
		//Try to make output easily viewable in Editor and device console
		result.AppendLine($"Tex Diff: {newTextures.Count} New +{newKb:N0}kb | {deletedTextures.Count} Deleted -{deletedKb:N0}kb");
		result.AppendLine($"Net: {newKb-deletedKb:N0}kb");
		result.AppendLine($"New textures ({newTextures.Count}):");
		result.Append(newTextureList);
		result.AppendLine($"Deleted textures: ({deletedTextures.Count})");
		result.Append(deletedTextureList);
		result.AppendLine($"New Total ({newTextures.Count}): {newKb:N0}kb");
		result.AppendLine($"Deleted Total ({deletedTextures.Count}): -{deletedKb:N0}kb");
		result.AppendLine($"Net: {newKb-deletedKb:N0}kb");
		
		newTextures.Clear();
		deletedTextures.Clear();

		return result.ToString();
	}
	
	/// Toggles On/Off rendering.
	private void toggleCameras()
	{
		if (disabledCameras == null)
		{
			disabledCameras = new List<Camera>();
			Camera[] cameras = Object.FindObjectsOfType(typeof(Camera)) as Camera[];
			foreach (Camera camera in cameras)
			{
				if (camera.enabled)
				{
					camera.enabled = false;
					disabledCameras.Add(camera);
				}
			}
		}
		else
		{
			foreach (Camera camera in disabledCameras)
			{
				camera.enabled = true;
			}
			disabledCameras = null;
		}
	}
	
	public static void onMemoryWarning()
	{
		memoryWarningCount++;
	}

	#if COUNT_PARTICLES
	private ParticleSystem.Particle[] particleStash = new ParticleSystem.Particle[3000];
	#endif

	/// Includes extra data in the performance data such as polygons, objects, particles,
	/// active bones, and colliders.
	private string getExtraInfo()
	{
		int renderedPolygons = 0;
		int renderedObjects = 0;
		int activeParticleSystems = 0;
		int renderedParticles = 0;
		int activeBones = 0;
		int activeColliders = 0;
		int materialCount = 0;
		int textureCount = 0;

		MeshFilter[] meshFilters = Object.FindObjectsOfType(typeof(MeshFilter)) as MeshFilter[];
		if (meshFilters != null && meshFilters.Length > 0)
		{
			foreach (MeshFilter meshFilter in meshFilters)
			{
				if (meshFilter.GetComponent<Renderer>() != null &&
					meshFilter.GetComponent<Renderer>().enabled &&
					meshFilter.gameObject != null &&
					meshFilter.gameObject.activeInHierarchy &&
					meshFilter.GetComponent<Renderer>() != null &&
					meshFilter.GetComponent<Renderer>().isVisible)
				{
					renderedObjects += 1;
					//renderedPolygons += meshFilter.mesh.triangles.Length / 3;
				}
			}
		}

		SkinnedMeshRenderer[] skinnedMeshes = Object.FindObjectsOfType(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer[];
		if (skinnedMeshes != null && skinnedMeshes.Length > 0)
		{
			foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
			{
				if (skinnedMesh.enabled &&
					skinnedMesh.gameObject != null &&
					skinnedMesh.gameObject.activeInHierarchy &&
					skinnedMesh.isVisible)
				{
					renderedObjects += 1;
					//renderedPolygons += skinnedMesh.mesh.triangles.Length / 3;
					activeBones += skinnedMesh.bones.Length;
				}
			}
		}

	#if COUNT_PARTICLES
		// because GetParticles cannot get the number of particles w/o a large buffer allocation to write into, 
		// going to skip this stat for now
		ParticleSystem[] particleSystems = Object.FindObjectsOfType(typeof(ParticleSystem)) as ParticleSystem[];
		if (particleSystems != null && particleSystems.Length > 0)
		{
			activeParticleSystems = particleSystems.Length;
			foreach (ParticleSystem particleSystem in particleSystems)
			{
				if (particleSystem.gameObject != null)
				{
					renderedParticles += particleSystem.GetParticles(particleStash);
				}
			}
		}
	#endif

		Collider[] colliders = Object.FindObjectsOfType(typeof(Collider)) as Collider[];
		if (colliders != null && colliders.Length > 0)
		{
			foreach (Collider collider in colliders)
			{
				if (collider.enabled &&
					collider.gameObject != null &&
					collider.gameObject.activeInHierarchy)
				{
					activeColliders += 1;
				}
			}
		}

		Material[] materials = Object.FindObjectsOfType(typeof(Material)) as Material[];
		if (materials != null && materials.Length > 0)
		{
			materialCount = materials.Length;
		}

		Texture2D[] textures = Object.FindObjectsOfType(typeof(Texture2D)) as Texture2D[];
		if (textures != null && textures.Length > 0)
		{
			textureCount = textures.Length;
		}

		System.Text.StringBuilder sb = new System.Text.StringBuilder(256);

		sb.AppendFormat("\nTotal rendered polygons: {0}", renderedPolygons);
		sb.AppendFormat("\nRendered objects: {0}",renderedObjects);
	#if COUNT_PARTICLES
		sb.AppendFormat("\nActive particle systems: {0}", activeParticleSystems);
		sb.AppendFormat("\nRendered particles: {0}", renderedParticles);
	#endif
		sb.AppendFormat("\nActive bones: {0}", activeBones);
		sb.AppendFormat("\nActive colliders: {0}", activeColliders);
		sb.AppendFormat("\nMaterial Count: {0}", materialCount);
		sb.AppendFormat("\nTexture Count: {0}", textureCount);

		return sb.ToString();
	}
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
