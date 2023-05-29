namespace Maui.Controls.BetterMaps
{
    public class Circle : MapElement, ICircleMapElement
    {
        public static readonly BindableProperty CenterProperty = BindableProperty.Create(
            nameof(Center),
            typeof(Position),
            typeof(Circle),
            default(Position));

        public static readonly BindableProperty RadiusProperty = BindableProperty.Create(
            nameof(Radius),
            typeof(Distance),
            typeof(Circle),
            default(Distance));

        public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(Circle),
            default);

        public Position Center
        {
            get => (Position)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        public Distance Radius
        {
            get => (Distance)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public Color FillColor
        {
            get => (Color)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        #region IStroke
        public Paint Fill => FillColor?.AsPaint();
        #endregion
    }
}