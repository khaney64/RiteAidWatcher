using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidChecker
{
    public static class Extensions
    {
        public static string Format(this ConditionType condition)
        {
            switch (condition)
            {
                case ConditionType.Diabetes:
                case ConditionType.Obesity:
                    return condition.ToString();
                case ConditionType.WeakendImmuneSystem:
                    return "Weakened Immune System";
                default:
                    throw new Exception($"Unexpected Condition {condition}");
            }
        }

        public static string Format(this OccupationType occupation)
        {
            switch (occupation)
            {
                case OccupationType.NoneOfTheAbove:
                    return "None of the Above";
                default:
                    throw new Exception($"Unexpected Condition {occupation}");
            }
        }
    }
}
