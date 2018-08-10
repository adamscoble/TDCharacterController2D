using System;
using System.Linq;
using UnityEngine;

[RequireComponent (typeof(CircleCollider2D))]
public class TDCharacterController2D : MonoBehaviour {
	[Tooltip("The Transform this character controller should move. Leave empty to use the TDCharacterController2D GameObject's Transform.")]
	public Transform Transform;
	[Tooltip("Layers to test for collisions. The TDCharacterController2D will move through anything not on these layers.")]
	public LayerMask CollisionLayers;
	[Tooltip("Additional distance between the CircleCollider2D's radius and collision testing.")]
    public float RadiusBuffer;

	CircleCollider2D _collider;
	float _radius;
	/// <summary>The minimum distance the TDCharacterController2D requires to move. Can prevent slight stutter in certain scenarios (i.e. moving against corners).</summary>
	float _minimumMovementThreshold;
	float _sqrMinimumMovementThreshold;

	readonly RaycastHit2D[] _collisions = new RaycastHit2D[2];
	readonly Collider2D[] _overlaps = new Collider2D[2];

    void Awake() {
	    if (Transform == null) { Transform = transform; }
	    _collider = GetComponent<CircleCollider2D>();

	    _radius = _collider.radius + RadiusBuffer;
	    _minimumMovementThreshold = _radius / 50f;
	    _sqrMinimumMovementThreshold = _minimumMovementThreshold * _minimumMovementThreshold;

	    SetRigidbodyKinematic();
	}

	void SetRigidbodyKinematic() {
		Rigidbody2D rigidbody2D = GetComponentInParent<Rigidbody2D>();

		if (rigidbody2D == null) {
			Debug.LogWarning("No Rigidbody found on GameObject or in parent of TDCharacterController2D.");
			return;
		}

		rigidbody2D.isKinematic = true;
	}

	/// <summary>Attempts to move the controller by <paramref name="motion"/>, the motion will only be constained by collisions. It will slide along colliders.</summary>
	public void Move(Vector2 motion) {
		Vector3 currentPosition = Transform.position;
		Vector3 newPosition = GetNewPosition(currentPosition, motion);
		newPosition.z = currentPosition.z;

		if ((newPosition - currentPosition).sqrMagnitude < _sqrMinimumMovementThreshold) { return; }

		Transform.position = newPosition;
	}

	Vector2 GetNewPosition(Vector2 currentPosition, Vector2 motion) {
		Vector2 direction = motion.normalized;
		float distance = motion.magnitude;

		int collisionCount = Physics2D.CircleCastNonAlloc(currentPosition, _radius, direction, _collisions, distance, CollisionLayers);

		if (IsPositionValid(collisionCount, _collisions[0].collider)) { return currentPosition + motion; }

		RaycastHit2D hit = _collisions.FirstOrDefault(h => h.collider != _collider);

		return GetValidPositionFromCollision(currentPosition, direction, distance, hit);
	}

	Vector2 GetValidPositionFromCollision(Vector2 currentPosition, Vector2 direction, float distance, RaycastHit2D hit) {
		Vector2 positionAtCollision = currentPosition + (direction * (distance * hit.fraction));
		positionAtCollision += hit.normal * _minimumMovementThreshold; // Move slightly further away from the point of collision to account for inaccuracy

		float normalizedAngle = GetNormalizedAngle(-direction, hit.normal);

		if (Math.Abs(normalizedAngle) < 0.01f) { return positionAtCollision; } // We have walked straight into the collision's normal

		float remainingDistance = distance * (1 - hit.fraction); // The amount of motion remaining at the point of collision
		remainingDistance *= Mathf.Abs(normalizedAngle); // Reduce the remaining distance according to the angle of the collision (moving directly into a collider's normal would result in 0 remaining distance)

		Vector2 adjustedDirection = Quaternion.AngleAxis(normalizedAngle < 0 ? -90 : 90, Vector3.back) * hit.normal; // Rotate the collision's normal by 90 degrees towards the direction of movement to adjust direction
		adjustedDirection *= remainingDistance;

		Vector2 adjustedPosition = positionAtCollision + adjustedDirection;

		int overlapCount = Physics2D.OverlapCircleNonAlloc(adjustedPosition, _radius, _overlaps, CollisionLayers);

		if(IsPositionValid(overlapCount, _overlaps[0])) { return adjustedPosition; }

		overlapCount = Physics2D.OverlapCircleNonAlloc(positionAtCollision, _radius, _overlaps, CollisionLayers);

		if (IsPositionValid(overlapCount, _overlaps[0])) { return positionAtCollision; }

		return positionAtCollision + hit.normal * _minimumMovementThreshold;
	}

	/// <summary>Helper function to test if a position is empty or if the only collider present is owned by this <see cref="TDCharacterController2D"/>.</summary>
	bool IsPositionValid(int colliderCountAtPosition, Collider2D firstColliderAtPosition) {
		return colliderCountAtPosition == 0 || colliderCountAtPosition == 1 && firstColliderAtPosition == _collider;
	}

	/// <summary>Returns a signed, normalized angle between <paramref name="a"/> and <paramref name="b"/>. 0 means they are the same, while -1 and 1 mean that <paramref name="b"/> is 90 degrees right or left from <paramref name="a"/>.</summary>
	static float GetNormalizedAngle(Vector2 a, Vector2 b) {
		return -a.x * b.y + a.y * b.x;
	}
}
