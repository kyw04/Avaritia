using System;
using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using Random = UnityEngine.Random;

// GC 비교용 스트레스 테스트. 씬에 붙이고 플레이하면 매 프레임 prefab을 spawnPerFrame개 생성하고
// lifetimeFrames 프레임 뒤 회수한다. usePool을 켜면 ObjectPoolManager로, 끄면 Instantiate/Destroy로 처리한다.
// Profiler(CPU Usage)에서 "PoolStressTest" 마커 아래 GC.Alloc, 타임라인의 GC.Collect 스파이크를 확인한다.
public class PoolStressTest : MonoBehaviour
{
    [SerializeField] private bool isStart = false;
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool usePool = true;
    [SerializeField] private int spawnPerFrame = 50;
    [SerializeField] private int lifetimeFrames = 30;
    [SerializeField] private float spawnRadius = 5f;

    [SerializeField] private TextMeshProUGUI startStopText;
    [SerializeField] private TextMeshProUGUI usePoolText;
    
    
    private static readonly ProfilerMarker SpawnMarker = new("PoolStressTest.Spawn");
    private static readonly ProfilerMarker DespawnMarker = new("PoolStressTest.Despawn");

    private readonly Queue<(GameObject instance, int despawnFrame)> alive = new();

    private void Start()
    {
        startStopText.text = isStart ? "중지" : "시작";
        usePoolText.text = usePool ? "풀사용 O" : "풀사용 X";
    }

    private void Update()
    {
        using (DespawnMarker.Auto())
        {
            while (alive.Count > 0 && alive.Peek().despawnFrame <= Time.frameCount)
            {
                var instance = alive.Dequeue().instance;
                // 프리팹이 스스로 회수됐으면(탄환 수명 등) 건너뛴다.
                if (instance == null || !instance.activeSelf)
                    continue;

                if (usePool)
                    ObjectPoolManager.Instance.Despawn(instance); // 비활성화 후 풀에 보관
                else
                    Destroy(instance);                            // 매번 파괴
            }
        }

        if (!isStart)
            return;
        
        using (SpawnMarker.Auto())
        {
            for (var i = 0; i < spawnPerFrame; i++)
            {
                var position = (Vector3)(Random.insideUnitCircle * spawnRadius);
                var instance = usePool
                    ? ObjectPoolManager.Instance.Spawn(prefab, position, Quaternion.identity) // 풀에서 꺼내 재사용
                    : Instantiate(prefab, position, Quaternion.identity);                     // 매번 새로 생성
                if (instance != null)
                    alive.Enqueue((instance, Time.frameCount + lifetimeFrames));
            }
        }
    }
    
    public void ClickChangePool()
    {
        usePool = !usePool;
        usePoolText.text = usePool ? "풀 사용 O" : "풀 사용 X";
    }

    public void ClickStartAndStop()
    {
        isStart = !isStart;
        startStopText.text = isStart ? "중지" : "시작";
    }
}
