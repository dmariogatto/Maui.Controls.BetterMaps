namespace Maui.Controls.BetterMaps
{
    public class Polygon : GeopathElement, IGeoPathMapElement, IFilledMapElement
    {
        public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(Polygon),
            default(Color));

        public Color FillColor
        {
            get => (Color)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public Polygon()
        {
        }

        #region IStroke
        public Paint Fill => FillColor?.AsPaint();
        #endregion
    }
}