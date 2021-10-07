using System;

// SymbolDisplayInfo - each game can optionally define an array of these for display in the paytable dialog.
public class SymbolDisplayInfo
{
	public string keyName;
	public string name;
	public string description;
	public bool isPaytableSymbol;		// Whether this symbol should appear in the more generic symbol list in the dialog.
	public bool isPaytableSpecial;		// Whether this symbol should appear in the more strongly highlighted "special" display section.
	
	public SymbolDisplayInfo (JSON data)
	{
		keyName = data.getString("symbol", "");
		name = data.getString("name", "");
		description = data.getString("description", "");
		
		isPaytableSymbol = data.getBool("is_paytable_symbol", false);
		isPaytableSpecial = data.getBool("is_paytable_special", false);
	}
}
