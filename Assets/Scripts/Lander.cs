using System;
using UnityEngine;

public class Lander : MonoBehaviour
{
    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }

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

    public const float GRAVITY_NORMAL = 0.7f;
    private const float GAMEPAD_DEADZONE = 0.4f;
    private const float SOFT_LANDING_SPEED = 4f;
    private const float MIN_LANDING_DOT = 0.9f;
    private const float MAX_LANDING_ANGLE_SCORE = 100f;
    private const float LANDING_ANGLE_PENALTY = 10f;
    private const float LANDING_SPEED_SCORE_MULTIPLIER = 100f;

    public static Lander Instance { get; private set; }

    public event EventHandler OnUpForce;
    public event EventHandler OnLeftForce;
    public event EventHandler OnRightForce;
    public event EventHandler OnBeforeForce;
    public event EventHandler OnCoinPickup;
    public event EventHandler OnFuelPickup;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public event EventHandler<OnLandedEventArgs> OnLanded;

    [SerializeField] private float force = 14f;
    [SerializeField] private float turnSpeed = 2f;

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

        Vector2 movementInput =
            GameInput.Instance.GetMovementInputVector2();

        switch (state)
        {
            case State.WaitingToStart:
                HandleWaitingToStart(movementInput);
                break;

            case State.Normal:
                HandleNormalFlight(movementInput);
                break;

            case State.GameOver:
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (state != State.Normal)
        {
            return;
        }

        if (!collision2D.gameObject.TryGetComponent(out LandingPad landingPad))
        {
            Debug.Log("Crashed on the terrain");

            CompleteLanding(new OnLandedEventArgs
            {
                landingType = LandingType.WrongLandingArea,
                dotVector = 0f,
                landingSpeed = 0f,
                scoreMultiplier = 0,
                score = 0,
            });

            return;
        }

        EvaluateLanding(collision2D, landingPad);
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
            OnFuelPickup?.Invoke(this, EventArgs.Empty);
            fuelPickup.DestroySelf();
        }

        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
            OnCoinPickup?.Invoke(this, EventArgs.Empty);
            coinPickup.DestroySelf();
        }
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
        if (fuelAmount <= amount * Time.deltaTime)
        {
            fuelAmount = 0f;
            return;
        }
        fuelAmount -= fuelConsumptionAmount * Time.deltaTime;
    }

    private void HandleWaitingToStart(Vector2 movementInput)
    {
        bool hasGamepadInput =
            movementInput.sqrMagnitude >
            GAMEPAD_DEADZONE * GAMEPAD_DEADZONE;

        if (GameInput.Instance.IsUpActionPressed() ||
            GameInput.Instance.IsLeftActionPressed() ||
            GameInput.Instance.IsRightActionPressed() ||
            hasGamepadInput)
        {
            landerRigidbody2D.gravityScale = GRAVITY_NORMAL;
            SetState(State.Normal);
        }
    }

    private void HandleNormalFlight(Vector2 movementInput)
    {
        if (fuelAmount <= 0f)
        {
            return;
        }

        if (GameInput.Instance.IsUpActionPressed() ||
            movementInput.y > GAMEPAD_DEADZONE)
        {
            landerRigidbody2D.AddForce(force * transform.up);
            ConsumeFuel(1f);
            OnUpForce?.Invoke(this, EventArgs.Empty);
        }

        if (GameInput.Instance.IsLeftActionPressed() ||
            movementInput.x < -GAMEPAD_DEADZONE)
        {
            landerRigidbody2D.AddTorque(turnSpeed);
            ConsumeFuel(0.3f);
            OnLeftForce?.Invoke(this, EventArgs.Empty);
        }

        if (GameInput.Instance.IsRightActionPressed() ||
            movementInput.x > GAMEPAD_DEADZONE)
        {
            landerRigidbody2D.AddTorque(-turnSpeed);
            ConsumeFuel(0.3f);
            OnRightForce?.Invoke(this, EventArgs.Empty);
        }
    }

    private void EvaluateLanding(
        Collision2D collision2D,
        LandingPad landingPad)
    {
        float landingSpeed =
            collision2D.relativeVelocity.magnitude;

        if (landingSpeed > SOFT_LANDING_SPEED)
        {
            Debug.Log("Landed too hard!");

            CompleteLanding(new OnLandedEventArgs
            {
                landingType = LandingType.TooFastLanding,
                dotVector = 0f,
                landingSpeed = landingSpeed,
                scoreMultiplier = 0,
                score = 0,
            });

            return;
        }

        float landingDot =
            Vector2.Dot(Vector2.up, transform.up);

        if (landingDot < MIN_LANDING_DOT)
        {
            Debug.Log("Landed on too steep angle.");

            CompleteLanding(new OnLandedEventArgs
            {
                landingType = LandingType.TooSteepAngle,
                dotVector = landingDot,
                landingSpeed = landingSpeed,
                scoreMultiplier = 0,
                score = 0,
            });

            return;
        }

        Debug.Log("Successful landing.");

        int scoreMultiplier = landingPad.GetScoreMultiplier();
        int score = CalculateLandingScore(
            landingDot,
            landingSpeed,
            scoreMultiplier);

        Debug.Log("score: " + score);

        CompleteLanding(new OnLandedEventArgs
        {
            landingType = LandingType.Success,
            dotVector = landingDot,
            landingSpeed = landingSpeed,
            scoreMultiplier = scoreMultiplier,
            score = score,
        });
    }

    private int CalculateLandingScore(
        float landingDot,
        float landingSpeed,
        int scoreMultiplier)
    {
        float landingAngleScore =
            MAX_LANDING_ANGLE_SCORE -
            Mathf.Abs(landingDot - 1f) *
            LANDING_ANGLE_PENALTY *
            MAX_LANDING_ANGLE_SCORE;

        float landingSpeedScore =
            (SOFT_LANDING_SPEED - landingSpeed) *
            LANDING_SPEED_SCORE_MULTIPLIER;

        Debug.Log("LandingAngleScore:" + landingAngleScore);
        Debug.Log("LandingSpeedScore:" + landingSpeedScore);

        return Mathf.RoundToInt(
            landingAngleScore + landingSpeedScore) *
            scoreMultiplier;
    }

    private void CompleteLanding(
        OnLandedEventArgs landingResult)
    {
        OnLanded?.Invoke(this, landingResult);
        SetState(State.GameOver);
    }
}
