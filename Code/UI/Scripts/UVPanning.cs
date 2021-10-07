using System.Collections;
using UnityEngine;

public class UVPanning : MonoBehaviour
{
	private int text = 0;
	new public Renderer renderer; // We're hiding the renderer variable in the base class here, but relinking everything seems like a bad idea.

	public float constantSpeedX = 0; // In case you just want this to pan on its own
	public float constantSpeedY = 0;

	private Vector2 savedOffset;    // This is in UV coords, so it's a percentage of the whole texture. 0 is dont move 1 is move the entire thing, 
									// .5 is half, etc.

	void Start()
	{
		savedOffset = renderer.sharedMaterial.GetTextureOffset("_MainTex");
	}

	public void hardSetOffsetX(float newX)
	{
		savedOffset.x = newX;
		renderer.sharedMaterial.SetTextureOffset("_MainTex", savedOffset);
	}

	public void setOffset(float newOffset)
	{
		savedOffset.x = Mathf.Repeat(savedOffset.x + newOffset, 1);
		renderer.sharedMaterial.SetTextureOffset("_MainTex", savedOffset);
	}

	private void Update()
	{
		if (constantSpeedX != 0 || constantSpeedY != 0)
		{
			savedOffset.x += constantSpeedX;
			savedOffset.y += constantSpeedY;
			savedOffset.x = Mathf.Repeat(savedOffset.x, 1);
			savedOffset.y = Mathf.Repeat(savedOffset.y, 1);

			renderer.sharedMaterial.SetTextureOffset("_MainTex", savedOffset);
		}
	}
}