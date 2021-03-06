﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Engine.Simulator.PublicInterfaces;

namespace RC.Engine.Simulator.PublicInterfaces
{
    /// <summary>
    /// Represents a constant value that is readonly and cannot be changed.
    /// </summary>
    /// <typeparam name="T">The data type of the constant.</typeparam>
    public class ConstValue<T> : IValueRead<T>
    {
        /// <summary>
        /// Constructs a ConstValue instance.
        /// </summary>
        /// <param name="value">The value of the constant.</param>
        public ConstValue(T value)
        {
            this.value = value;
        }

        /// <see cref="IValueRead<T>.Read"/>
        public T Read()
        {
            return this.value;
        }

        /// <summary>
        /// Gets the string representation of this constant value.
        /// </summary>
        /// <returns>The string representation of this constant value.</returns>
        public override string ToString()
        {
            return this.value.ToString();
        }

        /// <summary>
        /// The value of the constant.
        /// </summary>
        private T value;
    }
}
