using UnityEngine;
using UnityEngine.Rendering;

// 배경 레이어 하나를 카메라에 따라 패럴랙스로 움직이고, 가로로 이어 붙인 자식 이미지들을 카메라 주위로 돌려 써서 끝없이 이어지게 함
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("카메라 이동을 따라가는 비율. 0 = 월드에 고정, 1 = 화면에 고정 (멀리 있는 레이어일수록 1에 가깝게)")]
    [SerializeField] private Vector2 parallax = new(0.5f, 1f);
    [Tooltip("비워두면 Main Camera 사용")]
    [SerializeField] private Camera targetCamera;

    private Transform[] tiles;
    private float tileWidth;
    private Vector3 startPos;
    private Vector3 startCamPos;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        tiles = new Transform[transform.childCount];
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = transform.GetChild(i);
        tileWidth = tiles[0].GetComponent<SpriteRenderer>().bounds.size.x;

        startPos = transform.position;
        startCamPos = targetCamera.transform.position;
    }

    private void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    private void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

    // Cinemachine이 카메라를 옮긴 뒤, 렌더 직전에 맞춰야 배경이 떨리지 않음
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam != targetCamera)
            return;

        Vector3 camPos = cam.transform.position;
        Vector3 delta = camPos - startCamPos;
        transform.position = startPos + new Vector3(delta.x * parallax.x, delta.y * parallax.y, 0f);

        // 카메라에서 전체 길이의 절반 이상 벗어난 이미지는 반대편 끝으로 옮김
        float span = tileWidth * tiles.Length;
        foreach (var tile in tiles)
        {
            float shift = Mathf.Round((tile.position.x - camPos.x) / span) * span;
            if (shift != 0f)
                tile.position -= new Vector3(shift, 0f, 0f);
        }
    }
}
