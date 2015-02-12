﻿using System;
using RC.Common;

namespace RC.App.BizLogic.BusinessComponents.Core
{
    /// <summary>
    /// This class is responsible for minimap calculations.
    /// </summary>
    class Minimap : IMinimap, IDisposable
    {
        /// <summary>
        /// Constructs a Minimap instance.
        /// </summary>
        /// <param name="fullWindow">Reference to the full window.</param>
        /// <param name="attachedWindow">Reference to the currently attached window.</param>
        /// <param name="minimapControlPixelSize">The size of the minimap control in pixels.</param>
        public Minimap(IMapWindow fullWindow, IMapWindow attachedWindow, RCIntVector minimapControlPixelSize)
        {
            if (fullWindow == null) { throw new ArgumentNullException("fullWindow"); }
            if (attachedWindow == null) { throw new ArgumentNullException("attachedWindow"); }
            if (minimapControlPixelSize == RCIntVector.Undefined) { throw new ArgumentNullException("minimapControlPixelSize"); }

            this.isDisposed = false;
            this.fullWindow = fullWindow;
            this.attachedWindow = attachedWindow;

            this.windowIndicatorCache = new CachedValue<RCIntRectangle>(this.CalculateWindowIndicator);

            if (!Minimap.TryAlignMinimapHorizontally(minimapControlPixelSize, this.fullWindow.CellWindow.Size, out this.minimapPosition, out this.mapToMinimapTransformation) &&
                !Minimap.TryAlignMinimapVertically(minimapControlPixelSize, this.fullWindow.CellWindow.Size, out this.minimapPosition, out this.mapToMinimapTransformation))
            {
                throw new InvalidOperationException("Unable to align the minimap inside the minimap control!");
            }
        }

        #region IDisposable members

        /// <see cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            this.isDisposed = true;
        }

        #endregion IDisposable members

        #region IMinimap members

        /// <see cref="IMinimap.WindowIndicator"/>
        public RCIntRectangle WindowIndicator
        {
            get
            {
                if (this.isDisposed) { throw new ObjectDisposedException("IMinimap"); }
                return this.windowIndicatorCache.Value;
            }
        }

        /// <see cref="IMinimap.MinimapPosition"/>
        public RCIntRectangle MinimapPosition
        {
            get
            {
                if (this.isDisposed) { throw new ObjectDisposedException("IMinimap"); }
                return this.minimapPosition;
            }
        }

        #endregion IMinimap members

        #region Internal public methods

        /// <summary>
        /// Invalidates the cached data of this minimap.
        /// </summary>
        internal void Invalidate() { this.windowIndicatorCache.Invalidate(); }

        #endregion Internal public methods

        #region Private methods

        /// <summary>
        /// Calculates the window indicator.
        /// </summary>
        /// <returns>The calculated window indicator.</returns>
        private RCIntRectangle CalculateWindowIndicator()
        {
            bool alignToLeft = this.attachedWindow.WindowMapCoords.Left == this.fullWindow.WindowMapCoords.Left;
            bool alignToRight = this.attachedWindow.WindowMapCoords.Right == this.fullWindow.WindowMapCoords.Right;
            bool alignToTop = this.attachedWindow.WindowMapCoords.Top == this.fullWindow.WindowMapCoords.Top;
            bool alignToBottom = this.attachedWindow.WindowMapCoords.Bottom == this.fullWindow.WindowMapCoords.Bottom;
            
            RCIntVector topLeftCornerTransformed = this.mapToMinimapTransformation.TransformAB(this.attachedWindow.WindowMapCoords.Location).Round();
            RCIntVector size = this.mapToMinimapTransformation.TransformAB(this.attachedWindow.WindowMapCoords.Size).Round();

            int topLeftCornerX = alignToLeft ? 0 : (alignToRight ? this.minimapPosition.Size.X - size.X : topLeftCornerTransformed.X);
            int topLeftCornerY = alignToTop ? 0 : (alignToBottom ? this.minimapPosition.Size.Y - size.Y : topLeftCornerTransformed.Y);
            RCIntRectangle indicatorRect = new RCIntRectangle(topLeftCornerX, topLeftCornerY, size.X, size.Y) + this.minimapPosition.Location;

            return indicatorRect;
        }

