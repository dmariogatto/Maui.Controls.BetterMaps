namespace Maui.Controls.BetterMaps.Sample;

public partial class MapPage : ContentPage
{
    private readonly Random _random = new Random();
    private readonly string[] _imageUrls = new[]
    {
        "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/240/microsoft/310/monkey_1f412.png",
        "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/240/microsoft/310/dragon_1f409.png",
        "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/240/microsoft/310/rooster_1f413.png",
        "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/240/microsoft/310/tiger_1f405.png",
        "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/240/microsoft/310/pig_1f416.png",
        "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/240/microsoft/310/cow_1f404.png"
    };

    private bool _addingCircle;

    public MapPage()
    {
        InitializeComponent();
    }

    private void MauiMap_MapClicked(object sender, MapClickedEventArgs e)
    {
        if (_addingCircle)
        {
            _addingCircle = false;

            var circle = new Circle()
            {
                Center = new Position(e.Position.Latitude, e.Position.Longitude),
                Radius = Distance.FromKilometers(1),
                StrokeColor = Colors.LightSkyBlue,
                StrokeWidth = 8,
                FillColor = Colors.LightSkyBlue.WithAlpha(0.5f),
            };

            MauiMap.MapElements.Add(circle);
        }
        else
        {
            var pin = new Pin()
            {
                Label = $"Pin {MauiMap.Pins.Count + 1}",
                Position = e.Position
            };

            MauiMap.Pins.Add(pin);
        }
    }

    private void MauiMap_PinClicked(object sender, PinClickedEventArgs e)
    {
        var idx = _random.Next() % _imageUrls.Length;
        e.Pin.ImageSource = new UriImageSource()
        {
            Uri = new Uri(_imageUrls[idx])
        };
    }

    private void MauiMap_InfoWindowLongClicked(object sender, PinClickedEventArgs e)
    {
        e.Pin.TintColor = null;
        e.Pin.ImageSource = null;
    }

    private async void OnToggleShowUserLocation(object sender, EventArgs e)
    {
        if (await Permissions.RequestAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted)
        {
            MauiMap.ShowUserLocationButton =
                MauiMap.IsShowingUser = !MauiMap.IsShowingUser;
        }
    }

    private void OnToggleCompass(object sender, EventArgs e)
    {
        MauiMap.ShowCompass = !MauiMap.ShowCompass;
    }

    private void OnCircleClicked(object sender, EventArgs e)
    {
        _addingCircle = true;
        DisplayAlert("Map Element", "Tap on the map to add a circle!", "OK");
    }

    private async void OnThemeClicked(object sender, EventArgs e)
    {
        var result = await DisplayActionSheet("Theme", "Cancel", null, new[]
        {
            MapTheme.System.ToString(),
            MapTheme.Light.ToString(),
            MapTheme.Dark.ToString()

        });

        if (Enum.TryParse<MapTheme>(result, out var theme))
            MauiMap.MapTheme = theme;
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        _addingCircle = false;

        MauiMap.Pins.Clear();
        MauiMap.MapElements.Clear();
        MauiMap.ShowUserLocationButton = false;
        MauiMap.IsShowingUser = false;
        MauiMap.ShowCompass = false;
        MauiMap.MapTheme = MapTheme.System;
    }
}