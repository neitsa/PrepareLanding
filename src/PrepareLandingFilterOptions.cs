using System.ComponentModel;
using JetBrains.Annotations;

namespace PrepareLanding
{
    public class PrepareLandingFilterOptions : INotifyPropertyChanged
    {
        private bool _allowImpassableHilliness;
        private bool _allowInvalidTilesForNewSettlement;
        private bool _allowLiveFiltering;
        private bool _bypassMaxHighlightedTiles;
        private bool _disablePreFilterCheck;
        private bool _disableTileBlinking;
        private bool _showDebugTileId;
        private bool _showFilterHeaviness;

        /// <summary>
        ///     Allow "live filtering" or not (user doesn't have to click the filter button if active: filtering is done on the
        ///     fly).
        /// </summary>
        public bool AllowLiveFiltering
        {
            get { return _allowLiveFiltering; }
            set
            {
                if (value == _allowLiveFiltering)
                    return;

                _allowLiveFiltering = value;
                OnPropertyChanged(nameof(AllowLiveFiltering));
            }
        }

        public bool AllowImpassableHilliness
        {
            get { return _allowImpassableHilliness; }
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

        public bool BypassMaxHighlightedTiles
        {
            get { return _bypassMaxHighlightedTiles; }
            set
            {
                if (value == _bypassMaxHighlightedTiles)
                    return;

                _bypassMaxHighlightedTiles = value;
                OnPropertyChanged(nameof(BypassMaxHighlightedTiles));
            }
        }

        public bool ShowDebugTileId
        {
            get { return _showDebugTileId; }
            set
            {
                if (value == _showDebugTileId)
                    return;

                _showDebugTileId = value;
                OnPropertyChanged(nameof(ShowDebugTileId));
            }
        }

        public bool DisablePreFilterCheck
        {
            get { return _disablePreFilterCheck; }
            set
            {
                if (value == _disablePreFilterCheck)
                    return;

                _disablePreFilterCheck = value;
                OnPropertyChanged(nameof(DisablePreFilterCheck));
            }
        }


        public bool DisableTileBlinking
        {
            get { return _disableTileBlinking; }
            set
            {
                if (value == _disableTileBlinking)
                    return;

                _disableTileBlinking = value;
                OnPropertyChanged(nameof(DisableTileBlinking));
            }
        }

        public bool ShowFilterHeaviness
        {
            get { return _showFilterHeaviness; }
            set
            {
                if (value == _showFilterHeaviness)
                    return;

                _showFilterHeaviness = value;
                OnPropertyChanged(nameof(ShowFilterHeaviness));
            }
        }


        public bool AllowInvalidTilesForNewSettlement
        {
            get { return _allowInvalidTilesForNewSettlement; }
            set
            {
                if (value == _allowInvalidTilesForNewSettlement)
                    return;

                _allowInvalidTilesForNewSettlement = value;
                OnPropertyChanged(nameof(AllowInvalidTilesForNewSettlement));
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