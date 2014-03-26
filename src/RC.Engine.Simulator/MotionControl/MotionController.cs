﻿using RC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Engine.Maps.PublicInterfaces;

namespace RC.Engine.Simulator.MotionControl
{
    /// <summary>
    /// This class implements the motion controlling algorithm of the moving entities.
    /// </summary>
    class MotionController
    {
        /// <summary>
        /// Constructs a MotionController instance.
        /// </summary>
        /// <param name="actuator">The actuator of this entity that belongs to this controller.</param>
        public MotionController(IEntityActuator actuator)
        {
            if (actuator == null) { throw new ArgumentNullException("actuator"); }
            this.actuator = actuator;
        }

        /// <summary>
        /// Call this method to update the velocity of the controlled entity.
        /// </summary>
        public void UpdateVelocity()
        {
            List<RCNumVector> admissibleVelocities = this.actuator.GetAdmissibleVelocities();
            List<DynamicObstacleInfo> obstacles = this.actuator.GetDynamicObstacles();
            int bestVelocityIndex = -1;
            RCNumber leastPenalty = 0;

            for (int currIdx = 0; currIdx < admissibleVelocities.Count; currIdx++)
            {
                RCNumVector checkedVelocity = admissibleVelocities[currIdx] * 2 - this.actuator.CurrentVelocity; /// We calculate with RVOs instead of VOs.
                RCNumber timeToCollisionMin = -1;
                foreach (DynamicObstacleInfo obstacle in obstacles)
                {
                    RCNumber timeToCollision = MotionController.CalculateTimeToCollision(this.actuator.CurrentPosition, checkedVelocity, obstacle.Position, obstacle.Velocity);
                    if (timeToCollision >= 0 && (timeToCollisionMin < 0 || timeToCollision < timeToCollisionMin)) { timeToCollisionMin = timeToCollision; }
                }

                if (timeToCollisionMin != 0)
                {
                    RCNumber penalty = (timeToCollisionMin > 0 ? (RCNumber)1 / timeToCollisionMin : 0)
                                     + MapUtils.ComputeDistance(this.actuator.PreferredVelocity, admissibleVelocities[currIdx]);
                    if (bestVelocityIndex == -1 || penalty < leastPenalty)
                    {
                        leastPenalty = penalty;
                        bestVelocityIndex = currIdx;
                    }
                }
            }

            if (bestVelocityIndex != -1) { this.actuator.SetVelocity(bestVelocityIndex); }
        }

        /// <summary>
        /// Calculates the time to collision between two moving rectangular objects.
        /// </summary>
        /// <param name="rectangleA">The rectangular area of the first object.</param>
        /// <param name="velocityA">The velocity of the first object.</param>
        /// <param name="rectangleB">The rectangular area of the second object.</param>
        /// <param name="velocityB">The velocity of the second object.</param>
        /// <returns>The time to the collision between the two objects or a negative number if the two objects won't collide in the future.</returns>
        internal static RCNumber CalculateTimeToCollision(RCNumRectangle rectangleA, RCNumVector velocityA, RCNumRectangle rectangleB, RCNumVector velocityB)
        {
            /// Calculate the relative velocity of A with respect to B.
            RCNumVector relativeVelocityOfA = velocityA - velocityB;
            if (relativeVelocityOfA == new RCNumVector(0, 0)) { return -1; }

            /// Calculate the center of the first object and enlarge the area of the second object with the size of the first object.
            RCNumVector centerOfA = new RCNumVector((rectangleA.Left + rectangleA.Right) / 2,
                                                    (rectangleA.Top + rectangleA.Bottom) / 2);
            RCNumRectangle enlargedB = new RCNumRectangle(rectangleB.X - rectangleA.Width / 2,
                                                          rectangleB.Y - rectangleA.Height / 2,
                                                          rectangleA.Width + rectangleB.Width,
                                                          rectangleA.Height + rectangleB.Height);

            /// Calculate the collision time interval in the X dimension.
            bool isParallelX = relativeVelocityOfA.X == 0;
            RCNumber collisionTimeLeft = !isParallelX ? (enlargedB.Left - centerOfA.X) / relativeVelocityOfA.X : 0;
            RCNumber collisionTimeRight = !isParallelX ? (enlargedB.Right - centerOfA.X) / relativeVelocityOfA.X : 0;
            RCNumber collisionTimeBeginX = !isParallelX ? (collisionTimeLeft < collisionTimeRight ? collisionTimeLeft : collisionTimeRight) : 0;
            RCNumber collisionTimeEndX = !isParallelX ? (collisionTimeLeft > collisionTimeRight ? collisionTimeLeft : collisionTimeRight) : 0;

            /// Calculate the collision time interval in the Y dimension.
            bool isParallelY = relativeVelocityOfA.Y == 0;
            RCNumber collisionTimeTop = !isParallelY ? (enlargedB.Top - centerOfA.Y) / relativeVelocityOfA.Y : 0;
            RCNumber collisionTimeBottom = !isParallelY ? (enlargedB.Bottom - centerOfA.Y) / relativeVelocityOfA.Y : 0;
            RCNumber collisionTimeBeginY = !isParallelY ? (collisionTimeTop < collisionTimeBottom ? collisionTimeTop : collisionTimeBottom) : 0;
            RCNumber collisionTimeEndY = !isParallelY ? (collisionTimeTop > collisionTimeBottom ? collisionTimeTop : collisionTimeBottom) : 0;

            if (!isParallelX && !isParallelY)
            {
                /// Both X and Y dimensions have finite collision time interval.
                if (collisionTimeBeginX <= collisionTimeBeginY && collisionTimeBeginY < collisionTimeEndX && collisionTimeEndX <= collisionTimeEndY)
                {
                    return collisionTimeBeginY;
                }
                else if (collisionTimeBeginY <= collisionTimeBeginX && collisionTimeBeginX < collisionTimeEndX && collisionTimeEndX <= collisionTimeEndY)
                {
                    return collisionTimeBeginX;
                }
                else if (collisionTimeBeginY <= collisionTimeBeginX && collisionTimeBeginX < collisionTimeEndY && collisionTimeEndY <= collisionTimeEndX)
                {
                    return collisionTimeBeginX;
                }
                else if (collisionTimeBeginX <= collisionTimeBeginY && collisionTimeBeginY < collisionTimeEndY && collisionTimeEndY <= collisionTimeEndX)
                {
                    return collisionTimeBeginY;
                }
                else
                {
                    return -1;
                }
            }
            else if (!isParallelX && isParallelY)
            {
                /// Only X dimension has finite collision time interval.
                return centerOfA.Y > enlargedB.Top && centerOfA.Y < enlargedB.Bottom ? collisionTimeBeginX : -1;
            }
            else if (isParallelX && !isParallelY)
            {
                /// Only Y dimension has finite collision time interval.
                return centerOfA.X > enlargedB.Left && centerOfA.X < enlargedB.Right ? collisionTimeBeginY : -1;
            }
            else
            {
                /// None of the dimensions have finite collision time interval.
                return -1;
            }
        }

        /// <summary>
        /// Reference to the actuator of the entity that belongs to this controller.
        /// </summary>
        private IEntityActuator actuator;
    }
}