using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Window
{
    public abstract class MinimizableWindow : Verse.Window
    {
        // closed here means: not on the RimWorld window stack
        private bool _mainWindowClosed;
        protected MinimizedWindow MinimizedWindow;

        protected MinimizableWindow()
        {
            _mainWindowClosed = true;
            MinimizedWindow = new MinimizedWindow(this);
            MinimizedWindow.OnMinimizedWindowClosed += MinimizedWindowClosed;
        }

        public bool Minimized { get; protected set; }

        public bool IsClosed => _mainWindowClosed && MinimizedWindow.Closed;

        public override void Close(bool doCloseSound = true)
        {
            // the base class will remove it from the window stack for us.
            base.Close(doCloseSound);

            _mainWindowClosed = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
        }

        public virtual void ForceClose()
        {
            // close the minimized windows
            MinimizedWindow.Close(false);

            // close the main window
            Close();
        }

        public virtual void Show(bool showMinimized = false)
        {
            if (!IsClosed)
            {
                // window not closed, check if it's minimized
                if(Minimized && !showMinimized)
                    Maximize();

                return;
            }
            
            // defensive check, just to catch this abnormal state if code logic is very wrong
            if (Find.WindowStack.IsOpen(GetType())) //note: this.getType()
            {
                // getting here means the window is closed (not visible, hence it shouldn't be on the window stack) but it's still on the window stack...
                Log.Error("[PrepareLAnding] The main window is closed but it's still on the window stack...");
                return;
            }

            // so, the window is "closed" (it's just not shown). Try to add it to the window stack
            Find.WindowStack.Add(this);

            if(showMinimized)
                Minimize();

            _mainWindowClosed = false;
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

            _mainWindowClosed = false;
        }

        public virtual void Minimize()
        {
            if (MinimizedWindow == null)
            {
                Log.ErrorOnce("[PrepareLanding] Trying to minimize while there is no MinimizedWindow window available.",
                    0x1234cafe);
                return;
            }

            Minimized = true;

            // add the "minimized" window to the window stack
            Find.WindowStack.Add(MinimizedWindow);

            // close the main window
            Close();

            _mainWindowClosed = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();

            _mainWindowClosed = false;
        }

        protected void MinimizedWindowClosed()
        {
            Minimized = false;

            _mainWindowClosed = false;
        }
    }
}