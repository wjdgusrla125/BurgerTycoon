using UnityEngine;

public class BurgerPackPile : PileBase
{
    public void Awake()
	{
		_size = new Vector3(0.6f, 0.4f, 0.6f);
		_objectType = Define.EObjectType.BurgerPack;
	}
}
