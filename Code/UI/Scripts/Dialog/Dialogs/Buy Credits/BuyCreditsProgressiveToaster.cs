using UnityEngine;
using System.Collections;
using TMPro;

public class BuyCreditsProgressiveToaster : Toaster
{
	public TextMeshPro poolAmountLabel;
	public ParticleSystem coins;

	public override void init(ProtoToaster proto)
	{
		gameObject.SetActive(true);

		long credits = (long)proto.args.getWithDefault(D.TOTAL_CREDITS, 0L);

		poolAmountLabel.text = CreditsEconomy.convertCredits(credits);

		Audio.play("ProgressiveJackpotBell", 1.0f);

		introAnimation();
	}
}
