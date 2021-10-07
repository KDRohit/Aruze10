using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[ExecuteInEditMode]
public class AlphaFadeGroup : MonoBehaviour
{
	[SerializeField] private List<MeshRenderer> renderers;
	[SerializeField] private List<UISprite> sprites;
	[SerializeField] private List<TextMeshPro> tmPros;
	[SerializeField] private List<UITexture> uiTextures;
	
	public Color alphaValue;
	public Color currentAlphaValue = Color.clear;

	public bool shouldUseInEditor;
	
	public void Update()
	{
		if (!currentAlphaValue.Compare(alphaValue))
		{
			Color colorToBeChanged;

			for (int i = 0; i < renderers.Count; i++)
			{
				colorToBeChanged = renderers[i].sharedMaterial.color;
				colorToBeChanged.a = alphaValue.a;
				renderers[i].sharedMaterial.color = colorToBeChanged;
			}

			for (int i = 0; i < sprites.Count; i++)
			{
				colorToBeChanged = sprites[i].color;
				colorToBeChanged.a = alphaValue.a;
				sprites[i].color = colorToBeChanged;
			}

			for (int i = 0; i < tmPros.Count; i++)
			{
				colorToBeChanged = tmPros[i].color;
				colorToBeChanged.a = alphaValue.a;
				tmPros[i].color = colorToBeChanged;
			}

			for (int i = 0; i < uiTextures.Count; i++)
			{
				colorToBeChanged = uiTextures[i].color;
				colorToBeChanged.a = alphaValue.a;
				uiTextures[i].color = colorToBeChanged;
			}
			currentAlphaValue = alphaValue;
		}
	}
}
