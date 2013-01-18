﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common;

namespace RC.App.PresLogic
{
    /// <summary>
    /// The information panel of the selected game on the Select Game page.
    /// </summary>
    public class RCGameInfoPanel : RCAppPanel
    {
        /// <summary>
        /// Creates an RCGameInfoPanel instance.
        /// </summary>
        /// <param name="backgroundRect">The area of the background of the panel in workspace coordinates.</param>
        /// <param name="buttonRect">The area of the button inside the panel relative to the background rectangle.</param>
        /// <param name="showMode">The mode how the panel will appear on a page when being shown.</param>
        /// <param name="hideMode">The mode how the panel will disappear from a page when being hidden.</param>
        /// <param name="appearDuration">
        /// The duration of showing this UIPanel in milliseconds. This parameter will be ignored in case
        /// of ShowMode.Appear.
        /// </param>
        /// <param name="disappearDuration">
        /// The duration of hiding this UIPanel in milliseconds. This parameter will be ignored in case
        /// of HideMode.Disappear.
        /// </param>
        /// <param name="backgroundSprite">
        /// Name of the sprite resource that will be the background of this panel or null if there is no background.
        /// </param>
        /// <param name="buttonText">The text that should be displayed on the navigation button.</param>
        public RCGameInfoPanel(RCIntRectangle backgroundRect, RCIntRectangle buttonRect,
                               ShowMode showMode, HideMode hideMode,
                               int appearDuration, int disappearDuration,
                               string backgroundSprite)
            : base(backgroundRect, buttonRect, showMode, hideMode, appearDuration, disappearDuration, backgroundSprite)
        {
        }
    }
}
