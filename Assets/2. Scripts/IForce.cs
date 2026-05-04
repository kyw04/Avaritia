using UnityEngine;

public interface IForce
{
    Vector3 GetForce();
    bool IsFinished { get; }
}