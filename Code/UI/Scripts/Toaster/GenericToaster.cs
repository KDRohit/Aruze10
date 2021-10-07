using UnityEngine;
using System.Collections;
using TMPro;

public class GenericToaster : Toaster 
{
	public TextMeshPro textLabel;
	
	public override void init(ProtoToaster proto)
	{
		if (textLabel != null)
		{
			// The input text should be pre-localized, so don't localize here.
			textLabel.text = proto.args.getWithDefault(D.TITLE, "") as string;
		}
		base.init(proto);
	}
	
}
