using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Maui.Controls.BetterMaps
{
    public class Map : View, IEnumerable<Pin>, IMap
    {
        public const string MoveToRegionMessageName = "MapMoveToRegion";

        public static readonly BindableProperty MapThemeProperty =
            BindableProperty.Create(nameof(MapTheme), typeof(MapTheme), typeof(Map), MapTheme.System);

        public static readonly BindableProperty MapTypeProperty =
            BindableProperty.Create(nameof(MapType), typeof(MapType), typeof(Map), default(MapType));

        public static readonly BindableProperty IsShowingUserProperty =
            BindableProperty.Create(nameof(IsShowingUser), typeof(bool), typeof(Map), default(bool));

        public static readonly BindableProperty ShowUserLocationButtonProperty =
            BindableProperty.Create(nameof(ShowUserLocationButton), typeof(bool), typeof(Map), default(bool));

        public static readonly BindableProperty ShowCompassProperty =
            BindableProperty.Create(nameof(ShowCompass), typeof(bool), typeof(Map), true);

        public static readonly BindableProperty SelectedPinProperty =
            BindableProperty.Create(nameof(SelectedPin), typeof(Pin), typeof(Map), default(Pin),
                propertyChanged: (b, o, n) => OnSelectedPinChanged((Map)b, o as Pin, n as Pin));

        public static readonly BindableProperty TrafficEnabledProperty =
            BindableProperty.Create(nameof(TrafficEnabled), typeof(bool), typeof(Map), default(bool));

        public static readonly BindableProperty HasScrollEnabledProperty =
            BindableProperty.Create(nameof(HasScrollEnabled), typeof(bool), typeof(Map), true);

        public static readonly BindableProperty HasZoomEnabledProperty =
            BindableProperty.Create(nameof(HasZoomEnabled), typeof(bool), typeof(Map), true);

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(Map), default(IEnumerable),
                propertyChanged: (b, o, n) => ((Map)b).OnItemsSourcePropertyChanged((IEnumerable)o, (IEnumerable)n));

        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(Map), default(DataTemplate),
                propertyChanged: (b, o, n) => ((Map)b).OnItemTemplatePropertyChanged((DataTemplate)o, (DataTemplate)n));

        public static readonly BindableProperty ItemTemplateSelectorProperty =
            BindableProperty.Create(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(Map), default(DataTemplateSelector),
                propertyChanged: (b, o, n) => ((Map)b).OnItemTemplateSelectorPropertyChanged());

        public static readonly BindableProperty MoveToLastRegionOnLayoutChangeProperty =
            BindableProperty.Create(nameof(MoveToLastRegionOnLayoutChange), typeof(bool), typeof(Map), defaultValue: false);

        private readonly ObservableRangeCollection<Pin> _pins = new ObservableRangeCollection<Pin>();
        private readonly ObservableRangeCollection<MapElement> _mapElements = new ObservableRangeCollection<MapElement>();
        private MapSpan _visibleRegion;

        public Map(MapSpan region)
        {
            LastMoveToRegion = region;
            VerticalOptions = HorizontalOptions = LayoutOptions.Fill;
        }

        // center on Rome by default
        public Map() : this(new MapSpan(new Position(41.890202, 12.492049), 0.1, 0.1))
        {
        }

        public bool HasScrollEnabled
        {
            get => (bool)GetValue(HasScrollEnabledProperty);
            set => SetValue(HasScrollEnabledProperty, value);
        }

        public bool HasZoomEnabled
        {
            get => (bool)GetValue(HasZoomEnabledProperty);
            set => SetValue(HasZoomEnabledProperty, value);
        }
        public bool IsShowingUser
        {
            get => (bool)GetValue(IsShowingUserProperty);
            set => SetValue(IsShowingUserProperty, value);
        }

        public bool ShowUserLocationButton
        {
            get => (bool)GetValue(ShowUserLocationButtonProperty);
            set => SetValue(ShowUserLocationButtonProperty, value);
        }

        public bool ShowCompass
        {
            get => (bool)GetValue(ShowCompassProperty);
            set => SetValue(ShowCompassProperty, value);
        }

        public Pin SelectedPin
        {
            get => (Pin)GetValue(SelectedPinProperty);
            set => SetValue(SelectedPinProperty, value);
        }

        public bool TrafficEnabled
        {
            get => (bool)GetValue(TrafficEnabledProperty);
            set => SetValue(TrafficEnabledProperty, value);
        }

        public MapTheme MapTheme
        {
            get => (MapTheme)GetValue(MapThemeProperty);
            set => SetValue(MapThemeProperty, value);
        }

        public MapType MapType
        {
            get => (MapType)GetValue(MapTypeProperty);
            set => SetValue(MapTypeProperty, value);
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public DataTemplateSelector ItemTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
            set => SetValue(ItemTemplateSelectorProperty, value);
        }

        public bool MoveToLastRegionOnLayoutChange
        {
            get => (bool)GetValue(MoveToLastRegionOnLayoutChangeProperty);
            set => SetValue(MoveToLastRegionOnLayoutChangeProperty, value);
        }

        public ObservableCollection<Pin> Pins => _pins;
        public ObservableCollection<MapElement> MapElements => _mapElements;

        public event EventHandler<MapClickedEventArgs> MapClicked;
        public event EventHandler<MapSelectedPinChangedArgs> SelectedPinChanged;
        public event EventHandler<PinClickedEventArgs> PinClicked;
        public event EventHandler<PinClickedEventArgs> InfoWindowClicked;
        public event EventHandler<PinClickedEventArgs> InfoWindowLongClicked;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool CanSendMapClicked() => MapClicked != null;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SendMapClicked(Position position) => MapClicked?.Invoke(this, new MapClickedEventArgs(position));

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SendPinClick(Pin pin)
        {
            var args = new PinClickedEventArgs(pin);
            PinClicked?.Invoke(this, args);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SendInfoWindowClick(Pin pin)
        {
            var args = new PinClickedEventArgs(pin);
            InfoWindowClicked?.Invoke(this, args);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SendInfoWindowLongClick(Pin pin)
        {
            var args = new PinClickedEventArgs(pin);
            InfoWindowLongClicked?.Invoke(this, args);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetVisibleRegion(MapSpan value) => VisibleRegion = value;

        public MapSpan VisibleRegion
        {
            get => _visibleRegion;
            internal set
            {
                if (_visibleRegion == value)
                    return;
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                OnPropertyChanging();
                _visibleRegion = value;
                OnPropertyChanged();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public MapSpan LastMoveToRegion { get; private set; }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<Pin> GetEnumerator() => _pins.GetEnumerator();

        public void MoveToRegion(MapSpan mapSpan)
        {
            LastMoveToRegion = mapSpan ?? throw new ArgumentNullException(nameof(mapSpan));
            MessagingCenter.Send(this, MoveToRegionMessageName, mapSpan);
        }

        private void OnItemsSourcePropertyChanged(IEnumerable oldItemsSource, IEnumerable newItemsSource)
        {
            if (oldItemsSource is INotifyCollectionChanged ncc)
                ncc.CollectionChanged -= OnItemsSourceCollectionChanged;
            if (newItemsSource is INotifyCollectionChanged ncc1)
                ncc1.CollectionChanged += OnItemsSourceCollectionChanged;

            _pins.Clear();
            CreatePinItems();
        }

        private void OnItemTemplatePropertyChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
        {
            if (newItemTemplate is DataTemplateSelector)
            {
                throw new NotSupportedException(
                    $"The {nameof(Map)}.{ItemTemplateProperty.PropertyName} property only supports {nameof(DataTemplate)}." +
                    $" Set the {nameof(Map)}.{ItemTemplateSelectorProperty.PropertyName} property instead to use a {nameof(DataTemplateSelector)}");
            }

            _pins.Clear();
            CreatePinItems();
        }

        private void OnItemTemplateSelectorPropertyChanged()
        {
            _pins.Clear();
            CreatePinItems();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var itemsToAdd = e.NewItems?.Cast<object>()?.Select(o => CreatePin(o))?.ToList() ?? new List<Pin>(0);
            var itemsToRemove = (from i in e.OldItems?.Cast<object>() ?? Enumerable.Empty<object>()
                                 join p in _pins on i equals p.BindingContext
                                 select p).ToList();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _pins.AddRange(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _pins.RemoveRange(itemsToRemove, NotifyCollectionChangedAction.Remove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    _pins.RemoveRange(itemsToRemove, NotifyCollectionChangedAction.Remove);
                    _pins.AddRange(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _pins.Clear();

                    var newItems = ItemsSource?.Cast<object>()?.Select(o => CreatePin(o))?.ToList() ?? new List<Pin>(0);
                    _pins.AddRange(newItems);
                    break;
                default:
                    break;
            }
        }

        private void CreatePinItems()
        {
            if (ItemsSource == null || (ItemTemplate == null && ItemTemplateSelector == null)) return;
            _pins.AddRange(ItemsSource.Cast<object>().Select(o => CreatePin(o)));
        }

        private Pin CreatePin(object newItem)
        {
            var itemTemplate = ItemTemplate;

            if (itemTemplate == null)
                itemTemplate = ItemTemplateSelector?.SelectTemplate(newItem, this);

            if (itemTemplate == null)
                return null;

            var pin = (Pin)itemTemplate.CreateContent();
            pin.BindingContext = newItem;

            if (pin.Label == null)
                throw new ArgumentException("Pin must have a Label to be added to a map");

            return pin;
        }

        private static void OnSelectedPinChanged(Map map, Pin oldValue, Pin newValue)
            => map.SelectedPinChanged?.Invoke(map, new MapSelectedPinChangedArgs(oldValue, newValue));
    }
}