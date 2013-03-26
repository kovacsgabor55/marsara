﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common;
using RC.Engine.PublicInterfaces;

namespace RC.Engine.Core
{
    /// <summary>
    /// Represents a terrain object type in the tileset. 
    /// </summary>
    class TerrainObjectType : ITerrainObjectType
    {
        /// <summary>
        /// Constructs a TerrainObjectType instance.
        /// </summary>
        /// <param name="name">The name of the TerrainObjectType.</param>
        /// <param name="imageData">The byte sequence that contains the image data of the TerrainObjectType.</param>
        /// <param name="quadraticSize">The size of the TerrainObjectType in quadratic tiles.</param>
        public TerrainObjectType(string name, byte[] imageData, RCIntVector quadraticSize, TileSet tileset)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (imageData == null || imageData.Length == 0) { throw new ArgumentNullException("imageData"); }
            if (quadraticSize == RCIntVector.Undefined) { throw new ArgumentNullException("quadraticSize"); }
            if (tileset == null) { throw new ArgumentNullException("tileset"); }
            if (quadraticSize.X <= 0 || quadraticSize.Y <= 0) { throw new ArgumentOutOfRangeException("quadraticSize", "Quadratic size cannot be 0 in any direction!"); }

            this.name = name;
            this.imageData = imageData;
            this.quadraticSize = quadraticSize;
            this.tileset = tileset;
            this.areaCanBeExcluded = true;
            this.properties = new Dictionary<string, string>();
            this.constraints = new List<ITerrainObjectConstraint>();
            this.cellDataChangesets = new List<ICellDataChangeSet>();

            this.includedQuadCoords = new HashSet<RCIntVector>();
            for (int x = 0; x < quadraticSize.X; x++)
            {
                for (int y = 0; y < quadraticSize.Y; y++)
                {
                    this.includedQuadCoords.Add(new RCIntVector(x, y));
                }
            }
        }

        /// <summary>
        /// Adds a cell data changeset to this terrain object type.
        /// </summary>
        /// <param name="changeset">The changeset operation to add.</param>
        public void AddCellDataChangeset(ICellDataChangeSet changeset)
        {
            if (changeset == null) { throw new ArgumentNullException("changeset"); }
            if (changeset.Tileset != this.tileset) { throw new TileSetException("The given ICellDataChangeSet is in another TileSet!"); }
            if (this.tileset.IsFinalized) { throw new InvalidOperationException("TileSet already finalized!"); }

            this.cellDataChangesets.Add(changeset);
        }

        /// <summary>
        /// Excludes the given area from this TerrainObjectType. This method is only available if the tileset
        /// has not yet been finalized and constraints/data-changes have not yet been defined.
        /// </summary>
        /// <param name="area">The area that won't be part of this TerrainObjectType.</param>
        public void ExcludeArea(RCIntRectangle area)
        {
            if (area == RCIntRectangle.Undefined) { throw new ArgumentNullException("area"); }
            if (area.X < 0 || area.Y < 0 || area.Right > this.quadraticSize.X || area.Bottom > this.quadraticSize.Y) { throw new ArgumentOutOfRangeException("area", "The excluded area exceeds the borders of the TerrainObjectType!"); }
            if (this.tileset.IsFinalized) { throw new InvalidOperationException("TileSet already finalized!"); }
            if (!this.areaCanBeExcluded) { throw new InvalidOperationException("Excluding area is not allowed!"); }

            for (int x = area.X; x < area.Right; x++)
            {
                for (int y = area.Y; y < area.Bottom; y++)
                {
                    this.includedQuadCoords.Remove(new RCIntVector(x, y));
                }
            }
        }

