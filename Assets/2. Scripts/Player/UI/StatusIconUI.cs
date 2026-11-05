using System.Collections.Generic;
using UnityEngine;

// 플레이어에게 걸린 버프/디버프/실드/무적/아이템 쿨타임을 아이콘으로 나열한다.
// 배치는 이 오브젝트의 GridLayoutGroup이 맡는다(StartCorner LowerLeft + Horizontal이면 아래 줄부터 옆으로 쌓이고,
// 줄이 차면 위 줄로 올라간다). 새로 생긴 아이콘은 항상 맨 뒤에 붙고, 사라지면 뒤쪽이 앞으로 당겨진다.
// 시간이 흐르지 않는 항목은 아이콘과 종류가 같으면 한 칸으로 겹치고 숫자를 더한다(숫자가 없는 항목은 1로 센다).
// 시간이 흐르는 항목(버프 지속시간, 쿨타임)은 남은 시간이 서로 달라 겹치지 않는다.
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
        MergeUntimed();

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

    // 겹친 칸의 Key는 (아이콘, 종류)로 두어, 겹치는 개수가 바뀌어도 같은 칸을 이어 쓴다.
    private void MergeUntimed()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var first = entries[i];
            if (first.IsTimed || first.Icon == null)
                continue;

            int total = Mathf.Max(first.Count, 1);
            bool merged = false;
            for (int j = entries.Count - 1; j > i; j--)
            {
                var other = entries[j];
                if (other.IsTimed || other.Icon != first.Icon || other.Kind != first.Kind)
                    continue;
                total += Mathf.Max(other.Count, 1);
                entries.RemoveAt(j);
                merged = true;
            }

            first.Key = (first.Icon, first.Kind);
            if (merged)
                first.Count = total;
            entries[i] = first;
        }
    }
}
