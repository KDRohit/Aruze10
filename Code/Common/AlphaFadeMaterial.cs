using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class AlphaFadeMaterial : MonoBehaviour
{
	public MeshRenderer targetRenderer;

	private Vector4 currentPosition;
	private Vector4 fadePanelSizerPosition;
	private Vector4 parentOrigin;

	private Vector3 realPosition;

	public AlphaFadePanelSizer fadePanelSizer;

	public void setPanelSizer(AlphaFadePanelSizer panelSizer)
	{
		fadePanelSizer = panelSizer;
		setFadePanelProperties();
	}

	private const string SHADER_NAME = "Unlit/AlphaUnlitDistanceMask";

	public void Awake()
	{
		if (targetRenderer == null)
		{
			targetRenderer = GetComponent<MeshRenderer>();
			if (targetRenderer == null)
			{
				Debug.LogErrorFormat("AlphaFadeMaterial.cs -- Awake  -- No MeshRenderer on this object, not sure how this is possible as it is required, so you should probably check it out.");
				return;
			}
		}

		if (targetRenderer.material.shader != null && targetRenderer.material.shader.name != SHADER_NAME)
		{
			// If the shader isn't correct, try to find the right shader.
			targetRenderer.material.shader = ShaderCache.find(SHADER_NAME);
			if (targetRenderer.material.shader.name != SHADER_NAME)
			{
				// If we still can't, the bail and remove this script.
				Debug.LogErrorFormat("AlphaFadeMaterial.cs -- Update -- Incorrect Shader, please select the Unlit/AlphaUnlitDistanceMansk shader for this to work.");
				if (Application.isPlaying)
				{
					// Only destroy this if this is run-time
					Destroy(this);
				}
				return;
			}
		}

		Vector3 realPosition = transform.RealPosition();
		currentPosition = new Vector4(realPosition.x,
			realPosition.y,
			realPosition.z,
			1.0f);

		setFadePanelProperties();
		targetRenderer.material.SetVector("_PositionVector", currentPosition);
		targetRenderer.material.SetInt("_TextureHeight",Mathf.FloorToInt(transform.localScale.y));
		targetRenderer.material.SetInt("_TextureWidth", Mathf.FloorToInt(transform.localScale.x));
	}

	private void setFadePanelProperties()
	{
		if (fadePanelSizer != null)
		{

			Vector3 realRectPosition = fadePanelSizer.rectTransform.RealPosition();
			fadePanelSizerPosition = new Vector4(realRectPosition.x,
				realRectPosition.y,
				realRectPosition.z,
				1.0f);
			targetRenderer.material.SetVector("_AlphaRectCenter", fadePanelSizerPosition);
			targetRenderer.material.SetFloat("_AlphaRectWidth", fadePanelSizer.rect.size.x);
			targetRenderer.material.SetFloat("_AlphaRectHeight", fadePanelSizer.rect.size.y);			
		}

	}



	public void Update()
	{

#if UNITY_EDITOR
		setFadePanelProperties();
		targetRenderer.material.SetInt("_TextureHeight",Mathf.FloorToInt(transform.localScale.y));
		targetRenderer.material.SetInt("_TextureWidth", Mathf.FloorToInt(transform.localScale.x));
#endif
		
		realPosition = transform.RealPosition();		
		currentPosition.x = realPosition.x;
		currentPosition.y = realPosition.y;
		currentPosition.z = realPosition.z;
		targetRenderer.material.SetVector("_PositionVector", currentPosition);
	}
}