        /// <summary>
        /// Tries to align the minimap horizontally.
        /// </summary>
        /// <param name="minimapControlPixelSize">The size of the minimap control in pixels.</param>
        /// <param name="mapCellSize">The size of the map in cells.</param>
        /// <param name="minimapPosition">The position of the minimap on the minimap control in pixels.</param>
        /// <param name="transformation">The transformation between the map (A) and minimap (B) coordinate-systems.</param>
        /// <returns>True if the alignment was successfully; otherwise false.</returns>
        private static bool TryAlignMinimapHorizontally(
            RCIntVector minimapControlPixelSize,
            RCIntVector mapCellSize,
            out RCIntRectangle minimapPosition,
            out RCCoordTransformation transformation)
        {
            RCNumber horzAlignedMinimapHeight = (RCNumber)(minimapControlPixelSize.X * mapCellSize.Y) / (RCNumber)mapCellSize.X;
            if (horzAlignedMinimapHeight > minimapControlPixelSize.Y)
            {
                /// Cannot align horizontally.
                minimapPosition = RCIntRectangle.Undefined;
                transformation = null;
                return false;
            }

            /// Align horizontally
            int minimapPixelHeight = horzAlignedMinimapHeight > (int)horzAlignedMinimapHeight
                                   ? (int)horzAlignedMinimapHeight + 1
                                   : (int)horzAlignedMinimapHeight;
            minimapPosition = new RCIntRectangle(0, (minimapControlPixelSize.Y - minimapPixelHeight) / 2, minimapControlPixelSize.X, minimapPixelHeight);

            /// Create the coordinate transformation
            RCNumVector pixelSizeOnMap = new RCNumVector((RCNumber)mapCellSize.X / (RCNumber)minimapControlPixelSize.X,
                                                         (RCNumber)mapCellSize.X / (RCNumber)minimapControlPixelSize.X);
            RCNumVector nullVectorOfMinimap = (pixelSizeOnMap / 2) - (new RCNumVector(1, 1) / 2);
            RCNumVector baseVectorOfMinimapX = new RCNumVector(pixelSizeOnMap.X, 0);
            RCNumVector baseVectorOfMinimapY = new RCNumVector(0, pixelSizeOnMap.Y);
            transformation = new RCCoordTransformation(nullVectorOfMinimap, baseVectorOfMinimapX, baseVectorOfMinimapY);

            return true;
        }

        /// <summary>
        /// Tries to align the minimap vertically.
        /// </summary>
        /// <param name="minimapControlPixelSize">The size of the minimap control in pixels.</param>
        /// <param name="mapCellSize">The size of the map in cells.</param>
        /// <param name="minimapPosition">The position of the minimap on the minimap control in pixels.</param>
        /// <param name="transformation">The transformation between the map (A) and minimap (B) coordinate-systems.</param>
        /// <returns>True if the alignment was successfully; otherwise false.</returns>
        private static bool TryAlignMinimapVertically(
            RCIntVector minimapControlPixelSize,
            RCIntVector mapCellSize,
            out RCIntRectangle minimapPosition,
            out RCCoordTransformation transformation)
        {
            RCNumber vertAlignedMinimapWidth = (RCNumber)(minimapControlPixelSize.Y * mapCellSize.X) / (RCNumber)mapCellSize.Y;
            if (vertAlignedMinimapWidth > minimapControlPixelSize.X)
            {
                /// Cannot align vertically.
                minimapPosition = RCIntRectangle.Undefined;
                transformation = null;
                return false;
            }

            /// Align vertically
            int minimapPixelWidth = vertAlignedMinimapWidth > (int)vertAlignedMinimapWidth
                                  ? (int)vertAlignedMinimapWidth + 1
                                  : (int)vertAlignedMinimapWidth;
            minimapPosition = new RCIntRectangle((minimapControlPixelSize.X - minimapPixelWidth) / 2, 0, minimapPixelWidth, minimapControlPixelSize.Y);

            /// Create the coordinate transformation
            RCNumVector pixelSizeOnMap = new RCNumVector((RCNumber)mapCellSize.Y / (RCNumber)minimapControlPixelSize.Y,
                                                         (RCNumber)mapCellSize.Y / (RCNumber)minimapControlPixelSize.Y);
            RCNumVector nullVectorOfMinimap = (pixelSizeOnMap / 2) - (new RCNumVector(1, 1) / 2);
            RCNumVector baseVectorOfMinimapX = new RCNumVector(pixelSizeOnMap.X, 0);
            RCNumVector baseVectorOfMinimapY = new RCNumVector(0, pixelSizeOnMap.Y);
            transformation = new RCCoordTransformation(nullVectorOfMinimap, baseVectorOfMinimapX, baseVectorOfMinimapY);

            return true;
        }

        #endregion Private methods

        /// <summary>
        /// This flag indicates whether this minimap has already been disposed or not.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Reference to the currently attached window.
        /// </summary>
        private readonly IMapWindow attachedWindow;

        /// <summary>
        /// Reference to the current full window.
        /// </summary>
        private readonly IMapWindow fullWindow;

        /// <summary>
        /// The position of the minimap inside the minimap control.
        /// </summary>
        private RCIntRectangle minimapPosition;

        /// <summary>
        /// Cached window indicator.
        /// </summary>
        private CachedValue<RCIntRectangle> windowIndicatorCache;

        /// <summary>
        /// Coordinate transformation between the map (A) and minimap (B) coordinate-systems.
        /// </summary>
        private readonly RCCoordTransformation mapToMinimapTransformation;
    }
}
