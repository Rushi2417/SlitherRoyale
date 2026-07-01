using UnityEngine;
using WormCore;

namespace SlitherRoyale.Client.Networking
{
    public class ClientPrediction : MonoBehaviour
    {
        private WormState _predictedState;
        private ServerSnapshotMsg _lastSnapshot;
        private bool _hasPrediction;

        public WormState PredictedState => _predictedState;
        public bool HasPrediction => _hasPrediction;

        public void Initialize(WormState initialState)
        {
            _predictedState = initialState;
            _hasPrediction = true;
        }

        public void ApplyInput(float desiredHeading, bool boostHeld, float deltaTime)
        {
            if (!_hasPrediction) return;
            MovementMath.IntegrateMovement(ref _predictedState, desiredHeading, boostHeld, deltaTime);
            GrowthMath.ApplyBoostDrain(ref _predictedState, deltaTime);
        }

        public void Reconcile(ServerSnapshotMsg snapshot)
        {
            _lastSnapshot = snapshot;
            if (!_hasPrediction) return;

            float snapMass = snapshot.LocalWorm.Mass;
            const float tolerance = 0.1f;

            if (snapshot.LocalWorm.IsDead)
            {
                _predictedState.IsDead = true;
                return;
            }

            float dx = _predictedState.X - snapshot.LocalWorm.X;
            float dy = _predictedState.Y - snapshot.LocalWorm.Y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (dist > tolerance || Mathf.Abs(_predictedState.Mass - snapMass) > tolerance)
            {
                _predictedState.X = snapshot.LocalWorm.X;
                _predictedState.Y = snapshot.LocalWorm.Y;
                _predictedState.Heading = snapshot.LocalWorm.Heading;
                _predictedState.Mass = snapshot.LocalWorm.Mass;
                _predictedState.IsBoosting = false;
            }
        }

        public void Reset()
        {
            _hasPrediction = false;
        }
    }
}
