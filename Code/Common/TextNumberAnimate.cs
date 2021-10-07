/* 
Lite class to empower animators to control value of TextMeshPro text via animation keyframes
*/

using UnityEngine;
using TMPro;	

public class TextNumberAnimate : MonoBehaviour
{
	public TextMeshPro textMesh;
	public float value;
	public int decimalPlaces;
	public int padLeftWidth;
	public char padLeftChar;
	public bool commasRemove;

	void Awake ()
	{
		if (textMesh == null)
		{
			textMesh = GetComponent<TextMeshPro>();
		}
	}

	void Update ()
	{
		decimalPlaces = Mathf.Max(decimalPlaces, 0);
		padLeftWidth = Mathf.Max(padLeftWidth, 0);

		if (padLeftWidth > 0 && padLeftChar == default(char))
		{
			padLeftChar = '0';
		}

		textMesh.text = value.ToString("n" + decimalPlaces).PadLeft(padLeftWidth, padLeftChar);

		if (commasRemove)
		{
			textMesh.text = textMesh.text.Replace(",", "");
		}
	}
}
