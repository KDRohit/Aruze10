using Com.Rewardables;

public class RewardablePetBasicEnergy : Rewardable
{
	public bool hyperReached { get; private set; }
	public long amount { get; private set; }
	public JSON petStatus { get; protected set; }
	public const string TYPE = "virtual_pet_basic_energy";
	/// <inheritdoc/>
	public override void init(JSON data)
	{
		base.init(data);
		petStatus = data;
		amount = data.getLong("energy_gained", 0);
		hyperReached = data.getBool("hyper_reached", false);
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return TYPE; }
	}
}
