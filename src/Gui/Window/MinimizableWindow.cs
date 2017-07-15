using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Window
{
    public abstract class MinimizableWindow : Verse.Window
    {
        private bool _mainWindowClosed;
        protected MinimizedWindow MinimizedWindow;

        protected MinimizableWindow()
        {
            MinimizedWindow = new MinimizedWindow(this);
            MinimizedWindow.OnMinimizedWindowClosed += MinimizedWindowClosed;
        }

        public bool Minimized { get; protected set; }

        public bool IsClosed => _mainWindowClosed && MinimizedWindow.Closed;

        public virtual void Minimize()
        {
            if (MinimizedWindow == null)
            {
                Log.ErrorOnce("[PrepareLanding] Trying to minimize while there is no MinimizedWindow window available.",
                    0x1234cafe);
                return;
            }

            Minimized = true;
            // add the "minimized" window and close the main window
            Find.WindowStack.Add(MinimizedWindow);
            Close();
        }

        public virtual void Maximize()
        {
            if (MinimizedWindow == null)
            {
                Log.ErrorOnce("[PrepareLanding] Trying to maximize while there is no MinimizedWindow window available.",
                    0x1236cafe);
                return;
            }

            // close the minimized window; this will automatically get the parent window up and set 'Minimized' to false
            MinimizedWindow.Close();
        }

        protected void MinimizedWindowClosed()
        {
            Minimized = false;
        }

        public virtual void ForceClose()
        {
            // close the minimized windows
            MinimizedWindow.Close(false);
            // close the main window
            Close();
        }

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);

            _mainWindowClosed = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();

            _mainWindowClosed = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
        }
    }
}