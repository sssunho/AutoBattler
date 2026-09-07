using System;

namespace AutoBattler.HexGrid
{
    public sealed class HexMap
    {
        readonly HexCell[] _cells;

        public int Width { get; }
        public int Height { get; }

        public HexCell this[HexCoordinate coordinate]
        {
            get
            {
                Validate(coordinate);
                return _cells[GetIndex(coordinate)];
            }
        }

        public HexMap(int width, int height)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            _cells = new HexCell[checked(width * height)];

            for (int index = 0; index < _cells.Length; index++)
            {
                _cells[index] = new HexCell();
            }
        }

        public bool IsValid(HexCoordinate coordinate)
        {
            return coordinate.Q >= 0
                && coordinate.Q < Width
                && coordinate.R >= 0
                && coordinate.R < Height;
        }

        public bool TryGetCell(HexCoordinate coordinate, out HexCell cell)
        {
            if (!IsValid(coordinate))
            {
                cell = default;
                return false;
            }

            cell = _cells[GetIndex(coordinate)];
            return true;
        }

        int GetIndex(HexCoordinate coordinate)
        {
            return coordinate.Q + coordinate.R * Width;
        }

        void Validate(HexCoordinate coordinate)
        {
            if (!IsValid(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, null);
            }
        }
    }
}
