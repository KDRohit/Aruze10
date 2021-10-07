using UnityEngine;

public class SpinPanelV2Experiment : EosExperiment
{
	public float autoSpinHoldDuration { get; private set; }
	public float autoSpinTextCycleTime { get; private set; }
	public bool autoSpinOptions { get; private set; }
	private string autoSpinOptionsCount;

	public SpinPanelV2Experiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		autoSpinHoldDuration = getEosVarWithDefault (data, "auto_spin_hold_time", 3.0f);
		autoSpinTextCycleTime = getEosVarWithDefault (data, "auto_spin_text_time", 5.0f);
		autoSpinOptions = getEosVarWithDefault(data, "auto_spin_options", false);
		autoSpinOptionsCount = getEosVarWithDefault(data, "auto_spin_options_counts", "");
	}

	public int[] getAutoSpinOptionsCount()
	{
		if (string.IsNullOrEmpty(autoSpinOptionsCount))
		{
			return new int[0];	
		}
		
		string[] spinOptions = autoSpinOptionsCount.Split(',');
		int[] result = new int[spinOptions.Length];
		for (int i = 0; i < spinOptions.Length; i++)
		{
			if (!int.TryParse(spinOptions[i], out result[i]))
			{
				Debug.LogWarningFormat("Failed to parse int from {0}. Using default auto spin options", spinOptions[i]);
				return new int[0];
			}
		}

		return result;
	}

	public override void reset()
	{
		base.reset();
		autoSpinHoldDuration = 3.0f;
		autoSpinTextCycleTime = 5.0f;
		autoSpinOptions = false;
	}
}
