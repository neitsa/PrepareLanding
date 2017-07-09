using UnityEngine;
using Verse;

namespace PrepareLanding
{
    [StaticConstructorOnStartup]
    internal class MonoController : MonoBehaviour
    {
        public static readonly string GameObjectName = "PrepareLandingMonoController";

        public static MonoController Instance { get; private set; }

        static MonoController()
        {
            Log.Message("PrepareLandingMonoController Initialization");
            var gameObject = new GameObject(GameObjectName);

            DontDestroyOnLoad(gameObject);

            Instance = gameObject.AddComponent<MonoController>();
        }


        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }


        public virtual void Start()
        {
            Log.Message("PrepareLandingMonoController Start");
            enabled = false;
        }

        public void OnDestroy()
        {
            var currentGameObject = GameObject.Find(GameObjectName);
            Destroy(currentGameObject);

            Instance = null;

            Log.Message("Unloaded PrepareLandingMonoController");
        }
    }
}
