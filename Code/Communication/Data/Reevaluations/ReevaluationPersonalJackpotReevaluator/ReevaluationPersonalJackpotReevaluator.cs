using System.Collections.Generic;

//
// Reevaluation to deserialize and store "personal_jackpot_reevaluator" reevaluation. This
// is used by ProgressiveScatterJackpotsCallengeGameModule to extract data to be used in
// populating the person jackpot values when a challenge game loads.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Sep 9th 2019
//
// games : bettie02 - wheel
//
public class ReevaluationPersonalJackpotReevaluator : ReevaluationBase {

	public List<PersonalJackpot> personalJackpotList = new List<PersonalJackpot>();

	public ReevaluationPersonalJackpotReevaluator(JSON reevalJSON) : base(reevalJSON)
	{
		JSON allJackpotsJSON = reevalJSON.getJSON("jackpots");
		List<string> jackpotKeys = allJackpotsJSON.getKeyList();

		foreach (string jackpotKey in jackpotKeys)
		{
			PersonalJackpot personalJackpot = new PersonalJackpot();
			personalJackpot.name = jackpotKey;

			JSON jackpotJSON = allJackpotsJSON.getJSON(jackpotKey);
			personalJackpot.basePayout = jackpotJSON.getLong("base_payout", 0);

			JSON contributionJSON = jackpotJSON.getJSON("contributions");

			personalJackpot.contributionAmount = contributionJSON.getLong("amount", 0);
			personalJackpot.contributionBalance = contributionJSON.getLong("balance", 0);
			personalJackpotList.Add(personalJackpot);
		}
	}

	public class PersonalJackpot
	{
		public string name;
		public long basePayout;
		public long contributionAmount;
		public long contributionBalance;
	}
}
