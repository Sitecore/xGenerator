using System;
using System.Collections.Generic;
using System.Linq;

namespace Colossus.Statistics
{
    public class TrendBuilder
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double Shift { get; set; }
        public bool Cyclic { get; set; }

        public TrendBuilder(double? min = null, double? max = null, double? offset = null, double? level = null, bool cyclic = false)
        {
            Min = min;
            Max = max;
            Cyclic = cyclic;
            _offset = offset ?? min ?? 0;
            _level = level ?? 1;
        }

        private List<LinearGenerator> _lines = new List<LinearGenerator>();
        private List<KeyValuePair<IRandomGenerator, double>> _peaks = new List<KeyValuePair<IRandomGenerator, double>>();

        private double _offset;
        private double _level;

        public TrendBuilder SetLevel(double level)
        {
            _level = level;
            return this;
        }

        public TrendBuilder MoveAbsolute(double offset, double? level = null, bool pct = false)
        {
            _offset = MapPercentage(offset, pct, false);
            _level = level ?? _level;
            return this;
        }

        public TrendBuilder MoveAbsolutePercentage(double offset, double? level = null)
        {
            return MoveAbsolute(offset, level, true);
        }

        public TrendBuilder MoveRelative(double offset, double? level = null, bool pct = false)
        {
            return MoveAbsolute(_offset + MapPercentage(offset, pct, true), level, false);
        }

        public TrendBuilder MoveRelativePercentage(double offset, double? level = null)
        {
            return MoveRelative(offset, level, true);
        }

        public TrendBuilder LineAbsolute(double offset, double level, bool pct = false)
        {
            offset = MapPercentage(offset, pct, false);
            if (offset <= _offset) throw new ArgumentException("Offset must be greater than the current offset", "offset");
            _lines.Add(new LinearGenerator(_offset, offset, _level, level));

            return MoveAbsolute(offset, level, pct);
        }

        public TrendBuilder LineAbsolutePercentage(double offset, double level)
        {
            return LineAbsolute(offset, level, true);
        }

        public TrendBuilder LineRelative(double offset, double level, bool pct = false)
        {
            return LineAbsolute(_offset + MapPercentage(offset, pct, true), level, false);
        }

        public TrendBuilder LineRelativePercentage(double offset, double level)
        {
            return LineRelative(offset, level, true);
        }

        public TrendBuilder Uniform()
        {
            MoveAbsolute(0, 1, pct: true);
            LineAbsolute(1, 1, pct: true);

            return this;
        }

        public TrendBuilder AddPeak(double location, double scale, double shape = 0, double weight = 1, double? truncateLeft = null, double? truncateRight = null, bool pct = false)
        {
            _peaks.Add(new KeyValuePair<IRandomGenerator, double>(
                new TruncatedRandom(
                    new SkewNormalGenerator(MapPercentage(location, pct, false), MapPercentage(scale, pct, true), shape),
                    min: MapPercentage(truncateLeft, pct, false), max: MapPercentage(truncateRight, pct, false)), weight));
            return this;
        }

        public TrendBuilder AddPeak(IRandomGenerator generator, double weight = 1d)
        {
            _peaks.Add(new KeyValuePair<IRandomGenerator, double>(
                generator, weight));

            return this;
        }

        public TrendBuilder Boost(double value, double weight = 1d, bool pct = false)
        {
            value = MapPercentage(value, pct, false);
            _peaks.Add(new KeyValuePair<IRandomGenerator, double>(new Trend(this, () => value), weight));

            return this;
        }

        public TrendBuilder Weighted(Action<WeightedSetBuilder<double>> values, bool pct = false, double weight = 1d)
        {
            var builder = new WeightedSetBuilder<double>();
            values(builder);

            var builderValue = builder.Build();

            _peaks.Add(new KeyValuePair<IRandomGenerator, double>(
                new Trend(this, () => MapPercentage(builderValue(), pct, false)), weight));

            return this;
        }

        public TrendBuilder SetShift(double shift)
        {
            Shift = shift;
            return this;
        }

        public double Percentage(double percentage, bool relative = false)
        {
            return MapPercentage(percentage, true, relative);
        }

        double? MapPercentage(double? value, bool asPrecentage, bool relative)
        {
            return value.HasValue ? MapPercentage(value.Value, asPrecentage, relative) : (double?)null;
        }

        double MapPercentage(double value, bool asPrecentage, bool relative)
        {
            if (!asPrecentage) return value;

            if (!Min.HasValue || !Max.HasValue) throw new Exception("Percentages can only be used when minimum and maximum are specified");

            var val = value * (Max.Value - Min.Value);
            return relative ? val : Min.Value + val;
        }

        public TrendBuilder Clear()
        {
            _lines.Clear();
            _peaks.Clear();

            return this;
        }

        public IRandomGenerator Build()
        {
            if (_lines.Count == 0 && _peaks.Count == 0)
            {
                return null;
            }

            var set = Sets.Weighted<IRandomGenerator>(builder =>
            {

                var totalArea = 0d;

                foreach (var l in _lines)
                {
                    var area = (l.Max - l.Min) * (l.Start + 0.5 * (l.End - l.Start));
                    builder.Add(l, area);
                    totalArea += area;
                }

                foreach (var peak in _peaks)
                {
                    builder.Add(peak.Key, peak.Value * (totalArea > 0 ? totalArea : 1));
                }
            });

            return new TruncatedRandom(new Trend(this, () => set().Next()), Min, Max);
        }

        class Trend : IRandomGenerator
        {
            private readonly TrendBuilder _owner;
            private readonly Func<double> _generator;

            public Trend(TrendBuilder owner, Func<double> generator)
            {
                _owner = owner;
                _generator = generator;
            }

            public double Next()
            {
                var val = _generator();
                if (_owner.Min.HasValue && _owner.Max.HasValue)
                {
                    var min = _owner.Min.Value;
                    var max = _owner.Max.Value;

                    if (_owner.Cyclic)
                    {
                        val = (val - min + _owner.Shift) % (max - min);
                        if (val < 0)
                        {
                            val += (max - min);
                        }
                        val += min;
                    }
                }
                else
                {
                    val += _owner.Shift;
                }

                return val;
            }
        }
    }
}
