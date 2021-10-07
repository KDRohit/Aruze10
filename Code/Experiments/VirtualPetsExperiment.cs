using System.Collections.Generic;

namespace Com.HitItRich.Feature.VirtualPets
{
	public class VirtualPetsExperiment : EosVideoExperiment
	{
		public SortedDictionary<int, string> specialTreatPrices { get; private set; }
		public string[] treatOrder { get; private set; }
		
		public int idleTime { get; private set; }
		public VirtualPetsExperiment(string name) : base(name)
		{
			
		}
		
		protected override void init(JSON data)
		{
			base.init(data);
			string specialTreatsJsonStr = getEosVarWithDefault(data, "special_treats_price_points", null);
			JSON treatsJson = new JSON(specialTreatsJsonStr);
			
			specialTreatPrices = new SortedDictionary<int, string>();
			List<string> keys = treatsJson.getKeyList();
			for (int i = 0; i < keys.Count; i++)
			{
				string packageKey = treatsJson.getString(keys[i], "");
				PurchasablePackage package = PurchasablePackage.find(packageKey);
				if (package != null)
				{
					specialTreatPrices.Add(package.priceTier, keys[i]);
				}
			}

			string csvTreatOrder = getEosVarWithDefault(data, "treat_order", "");
			treatOrder = csvTreatOrder.Split(',');
			idleTime = getEosVarWithDefault(data, "idle_time_in_minutes", 10);
			
		}
	}    
}


