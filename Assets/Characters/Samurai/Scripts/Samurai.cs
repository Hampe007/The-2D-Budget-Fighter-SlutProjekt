using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Samurai : FighterBase
{
    #region Dash Pierce Settings
    [Header("Dash Pierce Settings")]
    [SerializeField] private float dashDistance = 10f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private int dashDamage = 10;

    [Header("Yone-style end placement")]
    [SerializeField] private float stopBehindOpponentOffset = 0.6f;
    [SerializeField] private float enemyStackVerticalSpacing = 0.02f;

    [Header("Hit / status")]
    [SerializeField] private float stunDuration = 0.5f;

    [Header("Detection")]
    [SerializeField] private float latchRadius = 0.5f;
    [SerializeField] private float extraSweepPadding = 0.1f;
    
    [Header("End locks (enemy stays locked longer)")]
    [SerializeField] private float samuraiEndLockTime = 0.06f;
    [SerializeField] private float enemyEndLockTime = 0.18f;

    private float lastDashTime;
    private bool isDashing;
    private bool isPerformingAbility;
    #endregion

    #region Colliders
    [Header("Colliders")]
    private Collider2D mainCollider;
    private BoxCollider2D leftWallCollider;
    private BoxCollider2D rightWallCollider;
    private BoxCollider2D floorCollider;
    #endregion

    #region Physics
    private Rigidbody2D rb;
    private int fightersMask;
    private int groundMask;
    #endregion

    #region Ghost afterImage
    [Header("Ghost afterImage")]
    public GameObject ghostPrefab;
    private GhostSpawner ghostSpawner;
    private float ghostSpawnInterval;
    private float ghostSpawnTimer;
    [SerializeField] private float numberOfGhost = 10f;
    #endregion

    #region Unity
    private void Start()
    {
        base.Start();

        ghostSpawner = GetComponent<GhostSpawner>();

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        mainCollider = GetComponent<Collider2D>();

        leftWallCollider = GameObject.Find("LeftWall")?.GetComponent<BoxCollider2D>();
        rightWallCollider = GameObject.Find("RightWall")?.GetComponent<BoxCollider2D>();
        floorCollider = GameObject.Find("Floor")?.GetComponent<BoxCollider2D>();

        if (leftWallCollider == null || rightWallCollider == null || floorCollider == null)
            Debug.LogError("LeftWall/RightWall/Floor colliders missing. Check scene object names.");

        fightersMask = LayerMask.GetMask("Fighters");
        groundMask = LayerMask.GetMask("Ground");
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (isDead) return;
        if (statusEffectManager != null && statusEffectManager.IsStunned()) return;

        if (Input.GetKeyDown(KeyCode.Space) && !isPerformingAbility && Time.time - lastDashTime > dashDuration)
            StartDashPierce();
    }
    #endregion

    #region Dash entry
    private void StartDashPierce()
    {
        if (isDashing) return;
        StartCoroutine(DashCoroutine());
    }
    #endregion

    #region Dash core
    private IEnumerator DashCoroutine()
    {
        if (rb == null || mainCollider == null)
            yield break;

        mainCollider.isTrigger = true;
        isDashing = true;
        isPerformingAbility = true;
        lastDashTime = Time.time;

        if (animator != null)
            animator.SetTrigger("AbilityTrigger");

        int originalLayer = gameObject.layer;
        float originalGravity = rb.gravityScale;
        
        gameObject.layer = LayerMask.NameToLayer("NoCollision");
        
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        Vector2 dashDirection = transform.localScale.x < 0 ? Vector2.left : Vector2.right;

        float minX = leftWallCollider != null ? leftWallCollider.bounds.max.x : float.NegativeInfinity;
        float maxX = rightWallCollider != null ? rightWallCollider.bounds.min.x : float.PositiveInfinity;
        float minY = floorCollider != null ? floorCollider.bounds.max.y : float.NegativeInfinity;

        float speed = dashDuration > 0f ? (dashDistance / dashDuration) : 0f;

        Vector2 startPos = rb.position;
        Vector2 dashEnd = startPos + dashDirection * dashDistance;

        dashEnd.x = Mathf.Clamp(dashEnd.x, minX, maxX);
        dashEnd.y = Mathf.Max(dashEnd.y, minY);

        var latched = new List<FighterBase>(8);
        var latchedSet = new HashSet<FighterBase>();

        ghostSpawnTimer = 0f;
        ghostSpawnInterval = numberOfGhost > 0f ? (dashDuration / numberOfGhost) : dashDuration;

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            float dt = Time.fixedDeltaTime;

            Vector2 step = dashDirection * (speed * dt);
            float sweepLen = step.magnitude + extraSweepPadding;

            Vector2 nextPos = rb.position + step;

            nextPos.x = Mathf.Clamp(nextPos.x, minX, maxX);
            nextPos.y = Mathf.Max(nextPos.y, minY);

            rb.MovePosition(nextPos);

            ghostSpawnTimer += dt;
            if (ghostSpawner != null && ghostSpawnInterval > 0f && ghostSpawnTimer >= ghostSpawnInterval)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                ghostSpawner.SpawnGhost(transform.position, sr, transform.localScale);
                ghostSpawnTimer = 0f;
            }

            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, dashDirection, sweepLen, groundMask);
            if (wallHit.collider != null)
            {
	            dashEnd = wallHit.point - dashDirection * 0.05f;
                dashEnd.x = Mathf.Clamp(dashEnd.x, minX, maxX);
                dashEnd.y = Mathf.Max(dashEnd.y, minY);

                break;
            }

            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                transform.position,
                latchRadius,
                dashDirection,
                sweepLen,
                fightersMask
            );

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i].collider;
                if (col == null) continue;

                FighterBase opponent = col.GetComponent<FighterBase>();
                if (opponent == null) continue;
                if (opponent == this) continue;

                if (!latchedSet.Add(opponent))
                    continue;

                latched.Add(opponent);

                DealDamageServerRpc(opponent.NetworkObject, this.NetworkObject, dashDamage);
                opponent.GetComponent<StatusEffectManager>()?.ApplyStun(stunDuration);

                Rigidbody2D opponentRb = opponent.GetComponent<Rigidbody2D>();
                if (opponentRb != null)
                {
	                opponentRb.linearVelocity = Vector2.zero;
	                opponentRb.angularVelocity = 0f;
                }
            }

            elapsed += dt;
            yield return new WaitForFixedUpdate();
        }

        SnapToEndPositions(dashEnd, dashDirection, latched, minX, maxX, minY);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        for (int i = 0; i < latched.Count; i++)
        {
	        Rigidbody2D oppRb = latched[i] != null ? latched[i].GetComponent<Rigidbody2D>() : null;
	        if (oppRb == null) continue;

	        oppRb.linearVelocity = Vector2.zero;
	        oppRb.angularVelocity = 0f;
        }

        if (samuraiEndLockTime > 0f || enemyEndLockTime > 0f)
	        StartCoroutine(EndLockCoroutine(latched, samuraiEndLockTime, enemyEndLockTime));
        
        gameObject.layer = originalLayer;
        rb.gravityScale = originalGravity;

        isDashing = false;
        isPerformingAbility = false;
        mainCollider.isTrigger = false;
    }

    private void SnapToEndPositions(
        Vector2 dashEnd,
        Vector2 dashDirection,
        List<FighterBase> latched,
        float minX,
        float maxX,
        float minY)
    {
        Vector2 playerEnd = dashEnd - dashDirection * stopBehindOpponentOffset;
        playerEnd.x = Mathf.Clamp(playerEnd.x, minX, maxX);
        playerEnd.y = Mathf.Max(playerEnd.y, minY);

        rb.MovePosition(playerEnd);

        for (int i = 0; i < latched.Count; i++)
        {
            FighterBase opponent = latched[i];
            if (opponent == null) continue;

            Rigidbody2D opponentRb = opponent.GetComponent<Rigidbody2D>();

            Vector2 enemyEnd = dashEnd;
            enemyEnd.y += i * enemyStackVerticalSpacing;
            enemyEnd.x = Mathf.Clamp(enemyEnd.x, minX, maxX);
            enemyEnd.y = Mathf.Max(enemyEnd.y, minY);

            if (opponentRb != null)
                opponentRb.MovePosition(enemyEnd);
            else
                opponent.transform.position = enemyEnd;
        }
    }
    
    private IEnumerator EndLockCoroutine(List<FighterBase> latched, float samuraiTime, float enemyTime)
    {
	    if (rb == null)
		    yield break;

	    RigidbodyType2D samuraiOriginalType = rb.bodyType;
	    rb.bodyType = RigidbodyType2D.Kinematic;

	    var cachedEnemyBodies = new List<(Rigidbody2D rb, RigidbodyType2D type)>(latched.Count);

	    for (int i = 0; i < latched.Count; i++)
	    {
		    FighterBase opp = latched[i];
		    if (opp == null) continue;

		    Rigidbody2D oppRb = opp.GetComponent<Rigidbody2D>();
		    if (oppRb == null) continue;

		    cachedEnemyBodies.Add((oppRb, oppRb.bodyType));
		    oppRb.bodyType = RigidbodyType2D.Kinematic;
	    }
	    
	    float firstWait = Mathf.Max(0f, Mathf.Min(samuraiTime, enemyTime));
	    float secondWait = Mathf.Max(0f, Mathf.Max(samuraiTime, enemyTime) - firstWait);

	    if (firstWait > 0f)
		    yield return new WaitForSeconds(firstWait);

	    if (samuraiTime <= enemyTime)
	    {
		    if (samuraiTime > 0f)
			    rb.bodyType = samuraiOriginalType;

		    if (secondWait > 0f)
			    yield return new WaitForSeconds(secondWait);

		    for (int i = 0; i < cachedEnemyBodies.Count; i++)
			    cachedEnemyBodies[i].rb.bodyType = cachedEnemyBodies[i].type;
	    }
	    else
	    {
		    if (enemyTime > 0f)
		    {
			    for (int i = 0; i < cachedEnemyBodies.Count; i++)
				    cachedEnemyBodies[i].rb.bodyType = cachedEnemyBodies[i].type;
		    }

		    if (secondWait > 0f)
			    yield return new WaitForSeconds(secondWait);

		    rb.bodyType = samuraiOriginalType;
	    }
    }
    #endregion

    #region Attacks
    public override void Attack(FighterBase opponent)
    {
        if (isDead) return;
        if (statusEffectManager != null && statusEffectManager.IsStunned()) return;

        if (opponent == null)
        {
            MeleeAttack();
            return;
        }

        PlayAttackAnimation();

        int damage = baseAttackPower + 5;
        DealDamageServerRpc(opponent.NetworkObject, this.NetworkObject, damage);
        Debug.Log($"{fighterName} attacks {opponent.fighterName} for {damage} damage!");
    }
    #endregion
}
