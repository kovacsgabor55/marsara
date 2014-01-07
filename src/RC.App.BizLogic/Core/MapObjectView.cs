﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.App.BizLogic.PublicInterfaces;
using RC.Engine.Maps.PublicInterfaces;
using RC.Common;
using RC.Engine.Simulator.PublicInterfaces;
using RC.Common.Diagnostics;
using RC.Engine.Simulator.Scenarios;

namespace RC.App.BizLogic.Core
{
    /// <summary>
    /// Implementation of views on the objects of the currently opened map.
    /// </summary>
    class MapObjectView : MapViewBase, IMapObjectView
    {
        /// <summary>
        /// Constructs a MapObjectView instance.
        /// </summary>
        /// <param name="scenario">The subject of this view.</param>
        public MapObjectView(Scenario scenario)
            : base(scenario.Map)
        {
            if (scenario == null) { throw new ArgumentNullException("scenario"); }
            this.scenario = scenario;
        }

        #region IMapObjectView methods

        /// <see cref="IMapObjectView.GetVisibleMapObjects"/>
        public List<MapObjectInstance> GetVisibleMapObjects(RCIntRectangle displayedArea)
        {
            if (displayedArea == RCIntRectangle.Undefined) { throw new ArgumentNullException("displayedArea"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(displayedArea)) { throw new ArgumentOutOfRangeException("displayedArea"); }

            RCIntRectangle cellWindow;
            RCIntVector displayOffset;
            this.CalculateCellWindow(displayedArea, out cellWindow, out displayOffset);

            List<MapObjectInstance> retList = new List<MapObjectInstance>();
            HashSet<Entity> visibleEntities = this.scenario.VisibleEntities.GetContents(
                new RCNumRectangle(cellWindow.X - HALF_VECT.X,
                                   cellWindow.Y - HALF_VECT.Y,
                                   cellWindow.Width,
                                   cellWindow.Height));
            foreach (Entity entity in visibleEntities)
            {
                RCIntRectangle displayRect =
                    (RCIntRectangle)((entity.Position - cellWindow.Location + HALF_VECT) * PIXEL_PER_NAVCELL_VECT) - displayOffset;
                List<MapSpriteInstance> entitySprites = new List<MapSpriteInstance>();
                foreach (AnimationPlayer animation in entity.CurrentAnimations)
                {
                    foreach (int spriteIdx in animation.CurrentFrame)
                    {
                        entitySprites.Add(new MapSpriteInstance()
                        {
                            Index = entity.ElementType.SpritePalette.Index,
                            DisplayCoords = displayRect.Location + entity.ElementType.SpritePalette.GetOffset(spriteIdx),
                            Section = entity.ElementType.SpritePalette.GetSection(spriteIdx)
                        });
                    }
                }

                StartLocation entityAsStartLoc = entity as StartLocation;
                retList.Add(new MapObjectInstance()
                {
                    Owner = entityAsStartLoc != null
                          ? (PlayerEnum)entityAsStartLoc.PlayerIndex.Read()
                          : (entity.Owner != null ? (PlayerEnum)entity.Owner.PlayerIndex : PlayerEnum.Neutral),
                    SelectionIndicator = RCIntRectangle.Undefined,
                    SelectionIndicatorColorIdx = -1,
                    Values = null,
                    Sprites = entitySprites
                });
            }
            return retList;
        }

        /// <see cref="IMapObjectView.GetMapObjectDisplayCoords"/>
        public RCIntVector GetMapObjectDisplayCoords(RCIntRectangle displayedArea, RCIntVector position)
        {
            if (displayedArea == RCIntRectangle.Undefined) { throw new ArgumentNullException("displayedArea"); }
            if (position == RCIntVector.Undefined) { throw new ArgumentNullException("position"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(displayedArea)) { throw new ArgumentOutOfRangeException("displayedArea"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(position)) { throw new ArgumentOutOfRangeException("displayedArea"); }

            RCIntRectangle cellWindow;
            RCIntVector displayOffset;
            this.CalculateCellWindow(displayedArea, out cellWindow, out displayOffset);

            RCIntVector navCellCoords = new RCIntVector((displayedArea + position).X / BizLogicConstants.PIXEL_PER_NAVCELL,
                                                        (displayedArea + position).Y / BizLogicConstants.PIXEL_PER_NAVCELL);
            foreach (Entity entity in this.scenario.VisibleEntities.GetContents(navCellCoords))
            {
                return ((RCIntRectangle)((entity.Position - cellWindow.Location + HALF_VECT) * PIXEL_PER_NAVCELL_VECT) - displayOffset).Location;
            }
            return RCIntVector.Undefined;
        }

        /// <see cref="IMapObjectView.GetMapObjectID"/>
        public int GetMapObjectID(RCIntRectangle displayedArea, RCIntVector position)
        {
            if (displayedArea == RCIntRectangle.Undefined) { throw new ArgumentNullException("displayedArea"); }
            if (position == RCIntVector.Undefined) { throw new ArgumentNullException("position"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(displayedArea)) { throw new ArgumentOutOfRangeException("displayedArea"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(position)) { throw new ArgumentOutOfRangeException("displayedArea"); }

            RCIntRectangle cellWindow;
            RCIntVector displayOffset;
            this.CalculateCellWindow(displayedArea, out cellWindow, out displayOffset);

            RCIntVector navCellCoords = new RCIntVector((displayedArea + position).X / BizLogicConstants.PIXEL_PER_NAVCELL,
                                                        (displayedArea + position).Y / BizLogicConstants.PIXEL_PER_NAVCELL);
            foreach (Entity entity in this.scenario.VisibleEntities.GetContents(navCellCoords))
            {
                return entity.ID.Read();
            }
            return -1;
        }

        #endregion IMapObjectView methods

        /// <summary>
        /// Reference to the scenario.
        /// </summary>
        private Scenario scenario;

        /// <summary>
        /// Constants for coordinate transformations.
        /// </summary>
        private static readonly RCNumVector PIXEL_PER_NAVCELL_VECT = new RCNumVector(BizLogicConstants.PIXEL_PER_NAVCELL, BizLogicConstants.PIXEL_PER_NAVCELL);
        private static readonly RCNumVector HALF_VECT = new RCNumVector(1, 1) / 2;
    }
}
