using UnityEngine;
using System.Collections;

public class VIPCarouselButtonV3 : MonoBehaviour, IRecycle
{
	public bool isDirty = false;
	public GameObject image;
	public UITexture imageRenderer = null;
	private Material clonedMaterial = null;


	public void reset()
	{
		//this.gameObject.SetActive(false);
	}

	public void init(Dict args)
	{

		object inputObj = null;
		if (args.TryGetValue(D.DATA, out inputObj))
		{
			Texture tex = inputObj as Texture;
			setup(tex);
			this.gameObject.SetActive(true);
		}
		else
		{
			Debug.LogWarning("Can't initialize carousel object -- no valid texture");
		}

	}
    public void setup(Texture tex)
    {
        if (image != null)
		{
			imageRenderer = image.GetComponent<UITexture>();
			if (clonedMaterial != null)
			{
				clonedMaterial.color = Color.black;
			}
			else
			{
				imageRenderer.material = new Material(optionShader);
				imageRenderer.material.color = Color.black;
				clonedMaterial = imageRenderer.material;
			}
			isDirty = true;
		}
		setImage(tex);
    }

	private void setImage(Texture tex)
	{
		if (tex != null)
		{
			if (clonedMaterial != null && clonedMaterial.mainTexture != tex)
			{
				clonedMaterial.color = Color.white;
				clonedMaterial.mainTexture = tex;
				isDirty = true;
			}
		}
	}

	public static Shader optionShader
	{
		get
		{
			if (_optionShader == null)
			{
				_optionShader = ShaderCache.find("Unlit/UnlitSimple (AlphaClip)");
			}
			return _optionShader;
		}
	}

	private static Shader _optionShader = null;

	private void OnDestroy()
	{
		if (!isDirty)
		{
			return;
		}

		if (clonedMaterial != null)
		{
			imageRenderer.material = null;
			Destroy(clonedMaterial);
		}
	}
}