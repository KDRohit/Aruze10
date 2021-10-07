using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LottoBlastExperiment : OrchestratorFeatureExperiment
{
	public string buyPageBannerPath { get; private set; }
	public string buffKeyname { get; private set; }
	public bool[] showTripleXpBuff { get; private set; }
	public int tripleXPDuration { get; private set; }
	public string package { get; private set; }
	
	public enum DialogCloseAction
	{
		NONE,
		EXTRA_DIALOG,
		BUY_PAGE
	}
	public DialogCloseAction dialogCloseAction { get; private set; }
	
	public LottoBlastExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		base.init(data);
		buyPageBannerPath = getEosVarWithDefault(data, "buy_page_header", "");
		buffKeyname = getEosVarWithDefault(data, "buff_key", "");
		tripleXPDuration = getEosVarWithDefault(data, "triple_xp_duration", 0);
		package = getEosVarWithDefault(data, "package_1", "");

		bool showOnPackage1 = getEosVarWithDefault(data, "package_1_triple_xp", false);
		bool showOnPackage2 = getEosVarWithDefault(data, "package_2_triple_xp", false);
		bool showOnPackage3 = getEosVarWithDefault(data, "package_3_triple_xp", false);
		bool showOnPackage4 = getEosVarWithDefault(data, "package_4_triple_xp", false);
		bool showOnPackage5 = getEosVarWithDefault(data, "package_5_triple_xp", false);
		bool showOnPackage6 = getEosVarWithDefault(data, "package_6_triple_xp", false);
		
		showTripleXpBuff = new[] {showOnPackage1, showOnPackage2, showOnPackage3, showOnPackage4, showOnPackage5, showOnPackage6};
		
		switch (getEosVarWithDefault(data, "closing_action", ""))
		{
			case "extra_dialog":
				dialogCloseAction = DialogCloseAction.EXTRA_DIALOG;
				break;
			case "buy_page":
				dialogCloseAction = DialogCloseAction.BUY_PAGE;
				break;
			default:
				dialogCloseAction = DialogCloseAction.NONE;
				break;
		}
	}
	
	public override void reset()
	{
		base.reset();
		buyPageBannerPath = "";
		buffKeyname = "";
		tripleXPDuration = 0;
		showTripleXpBuff = null;
		dialogCloseAction = DialogCloseAction.NONE;
		package = "";
	}
}