        /// <summary>
        /// Adds a constraint to this TerrainObjectType. Area exclusion is not possible after calling this method.
        /// </summary>
        /// <param name="constraint">The constraint to add.</param>
        public void AddConstraint(ITerrainObjectConstraint constraint)
        {
            if (constraint == null) { throw new ArgumentNullException("constraint"); }
            if (constraint.Tileset != this.tileset) { throw new TileSetException("The given ITerrainObjectConstraint is in another TileSet!"); }
            if (this.tileset.IsFinalized) { throw new InvalidOperationException("TileSet already finalized!"); }

            this.constraints.Add(constraint);
            this.areaCanBeExcluded = false;
        }

        /// <summary>
        /// Adds a property to this TerrainObjectType.
        /// </summary>
        /// <param name="propName">The name of the property.</param>
        /// <param name="propValue">The value of the property.</param>
        public void AddProperty(string propName, string propValue)
        {
            if (propName == null) { throw new ArgumentNullException("propName"); }
            if (propValue == null) { throw new ArgumentNullException("propValue"); }
            if (this.tileset.IsFinalized) { throw new InvalidOperationException("TileSet already finalized!"); }

            if (this.properties.ContainsKey(propName)) { throw new TileSetException(string.Format("TerrainObjectType already contains a property with name '{0}'!", propName)); }
            this.properties.Add(propName, propValue);
        }

        /// <summary>
        /// Sets the index of this TerrainObjectType in the tileset.
        /// </summary>
        /// <param name="newIndex">The new index.</param>
        public void SetIndex(int newIndex)
        {
            if (this.tileset.IsFinalized) { throw new InvalidOperationException("TileSet already finalized!"); }
            if (newIndex < 0) { throw new ArgumentOutOfRangeException("newIndex", "The index of the TerrainObjectTypes must be non-negative!"); }
            this.index = newIndex;
        }

        /// <summary>
        /// Check and finalize this TerrainObjectType. Buildup methods will be unavailable after calling this method.
        /// </summary>
        public void CheckAndFinalize()
        {
            this.areaCanBeExcluded = false;
        }

        #region ITerrainObjectType methods

        /// <see cref="ITerrainObjectType.Name"/>
        public string Name { get { return this.name; } }

        /// <see cref="ITerrainObjectType.QuadraticSize"/>
        public RCIntVector QuadraticSize { get { return this.quadraticSize; } }

        /// <see cref="ITerrainObjectType.CellDataChangesets"/>
        public IEnumerable<ICellDataChangeSet> CellDataChangesets { get { return this.cellDataChangesets; } }

        /// <see cref="ITerrainObjectType.Tileset"/>
        public ITileSet Tileset { get { return this.tileset; } }

        /// <see cref="ITerrainObjectType.ImageData"/>
        public byte[] ImageData { get { return this.imageData; } }

        /// <see cref="ITerrainObjectType.Index"/>
        public int Index { get { return this.index; } }

        /// <see cref="ITerrainObjectType.GetProperty"/>
        public string GetProperty(string propName)
        {
            if (propName == null) { throw new ArgumentNullException("propName"); }
            return this.properties.ContainsKey(propName) ? this.properties[propName] : null;
        }

        /// <see cref="ITerrainObjectType.CheckConstraints"/>
        public HashSet<RCIntVector> CheckConstraints(IMapAccess map, RCIntVector position)
        {
            if (map == null) { throw new ArgumentNullException("map"); }
            if (position == RCIntVector.Undefined) { throw new ArgumentNullException("position"); }

            /// Check against the constraints defined by this terrain object type.
            HashSet<RCIntVector> retList = new HashSet<RCIntVector>();
            foreach (ITerrainObjectConstraint contraint in this.constraints)
            {
                retList.UnionWith(contraint.Check(map, position));
            }

            for (int quadX = 0; quadX < this.quadraticSize.X; quadX++)
            {
                for (int quadY = 0; quadY < this.quadraticSize.Y; quadY++)
                {
                    RCIntVector relQuadCoords = new RCIntVector(quadX, quadY);
                    RCIntVector absQuadCoords = position + relQuadCoords;
                    if (absQuadCoords.X < 0 || absQuadCoords.X >= map.Size.X ||
                        absQuadCoords.Y < 0 || absQuadCoords.Y >= map.Size.Y)
                    {
                        /// Intersection with the boundaries of the map.
                        retList.Add(relQuadCoords);
                    }
                }
            }

            return retList;
        }

