using UnityEngine;


/*
    CRTP패턴으로 구현된 싱글톤 객체
    자식 클래스에서 씬 이동 시 dontDestroyOnLoad할 지 결정

    !!! 주의사항 !!!
    해당 클래스를 상속받는 추상 클래스의 경우, instance가 추상 클래스가 아닌 Singleton 객체를 참조하므로
    해당 클래스를 상속받지 말고, 대상 클래스 안에서 싱글톤 패턴을 구현하여야 에러 가능성이 줄어듬.
 */
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public readonly bool dontDestroy = true;

    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (T)FindAnyObjectByType(typeof(T));

                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name, typeof(T));
                    instance = obj.GetComponent<T>();
                }
            }
            return instance;
        }

    }

    // 이미 다른 인스턴스가 있으면 새로 생긴 쪽을 바로 지운다(씬을 다시 불러와 같은 싱글톤이 또 생긴 경우 등).
    // 지워지는 쪽은 OnAwake를 부르지 않아, 이벤트 구독이나 UIManager 등록이 원본과 겹치지 않는다.
    // Awake에서 꺼 두면 지워지기 전까지 OnEnable/Start/Update도 돌지 않는다.
    // 대신 OnDisable·OnDestroy는 불리므로, 거기서 OnAwake에서 만든 것을 쓰면 null 검사를 해야 한다.
    // DestroyImmediate는 Instantiate 중에 부르면 예외가 나므로 쓰지 않는다.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }
        instance = this as T;

        if (dontDestroy)
        {
            if (transform.parent != null && transform.root != null)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        OnAwake();
    }

    // 자식 클래스의 초기화. 살아남는 인스턴스에서만 불린다.
    protected virtual void OnAwake() { }
}