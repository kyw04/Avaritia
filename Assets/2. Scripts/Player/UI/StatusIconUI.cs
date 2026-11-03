using System.Collections.Generic;
using UnityEngine;

// 플레이어에게 걸린 버프/디버프/실드/무적/아이템 쿨타임을 아이콘으로 나열한다.
// 배치는 이 오브젝트의 GridLayoutGroup이 맡는다(StartCorner LowerLeft + Horizontal이면 아래 줄부터 옆으로 쌓이고,
// 줄이 차면 위 줄로 올라간다). 새로 생긴 아이콘은 항상 맨 뒤에 붙고, 사라지면 뒤쪽이 앞으로 당겨진다.
public class StatusIconUI : MonoBehaviour
{
    [SerializeField] private StatusIconSlot slotPrefab;
    [SerializeField] private Entity target;

    private readonly List<StatusIconEntry> entries = new();
    private readonly List<StatusIconSlot> slots = new();

    private void Awake()
    {
        if (target == null) target = FindAnyObjectByType<Player>();
    }

    private void Update()
    {
        entries.Clear();
        if (target != null && target.isActiveAndEnabled)
            target.CollectStatusIcons(entries);

        // 기존 칸은 같은 Key의 항목으로 갱신하고, 사라진 항목의 칸은 반납한다.
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            int index = entries.FindIndex(e => e.Key.Equals(slots[i].Key));
            if (index < 0)
            {
                ObjectPoolManager.Instance.Despawn(slots[i]);
                slots.RemoveAt(i);
                continue;
            }
            slots[i].Set(entries[index]);
            entries.RemoveAt(index);
        }

        // 남은 항목은 새로 생긴 것 — 맨 뒤에 붙인다.
        foreach (var entry in entries)
        {
            var slot = ObjectPoolManager.Instance.Spawn(slotPrefab, Vector3.zero, Quaternion.identity);
            slot.transform.SetParent(transform, false);
            slot.transform.SetAsLastSibling();
            slot.transform.localScale = Vector3.one;
            slot.Set(entry);
            slots.Add(slot);
        }
    }
}
