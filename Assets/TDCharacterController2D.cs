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
	[Tooltip("The minimum distance the TDCharacterController2D requires to move. Can prevent slight stutter in certain scenarios (i.e. moving against corners).")]
	public float MinimumMovementThreshold = 0.1f;
	[Tooltip("If the TDCharacterController2D is stuck on a collider after movement, it will attempt to push itself away from the collision up to this many times.")]
	public int PushAwayFromCollisionIterations = 20;

	CircleCollider2D _collider;
	float _radius;
	float _sqrMinimumMovementThreshold;

	static readonly RaycastHit2D[] _collisions = new RaycastHit2D[2];
	static readonly Collider2D[] _overlaps = new Collider2D[2];

    void Awake() {
	    if (Transform == null) { Transform = transform; }
	    _collider = GetComponent<CircleCollider2D>();

	    _radius = _collider.radius + RadiusBuffer;
	    _sqrMinimumMovementThreshold = MinimumMovementThreshold * MinimumMovementThreshold;

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

	/// <summary>Attempts to move the controller by <paramref name="motion"/>, the motion will only be constrained by collisions. It will slide along colliders.</summary>
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

		if (IsMovementValid(currentPosition, direction, distance)) { return currentPosition + motion; }

		return GetValidPositionFromCollision(currentPosition, direction, distance, _collisions.First(h => h.collider != _collider));
	}

	Vector2 GetValidPositionFromCollision(Vector2 currentPosition, Vector2 direction, float distance, RaycastHit2D hit) {
		Vector2 positionAtCollision = GetPositionAtCollision(currentPosition, direction, distance, hit);
		float normalizedAngle = GetNormalizedAngle(-direction, hit.normal);

		if (Math.Abs(normalizedAngle) < 0.01f) { return positionAtCollision; } // We have walked straight into the collision's normal

		float remainingDistance = distance * (1 - hit.fraction); // The amount of motion remaining at the point of collision
		remainingDistance *= Mathf.Abs(normalizedAngle); // Reduce the remaining distance according to the angle of the collision (moving directly into a collider's normal would result in 0 remaining distance)

		Vector2 slideDirection = Quaternion.AngleAxis(normalizedAngle < 0 ? -90 : 90, Vector3.back) * hit.normal; // Rotate the collision's normal by 90 degrees towards the direction of movement to slide along surface

		if(IsMovementValid(positionAtCollision, slideDirection, remainingDistance)) { return positionAtCollision + (slideDirection * remainingDistance); }

		RaycastHit2D slideHit = _collisions.First(h => h.collider != _collider);
		float slideHitDistance = remainingDistance * slideHit.fraction;

		if (slideHitDistance > MinimumMovementThreshold) { return GetPositionAtCollision(positionAtCollision, slideDirection, remainingDistance, slideHit); }

		int pushAwayFromCollisionIterations = PushAwayFromCollisionIterations;

		while (pushAwayFromCollisionIterations > 0 && !IsPositionValid(positionAtCollision, -MinimumMovementThreshold)) {
			positionAtCollision += hit.normal * MinimumMovementThreshold;

			pushAwayFromCollisionIterations--;
		}

		return positionAtCollision;
	}

	/// <summary>Returns true if there are no collisions along the movement (excepting this <see cref="TDCharacterController2D"/>'s collider).<para>Collision results are stored in <see cref="_collisions"/>.</para></summary>
	bool IsMovementValid(Vector2 position, Vector2 direction, float distance) {
		int collisionCount = Physics2D.CircleCastNonAlloc(position, _radius, direction, _collisions, distance, CollisionLayers);

		return IsNullOrSelfCollision(collisionCount, _collisions[0].collider);
	}

	bool IsNullOrSelfCollision(int colliderCount, Collider2D firstCollider) {
		return colliderCount == 0 || (colliderCount == 1 && firstCollider == _collider);
	}

	Vector2 GetPositionAtCollision(Vector2 position, Vector2 direction, float distance, RaycastHit2D hit) {
		Vector2 positionAtCollision = position + (direction * (distance * hit.fraction));
		positionAtCollision += hit.normal * MinimumMovementThreshold; // Move slightly further away from the point of collision to account for inaccuracy

		return positionAtCollision;
	}

	/// <summary>Returns a signed, normalized angle between <paramref name="a"/> and <paramref name="b"/>. 0 means they are the same, while -1 and 1 mean that <paramref name="b"/> is 90 degrees right or left from <paramref name="a"/>.</summary>
	static float GetNormalizedAngle(Vector2 a, Vector2 b) {
		return -a.x * b.y + a.y * b.x;
	}

	/// <summary>Returns true if there are no overlaps at <paramref name="position"/> (excepting this <see cref="TDCharacterController2D"/>'s collider).<para>Overlap results are stored in <see cref="_overlaps"/>.</para></summary>
	bool IsPositionValid(Vector2 position, float radiusAdjustment = 0) {
		int overlapCount = Physics2D.OverlapCircleNonAlloc(position, _radius + radiusAdjustment, _overlaps, CollisionLayers);

		return IsNullOrSelfCollision(overlapCount, _overlaps[0]);
	}
}
