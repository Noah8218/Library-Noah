namespace OpenVisionLab.Vision3D.Inspection
{
    /// <summary>
    /// Stable metric dictionary keys produced by the common height-map inspection tools.
    /// </summary>
    public static class ThreeDInspectionMetricNames
    {
        public static class Quality
        {
            public const string TotalSampleCount = "TotalSampleCount";
            public const string ValidSampleCount = "ValidSampleCount";
            public const string MissingSampleCount = "MissingSampleCount";
            public const string ValidCoverageRatio = "ValidCoverageRatio";
            public const string MinimumValidSamples = "MinimumValidSamples";
            public const string MinimumValidCoverageRatio = "MinimumValidCoverageRatio";
        }

        public static class Thickness
        {
            public const string Minimum = "Minimum";
            public const string Maximum = "Maximum";
            public const string Mean = "Mean";
            public const string Range = "Range";
            public const string LowerLimit = "LowerLimit";
            public const string UpperLimit = "UpperLimit";
            public const string BelowLowerLimitCount = "BelowLowerLimitCount";
            public const string AboveUpperLimitCount = "AboveUpperLimitCount";
        }

        public static class Warpage
        {
            public const string PeakToValley = "PeakToValley";
            public const string Rms = "Rms";
            public const string MinimumResidual = "MinimumResidual";
            public const string MaximumResidual = "MaximumResidual";
            public const string MaximumPeakToValley = "MaximumPeakToValley";
            public const string PlaneSlopeX = "PlaneSlopeX";
            public const string PlaneSlopeY = "PlaneSlopeY";
            public const string PlaneIntercept = "PlaneIntercept";
            public const string MaximumRms = "MaximumRms";
        }

        public static class DatumPlaneRawHeightDeviation
        {
            public const string MinimumRawHeightResidual = "MinimumRawHeightResidual";
            public const string MaximumRawHeightResidual = "MaximumRawHeightResidual";
            public const string MinimumResidualRow = "MinimumResidualRow";
            public const string MinimumResidualColumn = "MinimumResidualColumn";
            public const string MaximumResidualRow = "MaximumResidualRow";
            public const string MaximumResidualColumn = "MaximumResidualColumn";
            public const string PeakToValleyRawHeight = "PeakToValleyRawHeight";
            public const string RmsRawHeightResidual = "RmsRawHeightResidual";
            public const string MaximumPeakToValleyRawHeight = "MaximumPeakToValleyRawHeight";
            public const string MinimumAbsoluteNormalY = "MinimumAbsoluteNormalY";
            public const string PlaneNormalX = "PlaneNormalX";
            public const string PlaneNormalY = "PlaneNormalY";
            public const string PlaneNormalZ = "PlaneNormalZ";
            public const string PlaneOffset = "PlaneOffset";
        }
    }
}
