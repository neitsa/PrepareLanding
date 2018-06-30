using System;
using Verse;

namespace PrepareLanding.Core.Extensions
{
    public static class TemperatureDisplayModeExtension
    {

        public static string TemperatureDisplayModeUnit(this TemperatureDisplayMode temperatureDisplayMode)
        {
            switch (temperatureDisplayMode)
            {
                case TemperatureDisplayMode.Celsius:
                    return "C";

                case TemperatureDisplayMode.Fahrenheit:
                    return "F";

                case TemperatureDisplayMode.Kelvin:
                    return "K";
            }

            return "";
        }

        public static float TempDelta(this TemperatureDisplayMode fromUnit, float fromValue, TemperatureDisplayMode toUnit)
        {
            float retValue = fromValue;
            switch (fromUnit)
            {
                case TemperatureDisplayMode.Kelvin:
                case TemperatureDisplayMode.Celsius:
                    if (toUnit == TemperatureDisplayMode.Fahrenheit)
                        retValue = fromValue * 1.8f; // 1.8f = 9 / 5
                    break;

                case TemperatureDisplayMode.Fahrenheit:
                    if (toUnit == TemperatureDisplayMode.Celsius)
                        retValue = fromValue / 1.8f;
                    break;

                default:
                    Log.Error("[PrepareLanding] Unknown Temperature Unit");
                    retValue = float.NaN;
                    break;
            }

            return retValue;

        }
        
    }
}