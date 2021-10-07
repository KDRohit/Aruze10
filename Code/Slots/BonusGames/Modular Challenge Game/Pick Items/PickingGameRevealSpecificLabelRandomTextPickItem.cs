using System.Collections.Generic;
using UnityEngine;

public class PickingGameRevealSpecificLabelRandomTextPickItem : PickingGameBasePickItemAccessor
{
    [SerializeField] private List<RevealTypeTextInfo> textInfos = new List<RevealTypeTextInfo>();
    private Dictionary<PickItemRevealType, RevealTypeTextInfo> textInfoDict = new Dictionary<PickItemRevealType, RevealTypeTextInfo>();

    public enum PickItemRevealType
    {
        None,
        Credits,
        CardPack,
        GainLadderRung,
        LoseLadderRung
    }

    protected override void Awake()
    {
        base.Awake();
        for (int i = 0; i < textInfos.Count; i++)
        {
            textInfoDict[textInfos[i].revealType] = textInfos[i];
        }
    }

    public void setText(PickItemRevealType revealType)
    {
        if (textInfoDict.TryGetValue(revealType, out RevealTypeTextInfo info))
        {
            int randomIndex = Random.Range(0, info.localizationKeys.Count - 1);
            info.label.text = Localize.text(info.localizationKeys[randomIndex]);
        }
    }
    
    [System.Serializable]
    protected class RevealTypeTextInfo
    {
        public PickItemRevealType revealType;
        public List<string> localizationKeys = new List<string>();
        public LabelWrapperComponent label;
    }
}
