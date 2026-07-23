using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int score;

    private void Start()
    {
        Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
        Lander.Instance.OnLanded += Lander_OnLander;
    }

    private void Lander_OnCoinPickup(object sender, System.EventArgs e)
    {
        AddScore(500);
    }

    private void Lander_OnLander(object sender, Lander.OnLandedEventArgs e)
    {
        AddScore(e.score);
    }

    public void AddScore(int addScoreAmount)
    {
        score += addScoreAmount;
        Debug.Log(score);
    }
}
