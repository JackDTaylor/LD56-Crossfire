using System;
using System.Collections.Generic;
using UnityEngine;

public class ManagerProvider {
	static readonly Dictionary<Type, MonoBehaviour> RegisteredManagers = new Dictionary<Type, MonoBehaviour>();
	
	public static T Get<T>() where T: MonoBehaviour {
		return RegisteredManagers.GetValueOrDefault(typeof(T)) as T;
	}
	
	public static void Register<T>(T manager) where T: MonoBehaviour {
		RegisteredManagers.Add(typeof(T), manager);
	}
}