using System;
using UnityEngine;

[Serializable]
public struct EntitySounds
{
    [field: SerializeField] public AudioClip Land { get; private set; }
    [field: SerializeField] public AudioClip Dash { get; private set; }
    [field: SerializeField] public AudioClip Hit { get; private set; }
}
