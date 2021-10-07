using UnityEngine;
using System.Collections;

/**
This is a simple class that disables game objects if the given
shader names either don't exist or are not supported.
*/
public class DisableForUnsupportedShader : MonoBehaviour
{
	public string[] requiredShaderNames;	// The full exact shader names required
	public GameObject[] objectsToDisable;	// The GameObject references to disable if there is anything missing
	
	void Start()
	{
		if (requiredShaderNames != null && objectsToDisable != null)
		{
			// If the device is crappy, let's just assume we should turn off these objects regardless of capability
			bool shouldDisable = MobileUIUtil.isCrappyDevice;

			// Look for unsupported shaders
			if (!shouldDisable)
			{
				foreach (string shaderName in requiredShaderNames)
				{
					if (!string.IsNullOrEmpty(shaderName))
					{
						Shader shader = ShaderCache.find(shaderName);
						if (shader == null || !shader.isSupported)
						{
							Debug.LogWarning("DisableForUnsupportedShader disabled some things. " + gameObject.name, gameObject); 
							shouldDisable = true;
							break;
						}
					}
				}
			}
			
			// Disable objects
			if (shouldDisable)
			{
				foreach (GameObject go in objectsToDisable)
				{
					if (go != null)
					{
						go.SetActive(false);
					}
				}
			}
		}
		
		// Self destruct this behavior
		Common.selfDestructObject(this);
	}
}
