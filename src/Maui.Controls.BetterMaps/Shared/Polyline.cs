using System.Collections.ObjectModel;

namespace Maui.Controls.BetterMaps
{
    public class Polyline : MapElement
    {
        public IList<Position> Geopath { get; }

        public Polyline()
        {
            var observable = new ObservableCollection<Position>();
            observable.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Geopath));
            Geopath = observable;
        }
    }
}