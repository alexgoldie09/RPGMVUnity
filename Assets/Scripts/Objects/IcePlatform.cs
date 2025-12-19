using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a single vertical ice platform column.
/// - Spawns a top tile (ground) and grows upwards in discrete steps.
/// - At each step, the old top position becomes a column tile.
/// - The top tile moves smoothly upward over a short duration.
/// - Optionally destroys itself after a configurable lifetime.
/// </summary>
public class IcePlatform : MonoBehaviour
{
    [Header("Tile Settings")]
    [Tooltip("World units per tile (e.g. 1 for 16x16 if 1 unit = 1 tile).")]
    [SerializeField] private float tileSize = 1f;

    [Tooltip("Maximum height of the platform in tiles (including the top tile).")]
    [SerializeField] private int maxHeightTiles = 8;

    [Header("Prefabs & Parents")]
    [Tooltip("Parent transform that all spawned tiles will be put under.")]
    [SerializeField] private Transform tilesParent;

    [Tooltip("Prefab for the top tile (ground).")]
    [SerializeField] private GameObject topTilePrefab;

    [Tooltip("Prefab for the column tiles underneath.")]
    [SerializeField] private GameObject columnTilePrefab;

    [Header("Movement")]
    [Tooltip("How long (in seconds) one growth step takes to rise smoothly.")]
    [SerializeField] private float riseDuration = 0.12f;

    [Header("Lifetime")]
    [Tooltip("If true, the entire platform will destroy itself after 'lifeTime' seconds.")]
    [SerializeField] private bool useLifetime = true;

    [Tooltip("How long (in seconds) the platform exists after being initialized.")]
    [SerializeField] private float lifeTime = 5f;

    // Current height in tiles of the column (1 = just the top tile).
    private int currentHeightTiles = 0;

    // The single top tile instance the player stands on.
    private GameObject currentTopTile;

    // All column tiles we spawn underneath (mainly for debugging/cleanup if needed).
    private readonly List<GameObject> columnTiles = new List<GameObject>();

    // True while a growth step is animating.
    private bool isRising = false;

    // Internal timer for lifetime.
    private float lifeTimer = 0f;

    #region Unity Methods

    private void Reset()
    {
        // Try to auto-wire the Tiles child.
        Transform found = transform.Find("Tiles");
        if (found != null)
            tilesParent = found;
    }

    private void Update()
    {
        // Handle lifetime countdown once the platform has at least one tile.
        if (!useLifetime || currentHeightTiles == 0)
            return;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Initializes the platform at the given world position with a single top tile.
    /// Call this once after placing or spawning the platform.
    /// Resets the lifetime timer.
    /// </summary>
    /// <param name="baseWorldPosition">Bottom position of the column in world space.</param>
    public void Initialize(Vector2 baseWorldPosition)
    {
        if (tilesParent == null)
            tilesParent = transform;

        // Move root to base position (bottom of the column).
        transform.position = baseWorldPosition;

        // Clear any existing tiles (in case of re-use or testing).
        foreach (Transform child in tilesParent)
            Destroy(child.gameObject);

        columnTiles.Clear();
        currentTopTile = null;
        currentHeightTiles = 0;
        isRising = false;

        // Reset lifetime timer.
        if (useLifetime)
            lifeTimer = lifeTime;

        // Spawn the initial top tile at the base.
        if (topTilePrefab != null)
        {
            currentTopTile = Instantiate(
                topTilePrefab,
                baseWorldPosition,
                Quaternion.identity,
                tilesParent
            );
            currentHeightTiles = 1;
        }
        else
        {
            Debug.LogWarning("[IcePlatform] No topTilePrefab assigned.");
        }
    }

    /// <summary>
    /// Requests the platform to grow by one tile upwards.
    /// Returns true if a growth step started, false if it cannot grow further.
    /// </summary>
    public bool GrowOneTile()
    {
        // Don't start another rise while one is already in progress.
        if (isRising)
            return false;

        // Never build past max height.
        if (currentHeightTiles >= maxHeightTiles)
            return false;

        if (currentTopTile == null)
        {
            Debug.LogWarning("[IcePlatform] GrowOneTile called before Initialize or top tile missing.");
            return false;
        }

        StartCoroutine(GrowOneTileRoutine());
        return true;
    }

    /// <summary>
    /// True while a growth step is animating.
    /// </summary>
    public bool IsRising => isRising;

    /// <summary>
    /// Current world position of the top tile (or root if missing).
    /// Used by abilities to move the rider along with the platform.
    /// </summary>
    public Vector2 TopPosition =>
        currentTopTile != null ? (Vector2)currentTopTile.transform.position
                               : (Vector2)transform.position;

    /// <summary>
    /// Current built height in tiles.
    /// </summary>
    public int CurrentHeight => currentHeightTiles;

    /// <summary>
    /// Maximum allowed height in tiles.
    /// </summary>
    public int MaxHeight => maxHeightTiles;

    #endregion

    #region Coroutines

    /// <summary>
    /// Smoothly animates one growth step:
    /// - Spawns a column tile at the old top position.
    /// - Moves the top tile up by one tile over riseDuration.
    /// </summary>
    private IEnumerator GrowOneTileRoutine()
    {
        isRising = true;

        float step = tileSize;

        // 1) Cache start & target positions for the top tile.
        Vector2 startTopPos = currentTopTile.transform.position;
        Vector2 targetTopPos = startTopPos + Vector2.up * step;

        // 2) Spawn the column tile where the old top was.
        if (columnTilePrefab != null && tilesParent != null)
        {
            GameObject columnInstance = Instantiate(
                columnTilePrefab,
                startTopPos,
                Quaternion.identity,
                tilesParent
            );
            columnTiles.Add(columnInstance);
        }

        // 3) Smoothly move the top tile up over riseDuration.
        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);

            Vector2 newTopPos = Vector2.Lerp(startTopPos, targetTopPos, t);
            currentTopTile.transform.position = newTopPos;

            yield return null;
        }

        // Snap to exact target at the end to avoid drift.
        currentTopTile.transform.position = targetTopPos;

        currentHeightTiles++;
        isRising = false;
    }

    #endregion
}
