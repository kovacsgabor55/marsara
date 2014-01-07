﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.App.BizLogic.PublicInterfaces;
using RC.UI;
using RC.Common;
using RC.Common.Diagnostics;

namespace RC.App.PresLogic.Controls
{
    /// <summary>
    /// Adds the following new functionalities to the map display control:
    ///     - displays the map objects
    ///     - indicates the selected map objects
    ///     - displays the selection box if a selection is currently in progress
    /// </summary>
    public class RCMapObjectDisplay : RCMapDisplayExtension
    {
        /// <summary>
        /// Constructs an RCMapObjectDisplay extension for the given map display control.
        /// </summary>
        /// <param name="extendedControl">The map display control to extend.</param>
        /// <param name="mapObjectView">Reference to a map object view.</param>
        /// <param name="metadataView">Reference to the metadata view.</param>
        public RCMapObjectDisplay(RCMapDisplay extendedControl, IMapObjectView mapObjectView, IMetadataView metadataView)
            : base(extendedControl, mapObjectView)
        {
            if (mapObjectView == null) { throw new ArgumentNullException("mapObjectView"); }
            if (metadataView == null) { throw new ArgumentNullException("metadataView"); }

            this.mapObjectView = mapObjectView;

            this.mapObjectSprites = new List<SpriteGroup>();
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player0));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player1));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player2));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player3));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player4));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player5));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player6));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Player7));
            this.mapObjectSprites.Add(new MapObjectSpriteGroup(metadataView, PlayerEnum.Neutral));

            this.selectionBoxBrush = UIRoot.Instance.GraphicsPlatform.SpriteManager.CreateSprite(UIColor.LightGreen, new RCIntVector(1, 1), UIWorkspace.Instance.PixelScaling);
            this.selectionBoxBrush.Upload();
            this.CurrentMouseStatus = MouseStatus.None;
            this.selectionBoxStartPosition = RCIntVector.Undefined;
            this.selectionBoxCurrPosition = RCIntVector.Undefined;
            this.isMouseHandlingActive = false;
        }

        /// <summary>
        /// Gets the sprite group of the map object types for the given player.
        /// </summary>
        /// <param name="player">The player that owns the sprite group..</param>
        /// <returns>The sprite group of the map object types for the given player.</returns>
        public SpriteGroup GetMapObjectSprites(PlayerEnum player) { return this.mapObjectSprites[player != PlayerEnum.Neutral ? (int)player : this.mapObjectSprites.Count - 1]; }

        /// <summary>
        /// This event is raised when a mouse handling activity has been started on this display.
        /// </summary>
        public event EventHandler MouseActivityStarted;

        /// <summary>
        /// This event is raised when a mouse handling activity has been finished on this display.
        /// </summary>
        public event EventHandler MouseActivityFinished;

        /// <summary>
        /// Activates the mouse handling of this display. If mouse handling is currently active then this method
        /// has no effect.
        /// </summary>
        public void ActivateMouseHandling()
        {
            if (!this.isMouseHandlingActive)
            {
                this.MouseSensor.ButtonDown += this.OnMouseDown;
                this.MouseSensor.Move += this.OnMouseMove;
                this.MouseSensor.ButtonUp += this.OnMouseUp;
                this.MouseSensor.DoubleClick += this.OnMouseDoubleClick;
                this.isMouseHandlingActive = true;
            }
        }

        /// <summary>
        /// Deactivates the mouse handling of this display. If mouse handling is currently inactive then this method
        /// has no effect.
        /// </summary>
        public void DeactivateMouseHandling()
        {
            if (this.isMouseHandlingActive)
            {
                this.MouseSensor.ButtonDown -= this.OnMouseDown;
                this.MouseSensor.Move -= this.OnMouseMove;
                this.MouseSensor.ButtonUp -= this.OnMouseUp;
                this.MouseSensor.DoubleClick -= this.OnMouseDoubleClick;
                this.isMouseHandlingActive = false;
            }
        }

        #region Overrides

        /// <see cref="UISensitiveObject.ResetState"/>
        public override void ResetState()
        {
            this.CurrentMouseStatus = MouseStatus.None;
            this.selectionBoxStartPosition = RCIntVector.Undefined;
            this.selectionBoxCurrPosition = RCIntVector.Undefined;
        }

        /// <see cref="RCMapDisplayExtension.StartExtensionProc_i"/>
        protected override void StartExtensionProc_i()
        {
            foreach (SpriteGroup spriteGroup in this.mapObjectSprites)
            {
                spriteGroup.Load();
            }
        }

        /// <see cref="RCMapDisplayExtension.StopExtensionProc_i"/>
        protected override void StopExtensionProc_i()
        {
            foreach (SpriteGroup spriteGroup in this.mapObjectSprites)
            {
                spriteGroup.Unload();
            }
        }

        /// <see cref="RCMapDisplayExtension.RenderExtension_i"/>
        protected override void RenderExtension_i(IUIRenderContext renderContext)
        {
            /// Retrieve the list of the visible map objects.
            List<MapObjectInstance> mapObjects = this.mapObjectView.GetVisibleMapObjects(this.DisplayedArea);

            /// Render the object sprites.
            foreach (MapObjectInstance obj in mapObjects)
            {
                foreach (MapSpriteInstance spriteToDisplay in obj.Sprites)
                {
                    if (spriteToDisplay.Index != -1)
                    {
                        SpriteGroup spriteGroup = this.GetMapObjectSprites(obj.Owner);
                        renderContext.RenderSprite(spriteGroup[spriteToDisplay.Index],
                                                   spriteToDisplay.DisplayCoords,
                                                   spriteToDisplay.Section);
                    }
                }
            }

            /// Render the selection box if necessary.
            if (this.CurrentMouseStatus == MouseStatus.Selecting)
            {
                renderContext.RenderRectangle(this.selectionBoxBrush, this.CalculateSelectionBox());
            }
        }

        #endregion Overrides

        #region Overridables

        /// <summary>
        /// Handles the right click mouse event.
        /// </summary>
        /// <param name="position">The position of the right click.</param>
        protected virtual void OnRightClick(RCIntVector position) { }

        /// <summary>
        /// Handles the left click mouse event.
        /// </summary>
        /// <param name="position">The position of the left click.</param>
        protected virtual void OnLeftClick(RCIntVector position) { }

        /// <summary>
        /// Handles the double click mouse event.
        /// </summary>
        /// <param name="position">The position of the double click.</param>
        protected virtual void OnDoubleClick(RCIntVector position) { }

        /// <summary>
        /// Handles the selection box mouse event.
        /// </summary>
        /// <param name="selectionBox">The position of the selection box.</param>
        protected virtual void OnSelectionBox(RCIntRectangle selectionBox) { }

        #endregion Overridables

        #region Mouse event handling

        /// <summary>
        /// Enumerates the possible mouse statuses of the display.
        /// </summary>
        private enum MouseStatus
        {
            None = 0,
            LeftDown = 1,
            RightDown = 2,
            Selecting = 3,
            DoubleClicked = 4
        }

        /// <summary>
        /// Called when a mouse button has been pushed over the display.
        /// </summary>
        private void OnMouseDown(UISensitiveObject sender, UIMouseEventArgs evtArgs)
        {
            if (this.CurrentMouseStatus == MouseStatus.None)
            {
                if (evtArgs.Button == UIMouseButton.Right)
                {
                    /// Handle the mouse event.
                    this.OnRightClick(evtArgs.Position);
                    this.CurrentMouseStatus = MouseStatus.RightDown;
                }
                else if (evtArgs.Button == UIMouseButton.Left)
                {
                    this.CurrentMouseStatus = MouseStatus.LeftDown;

                    /// Start drawing the selection box.
                    this.selectionBoxStartPosition = evtArgs.Position;
                }
            }
        }

        /// <summary>
        /// Called when there was a double click happened over the display.
        /// </summary>
        private void OnMouseDoubleClick(UISensitiveObject sender, UIMouseEventArgs evtArgs)
        {
            if (evtArgs.Button == UIMouseButton.Left)
            {
                if (this.CurrentMouseStatus == MouseStatus.LeftDown)
                {
                    this.CurrentMouseStatus = MouseStatus.DoubleClicked;
                }
            }
        }

        /// <summary>
        /// Called when the mouse pointer has been moved over the display.
        /// </summary>
        private void OnMouseMove(UISensitiveObject sender, UIMouseEventArgs evtArgs)
        {
            if (this.CurrentMouseStatus == MouseStatus.LeftDown)
            {
                this.CurrentMouseStatus = MouseStatus.Selecting;

                /// Actualize the selection box
                this.selectionBoxCurrPosition = evtArgs.Position;
            }
            else if (this.CurrentMouseStatus == MouseStatus.Selecting)
            {
                /// Actualize the selection box
                this.selectionBoxCurrPosition = evtArgs.Position;
            }
            else if (this.CurrentMouseStatus == MouseStatus.DoubleClicked)
            {
                this.CurrentMouseStatus = MouseStatus.Selecting;

                /// Actualize the selection box
                this.selectionBoxCurrPosition = evtArgs.Position;
            }
        }

        /// <summary>
        /// Called when a mouse button has been released over the display.
        /// </summary>
        private void OnMouseUp(UISensitiveObject sender, UIMouseEventArgs evtArgs)
        {
            if (this.CurrentMouseStatus == MouseStatus.LeftDown && evtArgs.Button == UIMouseButton.Left)
            {
                this.CurrentMouseStatus = MouseStatus.None;

                /// Handle the mouse event.
                this.OnLeftClick(evtArgs.Position);

                /// Selection box off.
                this.selectionBoxStartPosition = RCIntVector.Undefined;
                this.selectionBoxCurrPosition = RCIntVector.Undefined;
            }
            else if (this.CurrentMouseStatus == MouseStatus.Selecting && evtArgs.Button == UIMouseButton.Left)
            {
                this.CurrentMouseStatus = MouseStatus.None;

                /// Handle the mouse event.
                this.OnSelectionBox(this.CalculateSelectionBox());

                /// Selection box off.
                this.selectionBoxStartPosition = RCIntVector.Undefined;
                this.selectionBoxCurrPosition = RCIntVector.Undefined;
            }
            else if (this.CurrentMouseStatus == MouseStatus.RightDown && evtArgs.Button == UIMouseButton.Right)
            {
                this.CurrentMouseStatus = MouseStatus.None;
            }
            else if (this.CurrentMouseStatus == MouseStatus.DoubleClicked && evtArgs.Button == UIMouseButton.Left)
            {
                /// Handle the mouse event.
                this.OnDoubleClick(evtArgs.Position);

                this.CurrentMouseStatus = MouseStatus.None;
            }
        }

        #endregion Mouse event handling

        #region Internal methods

        /// <summary>
        /// Calculates the current selection box in the coordinate-system of this display.
        /// </summary>
        /// <returns>
        /// The calculated selection box or RCIntRectangle.Undefined if there is no active selection box.
        /// </returns>
        private RCIntRectangle CalculateSelectionBox()
        {
            if (this.selectionBoxStartPosition != RCIntVector.Undefined && this.selectionBoxCurrPosition != RCIntVector.Undefined)
            {
                RCIntVector topLeftCorner = new RCIntVector(Math.Min(this.selectionBoxStartPosition.X, this.selectionBoxCurrPosition.X),
                                                            Math.Min(this.selectionBoxStartPosition.Y, this.selectionBoxCurrPosition.Y));
                RCIntVector bottomRightCorner = new RCIntVector(Math.Max(this.selectionBoxStartPosition.X, this.selectionBoxCurrPosition.X),
                                                                Math.Max(this.selectionBoxStartPosition.Y, this.selectionBoxCurrPosition.Y));
                return new RCIntRectangle(topLeftCorner.X,
                                          topLeftCorner.Y,
                                          bottomRightCorner.X - topLeftCorner.X + 1,
                                          bottomRightCorner.Y - topLeftCorner.Y + 1);
            }
            else
            {
                return RCIntRectangle.Undefined;
            }
        }

        /// <summary>
        /// Gets or sets the mouse status of this display.
        /// </summary>
        private MouseStatus CurrentMouseStatus
        {
            get { return this.currentMouseStatus; }
            set
            {
                MouseStatus oldValue = this.currentMouseStatus;
                this.currentMouseStatus = value;
                if (oldValue == MouseStatus.None && this.currentMouseStatus != MouseStatus.None)
                {
                    if (this.MouseActivityStarted != null) { this.MouseActivityStarted(this, new EventArgs()); }
                }
                else if (oldValue != MouseStatus.None && this.currentMouseStatus == MouseStatus.None)
                {
                    if (this.MouseActivityFinished != null) { this.MouseActivityFinished(this, new EventArgs()); }
                }
            }
        }

        #endregion Internal methods

        /// <summary>
        /// The brush for drawing the selection box.
        /// </summary>
        private UISprite selectionBoxBrush;

        /// <summary>
        /// This sprite-group contains the sprites of the map object types.
        /// The Nth sprite group in this list contains the variant of the sprites for PlayerN.
        /// The last sprite group in this list contains the neutral variants of the sprites.
        /// </summary>
        private List<SpriteGroup> mapObjectSprites;

        /// <summary>
        /// The current mouse status of this display.
        /// </summary>
        private MouseStatus currentMouseStatus;

        /// <summary>
        /// The starting position of the currently drawn selection box or RCIntVector.Undefined if there is no
        /// selection box currently being drawn.
        /// </summary>
        private RCIntVector selectionBoxStartPosition;

        /// <summary>
        /// The current position of the currently drawn selection box or RCIntVector.Undefined if there is no
        /// selection box currently being drawn.
        /// </summary>
        private RCIntVector selectionBoxCurrPosition;

        /// <summary>
        /// This flag indicates whether the mouse handing of this display is active or not.
        /// </summary>
        private bool isMouseHandlingActive;

        /// <summary>
        /// Reference to the map object view.
        /// </summary>
        private IMapObjectView mapObjectView;

        /// <summary>
        /// The default color of the transparent parts of the map object sprites.
        /// </summary>
        public static readonly UIColor DEFAULT_MAPOBJECT_TRANSPARENT_COLOR = new UIColor(255, 0, 255);

        /// <summary>
        /// The default owner mask color of the map object sprites.
        /// </summary>
        public static readonly UIColor DEFAULT_MAPOBJECT_OWNERMASK_COLOR = new UIColor(0, 255, 255);
    }
}
