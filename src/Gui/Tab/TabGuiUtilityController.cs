using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Tab
{
    /// <summary>
    ///     A tab controller used to control the tabs in a tabbed GUI view.
    /// </summary>
    public class TabGuiUtilityController
    {
        /// <summary>
        ///     List of tabs.
        /// </summary>
        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        /// <summary>
        ///     The currently selected tab in the GUI.
        /// </summary>
        public ITabGuiUtility SelectedTab { get; private set; }

        /// <summary>
        ///     Add a tab to the controller.
        /// </summary>
        /// <param name="tab">The tab to add.</param>
        public void AddTab(ITabGuiUtility tab)
        {
            _tabGuiUtilities.Add(tab);

            SetupTabs();
        }

        /// <summary>
        ///     Add a range of tabs to the controller.
        /// </summary>
        /// <param name="tabList">The list of tabs to add.</param>
        public void AddTabRange(List<ITabGuiUtility> tabList)
        {
            _tabGuiUtilities.AddRange(tabList);

            SetupTabs();
        }

        /// <summary>
        ///     Remove all tabs in the controller.
        /// </summary>
        public void Clear()
        {
            _tabGuiUtilities.Clear();
        }

        /// <summary>
        ///     Draw the frame around the tabs (and not the tab contents!).
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> of the tabs.</param>
        public void DrawTabs(Rect inRect)
        {
            if (_tabGuiUtilities.Count == 0)
                return;

            TabDrawer.DrawTabs(inRect,
                _tabGuiUtilities.Select(tab =>
                {
                    tab.TabRecord.selected = SelectedTab == tab;
                    return tab.TabRecord;
                }));
        }

        /// <summary>
        ///     Draw the content of the selected Tab.
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> in which to draw the tab content.</param>
        public void DrawSelectedTab(Rect inRect)
        {
            SelectedTab?.Draw(inRect);
        }

        /// <summary>
        ///     Get a Tab given its identifier (<see cref="ITabGuiUtility.Id" />).
        /// </summary>
        /// <param name="id">The identifier of the tab to get.</param>
        /// <returns>A tab if such a tab with the given id exists or null otherwise.</returns>
        public ITabGuiUtility TabById(string id)
        {
            return _tabGuiUtilities.FirstOrDefault(tab => tab.Id == id);
        }

        /// <summary>
        ///     Select a tab by its identifier  (<see cref="ITabGuiUtility.Id" />).
        /// </summary>
        /// <param name="id">The identifier of the tab to be selected.</param>
        public void SetSelectedTabById(string id)
        {
            var tab = TabById(id);
            if (tab == null)
                return;

            SelectedTab = tab;
        }

        /// <summary>
        ///     Setup the tabs to be displayed.
        /// </summary>
        protected void SetupTabs()
        {
            foreach (var tabGuiUtility in _tabGuiUtilities)
            {
                var currentTab = tabGuiUtility;

                currentTab.TabRecord = new TabRecord(currentTab.Name, delegate { SelectedTab = currentTab; }, false);
            }

            SelectedTab = _tabGuiUtilities[0];
        }
    }
}