using UnityEngine;
using System.Collections;

public class Gen09PickGameButton : PickGameButton 
{
	public GameObject celebration = null;
	public GameObject revealCharacterObj = null;

	public IEnumerator celebrate()
	{
		yield return new WaitForSeconds(2.0f);
		celebration.SetActive(true);
	}
	
	/// Gray out the character reveal renderer by changing shaders
	public void grayOutRevealCharacter()
	{
		MeshRenderer[] characterRenderers = revealCharacterObj.GetComponentsInChildren<MeshRenderer>(true);

		foreach (MeshRenderer meshRenderer in characterRenderers)
		{
			meshRenderer.material.shader = ShaderCache.find("Unlit/GUI Texture Monochrome");
		}
	}
}
