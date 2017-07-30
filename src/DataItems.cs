using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using JetBrains.Annotations;
using Verse;

namespace PrepareLanding
{
    public class ThreeStateItem : INotifyPropertyChanged
    {
        private MultiCheckboxState _state;

        public ThreeStateItem(MultiCheckboxState defaultSate = MultiCheckboxState.Off)
        {
            _state = defaultSate;
            DefaultState = defaultSate;
        }

        public MultiCheckboxState DefaultState { get; }

        public MultiCheckboxState State
        {
            get { return _state; }
            set
            {
                if (value == _state)
                    return;

                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class UsableMinMaxNumericItem<T> : INotifyPropertyChanged where T : struct, IComparable, IConvertible
    {
        private T _max;
        private string _maxString;
        private T _min;
        private string _minString;
        private bool _use;

        public bool AllowOnPropertyChangedOnStrings { get; set; }

        public T Min
        {
            get { return _min; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(_min, value))
                    return;

                _min = value;
                OnPropertyChanged(nameof(Min));
            }
        }

        public T Max
        {
            get { return _max; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(_max, value))
                    return;

                _max = value;
                OnPropertyChanged(nameof(Max));
            }
        }

        public bool Use
        {
            get { return _use; }
            set
            {
                if (value == _use)
                    return;

                _use = value;
                OnPropertyChanged(nameof(Use));
            }
        }

        public string MinString
        {
            get { return _minString; }
            set
            {
                if (value == _minString)
                    return;

                _minString = value;

                if (AllowOnPropertyChangedOnStrings)
                    OnPropertyChanged(nameof(MinString));
            }
        }

        public string MaxString
        {
            get { return _maxString; }
            set
            {
                if (value == _maxString)
                    return;

                _maxString = value;

                if (AllowOnPropertyChangedOnStrings)
                    OnPropertyChanged(nameof(MaxString));
            }
        }

        public bool IsCorrectRange => Comparer<T>.Default.Compare(_min, _max) <= 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool InRange(T value)
        {
            if (!_use)
                return false;

            if (!IsCorrectRange)
                return false;

            var lte = Comparer<T>.Default.Compare(value, _min);
            var gte = Comparer<T>.Default.Compare(value, _max);

            return lte >= 0 && gte <= 0;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MinMaxFromRestrictedListItem<T> : INotifyPropertyChanged where T : struct, IConvertible
    {
        private readonly List<T> _options;
        private T _max;
        private T _min;
        private bool _use;

        public MinMaxFromRestrictedListItem(List<T> options, T min = default(T), T max = default(T))
        {
            _options = options;
            _min = min;
            _max = max;
        }

        public T Min
        {
            get { return _min; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(_min, value))
                    return;

                if (!_options.Contains(value))
                    return;

                _min = value;
                OnPropertyChanged(nameof(Min));
            }
        }

        public T Max
        {
            get { return _max; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(_max, value))
                    return;

                if (!_options.Contains(value))
                    return;

                _max = value;
                OnPropertyChanged(nameof(Max));
            }
        }

        public bool Use
        {
            get { return _use; }
            set
            {
                if (value == _use)
                    return;

                _use = value;
                OnPropertyChanged(nameof(Use));
            }
        }

        public ReadOnlyCollection<T> Options => _options.AsReadOnly();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}