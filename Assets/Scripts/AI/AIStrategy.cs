using System.Collections.ObjectModel;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/AIStrategy", order = 1)]
public class AIStrategy : ScriptableObject {
	public float hungerWeight = 1.0f;
	public float fearWeight = 1.0f;
	public float exploreWeight = 1.0f;

	class EnemyAnt {
		public AntController ant;
		public GunSpawner pickup;

		public float fear;
		public float want;
	}

	float CalculateDesire(AntController hisAnt, AntController myAnt) {
		if(hisAnt.isDead) {
			return 1.0f;
		}

		// https://www.desmos.com/calculator/kvg12pmpiy
		var normalizedDifference = (float)hisAnt.health / myAnt.health;

		return Mathf.Clamp(-1.5f * normalizedDifference + 2f, -1f, 1f);
	}

	float CalculateDistanceMultiplier(Vector2 hisPosition, Vector2 myPosition) {
		return -(hisPosition - myPosition).magnitude / 10f + 2f;
	}

	float CalculateFearStateMultiplier(AntController ant) {
		return 1f / hungerWeight;
	}

	float CalculateWantStateMultiplier(AntController ant) {
		if(ant.isDead) {
			return 1f;
		}

		if(ant.isInvincible) {
			return 0f;
		}

		return 1f / fearWeight;
	}

	Vector2 PickExplorePoint(Collection<Vector2>[] explorePoints, Vector2 defaultPoint) {
		foreach(var points in explorePoints) {
			if(points.Count < 1) {
				continue;
			}

			// Random element but prefer elements at the start of the array
			// return points[ Mathf.FloorToInt(Mathf.Pow(Random.Range(0, points.Count), 2)) ];
			return points[ Random.Range(0, points.Count) ];
		}

		return defaultPoint;
	}

	Vector2 PickRunawayPoint(Vector2 enemyPosition, Vector2 myPosition, Collection<Vector2>[] explorePoints) {
		Vector2 mostDistantPoint = enemyPosition;
		bool runawayPointFound = false;

		foreach(var points in explorePoints) {
			foreach(var point in points) {
				if((point - enemyPosition).sqrMagnitude > (mostDistantPoint - enemyPosition).sqrMagnitude) {
					mostDistantPoint = point;
					runawayPointFound = true;
				}
			}
		}

		if(runawayPointFound) {
			return mostDistantPoint;
		}

		var fallbackOffset = myPosition - enemyPosition;

		return myPosition + fallbackOffset;
	}

	public AIStrategyResult Evaluate(AIStrategyRequest request) {
		var me = request.me;

		EnemyAnt maxWantEnemy = null;
		EnemyAnt maxFearEnemy = null;

		foreach(var ant in request.visibleAnts) {
			float desire = CalculateDesire(ant, me);
			float distanceMultiplier = CalculateDistanceMultiplier(ant.transform.position, me.transform.position);

			var enemy = new EnemyAnt {
				ant = ant,
				pickup = null,

				fear = Mathf.Clamp01(-desire) * fearWeight * CalculateFearStateMultiplier(ant) * distanceMultiplier,
				want = Mathf.Clamp01(+desire) * hungerWeight * CalculateWantStateMultiplier(ant) * distanceMultiplier,
			};

			if(maxFearEnemy == null || enemy.fear > maxFearEnemy.fear) {
				maxFearEnemy = enemy;
			}

			if(maxWantEnemy == null || enemy.want > maxWantEnemy.want) {
				maxWantEnemy = enemy;
			}
		}

		foreach(var pickup in request.visibleGuns) {
			var pickupEntry = new EnemyAnt {
				ant = null,
				pickup = pickup,

				fear = -1f,
				want = 1.25f,
			};

			if(maxWantEnemy == null || pickupEntry.want > maxWantEnemy.want) {
				maxWantEnemy = pickupEntry;
			}
		}

		var explorePoint = PickExplorePoint(request.explorePoints, request.position);

		var exploreDesire = 1.0f * exploreWeight * Mathf.Clamp01(CalculateDistanceMultiplier(explorePoint, request.position));
		var fearDesire = maxFearEnemy?.fear ?? 0;
		var eatDesire = maxWantEnemy?.want ?? 0;

		if(maxFearEnemy != null && fearDesire > eatDesire && fearDesire > exploreDesire) {
			return new AIStrategyResult {
				action = AIStrategyResult.Action.FEAR,
				target = PickRunawayPoint(maxFearEnemy.ant.transform.position, request.position, request.explorePoints),
				targetAnt = maxFearEnemy.ant,
			};
		}

		if(maxWantEnemy != null && eatDesire > fearDesire && eatDesire > exploreDesire) {
			if(maxWantEnemy.pickup != null) {
				return new AIStrategyResult {
					action = AIStrategyResult.Action.EXPLORE,
					target = maxWantEnemy.pickup.transform.position,
				};
			}

			return new AIStrategyResult {
				action = AIStrategyResult.Action.EAT,
				target = maxWantEnemy.ant.transform.position,
				targetAnt = maxWantEnemy.ant,
			};
		}

		// In any unclear situation go explore
		return new AIStrategyResult {
			action = AIStrategyResult.Action.EXPLORE,
			target = explorePoint,
		};
	}
}