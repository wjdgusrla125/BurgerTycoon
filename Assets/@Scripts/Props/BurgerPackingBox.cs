using UnityEngine;

public class BurgerPackingBox : MonoBehaviour
{
    public BurgerPile _burgerPile;
    public bool IsFull => (_burgerPile.ObjectCount >= Define.PACKING_BOX_MAX_BURGER_COUNT);
}
