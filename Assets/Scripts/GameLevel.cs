using UnityEngine;

public class GameLevel : MonoBehaviour
{
   [SerializeField] private int levelNumber;
   [SerializeField] private Transform landerStartPositionTransform;
   [SerializeField] private Transform cameraStartTargetTransform;
   [SerializeField] private float zoomedOutOrtographicSize;

   public int GetLevelNumber()
    {
        return levelNumber;
    }
    
    public Vector3 GetLanderStartPosition()
    {
        return landerStartPositionTransform.position;
    }

    public Transform GetCameraStartTargetTransform()
    {
        return cameraStartTargetTransform;
    }

    public float GetZoomedOutOrtographicSize()
    {
        return zoomedOutOrtographicSize;
    }
}
