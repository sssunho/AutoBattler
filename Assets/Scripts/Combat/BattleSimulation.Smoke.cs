using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        readonly BattleSmokeLine _smokeLine = new BattleSmokeLine();

        public bool HasSkillLineOfSight(HexCharacter actor, SkillType skill, HexCoordinate origin, HexCoordinate target)
        {
            if (actor == null || !actor.Skills.Contains(skill))
            {
                return false;
            }

            actor.Skills.GetRange(skill, out _, out int maximumRange);
            return _smokeLine.IsClear(_board, origin, target, maximumRange);
        }
    }
}
