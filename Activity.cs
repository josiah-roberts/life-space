using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

namespace LifeSpace
{
    record UrgencyDescriptor(Delta<DateTime> Interval, Delta<Urgency> Urgency)
    {
        /// <summary>
        /// Returns the current urgency, assuming linear interpolation of urgency between start and end
        /// This is probably wrong - we should have some more realistic curve
        /// </summary>
        public Urgency CurrentUrgency
        {
            get
            {
                var (now, startTime, endTime) = (DateTime.Now.Ticks, Interval.Start.Ticks, Interval.End.Ticks);
                var effectiveNow = Clamp(now, startTime, endTime);
                var ratio = (double)(effectiveNow - startTime) / (endTime - startTime);
                var currentUrgency = (int)Math.Round(Urgency.Start + (Urgency.End - Urgency.Start) * ratio);
                return new(currentUrgency);
            }
        }
    }

    record Activity(string Name, UrgencyDescriptor Urgency, Importance Importance, Effort Effort, Pleasure Pleasure)
    {
        public double PrioritySummary => Math.Round(Math.Log10((Urgency.CurrentUrgency * Importance) / Math.Max(Margin, 1)), 2);
        public double ValueForEffort => Math.Round((double)Importance / Effort, 2);
        public double PleasureForEffort => Math.Round((double)Pleasure / Effort, 2);
        public double Margin
        {
            get
            {
                var isolatedEffortAvailable = EffortDaysBetween(DateTime.Now, Urgency.Interval.End).Sum();
                var ratio = isolatedEffortAvailable / Effort;
                return Math.Round(ratio, 2);
            }

        }

        private IEnumerable<double> EffortDaysBetween(DateTime start, DateTime end)
        {
            DateTime t = start;
            while (t < end)
            {
                if (t.DayOfWeek == DayOfWeek.Saturday || t.DayOfWeek == DayOfWeek.Sunday)
                    yield return 2;
                else
                    yield return 0.5;

                t = t.AddDays(1);
            }
        }
    }
}