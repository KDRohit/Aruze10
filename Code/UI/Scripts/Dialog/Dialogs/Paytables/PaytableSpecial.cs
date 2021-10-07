using UnityEngine;
using System.Collections;
using TMPro;

public class PaytableSpecial : TICoroutineMonoBehaviour
{
	public TextMeshPro title;
	public TextMeshPro description;
	public UITexture symbolSlot;

	public void init(SymbolDisplayInfo info)
	{
		this.title.text = info.name;
		this.description.text = info.description;
	}

	public void hide()
	{
		this.gameObject.SetActive(false);
	}

}
