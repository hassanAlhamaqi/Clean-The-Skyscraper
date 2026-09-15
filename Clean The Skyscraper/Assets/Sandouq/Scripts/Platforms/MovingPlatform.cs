using System;
using UnityEngine;

namespace Sandouq.Platforms
{
    // The platform owns its state. A player button request is separate from applying that state.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovingPlatform : MonoBehaviour
    {
        public enum MotionState { StoppedBeforeAscent, Ascending, StoppedBeforeDescent, Descending }
        [Header("Travel")]
        [SerializeField, Min(0.1f)] private float travelHeight = 16f;
        [SerializeField, Min(0.1f)] private float speed = 1.2f;
        [Header("Offline authority")]
        [Tooltip("Disable when a future host/network adapter will control this platform.")]
        [SerializeField] private bool simulateLocally = true;
        [SerializeField] private string platformId = "facade-lift";
        [SerializeField] private MotionState state;
        private Rigidbody body;
        private Vector3 bottomPosition;
        public MotionState State => state;
        public float Height => transform.position.y - bottomPosition.y;
        public float TravelHeight => travelHeight;
        public string PlatformId => platformId;
        public event Action<MotionState> StateChanged;
        public event Action<string> ButtonPressRequested;
        public string NextAction => state == MotionState.StoppedBeforeAscent ? "Raise platform"
            : state == MotionState.StoppedBeforeDescent ? "Lower platform" : "Stop platform";

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            bottomPosition = transform.position;
            body.isKinematic = true;
            body.useGravity = false;
        }
        public void RequestButtonPress(string playerId)
        {
            ButtonPressRequested?.Invoke(playerId);
            if (simulateLocally) AdvanceState();
        }
        public void AdvanceState() => SetState((MotionState)(((int)state + 1) % 4));
        private void SetState(MotionState value)
        {
            if (state == value) return;
            state = value;
            StateChanged?.Invoke(state);
        }
        private void FixedUpdate()
        {
            if (simulateLocally) Simulate(Time.fixedDeltaTime);
        }
        public void Simulate(float deltaTime)
        {
            if (state != MotionState.Ascending && state != MotionState.Descending) return;
            float target = state == MotionState.Ascending ? travelHeight : 0;
            float height = Mathf.MoveTowards(body.position.y - bottomPosition.y, target, speed * deltaTime);
            body.MovePosition(bottomPosition + Vector3.up * height);
            // End stops are physical limits: reaching one selects the next safe direction.
            if (Mathf.Approximately(height, target))
                SetState(target > 0 ? MotionState.StoppedBeforeDescent : MotionState.StoppedBeforeAscent);
        }
    }
}
