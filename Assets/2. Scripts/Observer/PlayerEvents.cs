public struct PlayerIdleEvent : ISubject { }

public struct PlayerMovedEvent : ISubject
{
    public float Speed { get; private set; }
    public PlayerMovedEvent(float speed) { Speed = speed; }
}
public struct PlayerJumpedEvent : ISubject { }
public struct PlayerFallingEvent : ISubject { }
public struct PlayerLandedEvent : ISubject { }
public struct PlayerTurnEvent : ISubject { }


public struct PlayerDamagedEvent : ISubject
{
    public int Damage { get; private set; }
    public PlayerDamagedEvent(int damage) { Damage = damage; }
}

