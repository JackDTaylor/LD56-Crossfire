using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class AntController : MonoBehaviour {
	static readonly int AnimationTime = Animator.StringToHash("AnimationTime");

	const float AI_UPDATE_INTERVAL = 1f;
	const int MOVE_FRAME_COUNT = 120;
	const float EAT_SLOW_DOWN = 5f;
	const float BITE_COOLDOWN = 0.4f;
	const float REVERSE_MODE_COOLDOWN = 5f;
	const float DEATH_DECAY = 5f;
	const int STORED_RIGIDBODY_POSITIONS = 20 * 3; // 20 fixed updates per second, save for 5 seconds

	public int level = 1;
	public int health = 1;

	public Transform body;
	public Transform levelLabel;
	public TextMeshPro levelText;

	public Transform healthBarContainer;
	public Transform healthBar;
	public TextMeshPro healthBarText;

	public AntMouth mouth;

	public Player player;
	
	public bool isPlayerControlled => player != null;

	public bool isDead;
	public bool isEating;
	public bool isInvincible = true;

	public MeleeWeapon knife;
	public MeleeWeapon sabre;
	public RangeWeapon pistol;
	public RangeWeapon turret1;
	public RangeWeapon turret2;

	public bool hasKnife;
	public bool hasSabre;
	public bool hasPistol;
	public bool hasTurret1;
	public bool hasTurret2;

	public bool scaleImmediately;

	public AntSide[] sides;

	public Vector2 moveSpeed = new Vector2(60f, 30f);
	public Vector2 rotateSpeed = new Vector2(300f, 30f);

	public float moveTimeToAnimFrameScale = 0.5f;

	public float maxScaleChangePerFrame = 0.05f;

	public bool isReverseMode;
	public float reverseModeCooldown;
	public float reverseModeTimeLeft;
	
	public bool showAiDebugOverlay;
	
	public float hitAnimationSize = 0.2f;
	public float hitAnimationTime = 0.15f;

	public float timeBetweenBleeds = 3.5f;

	// Protected
	Animator animator;
	Rigidbody2D rigidbody2d;

	Vector2 prevFramePosition;

	float timeToAIUpdate;

	float desiredMove;
	float desiredRotation;

	float moveFrame;
	
	float hitAnimationState;

	float eatCooldown;
	float decayCooldown = DEATH_DECAY;
	
	float invincibilityTime;

	float birthStatsMultiplier = 1f;
	
	float timeToNextBleed;

	readonly Queue<float> lastMovements = new Queue<float>(STORED_RIGIDBODY_POSITIONS + 1);
	Vector2 lastRigidbodyPosition;


	// AI stuff
	public AIStrategy aiStrategy;

	AIStrategyResult.Action aiCurrentAction = AIStrategyResult.Action.EXPLORE;
	Vector2 aiTarget;
	AntController aiTargetAnt;

	Collection<AntController> visibleAnts = new Collection<AntController>();
	
	Collection<Vector2>[] debugAIExplorePoints = new Collection<Vector2>[6];

	void Start() {
		body.localScale = Vector3.zero;

		animator = GetComponent<Animator>();
		rigidbody2d = GetComponent<Rigidbody2D>();

		prevFramePosition = transform.position;
		lastRigidbodyPosition = rigidbody2d.position;

		// Do not mess with the player
		birthStatsMultiplier = isPlayerControlled ? 1f : Random.Range(0.85f, 1.05f);
	}

	public void Init() {
		invincibilityTime = GameManager.Get().spawnInvincibilityTime;

		health = CalcMaxHealth();
	}

	void Update() {
		isInvincible = invincibilityTime > 0;

		if(isInvincible) {
			invincibilityTime -= Time.deltaTime;
		}

		foreach(var side in sides) {
			var intensity = 0.25f;
			var offset = 0.25f;
			var frequency = 6f;

			// https://www.desmos.com/calculator/siwdz84ecf
			var opacity = (1f - intensity) + intensity * Mathf.Sin((invincibilityTime - offset) * frequency * Mathf.PI);

			side.opacity = isInvincible ? opacity : 1f;
		}

		if(!isDead) {
			if(isPlayerControlled) {
				UpdatePlayerControls();
			} else {
				UpdateAIControls();
			}

			UpdateEating();
		} else {
			decayCooldown -= Time.deltaTime;

			if(decayCooldown < 0) {
				DestroyBody();
				return;
			}
		}

		UpdateGuns();
		UpdateGfx();
	}

	void FixedUpdate() {
		UpdatePosition();
	}

	#region # Calculation functions
	float CalcMoveSpeed() {
		// 60..30
		var result = Mathf.Lerp(moveSpeed.x, moveSpeed.y, CalcNormalizedMultiplier());

		if(isEating) {
			result /= 4;
		}

		return result * birthStatsMultiplier;
	}

	float CalcRotateSpeed() {
		// 300..30
		return Mathf.Lerp(rotateSpeed.x, rotateSpeed.y, CalcNormalizedMultiplier()) * birthStatsMultiplier;
	}

	public float CalcNormalizedMultiplier() {
		// 0..1
		return (CalcScale() - 0.5f) / (3.15f - 0.5f);
	}

	public float CalcScale() {
		// 0.5..3.15
		// https://www.desmos.com/calculator/zpktmi9eoe
		return 0.15f + 3f / (1f + Mathf.Exp(-0.02f * (level - 100)));
	}

	int CalcMaxHealth() {
		// https://www.desmos.com/calculator/lr36tsigpc
		return Mathf.RoundToInt(Mathf.Pow(level, 1.25f));
	}

	int CalcMaxBiteSize() {
		// https://www.desmos.com/calculator/7vhr5pt1gx
		return Mathf.RoundToInt(Mathf.Max(1f, Mathf.Sqrt(level)));
	}


	#endregion
	
	#region # Update methods

	void UpdateGuns() {
		knife.gameObject.SetActive(hasKnife);
		sabre.gameObject.SetActive(hasSabre);
		pistol.gameObject.SetActive(hasPistol);
		turret1.gameObject.SetActive(hasTurret1);
		turret2.gameObject.SetActive(hasTurret2);
	}
	
	void UpdateCheats() {
		if(Input.GetKeyDown(KeyCode.F1)) {
			hasPistol = !hasPistol;
		}
		
		if(Input.GetKeyDown(KeyCode.F2)) {
			hasTurret1 = false;
			hasTurret2 = !hasTurret2;
		}
		
		if(Input.GetKeyDown(KeyCode.F4)) {
			level = 1;
			health = CalcMaxHealth();
		}

		if(Input.GetKeyDown(KeyCode.F5)) {
			level -= 1;
			health = CalcMaxHealth();
		}

		if(Input.GetKeyDown(KeyCode.F6)) {
			level += 1;
			health = CalcMaxHealth();
		}

		if(Input.GetKeyDown(KeyCode.F7)) {
			level += 50;
			health = CalcMaxHealth();
		}
	}

	void UpdateScale() {
		var targetScale = CalcScale();
		var currentScale = body.localScale.x;
		
		hitAnimationState -= Time.deltaTime / hitAnimationTime;
		
		if(hitAnimationState < 0) {
			hitAnimationState = 0f;
		}
		
		var additionalAnimScale = hitAnimationState * hitAnimationSize;

		if(scaleImmediately) {
			body.localScale = Vector3.one * (targetScale + additionalAnimScale);
			return;
		}

		if(Mathf.Abs(targetScale - currentScale) > maxScaleChangePerFrame) {
			currentScale += Mathf.Sign(targetScale - currentScale) * maxScaleChangePerFrame;
		} else {
			currentScale = targetScale;
		}

		body.localScale = Vector3.one * (currentScale + additionalAnimScale);
	}

	void UpdateGfx() {
		UpdateScale();

		// Level label
		levelText.text = level.ToString();
		levelLabel.transform.rotation = Quaternion.Euler(Vector3.forward * transform.rotation.z);

		// Health bar
		healthBar.localScale = new Vector3(health / (float)CalcMaxHealth(), 1f, 1f);
		healthBarText.text = $"{health} / {CalcMaxHealth()}";
		healthBarContainer.transform.rotation = Quaternion.Euler(Vector3.forward * transform.rotation.z);

		// Animation
		moveFrame += ((Vector2)transform.position - prevFramePosition).magnitude * Mathf.Sign(desiredMove);
		prevFramePosition = transform.position;

		int animFrame = Mathf.RoundToInt(moveFrame * moveTimeToAnimFrameScale) % MOVE_FRAME_COUNT;
		animator.SetFloat(AnimationTime, animFrame / (float)MOVE_FRAME_COUNT);

		foreach(var side in sides) {
			side.isDead = isDead;
			side.isEating = isEating;
		}
		
		
		if(health < CalcMaxHealth() * 0.15f) {
			timeToNextBleed -= Time.deltaTime;
			
			if(timeToNextBleed < 0) {
				timeToNextBleed = timeBetweenBleeds;

				ProjectileManager.Get().SpawnBloodSplash(
					position:  transform.position,
					normalAngle: 0f,
					intensity:   0.25f + CalcNormalizedMultiplier(),
					scale:       CalcScale()
				);
			}
		}
	}

	void UpdatePosition() {
		if(isDead) {
			rigidbody2d.angularVelocity = 0;
			return;
		}

		lastMovements.Enqueue((rigidbody2d.position - lastRigidbodyPosition).magnitude);
		lastRigidbodyPosition = rigidbody2d.position;

		if(lastMovements.Count >= STORED_RIGIDBODY_POSITIONS) {
			lastMovements.Dequeue();

			var averageSpeed = lastMovements.Sum() / lastMovements.Count / Time.fixedDeltaTime;

			if(averageSpeed < 0.5f && reverseModeCooldown < 0) {
				// Most likely stalled
				isReverseMode = true;
				reverseModeTimeLeft = REVERSE_MODE_COOLDOWN / 2f;
				reverseModeCooldown = REVERSE_MODE_COOLDOWN;
				timeToAIUpdate = 0;
			}
		}

		reverseModeCooldown -= Time.deltaTime;
		reverseModeTimeLeft -= Time.deltaTime;

		if(isReverseMode && reverseModeTimeLeft < 0) {
			isReverseMode = false;
			timeToAIUpdate = 0;
		}

		rigidbody2d.MovePosition(rigidbody2d.position + (Vector2)transform.up * (desiredMove * Time.fixedDeltaTime));
		rigidbody2d.angularVelocity = Mathf.Abs(desiredRotation) > Mathf.Epsilon ? -desiredRotation : Mathf.Lerp(rigidbody2d.angularVelocity, 0, 0.1f);
	}

	void UpdateEating() {
		if(isDead) {
			return;
		}

		eatCooldown -= Time.deltaTime;

		if(isInvincible) {
			return;
		}

		var ants = GetEatingAnts();

		if(ants.Count == 0) {
			isEating = false;
			return;
		}

		isEating = true;

		if(eatCooldown > 0) {
			return;
		}

		foreach(var ant in ants) {
			Bite(ant);
		}

		eatCooldown = BITE_COOLDOWN;
	}
	
	void UpdateAI() {
		var ants = new Collection<AntController>();
		var guns = new Collection<GunSpawner>();

		foreach(var gunSpawner in transform.parent.gameObject.GetComponentsInChildren<GunSpawner>()) {
			if(gunSpawner.currentPickup != GunSpawner.Pickup.NONE && IsPointVisible(gunSpawner.transform.position)) {
				guns.Add(gunSpawner);
			}
		}

		foreach(var ant in transform.parent.gameObject.GetComponentsInChildren<AntController>()) {
			if(ant == this) {
				continue;
			}

			if(IsPointVisible(ant.transform.position)) {
				ants.Add(ant);
			}
		}

		var scale = Mathf.Max(1f, CalcScale());

		var explorePoints = new Collection<Vector2>[6];
		explorePoints[0] = GenerateExplorePoints(45f, 15f * scale);
		explorePoints[1] = GenerateExplorePoints(45f, 5f * scale);
		explorePoints[2] = GenerateExplorePoints(90f, 15f * scale);
		explorePoints[3] = GenerateExplorePoints(90f, 5f * scale);
		explorePoints[4] = GenerateExplorePoints(180f, 15f * scale);
		explorePoints[5] = GenerateExplorePoints(180f, 5f * scale);

		visibleAnts = ants;

		debugAIExplorePoints = explorePoints;

		var aiRequest = new AIStrategyRequest {
			me = this,

			position = transform.position,
			direction = transform.up,

			explorePoints = explorePoints,
			visibleGuns = guns,
			visibleAnts = ants,
		};

		var response = aiStrategy.Evaluate(aiRequest);

		if(response == null) {
			return;
		}

		aiCurrentAction = response.action;
		aiTarget = response.target;
		aiTargetAnt = response.targetAnt;
	}

	void UpdateAIControls() {
		timeToAIUpdate -= Time.deltaTime;

		if(timeToAIUpdate < 0 || ShouldForceRecalculateAI()) {
			timeToAIUpdate = AI_UPDATE_INTERVAL + Random.Range(-0.1f, 0.1f); // Some randomness to spread the load

			UpdateAI();
		}

		if(aiCurrentAction == AIStrategyResult.Action.EAT) {
			// Correct position
			aiTarget = aiTargetAnt.transform.position;
		}

		var targetOffset = aiTarget - (Vector2)transform.position;
		var targetRotation = -Vector2.SignedAngle(transform.up, targetOffset.normalized);

		float rotateInput = targetOffset.magnitude < 1f ? 0 : Mathf.Clamp(targetRotation / 15f, -1f, 1f);
		float moveInput = Mathf.Clamp01(targetOffset.magnitude / (aiCurrentAction == AIStrategyResult.Action.EAT ? EAT_SLOW_DOWN : 1f));

		if(isReverseMode) {
			moveInput = -Mathf.Clamp01(targetOffset.magnitude);
			rotateInput = -rotateInput;
		}

		if(DebugUtilsManager.Get().aiWalkMode == DebugUtilsManager.AiWalkMode.STAND) {
			rotateInput = 0;
			moveInput = 0;
		} else if(DebugUtilsManager.Get().aiWalkMode == DebugUtilsManager.AiWalkMode.CIRCLES) {
			rotateInput = 1f;
			moveInput = 1f;
		}
		
		desiredMove = moveInput * CalcMoveSpeed();
		desiredRotation = rotateInput * CalcRotateSpeed();
	}

	void UpdatePlayerControls() {
		float moveInput = Input.GetAxis("Vertical");
		float rotateInput = Input.GetAxis("Horizontal");

		if(moveInput < 0) {
			moveInput /= 2;
		}

		desiredMove = moveInput * CalcMoveSpeed();
		desiredRotation = rotateInput * CalcRotateSpeed() * Mathf.Sign(desiredMove);

		UpdateCheats();
	}
	#endregion

	#region # Visuals

	
	public void PlayHitAnimation() {
		hitAnimationState = 1f;		
	}

	#endregion

	#region # Business actions

	void Bite(AntController victim) {
		// Debug.Log($"[BITE] {name} -> {victim.name}");

		if(isInvincible || victim.isInvincible) {
			return;
		}

		if(victim.isDead) {
			// Feed
			var biteSize = Mathf.Min(CalcMaxBiteSize(), victim.level);

			var healthBefore = (float)health / CalcMaxHealth();

			level += biteSize;
			health = Mathf.CeilToInt(CalcMaxHealth() * healthBefore);

			victim.level -= biteSize;

			if(victim.level <= 0) {
				victim.DestroyBody();
			}

			return;
		}

		// Attack
		victim.ReceiveDamage(level, this, mouth.transform.position);
		
		ProjectileManager.Get().SpawnBloodSlash(
			position:  mouth.transform.position,
			normalAngle: mouth.transform.rotation.eulerAngles.z - 180,
			intensity:   0.25f + CalcNormalizedMultiplier() / 2f,
			scale:       CalcScale()
		);
	}

	public void ReceiveDamage(int amount, AntController attacker, Vector2 hitPoint) {
		if(isDead || isInvincible) {
			return;
		}

		health -= amount;

		if(attacker.isPlayerControlled) {
			PlayerStatsManager.Get().SpawnXpOrb(hitPoint, Mathf.CeilToInt(level / 10f));
		}

		if(health <= 0) {
			Die();
		}
	}
	
	void Die() {
		ProjectileManager.Get().SpawnBloodSplash(
			position:  transform.position,
			normalAngle: 0f,
			intensity:   0.5f + CalcNormalizedMultiplier(),
			scale:       CalcScale()
		);

		isDead = true;
		decayCooldown = Mathf.Max(10f, level);

		rigidbody2d.constraints = RigidbodyConstraints2D.FreezeAll;
		body.gameObject.GetComponent<Collider2D>().isTrigger = true;
		healthBarContainer.gameObject.SetActive(false);

		hasKnife = false;
		hasSabre = false;
		hasPistol = false;
		hasTurret1 = false;
		hasTurret2 = false;
	}

	public void DestroyBody() {
		transform.SetParent(null); // Become batman
		Destroy(gameObject);
	}

	#endregion
	

	void OnDrawGizmosSelected() {
		if(!showAiDebugOverlay) {
			return;
		}

		if(debugAIExplorePoints[0] != null) {
			foreach(var point in debugAIExplorePoints[5]) { Gizmos.color = Color.red;     Gizmos.DrawSphere(point, 0.15f); }
			foreach(var point in debugAIExplorePoints[4]) { Gizmos.color = Color.magenta; Gizmos.DrawSphere(point, 0.15f); }
			foreach(var point in debugAIExplorePoints[3]) { Gizmos.color = Color.yellow;  Gizmos.DrawSphere(point, 0.15f); }
			foreach(var point in debugAIExplorePoints[2]) { Gizmos.color = Color.blue;    Gizmos.DrawSphere(point, 0.15f); }
			foreach(var point in debugAIExplorePoints[1]) { Gizmos.color = Color.cyan;    Gizmos.DrawSphere(point, 0.15f); }
			foreach(var point in debugAIExplorePoints[0]) { Gizmos.color = Color.green;   Gizmos.DrawSphere(point, 0.15f); }
		}

		foreach(var item in visibleAnts) {
			if(item == null)
				continue;

			Gizmos.color = new Color(1f, 0.5f, 0f);
			Gizmos.DrawSphere(item.transform.position, 0.5f);
		}

		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(aiTarget, 0.75f);

		var rangedTarget = GetRangedWeaponTarget(turret2);
		
		if(rangedTarget != null) {
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(GetRangedWeaponTarget(turret2).transform.position, 0.3f);
		}
	}

	bool IsPointVisible(Vector2 point) {
		var direction = (point - (Vector2)transform.position).normalized;
		var distance = (point - (Vector2)transform.position).magnitude;

		var hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Level"));

		return !hit.collider;
	}

	Collection<Vector2> GenerateExplorePoints(float angleOfView, float distance, float explorePointInterval = 5f) {
		var result = new Collection<Vector2>();

		for(float angleOffset = 0; angleOffset < angleOfView; angleOffset += explorePointInterval) {
			// Every 5 deg add point 15 units away in front
			var leftPoint = transform.position + Quaternion.Euler(0, 0, +angleOffset + (isReverseMode ? 180f : 0)) * transform.up * distance;
			var rightPoint = transform.position + Quaternion.Euler(0, 0, -angleOffset + (isReverseMode ? 180f : 0)) * transform.up * distance;

			if(IsPointVisible(leftPoint)) {
				result.Add(leftPoint);
			}

			if(IsPointVisible(rightPoint)) {
				result.Add(rightPoint);
			}
		}

		return result;
	}

	public AntController GetRangedWeaponTarget(RangeWeapon weapon) {
		var maxDistance = 12f * Mathf.Max(1f, CalcScale());
		
		AntController result = null;

		foreach(var ant in visibleAnts) {
			if(ant == null || ant.isInvincible || ant.isDead) {
				continue;
			}
			
			if(Vector2.Distance(transform.position, ant.transform.position) > maxDistance) {
				continue;
			}

			if(weapon.applyAngleConstraints) {
				Vector2 myForward = transform.up;
				Vector2 directionToPlayer = (ant.transform.position - transform.position).normalized; // направление на игрока

				float angle = Vector2.SignedAngle(myForward, directionToPlayer);

				if(angle < weapon.angleConstraints.x || angle > weapon.angleConstraints.y) {
					continue;
				}
			}

			if(result == null) {
				result = ant;
				continue;
			}

			var currentMinDistance = Vector2.Distance(result.transform.position, transform.position);
			var antMinDistance = Vector2.Distance(ant.transform.position, transform.position);

			if(antMinDistance < currentMinDistance) {
				result = ant;
			}
		}

		return result;
	}


	bool ShouldForceRecalculateAI() {
		var targetOffset = aiTarget - (Vector2)transform.position;

		if(aiCurrentAction == AIStrategyResult.Action.EXPLORE && targetOffset.magnitude < 1f) {
			// Reached explore destination
			return true;
		}

		if(aiCurrentAction == AIStrategyResult.Action.EAT && !aiTargetAnt) {
			// Target is destroyed
			return true;
		}

		if(aiCurrentAction == AIStrategyResult.Action.FEAR && (aiTargetAnt == null || aiTargetAnt.isDead)) {
			// Fear target is dead
			return true;
		}

		return false;
	}

	List<AntController> GetEatingAnts() {
		var colliders = new List<Collider2D>();

		mouth.collider2d.GetContacts(colliders);

		return colliders.Select(col => col.GetComponentInParent<AntController>()).Where(ant => ant).ToList();
	}

	public bool PickupWeapon(GunSpawner.Pickup weapon) {
		if(isDead) {
			return false;
		}

		if(hasTurret1 && weapon == GunSpawner.Pickup.TURRET2) {
			hasTurret1 = false;
		}

		if(hasTurret2 && weapon == GunSpawner.Pickup.TURRET1) {
			hasTurret2 = false;
		}

		switch(weapon) {
			case GunSpawner.Pickup.NONE: break;
			case GunSpawner.Pickup.KNIFE: hasKnife = true; break;
			case GunSpawner.Pickup.SABRE: hasSabre = true; break;
			case GunSpawner.Pickup.PISTOL: hasPistol = true; break;
			case GunSpawner.Pickup.TURRET1: hasTurret1 = true; break;
			case GunSpawner.Pickup.TURRET2: hasTurret2 = true; break;
		}
		
		return true;
	}
}