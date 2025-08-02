// PlayerController.cs
// Tarodev 2D Controller converted to Unity 6.1 NEW Input System (ASCII only)

using System;
using UnityEngine;
using UnityEngine.InputSystem;   // NEW INPUT SYSTEM

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [Header("Stats")]
        [SerializeField] private ScriptableStats _stats;

        // -------------------------------------------------------------
        // INPUT ACTION REFERENCES
        // -------------------------------------------------------------
        [Header("Input Actions")]
        public InputActionReference moveAction;   // Value  (Vector2)  "Move"
        public InputActionReference jumpAction;   // Button (Button)   "Jump"

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;

        private FrameInput _frameInput;
        private Vector2 _frameVelocity;

        private bool _cachedQueryStartInColliders;
        private float _time;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        [Range(0, 89)] public float MaxSlopeAngle = 55;

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[3];
        private ContactFilter2D _groundFilter;
        private float _currentSlopeAngle;    // 0-90°
        private Vector2 _currentSlopeNormal; // cached for HandleDirection()


        #endregion

        //------------------------------------------------------------------
        // ENABLE / DISABLE INPUT ACTIONS
        //------------------------------------------------------------------
        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (jumpAction != null) jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null) jumpAction.action.Disable();
        }

        //------------------------------------------------------------------
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
            _groundFilter = new ContactFilter2D
            {
                useTriggers = false,
                layerMask = ~(1 << gameObject.layer)   // “hit everything except myself”
            };
        }

        //------------------------------------------------------------------
        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        //------------------------------------------------------------------
        // NEW INPUT IMPLEMENTATION
        //------------------------------------------------------------------
        private void GatherInput()
        {
            Vector2 move = Vector2.zero;
            bool jumpDown = false;
            bool jumpHeld = false;

            if (moveAction != null)
                move = moveAction.action.ReadValue<Vector2>();

            if (jumpAction != null)
            {
                jumpDown = jumpAction.action.WasPressedThisFrame();
                jumpHeld = jumpAction.action.IsPressed();
            }

            _frameInput = new FrameInput
            {
                Move = move,
                JumpDown = jumpDown,
                JumpHeld = jumpHeld
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Math.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold
                    ? 0
                    : Math.Sign(_frameInput.Move.x);

                _frameInput.Move.y = Math.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold
                    ? 0
                    : Math.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        //------------------------------------------------------------------
        private void FixedUpdate()
        {
            CheckCollisions();
            HandleJump();
            HandleDirection();
            HandleGravity();
            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            // Temporarily stop hits that start inside colliders (same as the original)
            Physics2D.queriesStartInColliders = false;

            // --------------------------------------------------
            // 1. GROUND CHECK  (three short rays under the feet)
            // --------------------------------------------------
            bool groundHit = false;
            _currentSlopeAngle = 90f;       // 90 = wall; anything <= MaxSlopeAngle is valid ground
            _currentSlopeNormal = Vector2.up;

            Bounds b = _col.bounds;
            float rayLen = _stats.GrounderDistance + 0.02f;
            Vector2[] rayStarts = {
        new Vector2(b.min.x + 0.05f, b.min.y),   // left foot
        new Vector2(b.center.x,       b.min.y),  // centre
        new Vector2(b.max.x - 0.05f, b.min.y)    // right foot
    };

            foreach (Vector2 start in rayStarts)
            {
                int hitCount = Physics2D.Raycast(start, Vector2.down, _groundFilter, _groundHits, rayLen);
                if (hitCount == 0) continue;

                RaycastHit2D hit = _groundHits[0];
                float slope = Vector2.Angle(hit.normal, Vector2.up);

                if (slope <= MaxSlopeAngle)
                {
                    groundHit = true;
                    _currentSlopeAngle = slope;
                    _currentSlopeNormal = hit.normal;
                    break;                              // found solid ground; stop checking
                }
            }

            // ------------------------------
            // 2. CEILING CHECK (capsule cast)
            // ------------------------------
            bool ceilingHit = Physics2D.CapsuleCast(
                _col.bounds.center,
                _col.size,
                _col.direction,
                0f,
                Vector2.up,
                _stats.GrounderDistance,
                ~(1 << gameObject.layer));              // ignore only the player layer

            if (ceilingHit)
                _frameVelocity.y = Mathf.Min(0f, _frameVelocity.y);   // bump head, kill upward v

            // ------------------------------
            // 3. STATE TRANSITIONS
            // ------------------------------
            if (!_grounded && groundHit)
            {
                // landed
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                // left the ground
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0f);
            }

            // Restore original setting
            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable &&
                                        _time < _timeJumpWasPressed + _stats.JumpBuffer;

        private bool CanUseCoyote => _coyoteUsable &&
                                     !_grounded &&
                                     _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly &&
                !_grounded &&
                !_frameInput.JumpHeld &&
                _rb.linearVelocity.y > 0)
            {
                _endedJumpEarly = true;
            }

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                float decel = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, decel * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    _frameInput.Move.x * _stats.MaxSpeed,
                    _stats.Acceleration * Time.fixedDeltaTime);
            }
            // If standing on an over-limit slope and trying to walk further up it, kill X velocity
            if (_grounded &&
                _currentSlopeAngle > MaxSlopeAngle &&
                Mathf.Sign(_frameInput.Move.x) == Mathf.Sign(_currentSlopeNormal.x))
            {
                _frameVelocity.x = 0;
                return; // skip regular acceleration logic
            }

        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                float gravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0)
                    gravity *= _stats.JumpEndEarlyGravityModifier;

                _frameVelocity.y = Mathf.MoveTowards(
                    _frameVelocity.y,
                    -_stats.MaxFallSpeed,
                    gravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        //------------------------------------------------------------------
        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
                Debug.LogWarning(
                    "Please assign a ScriptableStats asset to the Player Controller's Stats slot",
                    this);
        }
#endif
    }

    //----------------------------------------------------------------------
    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        event Action<bool, float> GroundedChanged;
        event Action Jumped;
        Vector2 FrameInput { get; }
    }
}
