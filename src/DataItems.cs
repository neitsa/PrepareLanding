using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace PrepareLanding
{
    /// <summary>
    ///     An item that can have three states: On, Partial, Off.
    /// </summary>
    public class ThreeStateItem : INotifyPropertyChanged
    {
        // internal state of the item
        private MultiCheckboxState _state;


        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="defaultSate">The default state of the item.</param>
        public ThreeStateItem(MultiCheckboxState defaultSate = MultiCheckboxState.Partial)
        {
            _state = defaultSate;
            DefaultState = defaultSate;
        }

        /// <summary>
        ///     Get the default state of the item.
        /// </summary>
        public MultiCheckboxState DefaultState { get; }

        /// <summary>
        ///     Get the current state of the item.
        /// </summary>
        public MultiCheckboxState State
        {
            get => _state;
            set
            {
                if (value == _state)
                    return;

                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        /// <summary>Subscribe to this event to know if a property of the item has changed.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    ///     Boolean Filtering Type.
    /// </summary>
    public enum FilterBoolean
    {
        /// <summary>
        ///     Disjunction filtering.
        /// </summary>
        OrFiltering,
        /// <summary>
        ///     Conjunction filtering.
        /// </summary>
        AndFiltering,
        /// <summary>
        ///     Undefined filtering.
        /// </summary>
        Undefined,
    }

    public class ThreeStateItemContainer<T> : INotifyPropertyChanged, IEnumerable<KeyValuePair<T, ThreeStateItem>> where T : Def
    {
        protected readonly Dictionary<T, ThreeStateItem> ItemDictionary = new Dictionary<T, ThreeStateItem>();

        public ThreeStateItemContainer()
        {
            OffPartialNoSelect = true;
        }

        public ThreeStateItemContainer(IEnumerable<T> initCollection, string propertyChangedName = null, MultiCheckboxState defaultSate = MultiCheckboxState.Partial) 
        {
            SetContainer(initCollection, propertyChangedName, defaultSate);
        }

        /// <summary>
        ///     Initialize a container from an enumerable of RimWorld definitions (<see cref="Def" />)
        ///     The propertyChangedName makes it so that if a <see cref="ThreeStateItem" /> item changed an event is fired for the
        ///     whole dictionary rather than the contained item.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list parameter. <b>T</b> should be a RimWorld <see cref="Def" />.</typeparam>
        /// <param name="initCollection">A collection of <see cref="Def" /> (each entry will be used as a dictionary key).</param>
        /// <param name="propertyChangedName">
        ///     The bound property name (the name of the dictionary in this class). Each time a value
        ///     in the dictionary is changed, this fire an event related to the dictionary name and not the contained values.
        /// </param>
        /// <param name="defaultSate">The default state of the <see cref="ThreeStateItem" />.</param>
        public virtual void SetContainer(IEnumerable<T> initCollection, string propertyChangedName,
            MultiCheckboxState defaultSate = MultiCheckboxState.Partial)
        {
            ItemDictionary.Clear();
            foreach (var elementDef in initCollection)
            {
                var item = new ThreeStateItem(defaultSate);
                if (!string.IsNullOrEmpty(propertyChangedName))
                {
                    item.PropertyChanged += delegate
                    {
                        // cheat! rather than saying that a ThreeState item changed
                        //  just pretend the whole dictionary has changed.
                        // We don't need a finer grain control than that, as the dictionary will contain just a few elements.
                        OnPropertyChanged(propertyChangedName);
                    };
                }
                ItemDictionary.Add(elementDef, item);
            }

            FilterBooleanState = FilterBoolean.OrFiltering;
            OffPartialNoSelect = true;
        }

        /// <summary>
        ///     Initialize a container from an enumerable of RimWorld definitions (<see cref="Def" />)
        ///     The propertyChangedName makes it so that if a <see cref="ThreeStateItem" /> item changed an event is fired for the
        ///     whole dictionary rather than the contained item.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list parameter. <b>T</b> should be a RimWorld <see cref="Def" />.</typeparam>
        /// <param name="initCollection">A collection of <see cref="Def" /> (each entry will be used as a dictionary key).</param>
        /// <param name="propertyChangedName">
        ///     The bound property name (the name of the dictionary in this class). Each time a value
        ///     in the dictionary is changed, this fire an event related to the dictionary name and not the contained values.
        /// </param>
        public virtual void Reset(IEnumerable<T> initCollection, string propertyChangedName)
        {
            SetContainer(initCollection, propertyChangedName);
        }

        /// <summary>
        ///     Set all <see cref="ThreeStateItem" /> items to On state.
        /// </summary>
        public void All()
        {
            foreach (var kvp in ItemDictionary)
                kvp.Value.State = MultiCheckboxState.On;
        }

        /// <summary>
        ///     Set all <see cref="ThreeStateItem" /> items to Off state.
        /// </summary>
        public void None()
        {
            foreach (var kvp in ItemDictionary)
                kvp.Value.State = MultiCheckboxState.Off;
        }

        public ThreeStateItem this[T key]
        {
            get => ItemDictionary[key];
            set => ItemDictionary[key] = value;
        }

        public bool TryGetValue(T def, out ThreeStateItem item)
        {
            return ItemDictionary.TryGetValue(def, out item);
        }

        public FilterBoolean FilterBooleanState { get; set; }

        public bool OffPartialNoSelect { get; set; }

        public Dictionary<T, ThreeStateItem>.ValueCollection Values => ItemDictionary.Values;

        public int Count => ItemDictionary.Count;

        /// <summary>
        ///     Tells if the container is in its default state (where all fields have their default value).
        /// </summary>
        /// <returns>true if the container is in its default state, false otherwise.</returns>
        public virtual bool IsInDefaultState()
        {
            return ItemDictionary.All(def => def.Value.State == MultiCheckboxState.Partial)
                && FilterBooleanState == FilterBoolean.OrFiltering 
                && OffPartialNoSelect;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IEnumerable

        public IEnumerator<KeyValuePair<T, ThreeStateItem>> GetEnumerator()
        {
            foreach (var threeStateItem in ItemDictionary)
            {
                yield return threeStateItem;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public class ThreeStateItemContainerOrdered<T> : ThreeStateItemContainer<T> where T: Def
    {
        private readonly List<T> _orderedItems = new List<T>();

        public override void SetContainer(IEnumerable<T> initCollection, string propertyChangedName = null,
            MultiCheckboxState defaultSate = MultiCheckboxState.Partial)
        {
            base.SetContainer(initCollection, propertyChangedName, defaultSate);

            _orderedItems.Clear();
            foreach (var item in ItemDictionary)
                _orderedItems.Add(item.Key);

            // order by name at first
            _orderedItems.Sort((x, y) => string.Compare(x.LabelCap, y.LabelCap, StringComparison.Ordinal));

            OrderedFiltering = true;
        }

        public ReadOnlyCollection<T> OrderedItems => _orderedItems.AsReadOnly();

        /// <summary>
        ///     Tells whether the filtering should be order dependent (if true) or not (if false).
        /// </summary>
        public bool OrderedFiltering { get; set; } = true;

        public override bool IsInDefaultState()
        {
            return base.IsInDefaultState() && OrderedFiltering;
        }

        public void SetNewOrder(List<T> otherList)
        {
            if (ItemDictionary.Count != otherList.Count)
            {
                Log.Message($"[PrepareLanding] SetNewOrder: count mismatch ({ItemDictionary.Count} != {otherList.Count})");
                return;
            }

            _orderedItems.Clear();
            _orderedItems.AddRange(otherList);
        }

        /// <summary>
        /// Re-order items in the container.
        /// </summary>
        /// <param name="index">The old index of the element to move.</param>
        /// <param name="newIndex">The new index of the element to move.</param>
        public void ReorderElements(int index, int newIndex)
        {
            if ((index == newIndex) || (index < 0))
            {
                Log.Message($"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}");
                return;
            }

            if (_orderedItems.Count == 0)
            {
                Log.Message("[PrepareLanding] ReorderElements: elementsList count is 0.");
                return;
            }

            if ((index >= _orderedItems.Count) || (newIndex >= _orderedItems.Count))
            {
                Log.Message(
                    $"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}; elemntsList.Count: {_orderedItems.Count}");
                return;
            }

            var item = _orderedItems[index];
            _orderedItems.RemoveAt(index);
            _orderedItems.Insert(newIndex, item);
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

        public bool IsCorrectRange => Comparer<T>.Default.Compare(_min, _max) <= 0;

        public T Max
        {
            get => _max;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_max, value))
                    return;

                _max = value;
                OnPropertyChanged(nameof(Max));
            }
        }

        public string MaxString
        {
            get => _maxString;
            set
            {
                if (value == _maxString)
                    return;

                _maxString = value;

                if (AllowOnPropertyChangedOnStrings)
                    OnPropertyChanged(nameof(MaxString));
            }
        }

        public T Min
        {
            get => _min;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_min, value))
                    return;

                _min = value;
                OnPropertyChanged(nameof(Min));
            }
        }

        public string MinString
        {
            get => _minString;
            set
            {
                if (value == _minString)
                    return;

                _minString = value;

                if (AllowOnPropertyChangedOnStrings)
                    OnPropertyChanged(nameof(MinString));
            }
        }

        public bool Use
        {
            get => _use;
            set
            {
                if (value == _use)
                    return;

                _use = value;
                OnPropertyChanged(nameof(Use));
            }
        }

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

        public T Max
        {
            get => _max;
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

        public T Min
        {
            get => _min;
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

        public ReadOnlyCollection<T> Options => _options.AsReadOnly();

        public bool Use
        {
            get => _use;
            set
            {
                if (value == _use)
                    return;

                _use = value;
                OnPropertyChanged(nameof(Use));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum MostLeastCharacteristic
    {
        None = 0,
        Temperature = 1,
        Rainfall = 2,
        Elevation = 3
    }

    public enum MostLeastType
    {
        None = 0,
        Most = 1,
        Least = 2
    }

    public class MostLeastItem : INotifyPropertyChanged
    {
        private MostLeastCharacteristic _characteristic;
        private MostLeastType _characteristicType;
        private int _numberOfItems;

        public MostLeastCharacteristic Characteristic
        {
            get => _characteristic;
            set
            {
                if (value == _characteristic)
                    return;

                _characteristic = value;
                OnPropertyChanged(nameof(Characteristic));
            }
        }

        public MostLeastType CharacteristicType
        {
            get => _characteristicType;
            set
            {
                if (value == _characteristicType)
                    return;

                _characteristicType = value;
                OnPropertyChanged(nameof(CharacteristicType));
            }
        }

        public bool IsInDefaultState => Characteristic == MostLeastCharacteristic.None && CharacteristicType == MostLeastType.None;

        public int NumberOfItems
        {
            get => _numberOfItems;
            set
            {
                if (value == _numberOfItems)
                    return;

                _numberOfItems = value;
                OnPropertyChanged(nameof(NumberOfItems));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Reset()
        {
            Characteristic = MostLeastCharacteristic.None;
            CharacteristicType = MostLeastType.None;
            NumberOfItems = 0;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UsableFromList<T> : INotifyPropertyChanged  where T : struct, IConvertible
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly List<T> _options;
        private bool _use;
        private T _selected;

        public UsableFromList(List<T> options, T defaultSelected)
        {
            _options = options;
            _use = false;
            _selected = defaultSelected;
        }

        public ReadOnlyCollection<T> Options => _options.AsReadOnly();

        public bool Use
        {
            get => _use;
            set
            {
                if (value == _use)
                    return;

                _use = value;
                OnPropertyChanged(nameof(Use));
            }
        }

        public T Selected
        {
            get => _selected;
            set
            {
                if (!_use)
                    return;

                if (!_options.Contains(value))
                {
                    Log.Message("[PrepareLanding] Trying to set a value that is not in the default list.");
                    return;
                }

                if (EqualityComparer<T>.Default.Equals(_selected, value))
                    return;

                _selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }

        public void Reset(bool firePropertyChanged = true)
        {
            if (!firePropertyChanged)
            {
                _use = false;
                _selected = default(T);
            }
            else
            {
                Use = false;
                Selected = default(T);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}