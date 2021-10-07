using Com.Rewardables;

/*
 * Special pet treat you get through purchases. Automatically fills the pet's energy and puts the player's pet into
 * the hyper state for a duration based on the price of the package they purchased.
 */
public class RewardableSpecialPetEnergy : Rewardable
{
	public int hyperStateDuration { get; private set; }
	public const string TYPE = "virtual_pet_special_energy";
	/// <inheritdoc/>
	public override void init(JSON data)
	{
		base.init(data);
		hyperStateDuration = data.getInt("hyper_state_duration", 0);
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return TYPE; }
	}
}
