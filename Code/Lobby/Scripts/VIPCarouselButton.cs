using UnityEngine;
using System.Collections;

public class VIPCarouselButton : MonoBehaviour
{
	public GameObject image;
	public Renderer imageRenderer = null;
	
    public void setup(Texture tex)
    {
        if (image != null)
		{
			imageRenderer = image.GetComponent<Renderer>();
			imageRenderer.material = new Material(optionShader);
			imageRenderer.material.color = Color.black;
		}
		setImage(tex);
    }

	public void setImage(Texture tex)
	{
		if (tex != null)
		{
			if (imageRenderer != null)
			{
				Material mat = imageRenderer.material;
				mat.color = Color.white;
				mat.mainTexture = tex;
			}
		}
	}

	public static Shader optionShader
	{
		get
		{
			if (_optionShader == null)
			{
				_optionShader = ShaderCache.find("Unlit/GUI Texture");
			}
			return _optionShader;
		}
	}
	private static Shader _optionShader = null;
}