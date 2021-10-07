using UnityEngine;
using System.Collections;
using TMPro;

public class ProgressiveJackpotToaster : SocialToaster 
{
	public TextMeshPro poolAmountLabel;
	public TextMeshPro playerNameLabel;
	public ParticleSystem coins;
	
	protected ProgressiveJackpot jackpot = null;
	protected string fbId = "";
	
	public override void init(ProtoToaster proto)
	{
		base.init(proto);
		JSON data = proto.args.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
		
		string jpKey = data.getString("jackpot_key", "");
		
		jackpot = ProgressiveJackpot.find(jpKey);

		if (jackpot == null)
		{
			Debug.LogError("ProgressiveJackpotToaster: jackpot_key is invalid: " + jpKey);
		}
		else
		{
			long creditAmount = data.getLong("credits", 0L);
			if (creditAmount <= 0)
			{
				creditAmount = (long)proto.args.getWithDefault(D.TOTAL_CREDITS, 0L);
			}

			poolAmountLabel.text = CreditsEconomy.convertCredits(creditAmount);
			string prefix = Data.liveData.getBool("USE_TOASTER_PACKAGE", false) ? "WINNER: " : "";
			string name = "";

			if (proto.member == null)
			{
				fbId = data.getString("fbid", "");			
				if (fbId != "")
				{
					name = Localize.toUpper(CommonText.firstNameLastInitial(data.getString("first_name", ""), data.getString("last_name", "")));
				}
				else
				{
					// No user info, so it must have been an anonymous user.
					name = FakeNameGenerator.getFakeNameLastLetter() + ".";
				}	
			}
			else
			{
				name = proto.member.firstNameLastInitial;
			}
			playerNameLabel.text = prefix + name;
		}
	}

	protected override void onImageSet(bool didSucceed)
	{
		runImageSetLogic(didSucceed, true);
	}

	protected void runImageSetLogic(bool didSucceed, bool runBaseFunction)
	{
		if (this == null)
		{
			Debug.LogErrorFormat("ProgressiveJackpotToaster.cs -- onImageSet -- this is null!!!");
			return;
		}

		if (runBaseFunction)
		{
			base.onImageSet(didSucceed);	
		}
		
		if (GameState.game != null && jackpot != null && jackpot.game != null)
		{
			if (jackpot.game == GameState.game)
			{
				// We are currently playing the same game that the other player won the jackpot for!
				Audio.play("ProgressiveJackpotBell", 1.0f);
			}
			else
			{
				// We are currently playing a different game than the winner of the jackpot.
				Audio.play("ProgressiveJackpotBell", Random.Range(0.2f, 0.5f));
			}
		}
		else
		{
			// We are currently not playing a game (in the lobby most likely).
			Audio.play("ProgressiveJackpotBell", Random.Range(0.5f, 0.8f));
		}	
	}
	
	// Per-frame callback for animateFade().
	protected override void updateAnimateFade(float alpha)
	{
		base.updateAnimateFade(alpha);
		
		poolAmountLabel.alpha = alpha;
		playerNameLabel.alpha = alpha;		
	}
}
