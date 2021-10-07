using UnityEngine;

public class DisableGameObjectOnAwake : MonoBehaviour
{
	private void Awake()
	{
		gameObject.SetActive(false);
	}
}
