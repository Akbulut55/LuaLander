using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Lander : MonoBehaviour
{
    public const float GRAVITY_NORMAL = 0.7f;

    public static Lander Instance { get; private set; }

    public event EventHandler OnUpForce;
    public event EventHandler OnLeftForce;
    public event EventHandler OnRightForce;
    public event EventHandler OnBeforeForce;
    public event EventHandler OnCoinPickup;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }
    public event EventHandler<OnLandedEventArgs> OnLanded;
    public class OnLandedEventArgs : EventArgs
    {
        public LandingType landingType;
        public int score;
        public float dotVector;
        public float landingSpeed;
        public float scoreMultiplier;
    }

    public enum LandingType
    {
        Success,
        WrongLandingArea,
        TooSteepAngle,
        TooFastLanding
    }

    public enum State
    {
        WaitingToStart,
        Normal,
        GameOver,
    }

    private Rigidbody2D landerRigidbody2D;
    private float fuelAmount;
    private float fuelAmountMax = 10f;
    private State state;

    private void Awake()
    {
        Instance = this;

        fuelAmount = fuelAmountMax;
        state = State.WaitingToStart;

        landerRigidbody2D = GetComponent<Rigidbody2D>();
        landerRigidbody2D.gravityScale = 0f;
    }

    private void FixedUpdate()
    {
        OnBeforeForce?.Invoke(this, EventArgs.Empty);

        switch (state)
        {
            default:
                case State.WaitingToStart:
                    if (Keyboard.current.upArrowKey.isPressed ||
                        Keyboard.current.leftArrowKey.isPressed ||
                        Keyboard.current.rightArrowKey.isPressed)
                    {
                        landerRigidbody2D.gravityScale = GRAVITY_NORMAL;
                        SetState(State.Normal);
                    }
                    break;
                case State.Normal:
                    if (fuelAmount <= 0f)
                    {   
                        // No fuel
                        return;
                    }

                    if (Keyboard.current.upArrowKey.isPressed)
                    {
                        float force = 700f;
                        landerRigidbody2D.AddForce(force * transform.up * Time.deltaTime);
                        ConsumeFuel(1f);
                        OnUpForce?.Invoke(this, EventArgs.Empty);
                    }

                    if (Keyboard.current.leftArrowKey.isPressed)
                    {
                        float turnSpeed = 100f;
                        landerRigidbody2D.AddTorque(turnSpeed * Time.deltaTime);
                        ConsumeFuel(0.3f);
                        OnLeftForce?.Invoke(this, EventArgs.Empty);

                    }

                    if (Keyboard.current.rightArrowKey.isPressed)
                    {
                        float turnSpeed = -100f;
                        landerRigidbody2D.AddTorque(turnSpeed * Time.deltaTime);
                        ConsumeFuel(0.3f);
                        OnRightForce?.Invoke(this, EventArgs.Empty);

                    }
                    break;
                case State.GameOver:
                    break;
        }

    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (!collision2D.gameObject.TryGetComponent(out LandingPad landingPad))
        {
            Debug.Log("Crashed on the terrain");
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.WrongLandingArea,
                dotVector = 0f,
                landingSpeed = 0f,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        float softLandingVelocityMagnitude = 4f;
        float relativeVelocityMagnitude = collision2D.relativeVelocity.magnitude;
        if (relativeVelocityMagnitude > softLandingVelocityMagnitude)
        {
            Debug.Log("Landed too hard!");
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.TooFastLanding,
                dotVector = 0f,
                landingSpeed = relativeVelocityMagnitude,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        float dotVector = Vector2.Dot(Vector2.up, transform.up);
        float minDotVector = .90f;
        if (dotVector < minDotVector)
        {
            Debug.Log("Landed on too steep angle.");
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.TooSteepAngle,
                dotVector = dotVector,
                landingSpeed = relativeVelocityMagnitude,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        Debug.Log("Successful landing.");

        float maxScoreAmountLandingAngle = 100;
        float maxScoreDotVectorMultiplier = 10f;
        float landingAngleScore = maxScoreAmountLandingAngle - Mathf.Abs(dotVector - 1f) * maxScoreDotVectorMultiplier * maxScoreAmountLandingAngle;

        float maxScoreAmountLandingSpeed = 100;
        float landingSpeedScore = (softLandingVelocityMagnitude - relativeVelocityMagnitude) * maxScoreAmountLandingSpeed;

        Debug.Log("LandingAngleScore:" + landingAngleScore);
        Debug.Log("LandingSpeedScore:" + landingSpeedScore);

        int score = (Mathf.RoundToInt(landingAngleScore + landingSpeedScore) * landingPad.GetScoreMultiplier());

        Debug.Log("score: " + score);
        OnLanded?.Invoke(this, new OnLandedEventArgs
        {
            landingType = LandingType.Success,
            dotVector = dotVector,
            landingSpeed = relativeVelocityMagnitude,
            scoreMultiplier = landingPad.GetScoreMultiplier(),
            score = score,
        });
        SetState(State.GameOver);
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.gameObject.TryGetComponent(out FuelPickup fuelPickup))
        {
            float addFuelAmount = 10f;
            fuelAmount += addFuelAmount;
            if (fuelAmount > fuelAmountMax)
            {
                fuelAmount = fuelAmountMax;
            }
            fuelPickup.DestroySelf();
        }

        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
            OnCoinPickup?.Invoke(this, EventArgs.Empty);
            coinPickup.DestroySelf();
        }
    }

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
        {
            state = state,
        });
    }

    private void ConsumeFuel(float amount)
    {
        float fuelConsumptionAmount = amount;
        if (fuelAmount <= amount)
        {
            fuelAmount = 0f;
            return;
        }
        fuelAmount -= fuelConsumptionAmount * Time.deltaTime;
    }

    public float GetFuel()
    {
        return fuelAmount;
    }

    public float GetFuelAmountNormalized()
    {
        return fuelAmount / fuelAmountMax;
    }

    public float GetSpeedX()
    {
        return landerRigidbody2D.linearVelocityX;
    }

    public float GetSpeedY()
    {
        return landerRigidbody2D.linearVelocityY;
    }
}
