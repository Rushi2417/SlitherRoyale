using System.Collections.Generic;
using UnityEngine;

namespace SlitherRoyale.Client.Networking
{
    public class EntityInterpolation : MonoBehaviour
    {
        private class InterpState
        {
            public EntitySnapshot Prev;
            public EntitySnapshot Target;
            public float Time;
            public float Duration = 0.04f;
            public float LastSeen;
        }

        private Dictionary<int, InterpState> _entities = new Dictionary<int, InterpState>();
        private HashSet<int> _seenThisFrame = new HashSet<int>();
        private const float Timeout = 2f;

        private void Update()
        {
            float now = Time.time;
            var toRemove = new List<int>();
            foreach (var kv in _entities)
            {
                kv.Value.Time += Time.deltaTime;
                if (now - kv.Value.LastSeen > Timeout)
                    toRemove.Add(kv.Key);
            }
            foreach (int id in toRemove)
                _entities.Remove(id);
        }

        public void ApplySnapshot(List<EntitySnapshot> entities)
        {
            _seenThisFrame.Clear();
            foreach (var ent in entities)
            {
                _seenThisFrame.Add(ent.Id);
                if (_entities.TryGetValue(ent.Id, out var state))
                {
                    state.Prev = state.Target;
                    state.Target = ent;
                    state.Time = 0f;
                    state.LastSeen = Time.time;
                }
                else
                {
                    _entities[ent.Id] = new InterpState
                    {
                        Prev = ent,
                        Target = ent,
                        Time = 0f,
                        LastSeen = Time.time,
                    };
                }
            }
        }

        public bool TryGetInterpolated(int id, out float x, out float y, out float heading, out float mass)
        {
            x = 0; y = 0; heading = 0; mass = 0;
            if (!_entities.TryGetValue(id, out var state)) return false;

            float t = Mathf.Clamp01(state.Time / state.Duration);
            x = Mathf.Lerp(state.Prev.X, state.Target.X, t);
            y = Mathf.Lerp(state.Prev.Y, state.Target.Y, t);
            heading = Mathf.LerpAngle(state.Prev.Heading * Mathf.Rad2Deg, state.Target.Heading * Mathf.Rad2Deg, t) * Mathf.Deg2Rad;
            mass = Mathf.Lerp(state.Prev.Mass, state.Target.Mass, t);
            return true;
        }

        public bool HasEntity(int id) => _entities.ContainsKey(id);

        public void RemoveEntity(int id) => _entities.Remove(id);

        public void Clear() => _entities.Clear();
    }
}
