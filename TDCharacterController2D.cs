using UnityEngine;

[RequireComponent (typeof(CircleCollider2D))]
[RequireComponent (typeof(Rigidbody2D))]
public class TDCharacterController2D : MonoBehaviour {
	/// <summary>The Transform component of the GameObject this script is attached to.</summary>
	private Transform _transform;

	[Tooltip("Layers to test for collisions. The TDCharacterController2D will walk through anything not on these layers.")]
	public LayerMask collisionLayers = -1;
	[Tooltip("Used in Update() to check the desired position in advance, creating a buffer zone around the collider. Lower values decrease the 'bouncing' behavior sometimes observed on tight corners.")]
    public float raycastLead = 0.15f;

	[Tooltip("Should the editor draw debug lines for collision tests?")]
	public bool drawDebug = false;

	/// <summary>The radius of the CircleCollider2D attached to this GameObject.</summary>
	private float radius;
	/// <summary>An array to hold our collision test results in. This allows us to test through our own collider, by testing for up to two collisions.</summary>
	RaycastHit2D[] hits2D = new RaycastHit2D[2];

    void Awake(){
		_transform = transform;
		rigidbody2D.isKinematic = true;

		radius = ((CircleCollider2D)collider2D).radius;
	    radius += raycastLead;	// we use the extended radius for raycasts
	} 

	/// <summary>Attempts to move the controller by <paramref name="motion"/>, the motion will only be constained by collisions. It will slide along colliders.</summary>
	/// <param name="motion">The desired motion from the current position.</param>
	public void Move(Vector2 motion) {
		_transform.position = GetValidPosition(motion, 10f, 20f, 40f, 60f, 80f);          
	}

	/// <summary>Returns a valid Vector2 position based on <paramref name="motion"/> from the current position. Collisions are tested for in both <paramref name="motion"/> + and - <paramref name="testAngles"/>.</summary>
	/// <returns>The valid Vector2 position to move to.</returns>
	/// <param name="motion">The desired motion from the current position.</param>
	/// <param name="testAngles">Each angle to test in both the positive and negative z rotation, relative to <paramref name="motion"/>.</param>
	public Vector2 GetValidPosition(Vector2 motion, params float[] testAngles){
		Vector2 movementDirection = motion.normalized * radius;
		Vector2 targetPosition = (Vector2)_transform.position + motion;

		#if UNITY_EDITOR
		if(drawDebug){ Debug.DrawLine(targetPosition, targetPosition + movementDirection, Color.cyan); }
		#endif

		// Test the desired direction for a collision and update targetPosition if any is found
		targetPosition += GetValidDirectionAdjustment(targetPosition, movementDirection);

		// Test movementDirection + and - each testAngle for a collision and update targetPosition if any is found
		for (int i = 0; i < testAngles.Length; i++){
			targetPosition += GetValidDirectionAdjustment(targetPosition, Quaternion.Euler(0,0,testAngles[i]) * movementDirection);
			targetPosition += GetValidDirectionAdjustment(targetPosition, Quaternion.Euler(0,0,-testAngles[i]) * movementDirection);

			#if UNITY_EDITOR
				if(drawDebug){
					Debug.DrawLine(targetPosition, targetPosition + (Vector2)(Quaternion.Euler(0,0,testAngles[i]) * movementDirection), Color.cyan);
					Debug.DrawLine(targetPosition, targetPosition + (Vector2)(Quaternion.Euler(0,0,-testAngles[i]) * movementDirection), Color.cyan);
				}
			#endif
		}

		return targetPosition;
	}

	/// <summary>Tests <paramref name="direction"/> from <paramref name="targetPosition"/> + <paramref name="radius"/> for a collision. If one is found, returns a Vector2 adjustment to the closest valid position. Otherwise returns Vector2.zero.</summary>
	/// <returns>The closest valid position to <paramref name="targetPosition"/>.</returns>
	/// <param name="targetPosition">The desired position to move to.</param>
	/// <param name="direction">The direction to test for a collision.</param>
	private Vector2 GetValidDirectionAdjustment(Vector2 targetPosition, Vector2 direction){
		Vector2 validPositionAdjustment = Vector2.zero;

		int amountOfHits = Physics2D.RaycastNonAlloc(targetPosition, direction, hits2D, radius, collisionLayers);
		RaycastHit2D hit2D;

		/// Because the character has a collider, to ensure we can collide with other characters if desired
		/// we need to allow for up to two hit detections. One for the character's collider and the other 
		/// for our actual collision.
		if(amountOfHits == 0 || (amountOfHits == 1 && hits2D[0].fraction < 0.5f)){
			// We hit nothing, or we only hit ourselves
			return validPositionAdjustment;
		} else if (amountOfHits == 1 || (amountOfHits > 1 && hits2D[0].fraction > 0.5f)){
			// We hit one of more colliders, but none of them was ours
			hit2D = hits2D[0];
		} else {
			// We hit ourselves, but we also hit something else.
			hit2D = hits2D[1];
		}

		validPositionAdjustment = hit2D.normal.normalized * ((1.0f - hit2D.fraction) * radius);

		#if UNITY_EDITOR
		if(drawDebug){ Debug.DrawLine(hit2D.point, hit2D.point += validPositionAdjustment, Color.magenta); }
		#endif              

		return validPositionAdjustment;
    }
}
