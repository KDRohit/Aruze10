using UnityEngine;
using System.Collections;

public class LobbyOptionFeatureLabel : MonoBehaviour
{
	public UISprite featureLabel;

	public void SetActive(bool isActive)
	{
		this.gameObject.SetActive(isActive);
	}
}
