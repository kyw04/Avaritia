using System;
using UnityEngine;

[Serializable]
public struct EntityParticles
{
    [field: SerializeField] public PooledParticle Land { get; private set; }
    [field: SerializeField] public PooledParticle Turn { get; private set; }
    [field: SerializeField] public PooledParticle Run { get; private set; }
    [field: SerializeField] public PooledParticle Dash { get; private set; }
}