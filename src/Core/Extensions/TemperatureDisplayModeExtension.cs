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
        
    }
}