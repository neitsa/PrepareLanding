using System.ComponentModel;
using JetBrains.Annotations;

namespace PrepareLanding
{
    public class FilterOptions : INotifyPropertyChanged
    {
        private bool _allowImpassableHilliness;
        private bool _allowInvalidTilesForNewSettlement;
        private bool _allowLiveFiltering;
        private bool _viewPartialNoSelect;
        private bool _bypassMaxHighlightedTiles;
        private bool _disablePreFilterCheck;
        private bool _resetAllFieldsOnNewGeneratedWorld;
        private bool _disableTileBlinking;
        private bool _disableTileHighlighting;
        private bool _showDebugTileId;
        private bool _showFilterHeaviness;

        /// <summary>
        ///     Allow selection and filtering of impassable tiles.
        /// </summary>
        public bool AllowImpassableHilliness
        {
            get => _allowImpassableHilliness;
            set
            {
                if (value == _allowImpassableHilliness)
                    return;

                _allowImpassableHilliness = value;
                OnPropertyChanged(nameof(AllowImpassableHilliness));

                // note: turning this one on /off also turns on /off the AllowInvalidTilesForNewSettlement
                AllowInvalidTilesForNewSettlement = value;
            }
        }

        /// <summary>
        ///     Allow filtering of tiles that would normally not be deemed as valid for new settlements.
        /// </summary>
        public bool AllowInvalidTilesForNewSettlement
        {
            get => _allowInvalidTilesForNewSettlement;
            set
            {
                if (value == _allowInvalidTilesForNewSettlement)
                    return;

                _allowInvalidTilesForNewSettlement = value;
                OnPropertyChanged(nameof(AllowInvalidTilesForNewSettlement));
            }
        }

        /// <summary>
        ///     Allow "live filtering" or not (user doesn't have to click the filter button if active: filtering is done on the
        ///     fly).
        /// </summary>
        public bool AllowLiveFiltering
        {
            get => _allowLiveFiltering;
            set
            {
                if (value == _allowLiveFiltering)
                    return;

                _allowLiveFiltering = value;
                OnPropertyChanged(nameof(AllowLiveFiltering));
            }
        }

        /// <summary>
        ///     If true, Bypass the maximum number of allowed highlighted tiles (
        ///     <see cref="Core.Gui.World.TileHighlighter.MaxHighlightedTiles" />).
        /// </summary>
        public bool BypassMaxHighlightedTiles
        {
            get => _bypassMaxHighlightedTiles;
            set
            {
                if (value == _bypassMaxHighlightedTiles)
                    return;

                _bypassMaxHighlightedTiles = value;
                OnPropertyChanged(nameof(BypassMaxHighlightedTiles));
            }
        }

        /// <summary>
        ///     Disable the prefilter check (<see cref="WorldTileFilter.Prefilter" />).
        /// </summary>
        public bool DisablePreFilterCheck
        {
            get => _disablePreFilterCheck;
            set
            {
                if (value == _disablePreFilterCheck)
                    return;

                _disablePreFilterCheck = value;
                OnPropertyChanged(nameof(DisablePreFilterCheck));
            }
        }

        /// <summary>
        ///     Allow to view the button (for <see cref="ThreeStateItemContainer{T}"/>) for PartialOffNoSelect state.
        /// </summary>
        public bool ViewPartialOffNoSelect
        {
            get => _viewPartialNoSelect;
            set
            {
                if (value == _viewPartialNoSelect)
                    return;

                _viewPartialNoSelect = value;
                OnPropertyChanged(nameof(ViewPartialOffNoSelect));
            }
        }

        /// <summary>
        ///     If ON all the filters are reset on a new world (if false, the filters are kept in their previous state).
        /// </summary>
        public bool ResetAllFieldsOnNewGeneratedWorld
        {
            get => _resetAllFieldsOnNewGeneratedWorld;
            set
            {
                if (value == _resetAllFieldsOnNewGeneratedWorld)
                    return;

                _resetAllFieldsOnNewGeneratedWorld = value;
                OnPropertyChanged(nameof(ResetAllFieldsOnNewGeneratedWorld));
            }
        }

        /// <summary>
        ///     Disable filtered tiles blinking (if true).
        /// </summary>
        public bool DisableTileBlinking
        {
            get => _disableTileBlinking;
            set
            {
                if (value == _disableTileBlinking)
                    return;

                _disableTileBlinking = value;
                OnPropertyChanged(nameof(DisableTileBlinking));
            }
        }

        /// <summary>
        ///     If true, disable filtered tile highlighting altogether.
        /// </summary>
        public bool DisableTileHighlighting
        {
            get => _disableTileHighlighting;
            set
            {
                if (value == _disableTileHighlighting)
                    return;

                _disableTileHighlighting = value;
                OnPropertyChanged(nameof(DisableTileHighlighting));
            }
        }

        /// <summary>
        ///     If true, show the tile identifier on the world map.
        /// </summary>
        public bool ShowDebugTileId
        {
            get => _showDebugTileId;
            set
            {
                if (value == _showDebugTileId)
                    return;

                _showDebugTileId = value;
                OnPropertyChanged(nameof(ShowDebugTileId));
            }
        }

        /// <summary>
        ///     Show filter heaviness on the GUI with a color code (green: light, yellow: medium, red: heavy)
        /// </summary>
        public bool ShowFilterHeaviness
        {
            get => _showFilterHeaviness;
            set
            {
                if (value == _showFilterHeaviness)
                    return;

                _showFilterHeaviness = value;
                OnPropertyChanged(nameof(ShowFilterHeaviness));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}