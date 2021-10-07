public class DialogTransitionExperiment : EosExperiment
{
	public float animInTime { get; private set; }
	public float animOutTime { get; private set; }

	public Dialog.AnimPos slideInFrom { get; private set; }
	public Dialog.AnimScale scaleInFrom { get; private set; }
	public Dialog.AnimEase animInEaseType { get; private set; }

	public Dialog.AnimPos slideOutTo { get; private set; }
	public Dialog.AnimScale scaleOutTo { get; private set; }
	public Dialog.AnimEase animOutEaseType { get; private set; }

	public DialogTransitionExperiment(string experimentName) : base (experimentName)
	{
	}

	protected override void init(JSON data)
	{
		animInTime = getEosVarWithDefault(data, "anim_in_time", 0.25f);
		animOutTime = getEosVarWithDefault(data, "anim_out_time", 0.25f);
		slideInFrom = getPosition(getEosVarWithDefault(data, "slide_in_from", "TOP"));
		scaleInFrom = getScale(getEosVarWithDefault(data, "scale_in_from", "BACK"));
		animInEaseType = getEase(getEosVarWithDefault(data, "anim_in_ease_type", "BACK"));
		slideOutTo = getPosition(getEosVarWithDefault(data, "slide_out_to", "BOTTOM"));
		scaleOutTo = getScale(getEosVarWithDefault(data, "scale_out_to", "BACK"));
		animOutEaseType = getEase(getEosVarWithDefault(data, "anim_out_ease_type", "BACK"));

	}

	public override void reset()
	{
		animInTime = 0.25f;
		animOutTime = 0.25f;
		slideInFrom = Dialog.AnimPos.TOP;
		scaleInFrom = Dialog.AnimScale.FULL;
		animInEaseType = Dialog.AnimEase.BACK;
		slideOutTo = Dialog.AnimPos.BOTTOM;
		scaleOutTo = Dialog.AnimScale.FULL;
		animOutEaseType = Dialog.AnimEase.BACK;
	}

	private static Dialog.AnimPos getPosition(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return Dialog.AnimPos.TOP;
		}

		switch (data.Trim().ToUpper())
		{
			case "TOP":
				return Dialog.AnimPos.TOP;
			case "CENTER":
				return Dialog.AnimPos.CENTER;
			case "BOTTOM":
				return Dialog.AnimPos.BOTTOM;
			case "LEFT":
				return Dialog.AnimPos.LEFT;
			case "RIGHT":
				return Dialog.AnimPos.RIGHT;

			default:
				return Dialog.AnimPos.TOP;
		}
	}

	private static Dialog.AnimScale getScale(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return Dialog.AnimScale.FULL;
		}

		switch (data.Trim().ToUpper())
		{
			case "FULL":
				return Dialog.AnimScale.FULL;
			case "SMALL":
				return Dialog.AnimScale.SMALL;

			default:
				return Dialog.AnimScale.FULL;
		}
	}

	private static Dialog.AnimEase getEase(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return Dialog.AnimEase.BACK;
		}

		switch (data.Trim().ToUpper())
		{
			case "BACK":
				return Dialog.AnimEase.BACK;
			case "BOUNCE":
				return Dialog.AnimEase.BOUNCE;
			case "SMOOTH":
				return Dialog.AnimEase.SMOOTH;
			case "ELASTIC":
				return Dialog.AnimEase.ELASTIC;

			default:
				return Dialog.AnimEase.BACK;
		}
	}
}
