namespace SRTM
{
    public interface ISRTMDataCell
    {
        int Latitude { get; }

        int Longitude { get; }

        byte[] HgtData { get; }

        int PointsPerCell { get; }

        /// <summary>
        /// Gets the number of vertical points per cell.
        /// Useful for custom data cells that must have square shape.
        /// On majority of earth places it is feasible only when the number of horizontal and vertical points are different.
        /// It's because the size of vertical arc-second is constant while the size of horizontal one is different on different latitudes.
        /// </summary>
        int VerticalPointsPerCell { get; }

        double PointHeightInMeters { get; }

        double PointWidthInMeters { get; }

        int? GetElevation(double latitude, double longitude);
        
        double? GetElevationBilinear(double latitude, double longitude);

        int GetBytePositionByCoordinate(double latitude, double longitude);

        int? GetElevation(int bytesPos);
    }
}
