using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ObjectSpawn : MonoBehaviour
{
	[Header("바운스 효과 설정")]
	[SerializeField] private float duration = 0.5f;               // 애니메이션 시간
	[SerializeField] private float bounceScale = 1.2f;            // 최대 크기
    
	private Vector3 originalScale;
    
	void Start()
	{
		originalScale = transform.localScale;
		PlayBounceEffect();
	}
    
	void OnEnable()
	{
		if (originalScale != Vector3.zero)
			PlayBounceEffect();
	}
    
	public void PlayBounceEffect()
	{
		transform.DOKill();
		transform.localScale = Vector3.zero;
        
		transform.DOScale(originalScale * bounceScale, duration * 0.5f)
			.SetEase(Ease.OutBack)
			.OnComplete(() => {
				transform.DOScale(originalScale, duration * 0.5f)
					.SetEase(Ease.OutBounce);
			});
	}
    
	void OnDestroy()
	{
		transform.DOKill();
	}
}