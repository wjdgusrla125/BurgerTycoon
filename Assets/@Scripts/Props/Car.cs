using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
	public List<GameObject> cars = new List<GameObject>();
    
	private void Start()
	{
		ActivateRandomCar();
	}
    
	private void ActivateRandomCar()
	{
		if (cars.Count == 0)
			return;
        
		// 모든 차량 비활성화
		foreach (GameObject car in cars)
		{
			if (car != null)
				car.SetActive(false);
		}
        
		// 랜덤으로 1개 선택하여 활성화
		int randomIndex = Random.Range(0, cars.Count);
		if (cars[randomIndex] != null)
			cars[randomIndex].SetActive(true);
	}
}