using UnityEngine;

namespace DragonCourt
{
    public sealed class WhiteboxBootstrap : MonoBehaviour
    {
        void Awake() { CourtBuilder.Build(); }
    }
}
