using UnityEngine;

public class AntScaleDependentScale : MonoBehaviour {
	public Vector3 scaleFrom;
	public Vector3 scaleTo;

	AntController ant;

	void Start() {
		ant = GetComponentInParent<AntController>();
	}

	void Update() {
		var mult = ant.CalcNormalizedMultiplier();

		transform.localScale = new Vector3(
			Mathf.Lerp(scaleFrom.x, scaleTo.x, mult),
			Mathf.Lerp(scaleFrom.y, scaleTo.y, mult),
			Mathf.Lerp(scaleFrom.z, scaleTo.z, mult)
		);
	}
}