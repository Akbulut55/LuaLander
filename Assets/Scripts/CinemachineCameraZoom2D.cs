using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraZoom2D : MonoBehaviour
{
    private const float NORMAL_ORTHOGRAPHIC_SIZE = 10f;
    public static CinemachineCameraZoom2D Instance { get; private set; }
    [SerializeField] private CinemachineCamera cinemachineCamera;
    private float targetOrtographicSize = 10f;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        float zoomSpeed = 2f;
        cinemachineCamera.Lens.OrthographicSize = 
            Mathf.Lerp(cinemachineCamera.Lens.OrthographicSize, targetOrtographicSize, zoomSpeed * Time.deltaTime);
    }

    public void SetTargetOrtographicSize(float targetOrtographicSize)
    {
        this.targetOrtographicSize = targetOrtographicSize;
    }

    public void SetNormalOrtographicSize()
    {
        SetTargetOrtographicSize(NORMAL_ORTHOGRAPHIC_SIZE);
    }
}
