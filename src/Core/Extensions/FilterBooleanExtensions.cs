
namespace PrepareLanding.Core.Extensions
{
    public static class FilterBooleanExtensions
    {
        public static string ToStringHuman(this FilterBoolean filterBool)
        {
            switch (filterBool)
            {
                case FilterBoolean.AndFiltering:
                    return "AND";

                case FilterBoolean.OrFiltering:
                    return "OR";

                default:
                    return "UNK";
            }
        }

        public static FilterBoolean Next(this FilterBoolean filterBoolean)
        {
            return (FilterBoolean)(((int)filterBoolean + 1) % (int)FilterBoolean.Undefined);
        }
    }
}