using System;
using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    public static class RopeUtilities
    {
        const int MaxIterations = 30;

        public static float FindCatenaryConstant(float deltaX, float deltaY, float lengthSqr, bool debugLog = false, float minimumA = 0.002f)
        {
            float startTime = Time.realtimeSinceStartup;

            float referenceConstant = Mathf.Sqrt(lengthSqr - (deltaY * deltaY)) / 2.0f;
            const float boundary = 0.03f;
            float aValue = 1.0f;
            bool overshoot = false;

            float calcValue = (aValue < float.Epsilon ? minimumA : aValue) * (float)Math.Sinh(deltaX / aValue);
            bool lastDirectionGreater = calcValue > referenceConstant;

            float iterationVal = lastDirectionGreater ? 1.0f : -1.0f;
            int iteration = 0;
            while (iteration++ < MaxIterations)
            {
                float safeAValue = (aValue < float.Epsilon ? minimumA : aValue);
                calcValue = safeAValue * (float)Math.Sinh(deltaX / (2 * safeAValue));
                if (Mathf.Abs(calcValue - referenceConstant) < boundary)
                    break;

                if (calcValue > referenceConstant)
                {
                    if (!lastDirectionGreater)
                        overshoot = true;
                    lastDirectionGreater = true;
                }
                else
                {
                    if (lastDirectionGreater)
                        overshoot = true;
                    lastDirectionGreater = false;
                }

                if (overshoot)
                {
                    iterationVal /= 2.0f;
                    iterationVal = -iterationVal;
                    overshoot = false;
                }

                aValue += iterationVal;
            }

            if (debugLog)
            {
                if (iteration >= MaxIterations)
                    Debug.LogWarning(
                        $"Did not found exact aValue, difference left in optimization {Mathf.Abs(calcValue - referenceConstant)}");

                Debug.Log($"Found aValue {aValue} in {Time.realtimeSinceStartup - startTime} seconds");
            }

            return aValue;
        }

        public static float SimulatePeakXOffset(float aValue, float deltaX, float srcY, float targetY, bool debugLog = false)
        {
            float startTime = Time.realtimeSinceStartup;

            deltaX = Mathf.Abs(deltaX);

            float deltaY = srcY - targetY;
            float referenceConstant = deltaY / aValue;
            float boundary = 0.02f / aValue;
            bool overshoot = false;

            float xValue = 0.0f;
            float calcValue = Mathf.Abs((float)Math.Cosh(xValue / aValue)) -
                              Mathf.Abs((float)Math.Cosh((xValue + deltaX) / aValue));
            bool lastDirectionGreater = calcValue > referenceConstant;

            float iterationVal = (lastDirectionGreater ? 1.0f : -1.0f) * deltaX / 4.0f;
            int iteration = 0;
            while (iteration++ < MaxIterations)
            {
                calcValue = Mathf.Abs((float)Math.Cosh(xValue / aValue)) -
                            Mathf.Abs((float)Math.Cosh((xValue + deltaX) / aValue));
                if (Mathf.Abs(calcValue - referenceConstant) < boundary)
                    break;

                if (calcValue > referenceConstant)
                {
                    if (!lastDirectionGreater)
                        overshoot = true;
                    lastDirectionGreater = true;
                }
                else
                {
                    if (lastDirectionGreater)
                        overshoot = true;
                    lastDirectionGreater = false;
                }

                if (overshoot)
                {
                    iterationVal /= 2.0f;
                    iterationVal = -iterationVal;
                    overshoot = false;
                }

                xValue += iterationVal;
            }

            if (debugLog)
            {
                if (iteration >= MaxIterations)
                    Debug.LogWarning(
                        $"Did not found exact aValue, difference left in optimization {Mathf.Abs(calcValue - referenceConstant)}");

                Debug.Log($"Found peak Offset {xValue} in {Time.realtimeSinceStartup - startTime} seconds");
            }

            return -xValue / deltaX;
        }


        public static float CalculateHeight(float aValue, float startY, float horizontalDistance, float horizontalPercentage, float peakOffsetPerc)
        {
            float xValAbsMinimum = peakOffsetPerc * horizontalDistance;
            float heightOfSourcePoint = aValue * (float)Math.Cosh(-xValAbsMinimum / aValue);

            // Center xval around zero
            float xVal = horizontalDistance * (horizontalPercentage - peakOffsetPerc);
            float heightOnCurve = aValue * (float)Math.Cosh(xVal / aValue);

            float heightOfPoint = (startY - heightOfSourcePoint) + heightOnCurve;
            return heightOfPoint;
        }

        public static float CalculateDerivative(float aValue, float horizontalDistance, float horizontalPercentage, float peakOffsetPerc)
        {
            // Center xval around zero
            float xVal = horizontalDistance * (horizontalPercentage - peakOffsetPerc);
            float derivative = (float)Math.Sinh(xVal / aValue);

            return derivative;
        }
        
        public static Vector3 GetPointOnRope(float aValue, Vector3 start, Vector3 delta, float xPerc, float peakOffset, Transform space = null)
        {
            var matrix = space == null ? Matrix4x4.identity : space.worldToLocalMatrix;
            return GetPointOnRope(aValue, start, delta, xPerc, peakOffset, matrix);
        }
        
        public static Vector3 GetPointOnRope(float aValue, Vector3 start, Vector3 delta, float xPerc, float peakOffset, Matrix4x4 space)
        {
            float pointHeight = CalculateHeight(aValue, start.y, delta.x, xPerc, peakOffset);

            var middlePoint = xPerc * delta;
            middlePoint = space * middlePoint;
            middlePoint += start;
            middlePoint.y = pointHeight;
            return middlePoint;
        }
    }
}