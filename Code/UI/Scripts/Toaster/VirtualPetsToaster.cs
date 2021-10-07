using TMPro;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

namespace Com.HitItRich.Feature.VirtualPets
{
    public class VirtualPetsToaster : Toaster
    {
        [SerializeField] private TextMeshPro bodyLabel;

        public override void init(ProtoToaster proto)
        {
            if (null != proto.args)
            {
                if (bodyLabel != null)
                {
                    bodyLabel.text = (string) proto.args.getWithDefault(D.TITLE, "");
                }
                
                long coinReward = (long) proto.args.getWithDefault(D.AMOUNT, 0);
                if (coinReward > 0)
                {
                    SlotsPlayer.addFeatureCredits(coinReward, VirtualPetsFeature.PET_FEED_PENDING_CREDIT_SOURCE);
                }
            }
            base.init(proto);
        }
    }
}