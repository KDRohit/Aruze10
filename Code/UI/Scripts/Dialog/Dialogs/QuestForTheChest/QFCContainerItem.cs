
using TMPro;
using UnityEngine;

namespace QuestForTheChest
{
	public class QFCContainerItem : MonoBehaviour
	{
		public MultiLabelWrapperComponent label;
		public Transform anchor;

		public virtual void init(string labelText)
		{
			if (label != null)
			{
				label.text = labelText;
			}
		}
	}
}

