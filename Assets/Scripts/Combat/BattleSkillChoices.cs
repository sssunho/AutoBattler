using System;
using System.Collections.Generic;

namespace RaggedBanner.Combat
{
    public readonly struct SkillChoice
    {
        public int Id { get; }
        public string DisplayName { get; }

        internal SkillChoice(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }

    public static class BattleSkillChoices
    {
        static readonly IReadOnlyList<SkillChoice> _spirits = Array.AsReadOnly(new[]
        {
            new SkillChoice(1, "불"),
            new SkillChoice(2, "물"),
            new SkillChoice(3, "전기"),
            new SkillChoice(4, "독")
        });

        public static IReadOnlyList<SkillChoice> Get(SkillType skill)
        {
            return skill == SkillType.SpiritImbuement ? _spirits : Array.Empty<SkillChoice>();
        }

        internal static bool TryResolveSpirit(SkillType skill, int optionId, out SpiritType spirit)
        {
            spirit = SpiritType.None;
            if (skill != SkillType.SpiritImbuement || optionId < 1 || optionId > _spirits.Count)
            {
                return false;
            }

            spirit = (SpiritType)optionId;
            return true;
        }
    }
}
