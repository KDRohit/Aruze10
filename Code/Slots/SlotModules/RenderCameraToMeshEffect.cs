using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script for rendering camera output to an arbitrary mesh via RenderTexture. Allows us to create a bended 3D reel effect
/// for mechanical reel games by rendering the symbols camera to a convex quad.
/// First used by orig004 mechanical reel
/// Author: Caroline June 2020
/// </summary>
public class RenderCameraToMeshEffect : MonoBehaviour
{
	[Tooltip("Cameras to use as source for RenderTexture")]
	[SerializeField] private List<Camera> sourceCameras;
	[Tooltip("Mesh to apply RenderTexture to")]
	[SerializeField] private Renderer targetMesh;

	[Tooltip("Width of RenderTexture in pixels")]
	[SerializeField] private int renderTextureWidth = 1024;
	[Tooltip("Height of RenderTexture in pixels")]
	[SerializeField] private int renderTextureHeight = 512;
	[Tooltip("The render texture size of depth buffer in bits, choose from 0, 16 (no stencil), 24, or 32 (stencil)")]
	[SerializeField] private int renderTextureDepth = 16;

	private RenderTexture renderTexture;

	void Awake()
	{
		if (renderTexture == null)
		{
			renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, renderTextureDepth);
			renderTexture.Create();
		}

		foreach (Camera cam in sourceCameras)
		{
			cam.targetTexture = renderTexture;
		}

		targetMesh.material.mainTexture = renderTexture;
	}

	void OnDestroy()
	{
		if (renderTexture != null)
		{
			foreach (Camera cam in sourceCameras)
			{
				cam.targetTexture = null;
			}

			renderTexture.Release();
			renderTexture = null;
		}
	}
}
