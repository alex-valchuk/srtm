// The MIT License (MIT)

// Copyright (c) 2017 Alpine Chough Software, Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;

namespace SRTM
{
    /// <summary>
    /// SRTM data cell.
    /// </summary>
    /// <exception cref='FileNotFoundException'>
    /// Is thrown when a file path argument specifies a file that does not exist.
    /// </exception>
    /// <exception cref='ArgumentException'>
    /// Is thrown when an argument passed to a method is invalid.
    /// </exception>
    /// <exception cref='ArgumentOutOfRangeException'>
    /// Is thrown when an argument passed to a method is invalid because it is outside the allowable range of values as
    /// specified by the method.
    /// </exception>
    public class SRTMDataCell : ISRTMDataCell
    {
        #region Lifecycle

        private static readonly double _oneArcSecondAtEquator = 30.87f;

        /// <summary>
        /// Initializes a new instance of the <see cref="SRTM.SRTMDataCell"/> class.
        /// </summary>
        /// <param name='filepath'>
        /// Filepath.
        /// </param>
        /// <exception cref='FileNotFoundException'>
        /// Is thrown when a file path argument specifies a file that does not exist.
        /// </exception>
        /// <exception cref='ArgumentException'>
        /// Is thrown when an argument passed to a method is invalid.
        /// </exception>
        public SRTMDataCell(string filepath)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("File not found.", filepath);

            var filename = Path.GetFileName(filepath);
            filename = filename.Substring(0, filename.IndexOf('.')).ToLower(); // Path.GetFileNameWithoutExtension(filepath).ToLower();
            var fileCoordinate = filename.Split(new[] { 'e', 'w' });
            if (fileCoordinate.Length != 2)
                throw new ArgumentException("Invalid filename.", filepath);

            fileCoordinate[0] = fileCoordinate[0].TrimStart(new[] { 'n', 's' });

            Latitude = int.Parse(fileCoordinate[0]);
            if (filename.Contains("s"))
                Latitude *= -1;

            Longitude = int.Parse(fileCoordinate[1]);
            if (filename.Contains("w"))
                Longitude *= -1;

            if (filepath.EndsWith(".zip"))
            {
                using (var stream = File.OpenRead(filepath))
                using (var archive = new System.IO.Compression.ZipArchive(stream))
                using (var memoryStream = new MemoryStream())
                {
                    using (var hgt = archive.Entries[0].Open())
                    {
                        hgt.CopyTo(memoryStream);
                        HgtData = memoryStream.ToArray();
                    }
                }
            }
            else
            {
                HgtData = File.ReadAllBytes(filepath);
            }

