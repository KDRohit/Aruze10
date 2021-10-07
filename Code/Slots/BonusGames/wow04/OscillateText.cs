using UnityEngine;
using System.Collections;

public class OscillateText : TICoroutineMonoBehaviour
{
	//Upon starting, put this object starting towards the x location 80 to the left
	void Start()
	{
		Hashtable args = new Hashtable();
		args.Add("looptype","pingpong");
		args.Add("isLocal",true);
		args.Add("position",new Vector3(this.transform.localPosition.x - 80f, this.transform.localPosition.y, this.transform.localPosition.z));
		iTween.MoveTo(this.gameObject,args);
	}
}

