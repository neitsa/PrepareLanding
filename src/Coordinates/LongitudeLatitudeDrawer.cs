using UnityEngine;
using Verse;

namespace PrepareLanding.Coordinates
{
    public class LongitudeLatitudeDrawer
    {
        public const float ThetaScale = 0.01f;
        public const float Radius = 110f;
        private int _size;
        private readonly LineRenderer _lineRenderer;
        private float _theta = 0f;

        public LongitudeLatitudeDrawer()
        {
            Log.Message("[PrepareLanding] LatLongDrawer.Start()");
            _lineRenderer = MonoController.Instance.LineRenderer;
            Log.Message($"[PrepareLanding] _lineRenderer is null: {_lineRenderer == null}");
            var message = PrepareLanding.Instance == null ? "null" : "NOT null";
            Log.Message($"[PrepareLanding] Instance is {message}");
            message = PrepareLanding.Instance == null || PrepareLanding.Instance.EventHandler == null
                ? "null"
                : "NOT null";
            Log.Message($"[PrepareLanding] EventHandler is {message}");

            PrepareLanding.Instance.EventHandler.WorldInterfaceUpdate += Update;
        }

        public void UnRegister()
        {
            PrepareLanding.Instance.EventHandler.WorldInterfaceUpdate -= Update;
        }

        private void Update()
        {
            _theta = 0f;
            _size = (int) ((1f / ThetaScale) + 1f);
            _lineRenderer.positionCount = _size;
            for (var i = 0; i < _size; i++)
            {
                _theta += (2.0f * Mathf.PI * ThetaScale);
                var x = Radius * Mathf.Cos(_theta);
                var y = Radius * Mathf.Sin(_theta);
                _lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }
    }
}
