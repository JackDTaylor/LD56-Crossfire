using UnityEngine;

public class DebugUtilsManager : MonoBehaviour {
	public static DebugUtilsManager Get() => ManagerProvider.Get<DebugUtilsManager>();
	void Awake() => ManagerProvider.Register(this);
	
	public enum AiWalkMode { STAND, CIRCLES, DEFAULT };
	
	public Level startWithExistingLevel;

	public AiWalkMode aiWalkMode = AiWalkMode.DEFAULT;
	
	void Start() {
		if(startWithExistingLevel) {
			GameManager.Get().level = startWithExistingLevel;
			GameManager.Get().InitGame();
		}
	}
}