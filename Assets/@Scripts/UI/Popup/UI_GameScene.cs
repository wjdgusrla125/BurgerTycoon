using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using DG.Tweening;
using System;

public class UI_GameScene : MonoBehaviour
{
	[SerializeField]
	TextMeshProUGUI _moneyCountText;

	[SerializeField]
	TextMeshProUGUI _toastMessageText;
	
	[SerializeField]
	Slider _expSlider;
	
	[SerializeField]
	TextMeshProUGUI _levelText;
	
	[SerializeField]
	private GameObject _starPrefab;
	
	[SerializeField]
	private RectTransform _canvasTransform;
	
	[SerializeField]
	private RectTransform _targetUITransform;
	
	[SerializeField]
	private float _spreadRadius = 100f;

	private void OnEnable()
	{
		RefreshUI();
		RefreshExpUI();
		GameManager.Instance.AddEventListener(EEventType.MoneyChanged, RefreshUI);
		GameManager.Instance.AddEventListener(EEventType.ExpChanged, RefreshExpUI);
	}

	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.MoneyChanged, RefreshUI);
		GameManager.Instance.RemoveEventListener(EEventType.ExpChanged, RefreshExpUI);
	}

	public void RefreshUI()
	{
		long money = GameManager.Instance.Money;
		_moneyCountText.text = Utils.GetMoneyText(money);
	}
	
	public void RefreshExpUI()
	{
		_expSlider.value = GameManager.Instance.CurrentExp / GameManager.Instance.GetMaxExp();
		_levelText.text = GameManager.Instance.Level.ToString();
	}

	public void SetToastMessage(string message)
	{
		_toastMessageText.text = message;
		_toastMessageText.enabled = (string.IsNullOrEmpty(message) == false);
	}
	
	public void StarEffectFromWorldSpace(Vector3 worldPosition, Action onComplete = null)
	{
		Vector2 startUIPos;
		
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_canvasTransform, Camera.main.WorldToScreenPoint(worldPosition), null, out startUIPos
		);
		
		Vector2 targetUIPos = new Vector2(-260f, 794f);

		for (int i = 0; i < 3; i++)
		{
			GameObject star = Instantiate(_starPrefab, _canvasTransform);
			RectTransform starRect = star.GetComponent<RectTransform>();

			starRect.anchoredPosition = startUIPos;
			starRect.localScale = Vector3.zero;

			Vector2 randomOffset = UnityEngine.Random.insideUnitCircle.normalized * _spreadRadius;

			Sequence seq = DOTween.Sequence();
			seq.Append(starRect.DOAnchorPos(startUIPos + randomOffset, 0.3f).SetEase(Ease.OutBack));
			seq.Join(starRect.DOScale(1.2f, 0.3f).From(0f));

			seq.AppendInterval(0.2f);

			seq.Append(starRect.DOAnchorPos(targetUIPos, 0.8f).SetEase(Ease.InQuad));
			seq.OnComplete(() =>
			{
				Destroy(star);
				onComplete?.Invoke();
			});
		}
	}
}
