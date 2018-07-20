using UnityEngine;

namespace Loom.Client.Unity.Internal.UnityAsyncAwaitUtil
{
    internal class AsyncCoroutineRunner : MonoBehaviour
    {
        static AsyncCoroutineRunner _instance;

        public static AsyncCoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("AsyncCoroutineRunner")
                        .AddComponent<AsyncCoroutineRunner>();
                }

                return _instance;
            }
        }

        void Awake()
        {
            // Don't show in scene hierarchy
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            DontDestroyOnLoad(gameObject);
        }
    }
}
