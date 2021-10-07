using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 *  Interface for objects that can be recycle
 */
public interface IRecycle
{

	//reset object to uninitailized state
	void reset();

	//initialize object
	void init(Dict args);
}
