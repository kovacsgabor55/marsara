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
        public RCMapObjectDisplay(RCMapDisplay extendedControl, IMapObjectView mapObjectView)
            : base(extendedControl, mapObjectView)
        {
            if (mapObjectView == null) { throw new ArgumentNullException("mapObjectView"); }

            this.mapObjectView = mapObjectView;

            this.mapObjectSprites = new MapObjectSpriteGroup();
            this.brushPalette = new BrushPaletteSpriteGroup();
            this.selBoxBrush = null;
            this.CurrentMouseStatus = MouseStatus.None;
            this.selectionBoxStartPosition = RCIntVector.Undefined;
            this.selectionBoxCurrPosition = RCIntVector.Undefined;
            this.isMouseHandlingActive = false;
        }

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
            this.selBoxBrush = UIRoot.Instance.GraphicsPlatform.SpriteManager.CreateSprite(UIColor.LightGreen, new RCIntVector(1, 1), UIWorkspace.Instance.PixelScaling);
            this.selBoxBrush.Upload();

            this.mapObjectSprites.Load();
            this.brushPalette.Load();
        }

        /// <see cref="RCMapDisplayExtension.StopExtensionProc_i"/>
        protected override void StopExtensionProc_i()
        {
            this.mapObjectSprites.Unload();
            this.brushPalette.Unload();

            UIRoot.Instance.GraphicsPlatform.SpriteManager.DestroySprite(this.selBoxBrush);
            this.selBoxBrush = null;
        }

        /// <see cref="RCMapDisplayExtension.RenderExtension_i"/>
        protected override void RenderExtension_i(IUIRenderContext renderContext)
        {
            /// Retrieve the list of the visible map objects.
            List<MapObjectInstance> mapObjects = this.mapObjectView.GetVisibleMapObjects(this.DisplayedArea);

            /// Render the selection indicators and the object values.
            foreach (MapObjectInstance obj in mapObjects)
            {
                if (obj.SelectionIndicatorColorIdx != -1 && obj.SelectionIndicator != RCIntRectangle.Undefined)
                {
                    renderContext.RenderRectangle(this.brushPalette[obj.SelectionIndicatorColorIdx],
                                                  obj.SelectionIndicator);
                    int row = obj.SelectionIndicator.Bottom;
                    int col = obj.SelectionIndicator.Left;
                    foreach (Tuple<int, RCNumber> val in obj.Values)
                    {
                        int width = ((RCNumber)obj.SelectionIndicator.Width * val.Item2).Round();
                        if (width > 0)
                        {
                            renderContext.RenderRectangle(this.brushPalette[val.Item1],
                                                          new RCIntRectangle(col, row, width, 1));
                        }
                        row++;
                    }
                }
            }

            /// Render the object sprites.
            foreach (MapObjectInstance obj in mapObjects)
            {
                renderContext.RenderSprite(this.mapObjectSprites[obj.Sprite.Index],
                                           obj.Sprite.DisplayCoords,
                                           obj.Sprite.Section);
            }

            /// Render the selection box if necessary.
            if (this.CurrentMouseStatus == MouseStatus.Selecting)
            {
                renderContext.RenderRectangle(this.selBoxBrush, this.CalculateSelectionBox());
            }
        }

        #endregion Overrides

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
                    /// TODO: send command to the selected units
                    TraceManager.WriteAllTrace("RIGHT_CLICK", PresLogicTraceFilters.INFO);

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

                /// TODO: handle single unit selection
                TraceManager.WriteAllTrace("LEFT_CLICK", PresLogicTraceFilters.INFO);

                /// Selection box off.
                this.selectionBoxStartPosition = RCIntVector.Undefined;
                this.selectionBoxCurrPosition = RCIntVector.Undefined;
            }
            else if (this.CurrentMouseStatus == MouseStatus.Selecting && evtArgs.Button == UIMouseButton.Left)
            {
                this.CurrentMouseStatus = MouseStatus.None;

                /// TODO: handle selection box
                TraceManager.WriteAllTrace(string.Format("SELECTION {0}", this.CalculateSelectionBox()), PresLogicTraceFilters.INFO);

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
                /// TODO: handle double click
                TraceManager.WriteAllTrace("DOUBLE_CLICK", PresLogicTraceFilters.INFO);

                this.CurrentMouseStatus = MouseStatus.None;
            }
        }

        #endregion Mouse event handling

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

        /// <summary>
        /// Brush for drawing the selection box.
        /// </summary>
        private UISprite selBoxBrush;

        /// <summary>
        /// The brush palette for drawing the selection indicators and value bars of map objects.
        /// </summary>
        private SpriteGroup brushPalette;

        /// <summary>
        /// This sprite-group contains the sprites of the map object.
        /// </summary>
        private SpriteGroup mapObjectSprites;

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
    }
}