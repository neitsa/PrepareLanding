using RimWorld;
using Verse;

namespace PrepareLanding.Defs
{

    /// <summary>
    /// This class is called from a definition file when clicking the "World" button on the bottom menu bar while playing
    /// (see "\PrepareLanding\Patches\MainButtonDef_Patch.xml").
    /// </summary>
    public class MainButtonWorkerToggleWorld : MainButtonWorker_ToggleWorld
    {
        public override void Activate()
        {
            // default behavior (go to the world map)
            base.Activate();

            // do not show the main window if in tutorial mode
            if (TutorSystem.TutorialMode)
            {
                Log.Message(
                    "[PrepareLanding] MainButtonWorkerToggleWorld: Tutorial mode detected: not showing main window.");
                return;
            }

            // don't add a new window if the window is already there; if it's not create a new one.
            if (PrepareLanding.Instance.MainWindow == null)
                PrepareLanding.Instance.MainWindow = new MainWindow(PrepareLanding.Instance.GameData);

            // show the main window, minimized.
            PrepareLanding.Instance.MainWindow.Show(true);
        }
    }
}
