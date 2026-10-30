using System;
using UnityEngine;

[Serializable]
public struct EntityFeedbacks
{
    [field: SerializeField] public ActionFeedback Land { get; private set; }
    [field: SerializeField] public ActionFeedback Turn { get; private set; }
    [field: SerializeField] public ActionFeedback Run { get; private set; }
    [field: SerializeField] public ActionFeedback Dash { get; private set; }
    [field: SerializeField] public ActionFeedback Hit { get; private set; }
}
