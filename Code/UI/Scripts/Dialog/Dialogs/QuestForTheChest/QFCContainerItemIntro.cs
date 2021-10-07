using UnityEngine;

namespace QuestForTheChest
{
	public class QFCContainerItemIntro : QFCContainerItem
	{
		[SerializeField] private GameObject keyPrefab;
		[SerializeField] private Transform keyOverlayParent;

		public void init(string text, int keyNum)
		{
			QFCKeyObject key = NGUITools.AddChild(keyOverlayParent, keyPrefab).GetComponent<QFCKeyObject>();
			init(text);
			key.setupKeyObjects(keyNum);
		}
	}

}