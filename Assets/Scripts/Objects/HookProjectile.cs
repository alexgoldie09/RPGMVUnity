using UnityEngine;

/// <summary>
/// Simple projectile for the hook shot.
/// It travels in a straight line, checks for hits on hookable layers,
/// and notifies the owning HookAbility when it hits or reaches max distance.
/// Also drives a LineRenderer to visually represent the rope.
/// </summary>
public class HookProjectile : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Optional SpriteRenderer used to visually orient the hook.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Optional LineRenderer used to draw the rope from player to hook.")]
    [SerializeField] private LineRenderer lineRenderer;

    // Reference back to the ability that spawned this projectile.
    private HookAbility owner;

    // Movement configuration.
    private Vector2 direction;
    private float speed;
    private float maxDistance;

    // Layers.
    private LayerMask collisionLayerMask;  // everything that can block the projectile
    private LayerMask hookLayerMask;       // subset that counts as valid hook targets

    // Internal state.
    private Vector2 startPosition;
    private float travelledDistance;
    private bool hasFinished;

    // Rope start (player hook spawn point).
    private Transform ropeStartTransform;

    private void Awake()
    {
        // Auto-grab a SpriteRenderer if not set in the inspector.
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Auto-grab a LineRenderer if not set.
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>
    /// Initializes the projectile's behavior.
    /// </summary>
    /// <param name="owner">The HookAbility that spawned this projectile.</param>
    /// <param name="origin">Starting position of the projectile.</param>
    /// <param name="direction">Direction in which to travel.</param>
    /// <param name="speed">Travel speed of the projectile.</param>
    /// <param name="maxDistance">Maximum distance before the projectile expires.</param>
    /// <param name="collisionLayerMask">Layers that can block the projectile.</param>
    /// <param name="hookLayerMask">Layers that count as valid hook targets.</param>
    public void Initialize(
        HookAbility owner,
        Vector2 origin,
        Vector2 direction,
        float speed,
        float maxDistance,
        LayerMask collisionLayerMask,
        LayerMask hookLayerMask)
    {
        this.owner = owner;
        this.direction = direction.normalized;
        this.speed = speed;
        this.maxDistance = maxDistance;
        this.collisionLayerMask = collisionLayerMask;
        this.hookLayerMask = hookLayerMask;

        startPosition = origin;
        travelledDistance = 0f;
        hasFinished = false;

        transform.position = origin;

        // Orient the projectile so it visually points along its travel direction.
        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (spriteRenderer != null)
        {
            // Reset flips; adjust here if your art needs a fixed offset.
            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;
        }

        // Initialise rope positions (if we already know the start later).
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false; // will be enabled once we have a start transform
        }
    }

    /// <summary>
    /// Sets the transform used as the rope's start (typically the player's hook spawn point).
    /// Must be called by HookAbility after spawning the projectile.
    /// </summary>
    public void SetRopeStart(Transform startTransform)
    {
        ropeStartTransform = startTransform;

        if (lineRenderer != null && ropeStartTransform != null)
        {
            lineRenderer.enabled = true;
        }
    }

    private void FixedUpdate()
    {
        // Safety: if owner disappeared, just destroy the projectile.
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        // While not finished, handle movement and collision.
        if (!hasFinished)
        {
            float step = speed * Time.fixedDeltaTime;
            Vector2 currentPos = transform.position;

            // Raycast ahead to see if we hit something this frame.
            RaycastHit2D hit = Physics2D.Raycast(
                currentPos,
                direction,
                step,
                collisionLayerMask
            );

            if (hit)
            {
                hasFinished = true;

                // Snap to the hit point.
                transform.position = hit.point;

                // Check if this collider is on a hookable layer.
                bool isHookable =
                    (hookLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0;

                if (isHookable)
                {
                    // Valid hook target.
                    owner.OnHookProjectileHit(hit.point, this);
                    // Do NOT destroy here; HookAbility cleans up when done pulling.
                }
                else
                {
                    // Hit something non-hookable – treat as a miss and destroy immediately.
                    owner.OnHookProjectileMiss(this);
                    Destroy(gameObject);
                }

                // Even on hit, we still want the rope to update this frame,
                // so we do NOT early return here.
            }
            else
            {
                // No hit this frame; move forward.
                Vector2 nextPos = currentPos + direction * step;
                transform.position = nextPos;

                travelledDistance += step;

                // If we've traveled our max distance, notify miss and destroy.
                if (travelledDistance >= maxDistance)
                {
                    hasFinished = true;

                    owner.OnHookProjectileMiss(this);
                    Destroy(gameObject);
                    return; // rope will vanish with the projectile
                }
            }
        }

        // Always update rope after movement / hit logic so it reflects the latest positions.
        UpdateRope();
    }

    /// <summary>
    /// Updates the rope LineRenderer to stretch between the rope start (player) and this projectile.
    /// </summary>
    private void UpdateRope()
    {
        if (lineRenderer == null || ropeStartTransform == null)
            return;

        Vector3 start = ropeStartTransform.position;
        Vector3 end = transform.position;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        // Optional: adjust tiling of the rope texture based on length
        float length = Vector2.Distance(start, end);

        if (lineRenderer.material != null)
        {
            // X = tiling along length; tweak the multiplier to taste based on your texture.
            lineRenderer.material.mainTextureScale = new Vector2(length, 1f);
        }
    }
}