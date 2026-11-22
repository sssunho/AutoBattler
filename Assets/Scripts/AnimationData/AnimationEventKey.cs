using System;
using UnityEngine;

namespace RaggedBanner.Animations
{
    [Serializable]
    public sealed class AnimationEventKey
    {
        [SerializeField]
        int _frame;

        [SerializeField]
        AnimationEventKind _eventKind;

        [SerializeField]
        AnimationAttackKind _attackKind;

        public int Frame => _frame;
        public AnimationEventKind EventKind => _eventKind;
        public AnimationAttackKind AttackKind => _attackKind;

        public AnimationEventKey(int frame, AnimationEventKind eventKind, AnimationAttackKind attackKind)
        {
            _frame = frame;
            _eventKind = eventKind;
            _attackKind = attackKind;
        }
    }
}
