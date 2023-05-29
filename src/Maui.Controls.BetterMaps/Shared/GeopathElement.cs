using System.Collections;

namespace Maui.Controls.BetterMaps
{
    public class GeopathElement : MapElement, IList<Position>
    {
        private ObservableRangeCollection<Position> _geoPath;
        public IList<Position> Geopath
        {
            get
            {
                if (_geoPath is null)
                {
                    _geoPath = new ObservableRangeCollection<Position>();
                    _geoPath.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(Geopath));
                }

                return _geoPath;
            }
        }

        public void AddRange(IEnumerable<Position> collection)
            => ((ObservableRangeCollection<Position>)Geopath).AddRange(collection);

        public void RemoveRange(IEnumerable<Position> collection)
            => ((ObservableRangeCollection<Position>)Geopath).RemoveRange(collection);

        public void ReplaceRange(IEnumerable<Position> collection)
            => ((ObservableRangeCollection<Position>)Geopath).ReplaceRange(collection);

        #region IList
        public Position this[int index]
        {
            get => Geopath[index];
            set => Geopath[index] = value;
        }

        public int Count => Geopath.Count;

        public bool IsReadOnly => false;

        public void Add(Position item)
            => Geopath.Add(item);

        public void Clear()
            => Geopath.Clear();

        public bool Contains(Position item)
            => Geopath.Contains(item);

        public void CopyTo(Position[] array, int arrayIndex)
            => Geopath.CopyTo(array, arrayIndex);

        public IEnumerator<Position> GetEnumerator()
            => Geopath.GetEnumerator();

        public int IndexOf(Position item)
            => Geopath.IndexOf(item);

        public void Insert(int index, Position item)
            => Geopath.Insert(index, item);

        public bool Remove(Position item)
            => Geopath.Remove(item);

        public void RemoveAt(int index)
            => Geopath.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator()
            => Geopath.GetEnumerator();
        #endregion
    }
}