using UnityEngine;

public class SpecialMovement : MonoBehaviour
{
    #region Variables + Setup

    [SerializeField] private Rigidbody2D rb;

    private AttackSpawnSpec spec;
    private Player player;

    private Vector3 moveVelocity;
    private bool canMove;
    private float moveTimer;

    private SpecialMovementType currentMovementType;

    #region Homing Settings
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float homingGroundProbeDistance = 10f;
    [SerializeField] private float minGroundClearance = 1.5f;

    private enum HomingPhase
    {
        Attack,
        Recover
    }

    private CapsuleCollider2D playerCapsule;
    private Vector2 homingDirection;
    private float homingLowestY;
    private HomingPhase homingPhase;

    private enum HomingHeightBand
    {
        Feet,
        Body,
        Face
    }

    private HomingHeightBand homingHeightBand;

    #endregion

    #region Dive Settings
    private enum DivePhase
    {
        FlyToStart,
        Loop,
        AttackRise,
        AttackDrop
    }

    private DivePhase divePhase;
    private int diveLoopCount;
    private int diveTargetLoops;
    private float diveLoopAngle;
    private float diveLockedX;
    private float diveAttackSpeedBoost;
    private float diveCircleRadius;
    private float divePlayerClearance;
    private float diveDropHorizontalSpeed;
    private float lastPlayerX;
    private float playerXVelocity;
    private float diveDropVelocityX;
    #endregion

    public void Setup(AttackSpawnSpec _spec, Player _player)
    {
        spec = _spec;
        player = _player;
        rb = GetComponent<Rigidbody2D>();
        ConfigureFireDirection();
    }

    private void Update()
    {
        UpdateMovement();
    }

    #endregion

    #region Configure

    private void ConfigureFireDirection()
    {
        if (spec == null || player == null)
            return;

        switch (spec.movementType)
        {
            case SpecialMovementType.None:
                canMove = false;
                break;

            case SpecialMovementType.Straight:
                ConfigureStraight();
                break;

            case SpecialMovementType.Arch:
                ConfigureArch();
                break;

            case SpecialMovementType.Homing:
                ConfigureHoming();
                break;

            case SpecialMovementType.Dive:
                ConfigureDive();
                break;
        }
    }

