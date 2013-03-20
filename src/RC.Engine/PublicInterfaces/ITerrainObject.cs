﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common;

namespace RC.Engine.PublicInterfaces
{
    /// <summary>
    /// Defines the interface of a terrain object on the map.
    /// </summary>
    public interface ITerrainObject : IMapContent, ICellDataChangeSetTarget
    {
        /// <summary>
        /// Gets the type of this terrain object as it is defined in the tileset.
        /// </summary>
        ITerrainObjectType Type { get; }

        /// <summary>
        /// Gets or sets the coordinates of the top-left quadratic tile of this terrain object.
        /// </summary>
        RCIntVector MapCoords { get; }
    }
}