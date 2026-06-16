using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lib.Common;
using Lib.OpenCV.Property;
using Lib.OpenCV.Result;
using OpenCvSharp;

namespace Lib.OpenCV.Tool
{
    [Obsolete("Legacy compatibility API. Use ContourTool and ContourResult for new OpenVisionLab code.", false)]
    public partial class CVContour : COpenCVAlgorithmBase
    {
        public IOpenCVPropertyContour property;

        public List<CResultContour> results = new List<CResultContour>();

        public CVContour() { }

        public void SetProperty(IOpenCVPropertyContour propertyBase) => property = propertyBase;

        public override void Run()
        {
            RunWithContourTool(property?.USE_MULTI_ROI == true);
        }

        public bool SingleRun()
        {
            return RunWithContourTool(false);
        }

        public bool MultiRun()
        {
            return RunWithContourTool(true);
        }

        public bool SquareRun()
        {
            try
            {
                ContourTool tool = CreateContourTool();
                bool success = tool.SquareRun();
                CopyFrom(tool);
                return success;
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[ERROR] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name} Ex ==> {Desc.Message}");
                return false;
            }
        }

        private bool RunWithContourTool(bool useMultiRoi)
        {
            try
            {
                ContourTool tool = CreateContourTool();
                bool success = useMultiRoi ? tool.MultiRun() : tool.SingleRun();
                CopyFrom(tool);
                return success;
            }
            catch (Exception Desc)
            {
                CLOG.ABNORMAL($"[ERROR] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name} Ex ==> {Desc.Message}");
                return false;
            }
        }

        private ContourTool CreateContourTool()
        {
            if (property == null)
            {
                throw new InvalidOperationException("Contour property is not set.");
            }

            ContourTool tool = new ContourTool();
            tool.SetProperty(property);
            tool.SetSourceImage(imageSource);

            if (imageTemplate != null && !imageTemplate.Empty())
            {
                imageTemplate.CopyTo(tool.imageTemplate);
            }

            return tool;
        }

        private void CopyFrom(ContourTool tool)
        {
            results = tool.results
                .Select(ToLegacyResult)
                .OrderBy(result => result.Index)
                .ToList();

            size = tool.size;
            if (tool.imageResult != null && !tool.imageResult.Empty())
            {
                imageResult = tool.imageResult.Clone();
            }
        }

        private static CResultContour ToLegacyResult(ContourResult result)
        {
            Rect bounds = new Rect(
                result.Bounding.X,
                result.Bounding.Y,
                result.Bounding.Width,
                result.Bounding.Height);

            return new CResultContour(
                result.Index,
                result.Area,
                result.Center,
                bounds,
                result.Contours,
                result.Angle);
        }
    }
}
