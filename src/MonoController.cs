using UnityEngine;
using Verse;

namespace PrepareLanding
{
    [StaticConstructorOnStartup]
    internal class MonoController : MonoBehaviour
    {
        private const string GameObjectName = "PrepareLandingMonoController";

        public static MonoController Instance { get; private set; }

        static MonoController()
        {
            Log.Message("[PrepareLanding] MonoController Initialization");
            var gameObject = new GameObject(GameObjectName);

            DontDestroyOnLoad(gameObject);

            Instance = gameObject.AddComponent<MonoController>();
        }

        public LineRenderer LineRenderer { get; private set; }


        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }


        public virtual void Start()
        {
            Log.Message("[PrepareLanding] MonoController Start");
            enabled = false;
            LineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        public void OnDestroy()
        {
            var currentGameObject = GameObject.Find(GameObjectName);
            Destroy(currentGameObject);

            Instance = null;

            Log.Message("[PrepareLanding] MonoController OnDestroy");
        }
    }
}
