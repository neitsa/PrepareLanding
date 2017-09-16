using System;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core
{
    /// <summary>
    ///     Class used for ticking values.
    /// </summary>
    public class GameTicks
    {
        private static int _savedTickAbs;

        private static int _newTickAbs;

        // FPS accumulated over the interval
        private float _accum;

        // tells whether or not the instance has subscribed to the WorldInterfaceUpdate event.
        private bool _subscribedToWorldInterfaceUpdate;

        // time left for current interval
        private float _timeLeft;

        /// <summary>
        ///     Number of frames drawn during the update interval.
        /// </summary>
        public int FramesDrawn { get; private set; }

        public static bool TickManagerHasTickAbs => Find.TickManager.gameStartAbsTick != 0;

        /// <summary>
        ///     Interval (in seconds) at which the <see cref="TicksIntervalElapsed" /> event is fired.
        /// </summary>
        public float UpdateInterval { get; set; }

        /// <summary>
        ///     Methods can register to this event to be called when the Update() method (while on the world map) is called.
        ///     See also <seealso cref="ExecuteOnWorldInterfaceUpdate" />.
        /// </summary>
        public event Action TicksIntervalElapsed = delegate { };

        /// <summary>
        ///     Start the ticking engine.
        /// </summary>
        public void StartTicking()
        {
            if (_subscribedToWorldInterfaceUpdate)
                return;

            PrepareLanding.Instance.EventHandler.WorldInterfaceUpdate += ExecuteOnWorldInterfaceUpdate;

            _subscribedToWorldInterfaceUpdate = true;
        }

        /// <summary>
        ///     Stop the ticking engine.
        /// </summary>
        public void StopTicking()
        {
            if (!_subscribedToWorldInterfaceUpdate)
                return;

            PrepareLanding.Instance.EventHandler.WorldInterfaceUpdate -= ExecuteOnWorldInterfaceUpdate;

            _subscribedToWorldInterfaceUpdate = false;
        }

        /// <summary>
        ///     Called on each WorldInterfaceUpdate (from the game engine). It fires the <see cref="TicksIntervalElapsed" /> event
        ///     if the <see cref="UpdateInterval" /> time interval as elapsed.
        /// </summary>
        private void ExecuteOnWorldInterfaceUpdate()
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

        public static void PushTickAbs(int ticks = 1)
        {
            if (TickManagerHasTickAbs)
                return;

            _newTickAbs = ticks;
            _savedTickAbs = Find.TickManager.gameStartAbsTick;
            Find.TickManager.gameStartAbsTick = ticks;
        }

        public static void PopTickAbs()
        {
            if (Current.ProgramState == ProgramState.Playing)
                return;

            if (Find.TickManager.gameStartAbsTick == _newTickAbs)
                Find.TickManager.gameStartAbsTick = _savedTickAbs;
        }
    }
}