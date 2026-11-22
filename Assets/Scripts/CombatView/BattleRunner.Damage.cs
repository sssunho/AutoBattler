using System.Collections.Generic;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    public sealed partial class BattleRunner
    {
        readonly List<(BattleDamageEvent Damage, HexCharacterView View, Vector3 Position)> _damagePresentations
            = new List<(BattleDamageEvent, HexCharacterView, Vector3)>();
        readonly HashSet<int> _damageDeaths = new HashSet<int>();
        bool _isDrainingDamage;

        internal event System.Action<BattleDamageEvent, Vector3> DamageResolved;

        void DrainDamage(BattleSimulation simulation, bool present = true)
        {
            if (simulation == null || _isDrainingDamage)
            {
                return;
            }

            _isDrainingDamage = true;
            uint version = _presentationVersion;
            try
            {
                while (simulation.TryDequeueDamage(out BattleDamageEvent damage))
                {
                    _views.TryGetValue(damage.Result.TargetId, out HexCharacterView view);
                    Vector3 position = view != null ? view.OverlayWorldPosition
                        : _mapView != null ? _mapView.AxialToWorld(damage.Coordinate) : default;
                    _damagePresentations.Add((damage, view, position));
                    if (damage.Result.TargetDied)
                    {
                        _damageDeaths.Add(damage.Result.TargetId);
                    }
                }

                foreach (int unitId in _damageDeaths)
                {
                    if (_simulation != simulation)
                    {
                        return;
                    }

                    ReleaseView(unitId);
                }

                for (int i = 0; i < _damagePresentations.Count; i++)
                {
                    if (!present || version != _presentationVersion || !CanContinuePresentation(simulation))
                    {
                        break;
                    }

                    var entry = _damagePresentations[i];
                    DamageResolved?.Invoke(entry.Damage, entry.Position);
                    if (version != _presentationVersion || !CanContinuePresentation(simulation))
                    {
                        break;
                    }

                    if (entry.Damage.Origin == BattleDamageOrigin.Attack && entry.View != null)
                    {
                        PublishAttackResult(entry.Damage.Result, entry.View, entry.Position);
                    }
                }
            }
            finally
            {
                try
                {
                    if (!present || version != _presentationVersion || !CanContinuePresentation(simulation))
                    {
                        while (simulation.TryDequeueDamage(out BattleDamageEvent abandoned))
                        {
                            if (_simulation == simulation && abandoned.Result.TargetDied)
                            {
                                ReleaseView(abandoned.Result.TargetId);
                            }
                        }
                    }
                }
                finally
                {
                    _damagePresentations.Clear();
                    _damageDeaths.Clear();
                    _isDrainingDamage = false;
                }
            }
        }
    }
}
