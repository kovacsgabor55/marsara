﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using RC.Common;
using System.Windows.Forms;
using RC.Common.Diagnostics;

namespace RC.UI.XnaPlugin
{
    /// <summary>
    /// Source of mouse events.
    /// </summary>
    class XnaMouseEventSource : UIEventSourceBase
    {
        /// <summary>
        /// Constructs an XnaMouseEventSource
        /// </summary>
        public XnaMouseEventSource(XnaGraphicsPlatform platform)
            : base("RC.UI.XnaPlugin.XnaMouseEventSource")
        {
            this.isFormActive = true;
            this.platform = platform;
            this.prevPressedButtons = new HashSet<UIMouseButton>();
            this.prevScrollWheelPos = 0;
        }

        /// <summary>
        /// Sends a mouse event if necessary.
        /// </summary>
        public void Update()
        {
            if (this.IsActive && this.isFormActive)
            {
                MouseState mouseState = Mouse.GetState();
                RCIntVector delta = new RCIntVector(mouseState.X, mouseState.Y) - this.systemMousePos;

                HashSet<UIMouseButton> pressedButtons = new HashSet<UIMouseButton>();
                if (mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) { pressedButtons.Add(UIMouseButton.Left); }
                if (mouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) { pressedButtons.Add(UIMouseButton.Middle); }
                if (mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) { pressedButtons.Add(UIMouseButton.Right); }
                if (mouseState.XButton1 == Microsoft.Xna.Framework.Input.ButtonState.Pressed) { pressedButtons.Add(UIMouseButton.X1); }
                if (mouseState.XButton2 == Microsoft.Xna.Framework.Input.ButtonState.Pressed) { pressedButtons.Add(UIMouseButton.X2); }

                if (delta.X != 0 || delta.Y != 0 ||
                    !this.prevPressedButtons.SetEquals(pressedButtons) ||
                    this.prevScrollWheelPos != mouseState.ScrollWheelValue)
                {
                    /// Set back the system mouse and enqueue a mouse event.
                    Mouse.SetPosition(this.systemMousePos.X, this.systemMousePos.Y);
                    UIMouseSystemEventArgs evtArgs = new UIMouseSystemEventArgs(delta, pressedButtons, mouseState.ScrollWheelValue);
                    UIRoot.Instance.SystemEventQueue.EnqueueEvent<UIMouseSystemEventArgs>(evtArgs);
                    this.prevPressedButtons = pressedButtons;
                    this.prevScrollWheelPos = mouseState.ScrollWheelValue;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialPos"></param>
        public void Reset(RCIntVector initialPos)
        {
            this.systemMousePos = initialPos;
            Mouse.SetPosition(this.systemMousePos.X, this.systemMousePos.Y);
        }

        /// <see cref="UIEventSourceBase.Activate_i"/>
        protected override void Activate_i()
        {
            platform.Window.GotFocus += this.OnFormActivated;
            platform.Window.LostFocus += this.OnFormDeactivated;
        }

        /// <see cref="UIEventSourceBase.Deactivate_i"/>
        protected override void Deactivate_i()
        {
            platform.Window.GotFocus -= this.OnFormActivated;
            platform.Window.LostFocus -= this.OnFormDeactivated;
        }

        /// <summary>
        /// Called when the application form has been activated.
        /// </summary>
        private void OnFormActivated(object sender, EventArgs evt)
        {
            this.isFormActive = true;
            Mouse.SetPosition(this.systemMousePos.X, this.systemMousePos.Y);
        }

        /// <summary>
        /// Called when the application form has been deactivated.
        /// </summary>
        private void OnFormDeactivated(object sender, EventArgs evt)
        {
            this.isFormActive = false;
        }

        /// <summary>
        /// This flag indicates whether the application form is active or not.
        /// </summary>
        private bool isFormActive;

        /// <summary>
        /// Position of the system mouse cursor in screen coordinates.
        /// </summary>
        private RCIntVector systemMousePos;

        /// <summary>
        /// Reference to the platform.
        /// </summary>
        private XnaGraphicsPlatform platform;

        /// <summary>
        /// Set of the pressed buttons in the previous update.
        /// </summary>
        private HashSet<UIMouseButton> prevPressedButtons;

        /// <summary>
        /// The position of the mouse scroll wheel in the previous update.
        /// </summary>
        private int prevScrollWheelPos;
    }
}
