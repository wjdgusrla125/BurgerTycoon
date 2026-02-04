using UnityEditorInternal;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static bool _init = false;
	private static T _instance;

	public static T Instance
	{
		get
		{
			if (_instance == null && _init == false)
			{
				_init = true;
				_instance = (T)GameObject.FindAnyObjectByType(typeof(T));

				if (_instance == null)
				{
					GameObject go = new GameObject(); // typeof(T).Name, typeof(T)
					T t = go.AddComponent<T>();
					t.name = $"@{typeof(T).Name}";

					_instance = t;
				}
			}

			return _instance;
		}
	}
}