    private void ConfigureStraight()
    {
        Vector3 playerPos = player.transform.position;
        Vector3 dir = playerPos - transform.position;
        Vector3 direction = dir.normalized;

        moveVelocity = direction * spec.speed;
        canMove = true;
        currentMovementType = SpecialMovementType.Straight;

        if (rb != null)
            rb.gravityScale = 0f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ConfigureArch()
    {
        Vector2 startPos = transform.position;
        Vector2 playerPos = player.transform.position;

        float xOffset = playerPos.x - startPos.x;
        float xDistance = Mathf.Abs(xOffset);

        if (xDistance < 0.75f)
        {
            ConfigureStraight();
            return;
        }

        float flightTime = Mathf.Max(0.35f, xDistance / Mathf.Max(spec.speed, 0.01f));
        float gravity = Mathf.Abs(Physics2D.gravity.y);

        float horizontalSpeed = xOffset / flightTime;
        float verticalSpeed = (playerPos.y - startPos.y + 0.5f * gravity * flightTime * flightTime) / flightTime;

        moveVelocity = new Vector2(horizontalSpeed, verticalSpeed);
        canMove = true;
        currentMovementType = SpecialMovementType.Arch;

        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = moveVelocity;
        }

        float angle = Mathf.Atan2(moveVelocity.y, moveVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ConfigureHoming()
    {
        Vector2 startPos = transform.position;
        Vector2 playerPos = player.transform.position;
        Vector2 dir = (playerPos - startPos).normalized;

        if (dir.sqrMagnitude <= 0.001f)
            dir = Vector2.right;

        playerCapsule = player.GetComponent<CapsuleCollider2D>();

        float feetY = playerPos.y;
        float bodyY = playerPos.y;
        float faceY = playerPos.y;

        if (playerCapsule != null)
        {
            Bounds bounds = playerCapsule.bounds;
            feetY = bounds.min.y;
            bodyY = bounds.center.y;
            faceY = bounds.max.y - (bounds.size.y * 0.2f);
        }

        int heightPick = Random.Range(0, 3);

        if (heightPick == 0)
        {
            homingHeightBand = HomingHeightBand.Feet;
            homingLowestY = feetY;
        }
        else if (heightPick == 1)
        {
            homingHeightBand = HomingHeightBand.Body;
            homingLowestY = bodyY;
        }
        else
        {
            homingHeightBand = HomingHeightBand.Face;
            homingLowestY = faceY;
        }

        homingDirection = new Vector2(dir.x, Mathf.Max(dir.y, spec.homingRecoverUpStrength)).normalized;
        moveVelocity = homingDirection * spec.speed;
        moveTimer = spec.moveTimer;
        homingPhase = HomingPhase.Recover;

        canMove = true;
        currentMovementType = SpecialMovementType.Homing;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = moveVelocity;
        }

        float angle = Mathf.Atan2(moveVelocity.y, moveVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ConfigureDive()
    {
        canMove = true;
        currentMovementType = SpecialMovementType.Dive;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        divePhase = DivePhase.FlyToStart;
        diveLoopCount = 0;
        diveLoopAngle = Mathf.PI * 0.5f;
        diveLockedX = transform.position.x;

        diveAttackSpeedBoost = spec.attackSpeedBoost;
        diveCircleRadius = Mathf.Max(0.5f, spec.circleRadius);
        divePlayerClearance = Mathf.Max(0.5f, diveCircleRadius);
        diveDropHorizontalSpeed = spec.speed;

        int maxLoops = Mathf.Max(1, spec.maxLoops);
        diveTargetLoops = Random.Range(1, maxLoops + 1);

        lastPlayerX = player.transform.position.x;
        playerXVelocity = 0f;
    }

    #endregion

    #region Movement

    private void UpdateMovement()
    {
        if (!canMove || spec == null)
            return;

        switch (currentMovementType)
        {
            case SpecialMovementType.None:
                canMove = false;
                break;

            case SpecialMovementType.Straight:
                Straight();
                break;

            case SpecialMovementType.Arch:
                Arch();
                break;

            case SpecialMovementType.Homing:
                Homing();
                break;

            case SpecialMovementType.Dive:
                Dive();
                break;
        }
    }

    private void Straight()
    {
        if (rb == null)
            return;

        rb.linearVelocity = moveVelocity;
    }

    private void Arch()
    {
        if (rb == null)
            return;

        Vector2 velocity = rb.linearVelocity;

        if (velocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
    #region Homing
    #region Homing Core

    private void Homing()
    {
        if (!CanRunHoming())
            return;

        if (UpdateHomingTimer())
            return;

        Vector2 currentPos = rb.position;
        Vector2 playerPos = player.transform.position;

        bool groundTooClose = IsGroundTooClose(currentPos);
        bool ceilingTooClose = IsCeilingTooClose(currentPos);
        bool tooLow = IsTooLow(currentPos);
        bool hasSafeHeight = HasSafeHeight(currentPos, groundTooClose);
        bool tooHigh = IsTooHigh(currentPos);

        if (tooHigh)
            homingPhase = HomingPhase.Attack;
        else
            UpdateHomingPhase(tooLow, hasSafeHeight, groundTooClose);

        Vector2 desiredDirection;

        if (tooHigh)
            desiredDirection = GetTooHighDirection(currentPos, playerPos, groundTooClose, ceilingTooClose);
        else
            desiredDirection = GetHomingDirection(
                currentPos,
                playerPos,
                groundTooClose,
                ceilingTooClose,
                tooLow,
                tooHigh
            );

        ApplyHomingMovement(desiredDirection);
    }

    #endregion

    #region Homing Helpers

    private bool CanRunHoming()
    {
        return rb != null && player != null;
    }

    private bool UpdateHomingTimer()
    {
        moveTimer -= Time.deltaTime;

        if (moveTimer > 0f)
            return false;

        rb.gravityScale = 1f;
        currentMovementType = SpecialMovementType.Arch;
        return true;
    }

    private bool IsGroundTooClose(Vector2 currentPos)
    {
        RaycastHit2D groundHit = Physics2D.Raycast(
            currentPos,
            Vector2.down,
            homingGroundProbeDistance,
            groundLayer
        );

        if (groundHit.collider == null)
            return false;

        if (groundHit.collider.CompareTag("Platform"))
            return false;

        return groundHit.distance <= minGroundClearance;
    }

    private bool IsTooLow(Vector2 currentPos)
    {
        return currentPos.y <= homingLowestY;
    }
    private bool IsTooHigh(Vector2 currentPos)
    {
        return currentPos.y > GetCurrentPlayerBandY() + 2f;
    }

    private bool HasSafeHeight(Vector2 currentPos, bool groundTooClose)
    {
        return !groundTooClose && currentPos.y > homingLowestY + 0.5f;
    }

    private void UpdateHomingPhase(bool tooLow, bool hasSafeHeight, bool groundTooClose)
    {
        if (homingPhase == HomingPhase.Attack && (tooLow || groundTooClose))
            homingPhase = HomingPhase.Recover;
        else if (homingPhase == HomingPhase.Recover && hasSafeHeight)
            homingPhase = HomingPhase.Attack;
    }

    private Vector2 GetHomingDirection(
     Vector2 currentPos,
     Vector2 playerPos,
     bool groundTooClose,
     bool ceilingTooClose,
     bool tooLow,
     bool tooHigh)
    {
        if (homingPhase == HomingPhase.Recover && !ceilingTooClose)
            return GetRecoverDirection(currentPos, playerPos);

        return GetAttackDirection(currentPos, playerPos, groundTooClose, ceilingTooClose, tooLow, tooHigh);
    }

    private Vector2 GetRecoverDirection(Vector2 currentPos, Vector2 playerPos)
    {
        float xDir = Mathf.Sign(playerPos.x - currentPos.x);

        if (Mathf.Abs(playerPos.x - currentPos.x) <= 0.1f)
            xDir = Mathf.Sign(homingDirection.x);

        if (Mathf.Abs(xDir) <= 0.01f)
            xDir = 1f;

        return new Vector2(xDir, spec.homingRecoverUpStrength).normalized;
    }

    private Vector2 GetAttackDirection(
    Vector2 currentPos,
    Vector2 playerPos,
    bool groundTooClose,
    bool ceilingTooClose,
    bool tooLow,
    bool tooHigh)
    {
        Vector2 desiredDirection = (playerPos - currentPos).normalized;

        if (groundTooClose || tooLow)
        {
            desiredDirection = new Vector2(
                desiredDirection.x,
                Mathf.Max(desiredDirection.y, spec.homingRecoverUpStrength)
            ).normalized;
        }
        else if (ceilingTooClose)
        {
            desiredDirection = new Vector2(
                desiredDirection.x,
                Mathf.Min(desiredDirection.y, -spec.homingRecoverUpStrength)
            ).normalized;
        }
        else if (tooHigh)
        {
            desiredDirection = new Vector2(
                desiredDirection.x,
                Mathf.Min(desiredDirection.y, -0.75f)
            ).normalized;
        }

        return desiredDirection;
    }

    private void ApplyHomingMovement(Vector2 desiredDirection)
    {
        homingDirection = Vector2.Lerp(
            homingDirection,
            desiredDirection,
            spec.homingTurnSpeed * Time.deltaTime
        ).normalized;

        moveVelocity = homingDirection * spec.speed;
        rb.linearVelocity = moveVelocity;

        if (moveVelocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(moveVelocity.y, moveVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private float GetCurrentPlayerBandY()
    {
        if (player == null)
            return transform.position.y;

        Vector2 playerPos = player.transform.position;

        if (playerCapsule == null)
            playerCapsule = player.GetComponent<CapsuleCollider2D>();

        if (playerCapsule == null)
            return playerPos.y;

        Bounds bounds = playerCapsule.bounds;

        switch (homingHeightBand)
        {
            case HomingHeightBand.Feet:
                return bounds.min.y;

            case HomingHeightBand.Body:
                return bounds.center.y;

            case HomingHeightBand.Face:
                return bounds.max.y - (bounds.size.y * 0.2f);
        }

        return bounds.center.y;
    }
    private bool IsCeilingTooClose(Vector2 currentPos)
    {
        RaycastHit2D ceilingHit = Physics2D.Raycast(
            currentPos,
            Vector2.up,
            homingGroundProbeDistance,
            groundLayer
        );

        if (ceilingHit.collider == null)
            return false;

        if (ceilingHit.collider.CompareTag("Platform"))
            return false;

        return ceilingHit.distance <= minGroundClearance;
    }
    private Vector2 GetTooHighDirection(
    Vector2 currentPos,
    Vector2 playerPos,
    bool groundTooClose,
    bool ceilingTooClose)
    {
        Vector2 desiredDirection = (playerPos - currentPos).normalized;

        if (groundTooClose)
        {
            desiredDirection = new Vector2(
                desiredDirection.x,
                Mathf.Max(desiredDirection.y, spec.homingRecoverUpStrength)
            ).normalized;
        }
        else if (ceilingTooClose)
        {
            desiredDirection = new Vector2(
                desiredDirection.x,
                Mathf.Min(desiredDirection.y, -spec.homingRecoverUpStrength)
            ).normalized;
        }
        else
        {
            desiredDirection = new Vector2(
                desiredDirection.x,
                Mathf.Min(desiredDirection.y, -0.85f)
            ).normalized;
        }

        return desiredDirection;
    }

    #endregion

    #endregion

    #region Dive

    #region Dive Core

    private void Dive()
    {
        if (!CanRunDive())
            return;

        UpdatePlayerXVelocity();

        switch (divePhase)
        {
            case DivePhase.FlyToStart:
                DiveFlyToStart();
                break;

            case DivePhase.Loop:
                DiveLoop();
                break;

            case DivePhase.AttackRise:
                DiveAttackRise();
                break;

            case DivePhase.AttackDrop:
                DiveAttackDrop();
                break;
        }
    }

    private void DiveFlyToStart()
    {
        Vector2 startTarget = GetDiveLoopStart();

        MoveDiveTowards(startTarget, spec.speed);
        FaceDiveVelocity();

        if (Vector2.Distance(rb.position, startTarget) <= 0.2f)
        {
            rb.linearVelocity = Vector2.zero;
            diveLoopAngle = Mathf.PI * 0.5f;
            divePhase = DivePhase.Loop;
        }
    }

    private void DiveLoop()
    {
        diveLoopAngle += spec.speed * Time.deltaTime;

        Vector2 targetPos = GetDiveLoopPosition(diveLoopAngle);
        rb.position = targetPos;

        moveVelocity = GetDiveLoopTangent(diveLoopAngle) * spec.speed;
        FaceDiveVelocity();

        if (diveLoopAngle >= Mathf.PI * 2.5f)
        {
            diveLoopAngle -= Mathf.PI * 2f;
            diveLoopCount++;

            if (diveLoopCount >= diveTargetLoops)
                divePhase = DivePhase.AttackRise;
        }
    }

    private void DiveAttackRise()
    {
        Vector2 riseTarget = GetDiveRiseTarget();
        rb.position = riseTarget;

        moveVelocity = Vector2.up * spec.speed;
        FaceDiveVelocity();

        float leadTime = 0.2f;
        float maxLead = diveCircleRadius;
        float predictedX = player.transform.position.x + (playerXVelocity * leadTime);

        diveLockedX = Mathf.Clamp(
            predictedX,
            player.transform.position.x - maxLead,
            player.transform.position.x + maxLead
        );

        float dropDistanceX = diveLockedX - rb.position.x;
        float dropSpeedY = spec.speed + diveAttackSpeedBoost;
        float estimatedDropTime = (diveCircleRadius * 2f) / Mathf.Max(dropSpeedY, 0.01f);

        diveDropVelocityX = dropDistanceX / Mathf.Max(estimatedDropTime, 0.01f);
        divePhase = DivePhase.AttackDrop;
    }

    private void DiveAttackDrop()
    {
        moveVelocity = new Vector2(
            diveDropVelocityX,
            -(spec.speed + diveAttackSpeedBoost)
        );

        rb.linearVelocity = moveVelocity;
        FaceDiveVelocity();
    }

    #endregion

    #region Dive Helpers
    private void UpdatePlayerXVelocity()
    {
        if (player == null)
            return;

        float currentX = player.transform.position.x;
        playerXVelocity = (currentX - lastPlayerX) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPlayerX = currentX;
    }

    private Vector2 GetDiveLoopTangent(float angle)
    {
        float x = -Mathf.Sin(angle);
        float y = Mathf.Cos(angle);
        return new Vector2(x, y).normalized;
    }

    private Vector2 GetDiveAnchor()
    {
        Vector2 playerPos = player.transform.position;
        float playerTopY = GetDivePlayerTopY();

        return new Vector2(playerPos.x, playerTopY + divePlayerClearance + diveCircleRadius);
    }

    private Vector2 GetDiveLoopStart()
    {
        return GetDiveAnchor() + Vector2.up * diveCircleRadius;
    }

    private Vector2 GetDiveLoopPosition(float angle)
    {
        return GetDiveAnchor() + GetDiveLoopOffset(angle);
    }

    private Vector2 GetDiveRiseTarget()
    {
        Vector2 playerPos = player.transform.position;
        float playerTopY = GetDivePlayerTopY();

        return new Vector2(playerPos.x, playerTopY + divePlayerClearance + (diveCircleRadius * 2f));
    }

    private float GetDivePlayerTopY()
    {
        if (player == null)
            return transform.position.y;

        if (playerCapsule == null)
            playerCapsule = player.GetComponent<CapsuleCollider2D>();

        if (playerCapsule == null)
            return player.transform.position.y;

        return playerCapsule.bounds.max.y;
    }

    private bool CanRunDive()
    {
        return rb != null && player != null;
    }

    private Vector2 GetDiveLoopOffset(float angle)
    {
        float x = Mathf.Cos(angle) * diveCircleRadius;
        float y = Mathf.Sin(angle) * diveCircleRadius;
        return new Vector2(x, y);
    }

    private void MoveDiveTowards(Vector2 targetPos, float speed)
    {
        Vector2 direction = (targetPos - rb.position).normalized;
        moveVelocity = direction * speed;
        rb.linearVelocity = moveVelocity;
    }

    private void FaceDiveVelocity()
    {
        if (moveVelocity.sqrMagnitude <= 0.001f)
            return;

        float angle = Mathf.Atan2(moveVelocity.y, moveVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    #endregion

    #endregion

    #endregion

    #region Controls / Utility

    public void StopMovement()
    {
        canMove = false;
        currentMovementType = SpecialMovementType.None;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
    }

    public void ChangeDirection()
    {
        if (rb != null && (currentMovementType == SpecialMovementType.Arch || currentMovementType == SpecialMovementType.Homing))
        {
            float xDir = -Mathf.Sign(rb.linearVelocity.x);

            if (Mathf.Abs(rb.linearVelocity.x) <= 0.01f)
                xDir = -Mathf.Sign(moveVelocity.x);

            if (Mathf.Abs(xDir) <= 0.01f)
                xDir = -1f;

            moveVelocity = new Vector2(xDir * spec.speed, 0f);
            rb.gravityScale = 0f;
            rb.linearVelocity = moveVelocity;
            currentMovementType = SpecialMovementType.Straight;
        }
        else
        {
            moveVelocity *= -1f;

            if (rb != null)
                rb.linearVelocity = moveVelocity;
        }

        float angle = Mathf.Atan2(moveVelocity.y, moveVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    #endregion
}