using UnityEngine;

namespace QuestForTheChest
{
    public class QFCRewardItemKeys : QFCContainerItem
    {
        private const string QFC_FOUND_KEY = "qfc_reward_key";
        private const string QFC_FOUND_KEYS = "qfc_reward_keys";
        
        [SerializeField] private GameObject keyPrefab;
        [SerializeField] private Transform keyOverlayParent;
        [System.NonSerialized] public QFCKeyOverlay keyOverlay;
        public void init(int keysAmount)
        {
           init(keysAmount == 1 ? Localize.text(QFC_FOUND_KEY, keysAmount): Localize.text(QFC_FOUND_KEYS, keysAmount));
           if (keyOverlay == null)
           {
	           GameObject obj = NGUITools.AddChild(keyOverlayParent, keyPrefab);
	           if (obj != null)
	           {
		           keyOverlay = obj.GetComponent<QFCKeyOverlay>();
	           }
           }

           if (keyOverlay != null)
           {
               keyOverlay.initStaticRewardItem(keysAmount);
           }
        }
    }
}