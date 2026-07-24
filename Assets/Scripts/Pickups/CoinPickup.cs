using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public void DestroySelf()
    {
        Destroy(this.gameObject);
    }
}
