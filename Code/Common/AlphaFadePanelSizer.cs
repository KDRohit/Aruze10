using UnityEngine;

public class AlphaFadePanelSizer : MonoBehaviour
{
	public RectTransform rectTransform;

	public Rect rect
	{
		get
		{
			return rectTransform.rect;

		}
	}
	
	void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;

		Gizmos.DrawWireCube(transform.position, new Vector3(rect.size.x * transform.root.localScale.x, rect.size.y * transform.root.localScale.y, 1.0f));
	}
}