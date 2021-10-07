using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReevaluationBase
{
	public string type;
	public JSON[] outcomes;

	public ReevaluationBase(JSON reevalJSON)
	{
		type = reevalJSON.getString("type", "");
		outcomes = reevalJSON.getJsonArray("outcomes");
	}
}