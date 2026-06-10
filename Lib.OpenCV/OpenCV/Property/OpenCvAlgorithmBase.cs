using Lib.Common;
using Lib.OpenCV.Tool;
using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace Lib.OpenCV.Property
{
    public abstract class OpenCvAlgorithmBase : IVisionTool
    {
        public OpenCvAlgorithmBase() { }
        public Mat imageSource { get; set; } = new Mat();
        public Mat imageResult { get; set; } = new Mat();
        public Mat imageTemplate { get; set; } = new Mat();

        public Stopwatch swTaktTimems = new Stopwatch();

        public OpenCvSharp.Size size { get; set; } = new OpenCvSharp.Size();

        public virtual void SetSourceImage(Mat Image)
        {
            Image.CopyTo(imageSource);
            size = imageSource.Size();
        }

        public virtual void SetSourceImage(Bitmap Image)
        {
            imageSource = BitmapImageConverter.ToMat(Image);
            size = imageSource.Size();
        }

        public abstract void Run();

        public virtual string Name => GetType().Name;

        public virtual VisionToolResult Execute(Mat source)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                if (OpenCvHelper.IsImageEmpty(source))
                {
                    throw new InvalidOperationException("Source image is not loaded.");
                }

                SetSourceImage(source);
                Run();
                stopwatch.Stop();

                Mat resultImage = OpenCvHelper.IsImageEmpty(imageResult)
                    ? imageSource?.Clone()
                    : imageResult.Clone();

                return VisionToolResult.Passed(resultImage, stopwatch.Elapsed, CollectMetrics(), CollectOverlays());
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return VisionToolResult.Failed(ex.Message, stopwatch.Elapsed, ex);
            }
        }

        protected virtual IDictionary<string, double> CollectMetrics()
        {
            Dictionary<string, double> metrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            AddResultListMetrics(metrics, "results");
            AddResultListMetrics(metrics, "resultList");
            return metrics;
        }

        protected virtual IEnumerable<VisionToolOverlay> CollectOverlays()
        {
            List<VisionToolOverlay> overlays = new List<VisionToolOverlay>();
            AddResultListOverlays(overlays, "results");
            AddResultListOverlays(overlays, "resultList");
            return overlays;
        }

        private void AddResultListMetrics(Dictionary<string, double> metrics, string memberName)
        {
            object value = GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(this)
                ?? GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(this);

            if (!(value is IEnumerable enumerable) || value is string)
            {
                return;
            }

            List<object> items = enumerable.Cast<object>().Where(item => item != null).ToList();
            metrics["ResultCount"] = items.Count;
            AddNumericAggregate(metrics, items, "Area", "Area");
            AddNumericAggregate(metrics, items, "Score", "Score");
            AddNumericAggregate(metrics, items, "Angle", "Angle");
            AddNumericAggregate(metrics, items, "meanValue", "MeanValue");
            AddNestedCount(metrics, items, "Results_List", "EdgeCount");
            AddNestedCount(metrics, items, "edgeList", "EdgePointCount");
        }

        private static void AddNumericAggregate(Dictionary<string, double> metrics, IList<object> items, string propertyName, string metricName)
        {
            List<double> values = items
                .Select(item => ReadDoubleProperty(item, propertyName))
                .Where(value => value.HasValue)
                .Select(value => value.Value)
                .ToList();

            if (values.Count == 0)
            {
                return;
            }

            metrics[$"{metricName}Min"] = values.Min();
            metrics[$"{metricName}Max"] = values.Max();
            metrics[$"{metricName}Avg"] = values.Average();
        }

        private static void AddNestedCount(Dictionary<string, double> metrics, IList<object> items, string propertyName, string metricName)
        {
            int count = 0;
            foreach (object item in items)
            {
                object value = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(item);
                if (value is IEnumerable enumerable && !(value is string))
                {
                    count += enumerable.Cast<object>().Count();
                }
            }

            if (count > 0)
            {
                metrics[metricName] = count;
            }
        }

        private static double? ReadDoubleProperty(object item, string propertyName)
        {
            object value = item.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(item);
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return null;
            }
        }

        private void AddResultListOverlays(List<VisionToolOverlay> overlays, string memberName)
        {
            object value = GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(this)
                ?? GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(this);

            if (!(value is IEnumerable enumerable) || value is string)
            {
                return;
            }

            int fallbackIndex = 1;
            foreach (object item in enumerable.Cast<object>().Where(item => item != null))
            {
                int index = ReadIntProperty(item, "Index") ?? ReadIntProperty(item, "index") ?? fallbackIndex;
                string label = CreateOverlayLabel(index, item);
                RectangleF? bounds = ReadRectangleProperty(item, "Bounding");
                PointF? center = ReadPointProperty(item, "Center");
                double angle = ReadDoubleProperty(item, "Angle") ?? 0d;

                if (bounds.HasValue)
                {
                    overlays.Add(new VisionToolOverlay
                    {
                        Kind = VisionToolOverlayKind.Rectangle,
                        Label = label,
                        Bounds = bounds.Value,
                        Center = center ?? CenterOf(bounds.Value),
                        Angle = angle
                    });
                }
                else if (center.HasValue)
                {
                    overlays.Add(new VisionToolOverlay
                    {
                        Kind = VisionToolOverlayKind.Point,
                        Label = label,
                        Center = center.Value,
                        Angle = angle
                    });
                }

                List<PointF> points = ReadPointListProperty(item, "edgeList");
                if (points.Count > 0)
                {
                    VisionToolOverlay pointOverlay = new VisionToolOverlay
                    {
                        Kind = VisionToolOverlayKind.Points,
                        Label = string.IsNullOrWhiteSpace(label) ? "Edges" : label
                    };
                    pointOverlay.Points.AddRange(points);
                    overlays.Add(pointOverlay);
                }

                LineOverlayGeometry? line = ReadLineProperty(item, "FitLine");
                if (line.HasValue)
                {
                    overlays.Add(new VisionToolOverlay
                    {
                        Kind = VisionToolOverlayKind.Line,
                        Label = CreateLineOverlayLabel(index, item),
                        Start = line.Value.Start,
                        End = line.Value.End,
                        Center = CenterOf(line.Value.Start, line.Value.End)
                    });
                }

                fallbackIndex++;
            }
        }

        private static string CreateOverlayLabel(int index, object item)
        {
            List<string> parts = new List<string> { $"#{index}" };
            double? area = ReadDoubleProperty(item, "Area");
            double? score = ReadDoubleProperty(item, "Score");
            double? mean = ReadDoubleProperty(item, "meanValue");
            double? angle = ReadDoubleProperty(item, "Angle");
            PointF? center = ReadPointProperty(item, "Center");
            int? edgeCount = ReadEnumerableCount(item, "Results_List") ?? ReadEnumerableCount(item, "edgeList");

            if (score.HasValue)
            {
                parts.Add($"S:{score.Value:0.#}");
            }

            if (area.HasValue)
            {
                parts.Add($"A:{area.Value:0}");
            }

            if (mean.HasValue)
            {
                parts.Add($"M:{mean.Value:0.#}");
            }

            if (angle.HasValue && Math.Abs(angle.Value) > 0.000001)
            {
                parts.Add($"Ang:{angle.Value:0.#}");
            }

            if (edgeCount.HasValue && edgeCount.Value > 0)
            {
                parts.Add($"E:{edgeCount.Value}");
            }

            if (center.HasValue)
            {
                parts.Add($"C:{center.Value.X:0},{center.Value.Y:0}");
            }

            return string.Join(" ", parts);
        }

        private static string CreateLineOverlayLabel(int index, object item)
        {
            int? edgeCount = ReadEnumerableCount(item, "Results_List") ?? ReadEnumerableCount(item, "edgeList");
            List<string> parts = new List<string> { $"#{index}", "Fit" };
            if (edgeCount.HasValue && edgeCount.Value > 0)
            {
                parts.Add($"E:{edgeCount.Value}");
            }

            return string.Join(" ", parts);
        }

        private static RectangleF? ReadRectangleProperty(object item, string propertyName)
        {
            object value = ReadPropertyValue(item, propertyName);
            if (value == null)
            {
                return null;
            }

            float? x = ReadFloatProperty(value, "X");
            float? y = ReadFloatProperty(value, "Y");
            float? width = ReadFloatProperty(value, "Width");
            float? height = ReadFloatProperty(value, "Height");
            if (!x.HasValue || !y.HasValue || !width.HasValue || !height.HasValue)
            {
                return null;
            }

            if (width.Value <= 0 || height.Value <= 0)
            {
                return null;
            }

            return new RectangleF(x.Value, y.Value, width.Value, height.Value);
        }

        private static PointF? ReadPointProperty(object item, string propertyName)
        {
            object value = ReadPropertyValue(item, propertyName);
            if (value == null)
            {
                return null;
            }

            return ReadPointValue(value);
        }

        private static List<PointF> ReadPointListProperty(object item, string propertyName)
        {
            object value = ReadPropertyValue(item, propertyName);
            if (!(value is IEnumerable enumerable) || value is string)
            {
                return new List<PointF>();
            }

            return enumerable
                .Cast<object>()
                .Select(ReadPointValue)
                .Where(point => point.HasValue)
                .Select(point => point.Value)
                .ToList();
        }

        private static PointF? ReadPointValue(object value)
        {
            float? x = ReadFloatProperty(value, "X");
            float? y = ReadFloatProperty(value, "Y");
            if (!x.HasValue || !y.HasValue)
            {
                return null;
            }

            return new PointF(x.Value, y.Value);
        }

        private static object ReadPropertyValue(object item, string propertyName)
        {
            if (item == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            Type type = item.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(item);
            }

            return type.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(item);
        }

        private static int? ReadIntProperty(object item, string propertyName)
        {
            object value = ReadPropertyValue(item, propertyName);
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        private static float? ReadFloatProperty(object item, string propertyName)
        {
            object value = ReadPropertyValue(item, propertyName);
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToSingle(value);
            }
            catch
            {
                return null;
            }
        }

        private static int? ReadEnumerableCount(object item, string propertyName)
        {
            object value = ReadPropertyValue(item, propertyName);
            if (!(value is IEnumerable enumerable) || value is string)
            {
                return null;
            }

            return enumerable.Cast<object>().Count();
        }

        private static LineOverlayGeometry? ReadLineProperty(object item, string propertyName)
        {
            object value = ReadPropertyValue(item, propertyName);
            if (value == null)
            {
                return null;
            }

            if (TryReadLinePoints(value, "P1", "P2", out LineOverlayGeometry line)
                || TryReadLinePoints(value, "Point1", "Point2", out line)
                || TryReadLinePoints(value, "Start", "End", out line)
                || TryReadLinePoints(value, "StartPoint", "EndPoint", out line)
                || TryReadLinePoints(value, "Begin", "End", out line))
            {
                return line;
            }

            return null;
        }

        private static bool TryReadLinePoints(object value, string startMember, string endMember, out LineOverlayGeometry line)
        {
            line = default;
            object start = ReadPropertyValue(value, startMember);
            object end = ReadPropertyValue(value, endMember);
            PointF? startPoint = ReadPointValue(start);
            PointF? endPoint = ReadPointValue(end);
            if (!startPoint.HasValue || !endPoint.HasValue)
            {
                return false;
            }

            if (Math.Abs(startPoint.Value.X - endPoint.Value.X) < 0.000001
                && Math.Abs(startPoint.Value.Y - endPoint.Value.Y) < 0.000001)
            {
                return false;
            }

            line = new LineOverlayGeometry
            {
                Start = startPoint.Value,
                End = endPoint.Value
            };
            return true;
        }

        private static PointF CenterOf(RectangleF bounds)
        {
            return new PointF(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
        }

        private static PointF CenterOf(PointF start, PointF end)
        {
            return new PointF((start.X + end.X) / 2f, (start.Y + end.Y) / 2f);
        }

        private struct LineOverlayGeometry
        {
            public PointF Start { get; set; }
            public PointF End { get; set; }
        }

    }
}
