using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This only ESTIMATES memory use, special components might not be represented correctly.

To use this, drag it onto something in the scene hierachy at runtime.
Yes, you read correctly above^ drag the script onto whatever you want AT RUNTIME.
*/
public class HierarchyMemoryUse : TICoroutineMonoBehaviour
{
	public bool doRecheck = true;

	public long audioMemory = 0;
	public int audioCount = 0;
	public long meshMemory = 0;
	public int meshCount = 0;
	public long textureMemory = 0;
	public int textureCount = 0;
	
#if UNITY_EDITOR
	void Update()
	{
		if (doRecheck)
		{
			doRecheck = false;
			
			string[] textureProperties =
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
		
			audioMemory = 0;
			audioCount = 0;
			meshMemory = 0;
			meshCount = 0;
			textureMemory = 0;
			textureCount = 0;
			
			Dictionary<AudioClip, bool> audioUses = new Dictionary<AudioClip, bool>();
			Dictionary<Mesh, bool> meshUses = new Dictionary<Mesh, bool>();
			Dictionary<Texture, bool> textureUses = new Dictionary<Texture, bool>();
			
			foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
			{
				// Tally audio memory
				foreach (AudioSource audioSource in child.GetComponents<AudioSource>())
				{
					if (audioSource.clip != null && !audioUses.ContainsKey(audioSource.clip))
					{
						audioUses.Add(audioSource.clip, true);
						audioMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(audioSource.clip);
						audioCount++;
					}
				}
				
				// Tally mesh memory
				foreach (MeshFilter meshFilter in child.GetComponents<MeshFilter>())
				{
					if (meshFilter.sharedMesh != null && !meshUses.ContainsKey(meshFilter.sharedMesh))
					{
						meshUses.Add(meshFilter.sharedMesh, true);
						meshMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(meshFilter.sharedMesh);
						meshCount++;
					}
				}
				foreach (SkinnedMeshRenderer skinnedMeshRenderer in child.GetComponents<SkinnedMeshRenderer>())
				{
					if (skinnedMeshRenderer.sharedMesh != null && !meshUses.ContainsKey(skinnedMeshRenderer.sharedMesh))
					{
						meshUses.Add(skinnedMeshRenderer.sharedMesh, true);
						meshMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(skinnedMeshRenderer.sharedMesh);
						meshCount++;
					}
				}
				
				// Tally texture memory
				foreach (Renderer renderer in child.GetComponents<Renderer>())
				{
					foreach (Material material in renderer.sharedMaterials)
					{
						if (material != null)
						{
							foreach (string textureProperty in textureProperties)
							{
								if (material.HasProperty(textureProperty))
								{
									Texture texture = material.GetTexture(textureProperty) as Texture;
									if (texture != null && !textureUses.ContainsKey(texture))
									{
										textureUses.Add(texture, true);
										textureMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
										textureCount++;
									}
								}
							}
						}
					}
				}
				foreach (UIPanel uiPanel in child.GetComponents<UIPanel>())
				{
					foreach (UIDrawCall drawCall in uiPanel.drawCalls)
					{
						// Each draw call is one mesh for UIPanels
						// bytesPerFloat * floatsPerPoint * pointsPerQuad * numberOfQuads
						meshMemory += 4 * 3 * 4 * (drawCall.triangles / 2);
						meshCount += 1;
						
						if (drawCall.material != null)
						{
							foreach (string textureProperty in textureProperties)
							{
								if (drawCall.material.HasProperty(textureProperty))
								{
									Texture texture = drawCall.material.GetTexture(textureProperty) as Texture;
									if (texture != null && !textureUses.ContainsKey(texture))
									{
										textureUses.Add(texture, true);
										textureMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
										textureCount++;
									}
								}
							}
						}
					}
				}
			}
		}
	}
#endif
}
