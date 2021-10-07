using System.Collections.Generic;

public class ReevaluationFreespinMeter : ReevaluationBase
{
	public FreespinMeterBonusData bonus;
	public List<FreespinMeterData> meters;
	public List<FreespinMeterValue> meterValues;

	public ReevaluationFreespinMeter(JSON reevalJSON) : base(reevalJSON)
	{
		bonus = new FreespinMeterBonusData(reevalJSON.getJSON("bonus"));

		JSON[] metersJSONArray = reevalJSON.getJsonArray("meters", true);
		if (metersJSONArray != null && metersJSONArray.Length > 0)
		{
			meters = new List<FreespinMeterData>();
			foreach (JSON meterJSON in metersJSONArray)
			{
				meters.Add(new FreespinMeterData(meterJSON));
			}
		}

		JSON[] meterValuesJSONArray = reevalJSON.getJsonArray("meter_values", true);
		if (meterValuesJSONArray != null && meterValuesJSONArray.Length > 0)
		{
			meterValues = new List<FreespinMeterValue>();
			foreach (JSON meterValueJSON in meterValuesJSONArray)
			{
				meterValues.Add(new FreespinMeterValue(meterValueJSON));
			}
		}
	}
}

