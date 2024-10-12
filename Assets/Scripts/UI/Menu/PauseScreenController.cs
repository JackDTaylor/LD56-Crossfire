using UnityEngine;

public class PauseScreenController : MonoBehaviour {
	void Update() {
		if(Input.GetKeyDown(KeyCode.Escape)) {
			GameManager.Get().ResumeGame();
		}
	}
}