using UnityEngine;
using System.Collections;
using TMPro;

public class TicketTumblerToaster : SocialToaster 
{
	public TextMeshPro prizeAmount;
	public Renderer playerIcon;
	public TextMeshPro playerNameLabel;
	private string fbId = "";
	
	public override void init(ProtoToaster proto)
	{
		base.init(proto);

		SafeSet.labelText(prizeAmount, CreditsEconomy.convertCredits((long)proto.args.getWithDefault(D.VALUE, 0L)));
	}
}