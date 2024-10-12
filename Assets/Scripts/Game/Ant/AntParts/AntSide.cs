using UnityEngine;

public class AntSide : MonoBehaviour
{
	public bool isDead;
	public bool isEating;

	public Transform head;

	public Transform liveEye;
	public Transform deadEye;

	public float opacity = 1.0f;

	public float eatDuration = 0.4f;

	bool isEatingInProgress = false;
	float eatingTimeLeft = 0;

	void Update() {
		if(isDead) {
			liveEye.gameObject.SetActive(false);
			deadEye.gameObject.SetActive(true);
		} else {
			liveEye.gameObject.SetActive(true);
			deadEye.gameObject.SetActive(false);
		}

		UpdateOpacity();

		HandleEating();
	}

	Color ApplyOpacity(Color c) {
		return new Color(c.r, c.g, c.b, opacity);
	}

	void UpdateOpacity() {
		foreach(var r in GetComponentsInChildren<SpriteRenderer>()) {
			r.color = ApplyOpacity(r.color);
		}

		foreach(var r in GetComponentsInChildren<LineRenderer>()) {
			r.startColor = ApplyOpacity(r.startColor);
			r.endColor = ApplyOpacity(r.endColor);
		}
	}

	void HandleEating() {
		if(isDead) {
			return;
		}

		if(isEating && !isEatingInProgress) {
			isEatingInProgress = true;
			eatingTimeLeft = eatDuration;
		}

		if(isEatingInProgress) {
			if(eatingTimeLeft > 0) {
				var offset = eatDuration - 2f * Mathf.Abs(eatDuration * (eatingTimeLeft/eatDuration) - eatDuration/2);

				head.localPosition = Vector3.up * (3f + offset * 0.5f);
			} else if(isEating) {
				eatingTimeLeft = eatDuration;
			} else {
				head.localPosition = Vector3.up * 3f;
				isEatingInProgress = false;
			}
		}

		eatingTimeLeft -= Time.deltaTime;
	}
}