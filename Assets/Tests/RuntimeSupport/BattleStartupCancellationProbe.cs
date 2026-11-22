using RaggedBanner.Combat.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    [ExecuteAlways]
    public sealed class BattleStartupCancellationProbe : MonoBehaviour
    {
        public static BattleStartController Target { get; set; }

        void OnEnable()
        {
            BattleStartController target = Target;
            Target = null;

            if (target != null)
            {
                target.enabled = false;
            }
        }
    }
}
