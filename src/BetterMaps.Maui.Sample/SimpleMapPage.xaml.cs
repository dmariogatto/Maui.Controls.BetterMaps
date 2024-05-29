namespace BetterMaps.Maui.Sample;

public partial class SimpleMapPage : ContentPage
{
    public SimpleMapPage()
    {
        InitializeComponent();
    }

    private void ContentPage_Unloaded(object sender, EventArgs e)
    {
        MauiMap.Handler?.DisconnectHandler();
    }
}