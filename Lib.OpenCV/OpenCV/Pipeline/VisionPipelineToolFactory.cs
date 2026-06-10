using Lib.OpenCV.Property;
using Lib.OpenCV.Tool;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lib.OpenCV.Pipeline
{
    public static class VisionPipelineToolFactory
    {
        public static IVisionTool Create(VisionPipelineStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            string toolType = NormalizeToolType(step.ToolType);

            switch (toolType)
            {
                case "threshold":
                    return CreateThresholdTool(step.Parameters);
                case "morphology":
                    return CreateMorphologyTool(step.Parameters);
                case "filter":
                    return CreateFilterTool(step.Parameters);
                case "edgedetection":
                case "edge":
                    return CreateEdgeDetectionTool(step.Parameters);
                default:
                    throw new NotSupportedException($"Unsupported vision tool type '{step.ToolType}'.");
            }
        }

        private static IVisionTool CreateThresholdTool(IDictionary<string, string> parameters)
        {
            ThresholdToolProperty property = new ThresholdToolProperty
            {
                Mode = GetEnum(parameters, nameof(ThresholdToolProperty.Mode), ThresholdToolMode.Threshold),
                Threshold = GetDouble(parameters, nameof(ThresholdToolProperty.Threshold), 1),
                MaxValue = GetDouble(parameters, nameof(ThresholdToolProperty.MaxValue), 255),
                ThresholdType = GetEnum(parameters, nameof(ThresholdToolProperty.ThresholdType), ThresholdTypes.Binary),
                RangeMin = GetInt(parameters, nameof(ThresholdToolProperty.RangeMin), 1),
                RangeMax = GetInt(parameters, nameof(ThresholdToolProperty.RangeMax), 255),
                AdaptiveType = GetEnum(parameters, nameof(ThresholdToolProperty.AdaptiveType), AdaptiveThresholdTypes.MeanC),
                AdaptiveThresholdType = GetEnum(parameters, nameof(ThresholdToolProperty.AdaptiveThresholdType), ThresholdTypes.Binary),
                BlockSize = GetInt(parameters, nameof(ThresholdToolProperty.BlockSize), 25),
                Weight = GetInt(parameters, nameof(ThresholdToolProperty.Weight), 5)
            };

            ThresholdTool tool = new ThresholdTool();
            tool.SetProperty(property);
            return tool;
        }

        private static IVisionTool CreateMorphologyTool(IDictionary<string, string> parameters)
        {
            MorphologyToolProperty property = new MorphologyToolProperty
            {
                Shape = GetEnum(parameters, nameof(MorphologyToolProperty.Shape), MorphShapes.Rect),
                Operator = GetEnum(parameters, nameof(MorphologyToolProperty.Operator), MorphTypes.Erode),
                KernelWidth = GetInt(parameters, nameof(MorphologyToolProperty.KernelWidth), 3),
                KernelHeight = GetInt(parameters, nameof(MorphologyToolProperty.KernelHeight), 3),
                Iterations = GetInt(parameters, nameof(MorphologyToolProperty.Iterations), 1)
            };

            MorphologyTool tool = new MorphologyTool();
            tool.SetProperty(property);
            return tool;
        }

        private static IVisionTool CreateFilterTool(IDictionary<string, string> parameters)
        {
            FilterToolProperty property = new FilterToolProperty
            {
                FilterType = GetEnum(parameters, nameof(FilterToolProperty.FilterType), FilterToolType.Blur),
                KernelWidth = GetInt(parameters, nameof(FilterToolProperty.KernelWidth), 3),
                KernelHeight = GetInt(parameters, nameof(FilterToolProperty.KernelHeight), 3),
                MedianKernelSize = GetInt(parameters, nameof(FilterToolProperty.MedianKernelSize), 3),
                Diameter = GetInt(parameters, nameof(FilterToolProperty.Diameter), 3),
                SigmaColor = GetInt(parameters, nameof(FilterToolProperty.SigmaColor), 3),
                SigmaSpace = GetInt(parameters, nameof(FilterToolProperty.SigmaSpace), 3),
                BorderType = GetEnum(parameters, nameof(FilterToolProperty.BorderType), BorderTypes.Reflect101)
            };

            FilterTool tool = new FilterTool();
            tool.SetProperty(property);
            return tool;
        }

        private static IVisionTool CreateEdgeDetectionTool(IDictionary<string, string> parameters)
        {
            EdgeDetectionToolProperty property = new EdgeDetectionToolProperty
            {
                EdgeType = GetEnum(parameters, nameof(EdgeDetectionToolProperty.EdgeType), EdgeDetectionToolType.Canny),
                CannyThresholdLow = GetInt(parameters, nameof(EdgeDetectionToolProperty.CannyThresholdLow), 100),
                CannyThresholdHigh = GetInt(parameters, nameof(EdgeDetectionToolProperty.CannyThresholdHigh), 200),
                CannyApertureSize = GetInt(parameters, nameof(EdgeDetectionToolProperty.CannyApertureSize), 3),
                UseL2Gradient = GetBool(parameters, nameof(EdgeDetectionToolProperty.UseL2Gradient), true),
                SobelDegreeX = GetInt(parameters, nameof(EdgeDetectionToolProperty.SobelDegreeX), 0),
                SobelDegreeY = GetInt(parameters, nameof(EdgeDetectionToolProperty.SobelDegreeY), 0),
                SobelKernelSize = GetInt(parameters, nameof(EdgeDetectionToolProperty.SobelKernelSize), 1),
                ScharrDegreeX = GetInt(parameters, nameof(EdgeDetectionToolProperty.ScharrDegreeX), 0),
                ScharrDegreeY = GetInt(parameters, nameof(EdgeDetectionToolProperty.ScharrDegreeY), 0),
                LaplacianKernelSize = GetInt(parameters, nameof(EdgeDetectionToolProperty.LaplacianKernelSize), 1)
            };

            EdgeDetectionTool tool = new EdgeDetectionTool();
            tool.SetProperty(property);
            return tool;
        }

        private static string NormalizeToolType(string toolType)
        {
            string value = (toolType ?? string.Empty).Trim();
            if (value.EndsWith("Tool", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - 4);
            }

            return value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }

        private static string GetValue(IDictionary<string, string> parameters, string key)
        {
            if (parameters == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            foreach (KeyValuePair<string, string> item in parameters)
            {
                if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return item.Value;
                }
            }

            return null;
        }

        private static int GetInt(IDictionary<string, string> parameters, string key, int defaultValue)
        {
            string value = GetValue(parameters, key);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : defaultValue;
        }

        private static double GetDouble(IDictionary<string, string> parameters, string key, double defaultValue)
        {
            string value = GetValue(parameters, key);
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                ? result
                : defaultValue;
        }

        private static bool GetBool(IDictionary<string, string> parameters, string key, bool defaultValue)
        {
            string value = GetValue(parameters, key);
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        private static TEnum GetEnum<TEnum>(IDictionary<string, string> parameters, string key, TEnum defaultValue)
            where TEnum : struct
        {
            string value = GetValue(parameters, key);
            return Enum.TryParse(value, true, out TEnum result) ? result : defaultValue;
        }
    }
}
