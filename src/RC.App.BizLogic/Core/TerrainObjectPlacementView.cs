﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.App.BizLogic.PublicInterfaces;
using RC.Engine.PublicInterfaces;
using RC.Common;

namespace RC.App.BizLogic.Core
{
    /// <summary>
    /// Implementation of object placement views for terrain objects.
    /// </summary>
    class TerrainObjectPlacementView : IObjectPlacementView
    {
        /// <summary>
        /// Constructs a TerrainObjectPlacementView instance.
        /// </summary>
        /// <param name="terrainObjectType">Reference to the type of the terrain object being placed.</param>
        /// <param name="map">Reference to the map.</param>
        public TerrainObjectPlacementView(ITerrainObjectType terrainObjectType, IMapAccess map)
        {
            if (terrainObjectType == null) { throw new ArgumentNullException("terrainObjectType"); }
            if (map == null) { throw new ArgumentNullException("map"); }

            this.terrainObjectType = terrainObjectType;
            this.map = map;
        }

        #region IObjectPlacementView methods

        /// <see cref="IMapView.MapSize"/>
        public RCIntVector MapSize { get { return this.map.CellSize * new RCIntVector(BizLogicConstants.PIXEL_PER_NAVCELL, BizLogicConstants.PIXEL_PER_NAVCELL); } }

        /// <see cref="IObjectPlacementView.GetObjectPlacementBox"/>
        public ObjectPlacementBox GetObjectPlacementBox(RCIntRectangle displayedArea, RCIntVector position)
        {
            if (displayedArea == RCIntRectangle.Undefined) { throw new ArgumentNullException("displayedArea"); }
            if (position == RCIntVector.Undefined) { throw new ArgumentNullException("position"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(displayedArea)) { throw new ArgumentOutOfRangeException("displayedArea"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(position)) { throw new ArgumentOutOfRangeException("displayedArea"); }

            RCIntRectangle cellWindow = new RCIntRectangle(displayedArea.X / BizLogicConstants.PIXEL_PER_NAVCELL,
                                                           displayedArea.Y / BizLogicConstants.PIXEL_PER_NAVCELL,
                                                           (displayedArea.Right - 1) / BizLogicConstants.PIXEL_PER_NAVCELL - displayedArea.X / BizLogicConstants.PIXEL_PER_NAVCELL + 1,
                                                           (displayedArea.Bottom - 1) / BizLogicConstants.PIXEL_PER_NAVCELL - displayedArea.Y / BizLogicConstants.PIXEL_PER_NAVCELL + 1);
            RCIntVector displayOffset = new RCIntVector(displayedArea.X % BizLogicConstants.PIXEL_PER_NAVCELL, displayedArea.Y % BizLogicConstants.PIXEL_PER_NAVCELL);

            RCIntVector navCellCoords = new RCIntVector((displayedArea + position).X / BizLogicConstants.PIXEL_PER_NAVCELL,
                                                        (displayedArea + position).Y / BizLogicConstants.PIXEL_PER_NAVCELL);
            IQuadTile quadTileAtPos = this.map.GetCell(navCellCoords).ParentQuadTile;
            RCIntVector topLeftQuadCoords = quadTileAtPos.MapCoords - this.terrainObjectType.QuadraticSize / 2;

            MapSpriteInstance spriteInstance =
                new MapSpriteInstance()
                {
                    Index = this.terrainObjectType.Index,
                    DisplayCoords = (this.map.QuadToCellRect(new RCIntRectangle(topLeftQuadCoords, new RCIntVector(1, 1))).Location - cellWindow.Location)
                                  * new RCIntVector(BizLogicConstants.PIXEL_PER_NAVCELL, BizLogicConstants.PIXEL_PER_NAVCELL)
                                  - displayOffset
                };

            ObjectPlacementBox placementBox = new ObjectPlacementBox()
            {
                Sprite = spriteInstance,
                IllegalParts = new List<RCIntRectangle>(),
                LegalParts = new List<RCIntRectangle>()
            };

            HashSet<RCIntVector> violatingQuadCoords = this.terrainObjectType.CheckConstraints(this.map, topLeftQuadCoords);
            violatingQuadCoords.UnionWith(this.terrainObjectType.CheckTerrainObjectIntersections(this.map, topLeftQuadCoords));
            for (int x = 0; x < this.terrainObjectType.QuadraticSize.X; x++)
            {
                for (int y = 0; y < this.terrainObjectType.QuadraticSize.Y; y++)
                {
                    RCIntVector relativeQuadCoords = new RCIntVector(x, y);
                    RCIntVector absQuadCoords = topLeftQuadCoords + relativeQuadCoords;
                    RCIntRectangle partRect = (this.map.QuadToCellRect(new RCIntRectangle(absQuadCoords, new RCIntVector(1, 1))) - cellWindow.Location)
                                            * new RCIntVector(BizLogicConstants.PIXEL_PER_NAVCELL, BizLogicConstants.PIXEL_PER_NAVCELL)
                                            - displayOffset;
                    if (violatingQuadCoords.Contains(relativeQuadCoords))
                    {
                        placementBox.IllegalParts.Add(partRect);
                    }
                    else
                    {
                        placementBox.LegalParts.Add(partRect);
                    }
                }
            }
            
            return placementBox;
        }

        #endregion IObjectPlacementView methods

        /// <summary>
        /// Reference to the type of the terrain object being placed.
        /// </summary>
        private ITerrainObjectType terrainObjectType;

        /// <summary>
        /// Reference to the map.
        /// </summary>
        private IMapAccess map;
    }
}
