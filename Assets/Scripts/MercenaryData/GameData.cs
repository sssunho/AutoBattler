using System;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [DisallowMultipleComponent]
    public sealed class GameData : MonoBehaviour
    {
        [SerializeField]
        MercenaryDataList _mercenaryDataList;

        static GameData _instance;

        public static MercenaryDataList Mercenaries
        {
            get
            {
                if (_instance == null || _instance._mercenaryDataList == null)
                {
                    throw new InvalidOperationException("GameData is not initialized. Initialize GameData in the entry scene before accessing Mercenaries.");
                }

                return _instance._mercenaryDataList;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticState()
        {
            _instance = null;
        }

        void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            if (transform.parent != null)
            {
                throw new InvalidOperationException("GameData must be attached to a root GameObject.");
            }

            if (_mercenaryDataList == null)
            {
                throw new InvalidOperationException("GameData requires a MercenaryDataList asset.");
            }

            if (!_mercenaryDataList.Validate(out string error))
            {
                throw new InvalidOperationException(error);
            }

            DontDestroyOnLoad(gameObject);
            _instance = this;
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