        /// <see cref="ITerrainObjectType.CheckTerrainObjectIntersections"/>
        public HashSet<RCIntVector> CheckTerrainObjectIntersections(IMapAccess map, RCIntVector position)
        {
            if (map == null) { throw new ArgumentNullException("map"); }
            if (position == RCIntVector.Undefined) { throw new ArgumentNullException("position"); }

            HashSet<RCIntVector> retList = new HashSet<RCIntVector>();
            for (int quadX = 0; quadX < this.quadraticSize.X; quadX++)
            {
                for (int quadY = 0; quadY < this.quadraticSize.Y; quadY++)
                {
                    RCIntVector relQuadCoords = new RCIntVector(quadX, quadY);
                    RCIntVector absQuadCoords = position + relQuadCoords;
                    if (absQuadCoords.X >= 0 && absQuadCoords.X < map.Size.X &&
                        absQuadCoords.Y >= 0 && absQuadCoords.Y < map.Size.Y)
                    {
                        /// Check intersection with other terrain objects on the map.
                        RCNumRectangle quadTileRect = ((RCNumRectangle)map.QuadToCellRect(new RCIntRectangle(absQuadCoords, new RCIntVector(1, 1))))
                                                    - new RCNumVector(1, 1) / 2;
                        foreach (ITerrainObject objToCheck in map.TerrainObjects.GetContents(quadTileRect))
                        {
                            if (!this.IsExcluded(relQuadCoords) && !objToCheck.Type.IsExcluded(absQuadCoords - objToCheck.MapCoords))
                            {
                                retList.Add(relQuadCoords);
                                break;
                            }
                        }
                    }
                }
            }
            return retList;
        }

        /// <see cref="ITerrainObjectType.IsExcluded"/>
        public bool IsExcluded(RCIntVector position)
        {
            if (position == RCIntVector.Undefined) { throw new ArgumentNullException("position"); }
            return !this.includedQuadCoords.Contains(position);
        }

        #endregion ITerrainObjectType methods

        /// <summary>
        /// Gets the constraints of this TerrainObjectType.
        /// </summary>
        //public IEnumerable<ITerrainObjectConstraint> Constraints { get { return this.constraints; } }

        /// <summary>
        /// The size of the TerrainObjectType in quadratic tiles.
        /// </summary>
        private RCIntVector quadraticSize;

        /// <summary>
        /// The byte sequence that contains the image data of this TerrainObjectType.
        /// </summary>
        private byte[] imageData;

        /// <summary>
        /// List of the properties of this TerrainObjectType mapped by their name.
        /// </summary>
        private Dictionary<string, string> properties;

        /// <summary>
        /// The name of this TerrainObjectType.
        /// </summary>
        private string name;

        /// <summary>
        /// List of the constraints of this TerrainObjectType.
        /// </summary>
        private List<ITerrainObjectConstraint> constraints;

        /// <summary>
        /// List of the quadratic coordinates that are included in this TerrainObjectType.
        /// </summary>
        private HashSet<RCIntVector> includedQuadCoords;

        /// <summary>
        /// List of the cell data changesets of this terrain object type.
        /// </summary>
        private List<ICellDataChangeSet> cellDataChangesets;

        /// <summary>
        /// This flag indicates whether excluding area is allowed or not.
        /// </summary>
        private bool areaCanBeExcluded;

        /// <summary>
        /// The tileset that this TerrainObjectType belongs to.
        /// </summary>
        private TileSet tileset;

        /// <summary>
        /// The index of this TerrainObjectType in the tileset.
        /// </summary>
        private int index;
    }
}
