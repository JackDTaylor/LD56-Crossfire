using UnityEngine;

public class PlayerCamera : MonoBehaviour {
	public float followSpeed = 0.025f;
	public float jumpThreshold = 0.25f;
	public float snapThreshold = 0.05f;
	public float unsnapThreshold = 5f;

	bool isSnapEnabled;

	Transform currentTarget;

	void Update() {
		if(currentTarget == null || currentTarget.GetComponent<AntController>().isDead) {
			currentTarget = FindSuitableTarget();
		}

		if(currentTarget == null) {
			return;
		}

		var distanceToTarget = ((Vector2)currentTarget.position - (Vector2)transform.position).magnitude;

		if(distanceToTarget < snapThreshold && !isSnapEnabled) {
			isSnapEnabled = true;
		}

		if(distanceToTarget > unsnapThreshold) {
			isSnapEnabled = false;
		}

		if(isSnapEnabled && distanceToTarget < jumpThreshold) {
			transform.position = new Vector3(currentTarget.position.x, currentTarget.position.y, transform.position.z);
			return;
		}

		transform.position = new Vector3(
			Mathf.Lerp(transform.position.x, currentTarget.position.x, followSpeed),
			Mathf.Lerp(transform.position.y, currentTarget.position.y, followSpeed),
			transform.position.z
		);
	}

	Transform FindSuitableTarget() {
		return GameManager.Get().GetPlayerAnt()?.transform;
	}
}