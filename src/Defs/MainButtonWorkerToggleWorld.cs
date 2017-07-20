using RimWorld;
using Verse;

namespace PrepareLanding.Defs
{

    /// <summary>
    /// This class is called from a definition file when clicking the "World" button on the bottom menu bar while playing
    /// (see "PrepareLanding/Defs/Misc/MainButtonDefs/MainButtons.xml").
    /// </summary>
    public class MainButtonWorkerToggleWorld : MainButtonWorker_ToggleWorld
    {
        public override void Activate()
        {

            // default behavior
            base.Activate();

            // do not show the main window if in tutorial mode
            if (TutorSystem.TutorialMode)
            {
                Log.Message(
                    "[PrepareLanding] MainButtonWorkerToggleWorld: Tutorial mode detected: not showing main window.");
                return;
            }

            // show the main window, minimized.
            PrepareLanding.Instance.MainWindow.Show(true);
        }
    }
}
