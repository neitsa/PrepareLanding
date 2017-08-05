using System;
using UnityEngine;

namespace PrepareLanding.Core
{
    public class GameTicks
    {
        // FPS accumulated over the interval
        private float _accum;

        // Left time for current interval
        private float _timeLeft;

        public GameTicks()
        {
            PrepareLanding.Instance.OnWorldInterfaceUpdate += WorldInterfaceUpdate;
        }

        public float UpdateInterval { get; set; }

        /// <summary>
        ///     Number of frames drawn during the update interval.
        /// </summary>
        public int FramesDrawn { get; private set; }

        /// <summary>
        ///     Methods can register to this event to be called when the Update() method (while on the world map) is called.
        ///     See also <seealso cref="WorldInterfaceUpdate" />.
        /// </summary>
        public event Action TicksIntervalElapsed = delegate { };

        public void ResetUpdateInterval()
        {
            UpdateInterval = 0.0f;
        }

        private void WorldInterfaceUpdate()
        {
            if (UpdateInterval == 0.0f)
                return;

            _timeLeft -= Time.deltaTime;
            _accum += Time.timeScale / Time.deltaTime;
            ++FramesDrawn;


            if (!(_timeLeft < 0.0f))
                return;

            _timeLeft = UpdateInterval;
            _accum = 0.0F;
            FramesDrawn = 0;

            TicksIntervalElapsed?.Invoke();
        }
    }
}