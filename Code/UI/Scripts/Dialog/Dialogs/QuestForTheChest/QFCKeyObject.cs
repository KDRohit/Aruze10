using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace QuestForTheChest
{
	public class QFCKeyObject : MonoBehaviour
	{
		[SerializeField] private List<GameObject> keyItems;
		[SerializeField] private MultiLabelWrapperComponent keyLabels;

		public void setupKeyObjects()
		{
			//attempt to get keys from label
			if (keyLabels == null)
			{
				return;
			}

			int numKeys = 0;
			if (int.TryParse(keyLabels.text, out numKeys))
			{
				setupKeyObjects(numKeys);
			}
		}
		public void setupKeyObjects(int numKeys)
		{
			if (keyLabels != null)
			{
				keyLabels.text = 0 == numKeys ? "?" : CommonText.formatNumber(numKeys);
			}

			if (numKeys <= 0)
			{
				for (int i = 0; i < keyItems.Count; ++i)
				{
					keyItems[i].SetActive(i == (keyItems.Count - 1));
				}
			}
			else
			{
				for (int i = 0; i < keyItems.Count; ++i)
				{
					if (i == (numKeys - 1) || (numKeys >= keyItems.Count && i == (keyItems.Count-1)))
					{
						keyItems[i].SetActive(true);
					}
					else
					{
						keyItems[i].SetActive(false);
					}
				}
			}

		}

	}

}