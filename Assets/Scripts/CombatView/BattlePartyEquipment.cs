using System;
using System.Collections.Generic;
using RaggedBanner.MercenaryData;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [Serializable]
    public struct BattlePartyEquipment
    {
        [SerializeField, Min(0)] int _partyIndex;
        [SerializeField] EquipmentDefinition[] _equipment;

        public int PartyIndex => _partyIndex;

        public BattlePartyEquipment(int partyIndex, EquipmentDefinition[] equipment)
        {
            _partyIndex = partyIndex;
            _equipment = equipment;
        }

        internal static bool TryCapture(int partyCount, IReadOnlyList<BattlePartyEquipment> assignments,
            out BattleEquipmentLoadout[] captured, out string error)
        {
            captured = Array.Empty<BattleEquipmentLoadout>();
            if (partyCount < 0)
            {
                error = "The party count must not be negative.";
                return false;
            }

            BattleEquipmentLoadout[] result = new BattleEquipmentLoadout[partyCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = BattleEquipmentLoadout.Empty;
            }

            HashSet<int> indices = new HashSet<int>();
            if (assignments != null)
            {
                for (int i = 0; i < assignments.Count; i++)
                {
                    BattlePartyEquipment entry = assignments[i];
                    if ((uint)entry.PartyIndex >= (uint)partyCount || !indices.Add(entry.PartyIndex)
                        || entry._equipment != null && entry._equipment.Length > 3)
                    {
                        error = "Equipment assignments require unique party indices and at most three items per unit.";
                        return false;
                    }

                    int count = entry._equipment == null ? 0 : entry._equipment.Length;
                    BattleEquipmentItem[] items = new BattleEquipmentItem[count];
                    for (int slot = 0; slot < count; slot++)
                    {
                        if (entry._equipment[slot] == null)
                        {
                            error = "An equipment slot cannot contain a missing definition.";
                            return false;
                        }

                        if (!entry._equipment[slot].TryCreateSnapshot(out items[slot], out error))
                        {
                            return false;
                        }
                    }

                    try
                    {
                        result[entry.PartyIndex] = new BattleEquipmentLoadout(items);
                    }
                    catch (ArgumentException exception)
                    {
                        error = exception.Message;
                        return false;
                    }
                    catch (OverflowException)
                    {
                        error = "Equipment bonuses exceed the supported stat range.";
                        return false;
                    }
                }
            }

            captured = result;
            error = null;
            return true;
        }
    }
}
