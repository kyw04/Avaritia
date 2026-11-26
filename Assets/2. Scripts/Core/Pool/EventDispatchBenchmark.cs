using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// 이벤트 호출 비용 측정. 적을 counts의 수만큼 한 번에 깔아 두고(생성·회수 비용이 섞이지 않게)
// warmupFrames 뒤 sampleFrames 동안 프레임 시간, EventBus/EntityEventHub Publish 시간, OnNotify 호출 수의 평균을 로그로 남긴다.
public class EventDispatchBenchmark : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int[] counts = { 100, 300, 900 };
    [SerializeField] private int warmupFrames = 60;
    [SerializeField] private int sampleFrames = 300;
    [SerializeField] private float spacing = 0.5f;
    [SerializeField] private int columns = 30;
    [SerializeField] private Vector2 origin;

    public bool IsDone { get; private set; }

    public void Run(GameObject enemyPrefab)
    {
        prefab = enemyPrefab;
        StartCoroutine(RunAll());
    }

    private IEnumerator RunAll()
    {
        foreach (int count in counts)
            yield return Measure(count);
        IsDone = true;
    }

    private IEnumerator Measure(int count)
    {
        var spawned = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
        {
            var position = origin + new Vector2(i % columns * spacing, i / columns * spacing);
            spawned.Add(ObjectPoolManager.Instance.Spawn(prefab, position, Quaternion.identity));
        }

        for (int i = 0; i < warmupFrames; i++)
            yield return null;

        using var mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        using var globalPublish = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "EventBus.Publish");
        using var localPublish = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "EntityEventHub.Publish");
        yield return null;

        double frameNs = 0, globalNs = 0, localNs = 0;
        long notifies = 0;
        for (int i = 0; i < sampleFrames; i++)
        {
            int before = EventBus.NotifyCount;
            yield return null;
            notifies += EventBus.NotifyCount - before;
            frameNs += mainThread.LastValue;
            globalNs += globalPublish.LastValue;
            localNs += localPublish.LastValue;
        }

        Debug.Log($"[EventBenchmark] N={count} frame={frameNs / sampleFrames / 1e6:F3}ms " +
                  $"EventBus.Publish={globalNs / sampleFrames / 1e6:F3}ms " +
                  $"EntityEventHub.Publish={localNs / sampleFrames / 1e6:F3}ms " +
                  $"OnNotify/frame={(double)notifies / sampleFrames:F0} " +
                  $"(recorders valid: {mainThread.Valid}/{globalPublish.Valid}/{localPublish.Valid})");

        foreach (var instance in spawned)
            if (instance != null && instance.activeSelf)
                ObjectPoolManager.Instance.Despawn(instance);
        yield return null;
    }
}
