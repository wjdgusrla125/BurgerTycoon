using System;
using UnityEngine;

public class GrillingSound : MonoBehaviour
{
    [SerializeField]
    private SphereCollider sphereCollider;

    private void OnTriggerEnter(Collider other)
    {
	    //SoundManager.Instance.PlaySfx(SoundType.Grilling);
    }

    private void OnTriggerExit(Collider other)
    {
	    SoundManager.Instance.StopSfx(SoundType.Grilling);
    }
}
