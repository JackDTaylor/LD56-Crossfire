using System;
using System.Collections.Generic;
using UnityEngine;

public class Manager {
	static readonly Dictionary<Type, MonoBehaviour> RegisteredManagers = new Dictionary<Type, MonoBehaviour>();
	
	public static T Get<T>() where T: MonoBehaviour {
		return RegisteredManagers.GetValueOrDefault(typeof(T)) as T;
	}
	
	public static void Register<T>(T manager) where T: MonoBehaviour {
		Debug.Log($"Registered {typeof(T)}");
		RegisteredManagers.Add(typeof(T), manager);
	}
}