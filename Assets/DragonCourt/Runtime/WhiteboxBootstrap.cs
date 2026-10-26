using UnityEngine;

namespace DragonCourt
{
    public sealed class WhiteboxBootstrap : MonoBehaviour
    {
        void Awake()
        {
            // Legacy entry scenes may still contain this component.
            // Never create a duplicate level next to the committed scene.
            if (FindFirstObjectByType<CourtFlow>() == null) CourtBuilder.Build();
        }
    }
}
