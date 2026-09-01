public enum BounceSurface
{
    Floor,
    RightWall,
    Ceiling,
    LeftWall
}

public enum BouncePhase
{
    Attach,
    Launch,
    Airborne,
    Complete
}

public enum BounceJumpType
{
    Normal,
    FloorToCeiling
}

public enum BounceJumpSide
{
    Left,
    Right
}

public enum BounceAirMoveType
{
    None,
    Arc,
    Curve,
    Edge
}

public enum BounceEdgePoint
{
    None,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight
}

internal enum MovementRuntimeType
{
    Arc,
    Curve
}
