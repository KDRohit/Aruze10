using UnityEngine;
using System.Collections.Generic;

public static class CommonMaterial
{
	// Colors all materials in the array.
	public static void colorMaterials(Material[] materials, Color color)
	{
		if (materials == null)
		{
			return;
		}

		foreach (Material mat in materials)
		{
			colorMaterial(mat, color);
		}
	}

	/// Colors a material and returns whether the material was non null and colored.
	public static bool colorMaterial(Material material, Color color)
	{
		if (material != null)
		{
			Shader shader = material.shader;
			if (shader != null)
			{
				if (material.HasProperty("_Color"))
				{
					material.color = color;
				}
				if (material.HasProperty("_MonoColor"))
				{
					material.SetColor("_MonoColor", color);
				}
				if (material.HasProperty("_TintColor"))
				{
					material.SetColor("_TintColor", color);
				}
				if (material.HasProperty("_ColorTint"))
				{
					material.SetColor("_ColorTint", color);
				}
				if (material.HasProperty("_MultColor"))
				{
					material.SetColor("_MultColor", color);
				}
				if (material.HasProperty("_StartColor"))
				{
					material.SetColor("_StartColor", color);
				}
				return true;
			}
		}
		return false;
	}

	/// Checks if a material has the ability to be controlled on the alpha channel
	public static bool canAlphaMaterial(Material material)
	{
		if (material == null)
		{
			return false;
		}

		return material.HasProperty("_TintColor") 
			|| material.HasProperty("_Color")
			|| material.HasProperty("_Alpha")
			|| material.HasProperty("_MonoColor")
			|| material.HasProperty("_ColorTint")
			|| material.HasProperty("_MultColor")
			|| material.HasProperty("_StartColor");
	}

	/// Returns the current alpha value on a material
	public static float getAlphaOnMaterial(Material material)
	{
		if (canAlphaMaterial(material))
		{
			Color color;

			if (material.HasProperty("_Alpha"))
			{
				// If the shader has an alpha property, just set it directly instead of messing with the color.
				// This is the case for the "Special HSV" shader.
				return material.GetFloat("_Alpha");
			}
			else
			{
				if (material.HasProperty("_TintColor"))
				{
					// If using the TintColor shader, use the color from that instead of the default color.
					color = material.GetColor("_TintColor");
					return color.a * 2;	// Half alpha on this shader is fully visible 
				}
				else if (material.HasProperty("_MonoColor"))
				{
					// support fading on GUI Texture Monochrome
					color = material.GetColor("_MonoColor");
					return color.a;
				}
				else if (material.HasProperty("_ColorTint"))
				{
					color = material.GetColor("_ColorTint");
					return color.a;
				}
				else if (material.HasProperty("_MultColor"))
				{
					color = material.GetColor("_MultColor");
					return color.a;
				}
				else if (material.HasProperty("_StartColor"))
				{
					color = material.GetColor("_StartColor");
					return color.a;
				}
				else
				{
					color = material.color;
					return color.a;
				}
			}
		}
		else
		{
			return 0;
		}
	}

	// Set the alpha value of an array of materials.
	public static void setAlphaOnMaterials(Material[] materials, float alpha)
	{
		if (materials == null)
		{
			return;
		}

		foreach (Material mat in materials)
		{
			setAlphaOnMaterial(mat, alpha);
		}
	}

	/// Try and adjust the alpha component of a material
	public static void setAlphaOnMaterial(Material material, float alpha)
	{
		if (canAlphaMaterial(material))
		{
			Color color;

			if (material.HasProperty("_Alpha"))
			{
				// If the shader has an alpha property, just set it directly instead of messing with the color.
				// This is the case for the "Special HSV" shader.
				material.SetFloat("_Alpha", alpha);
			}
			else
			{
				if (material.HasProperty("_TintColor"))
				{
					// If using the TintColor shader, use the color from that instead of the default color.
					color = material.GetColor("_TintColor");
					color.a = alpha * .5f;	// If using the TintColor shader, .5 is considered full alpha.
				}
				else if (material.HasProperty("_MonoColor")) // support for setting GUI Texture Monochrome
				{
					color = material.GetColor("_MonoColor");
					color.a = alpha;
				}
				else if (material.HasProperty("_ColorTint")) //suport for the Unlit/Glint shader
				{
					color = material.GetColor("_ColorTint");
					color.a = alpha;
				}
				else if (material.HasProperty("_MultColor"))
				{
					color = material.GetColor("_MultColor");
					color.a = alpha;
				}
				else if (material.HasProperty("_StartColor"))
				{
					color = material.GetColor("_StartColor");
					color.a = alpha;
				}
				else
				{
					color = material.color;
					color.a = alpha;
				}

				colorMaterial(material, color);
			}
		}
	}

	// Downloads a texture and applies it to the material with a particular name on the given renderer.
	// Used when a renderer has more than one material.
	public static void loadTextureToRendererMaterial(Renderer renderer, string materialName, string url)
	{
		Material material = findMaterial(renderer.materials, materialName);
		material.color = Color.black;	// Default to black until texture is loaded.

		RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(
			url,
			loadTextureToRendererMaterialCallback,
			Dict.create(
				D.OPTION, material,
				D.IMAGE_TRANSFORM, renderer.transform
			)
		));
	}

	// Callback function for loadTextureToRendererMaterial() call.
	private static void loadTextureToRendererMaterialCallback(Texture2D tex, Dict texData)
	{
		if (tex != null)
		{
			Material material = texData.getWithDefault(D.OPTION, null) as Material;
			material.mainTexture = tex;
			material.color = Color.white;
		}
	}

	// Finds the material on a renderer that starts with the given name.
	public static Material findMaterial(Renderer renderer, string name)
	{
		if (renderer != null)
		{
			return findMaterial(renderer.materials, name);
		}
		return null;
	}

	// Finds the material in an array that starts with the given name.
	public static Material findMaterial(Material[] materials, string materialName)
	{
		for (int i = 0; i < materials.Length; i++)
		{
			Material mat = materials[i];
			if (mat.name.FastStartsWith(materialName))	// Must use "StartsWith" instead of an exact match due to the material being an instance.
			{
				return mat;
			}
		}
		return null;
	}
}