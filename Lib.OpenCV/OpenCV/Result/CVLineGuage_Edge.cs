using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.OpenCV.Result
{
    [Obsolete("Legacy compatibility result. Use LineGaugeEdge for new OpenVisionLab code.", false)]
    public class CVLineGuage_Edge
    {
        public int NO { get; set; } = 0;
        public OpenCvSharp.Point MeasPos { get; set; } = new OpenCvSharp.Point();
        public bool UseEdge { get; set; } = true;

        public CVLineGuage_Edge(OpenCvSharp.Point ptMeasPos, bool UseEdge = true)
        {
            MeasPos = ptMeasPos;
            this.UseEdge = UseEdge;
        }

        public CVLineGuage_Edge(int Index, OpenCvSharp.Point ptMeasPos, bool UseEdge = true)
        {
            NO = Index;
            MeasPos = ptMeasPos;
            this.UseEdge = UseEdge;
        }

        public CVLineGuage_Edge() { }
    }
}