            switch (HgtData.Length)
            {
                // https://www.esri.com/news/arcuser/0400/wdside.html#:~:text=At%20the%20equator%2C%20an%20arc,101.27%20feet%20or%2030.87%20meters).
                // At the equator, an arc-second of longitude approximately equals an arc-second of latitude, which is 1 / 60th of a nautical mile(or 101.27 feet or 30.87 meters).
                // Arc-seconds of latitude remain nearly constant, while arc-seconds of longitude decrease in a trigonometric cosine-based fashion as one moves toward the earth's poles.
                // E.g. at 49 degrees north latitude, along the northern boundary of the Concrete sheet, an arc-second of longitude equals 30.87 meters * 0.6561 (cos 49) or 20.250 meters. 
                case 1201 * 1201 * 2: // SRTM-3
                    PointsPerCell = 1201;
                    VerticalPointsPerCell = 1201;
                    PointHeightInMeters = _oneArcSecondAtEquator * 3; // 3 arc-seconds
                    PointWidthInMeters = _oneArcSecondAtEquator * 3 * Math.Cos(ConvertToRadians(Latitude));
                    break;
                case 3601 * 3601 * 2: // SRTM-1
                    PointsPerCell = 3601;
                    VerticalPointsPerCell = 3601;
                    PointHeightInMeters = _oneArcSecondAtEquator; // 1 arc-second
                    PointWidthInMeters = _oneArcSecondAtEquator * Math.Cos(ConvertToRadians(Latitude));
                    break;
                default:
                    throw new ArgumentException("Invalid file size.", filepath);
            }
        }
        private double ConvertToRadians(double angle)
        {
            return Math.PI / 180 * angle;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the hgt data.
        /// </summary>
        /// <value>
        /// The hgt data.
        /// </value>
        public byte[] HgtData { get; }

        /// <summary>
        /// Gets or sets the points per cell.
        /// </summary>
        /// <value>
        /// The points per cell.
        /// </value>
        public int PointsPerCell { get; }

        /// <summary>
        /// Gets or sets the vertical points per cell.
        /// </summary>
        /// <value>
        /// The vertical points per cell.
        /// </value>
        public int VerticalPointsPerCell { get; }

        /// <summary>
        /// Gets the point height in meters.
        /// </summary>
        /// <value>
        /// The point height in meters.
        /// </value>
        public double PointHeightInMeters { get; }

        /// <summary>
        /// Gets the point width in meters.
        /// </summary>
        /// <value>
        /// The point width in meters.
        /// </value>
        public double PointWidthInMeters { get; }

        /// <summary>
        /// Gets or sets the latitude of the srtm data file.
        /// </summary>
        /// <value>
        /// The latitude.
        /// </value>
        public int Latitude { get; }

        /// <summary>
        /// Gets or sets the longitude of the srtm data file.
        /// </summary>
        /// <value>
        /// The longitude.
        /// </value>
        public int Longitude { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the elevation.
        /// </summary>
        /// <returns>
        /// The height. Null, if elevation is not available.
        /// </returns>
        /// <param name='latitude'></param>
        /// <param name='longitude'></param>
        /// <exception cref='Exception'>
        /// Represents errors that occur during application execution.
        /// </exception>
        public int? GetElevation(double latitude, double longitude)
        {
            var bytesPos = GetBytePositionByCoordinate(latitude, longitude);
            return ReadByteData(bytesPos);
        }

        /// <summary>
        /// Method responsible for obtaining a byte position by coordinate.
        /// </summary>
        /// <param name="localLat">Local latitude within the data cell</param>
        /// <param name="localLon">Local longitude within the data cell</param>
        /// <returns>Byte position</returns>
        public int GetBytePositionByCoordinate(double latitude, double longitude)
        {
            int localLat = (int)((latitude - Latitude) * PointsPerCell);
            int localLon = (int)((longitude - Longitude) * PointsPerCell);
            return ((PointsPerCell - localLat - 1) * PointsPerCell * 2) + localLon * 2;
        }

        /// <summary>
        /// Method responsible for obtaining an elevation by byte position.
        /// </summary>
        /// <param name="bytesPos">The byte position</param>
        /// <returns>The elevation</returns>
        public int? GetElevation(int bytesPos)
        {
            if ((HgtData[bytesPos] == 0x80) && (HgtData[bytesPos + 1] == 0x00))
                return null;

            // Motorola "big-endian" order with the most significant byte first
            return (HgtData[bytesPos]) << 8 | HgtData[bytesPos + 1];
        }

        /// <summary>
        /// Gets the elevation. Data is smoothed using bilinear interpolation.
        /// </summary>
        /// <returns>
        /// The height. Null, if elevation is not available.
        /// </returns>
        /// <param name='latitude'></param>
        /// <param name='longitude'></param>
        /// <exception cref='Exception'>
        /// Represents errors that occur during application execution.
        /// </exception>
        public double? GetElevationBilinear(double latitude, double longitude)
        {
            double localLat = (latitude - Latitude) * PointsPerCell;
            double localLon = (longitude - Longitude) * PointsPerCell;

            int localLatMin = (int) Math.Floor(localLat);
            int localLonMin = (int) Math.Floor(localLon);
            int localLatMax = (int) Math.Ceiling(localLat);
            int localLonMax = (int) Math.Ceiling(localLon);

            int? elevation00 = ReadByteData(localLatMin, localLonMin);
            int? elevation10 = ReadByteData(localLatMax, localLonMin);
            int? elevation01 = ReadByteData(localLatMin, localLonMax);
            int? elevation11 = ReadByteData(localLatMax, localLonMax);

            if (!elevation00.HasValue || !elevation10.HasValue || !elevation01.HasValue || !elevation11.HasValue)
            {
                //Can't do bilinear if missing one of the points. Default to regular.
                return (double)GetElevation(latitude, longitude);
            }
            
            double deltaLat = localLatMax - localLat;
            double deltaLon = localLonMax - localLon;

            return Blerp((double) elevation00, (double) elevation10, (double) elevation01, (double) elevation11,
                deltaLat, deltaLon);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Method responsible for reading byte data from data cell file.
        /// </summary>
        /// <param name="localLat">Local latitude within the data cell</param>
        /// <param name="localLon">Local longitude within the data cell</param>
        /// <returns>Height read from data cell file</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private int? ReadByteData(int localLat, int localLon)
        {
            int bytesPos = ((PointsPerCell - localLat - 1) * PointsPerCell * 2) + localLon * 2;
            return ReadByteData(bytesPos);
        }

        /// <summary>
        /// Method responsible for reading byte data from data cell file.
        /// </summary>
        /// <param name="bytesPos">The byte position</param>
        /// <returns>Height read from data cell file</returns>
        private int? ReadByteData(int bytesPos)
        {
            if (bytesPos < 0 || bytesPos > PointsPerCell * PointsPerCell * 2)
                throw new ArgumentOutOfRangeException("Coordinates out of range.", "coordinates");

            if (bytesPos >= HgtData.Length)
                return null;

            return GetElevation(bytesPos);
        }

        private double Lerp(double start, double end, double delta)
        {
            return start + (end - start) * delta;
        }

        private double Blerp(double val00, double val10, double val01, double val11, double deltaX, double deltaY)
        {
            return Lerp(Lerp(val11, val01, deltaX), Lerp(val10, val00, deltaX), deltaY);
        }
        
        #endregion
    }
}