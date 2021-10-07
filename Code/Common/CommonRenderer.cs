using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public static class CommonRenderer 
{

	/// Sets the color of every material in a given renderer.
	public static void colorRenderer(Renderer renderer, Color color, int ignoreMask = 0)
	{
		int layerMask = 1 << renderer.gameObject.layer;
		if ((ignoreMask & layerMask) == 0)
		{
			Material[] materials = renderer.materials;
			
			CommonMaterial.colorMaterials(materials, color);
			
			renderer.materials = materials;
		}
	}
	
	/// Sets the color of every material in a given renderer by slot if we only want to effect one material in a slot
	public static void colorRendererBySlot(Renderer renderer, Color color, int slot = 0, int ignoreMask = 0)
	{
		int layerMask = 1 << renderer.gameObject.layer;
		if ((ignoreMask & layerMask) == 0)
		{
			Material material = renderer.materials[slot];
			if (CommonMaterial.colorMaterial(material, color))
			{
				renderer.materials[slot] = material;
			}
		}
	}

	// Fade a renderer's materials from their current values to the target endValue
	// uses start values grabbed using getRendererAlphaValues()
	public static void alphaRendererFromStartValues(Renderer renderer, List<float> startingValues, float endValue, float t, bool doEaseOutCubic)
	{
		if (renderer != null)
		{
			Material[] materials = renderer.sharedMaterials;

			if (Application.isPlaying)
			{
				materials = renderer.materials;
			}

			List<float> materialAlphaValues = new List<float>();

			for (int i = 0; i < materials.Length; i++)
			{
				float currentAlpha = CommonMath.getInterpolatedFloatValue(startingValues[i], endValue, t, doEaseOutCubic);
				CommonMaterial.setAlphaOnMaterial(materials[i], currentAlpha);
			}

			if (Application.isPlaying)
			{
				renderer.materials = materials;
			}
			else
			{
				renderer.sharedMaterials = materials;
			}
		}
	}

	public static List<float> getRendererAlphaValues(Renderer renderer)
	{
		if (renderer == null)
		{
			return new List<float>();
		}
		else
		{
			Material[] materials = renderer.sharedMaterials;

			if (Application.isPlaying)
			{
				materials = renderer.materials;
			}

			List<float> materialAlphaValues = new List<float>();

			for (int i = 0; i < materials.Length; i++)
			{
				materialAlphaValues.Add(CommonMaterial.getAlphaOnMaterial(materials[i]));
			}

			return materialAlphaValues;
		}
	}

	public static void alphaRenderer(Renderer renderer, float alpha)
	{
		Material[] materials = renderer.sharedMaterials;
		
		if (Application.isPlaying)
		{
			materials = renderer.materials;
		}
		
		CommonMaterial.setAlphaOnMaterials(materials, alpha);
		
		if (Application.isPlaying)
		{
			renderer.materials = materials;
		}
		else
		{
			renderer.sharedMaterials = materials;
		}
	}
}
