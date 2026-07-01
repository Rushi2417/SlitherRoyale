using UnityEngine;

namespace SlitherRoyale.Client.Gameplay
{
    /// <summary>
    /// BUG-05 FIX: Marker component used to identify giant pellets.
    /// Replaces the string tag "GiantPellet" which threw UnityException because
    /// it was never registered in Edit → Project Settings → Tags and Layers.
    /// GetComponent&lt;GiantPelletMarker&gt;() != null is equivalent to CompareTag but safe.
    /// </summary>
    public class GiantPelletMarker : MonoBehaviour { }
}
