using System;
using System.Collections.Generic;
using UnityEngine;

public class ImageCountBar : MonoBehaviour
{
    [SerializeField] private ImageCountPoolable imagePrefab;
    [SerializeField] private int currentCount;
    [SerializeField] private int maxCount;

    private List<ImageCountPoolable> images = new();

    private Func<int> getCurrentCount;
    private Func<int> getMaxCount;
    
    public bool IsActive { get; private set; }

    private void Awake()
    {
        Init(imagePrefab, () => currentCount, () => maxCount);
        Refresh();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;

        foreach (var img in images)
            img.gameObject.SetActive(false);
    }
    
    public void Init(ImageCountPoolable prefab, Func<int> currentCountBridge, Func<int> maxCountBridge)
    {
        imagePrefab = prefab;
        getCurrentCount = currentCountBridge;
        getMaxCount = maxCountBridge;
        maxCount = getMaxCount();
        
        foreach (var img in images)
        {
            img.Set(prefab);
            img.transform.SetParent(transform);
        }
        for (int i = images.Count; i < maxCount; i++)
        {
            var newImage = ObjectPoolManager.Instance.Spawn(imagePrefab, Vector3.zero, Quaternion.identity);
            newImage.transform.SetParent(transform);
        }
        images.Clear();
        transform.GetComponentsInChildren(true, images);
        Refresh();
    }
    
    public void Refresh()
    {
        currentCount = getCurrentCount();
        currentCount = currentCount < 0 ? maxCount : Mathf.Clamp(currentCount, 0, maxCount);
        for (int i = 0; i < images.Count; i++)
        {
            if (i >= maxCount)
            {
                images[i].gameObject.SetActive(false);
                continue;
            }

            images[i].gameObject.SetActive(i < currentCount);
        }
    }
}