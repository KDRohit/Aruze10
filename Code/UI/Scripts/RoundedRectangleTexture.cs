using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(UITexture))]
public class RoundedRectangleTexture : MonoBehaviour
{
	[Range(0, 1000)] public int radius;
	private UITexture texture;
	private int currentRadius = -1;

	void Awake()
	{
		texture = GetComponent<UITexture>();
	}

	void Update()
	{
		if (Application.isPlaying)
		{
			// Awake creates the mesh, then we don't need this anymore at runtime.
			enabled = false;
		}
		else
		{
			updateShader();
		}
	}

	private void updateShader()
	{
		if (texture != null && currentRadius != radius)
		{
			texture.material.SetFloat("_Radius", radius);
			texture.material.SetVector("_WidthHeight", new Vector4(transform.localScale.x, transform.localScale.y, 0, 0));
			currentRadius = radius;
			texture.MarkAsChanged();
		}
	}
}