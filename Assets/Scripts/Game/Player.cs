using UnityEngine;

public class Player : MonoBehaviour {
	public enum Type { HUMAN, NETWORK, AI }
	
	public Type type;
	public string playerName;

	public void BeforeDestroy() {
		// Do nothing
		var a = this;
	}
}