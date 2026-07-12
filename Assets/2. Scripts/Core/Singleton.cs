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

    protected virtual void Awake()
    {
        if (dontDestroy == false)
            return;

        if (transform.parent != null && transform.root != null)
        {
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}