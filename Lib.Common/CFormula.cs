using Lib.Line;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace Lib.Common
{
    public static class CFormula
    {
        public enum PROJECTION_POLARITY : uint { BTOW = 0, WTOB = 1, ALL = 2, WTOBTOW };
        public enum PROJECTION_DIR : uint { X_LTOR = 0, X_RTOL, Y_TTOB, Y_BTOT, DIAG };

        public static bool FindIntersection(CLine BaseLine/*수직선*/, CLine BaseTarget/*상판이나 하판*/, out OpenCvSharp.Point ptIntersection)
        {
            try
            {
                ptIntersection = new OpenCvSharp.Point();

                OpenCvSharp.Point p1 = BaseLine.Start; // BaseLine.ptStart부분 필요
                OpenCvSharp.Point p2 = BaseLine.End;
                OpenCvSharp.Point p3 = BaseTarget.Start;
                OpenCvSharp.Point p4 = BaseTarget.End;

                double dFactor = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
                if (dFactor == 0)
                {
                    return false;
                }

                double dPre = (p1.X * p2.Y - p1.Y * p2.X);
                double dPost = (p3.X * p4.Y - p3.Y * p4.X);

                double dX = (dPre * (p3.X - p4.X) - (p1.X - p2.X) * dPost) / dFactor;
                double dY = (dPre * (p3.Y - p4.Y) - (p1.Y - p2.Y) * dPost) / dFactor;

                ptIntersection = new OpenCvSharp.Point(dX, dY);
                return true;
            }
            catch (Exception Desc)
            {
                ptIntersection = new OpenCvSharp.Point();                
                return false;
            }
        }

        public static void GetLineCoef(OpenCvSharp.Point ptStart, OpenCvSharp.Point ptEnd, OpenCvSharp.Point ptBase, OpenCvSharp.Point ptImageSize, out List<OpenCvSharp.Point> listPtVert)
        {
            try
            {
                listPtVert = new List<OpenCvSharp.Point>();
                int nImageWidth = ptImageSize.X;
                int nImageHeight = ptImageSize.Y;
                int nStartBaseX = ptBase.X;
                int nStartBaseY = ptBase.Y;

                // 포인트 1, 2의 기울기 구함
                int nVertY = 0;
                double dLineAngle = (double)(ptEnd.Y - ptStart.Y) / (double)(ptEnd.X - ptStart.X);

                // 직선 A와 직선 B가 수직이면
                // 직선 A 기울기 * 직선 B 기울기 == -1
                // 그러기 때문에 아래와 같은 공식이 됨
                double dVerticalAngle = -(1.0 / dLineAngle);

                if (ptStart.X - ptEnd.X != 0)
                {
                    if (ptStart.Y - ptEnd.Y != 0)
                    {
                        OpenCvSharp.Point ptVerticalX = new OpenCvSharp.Point();
                        for (int nIndex = 0; nIndex < nImageWidth; nIndex++)
                        {
                            nVertY = ((int)(dVerticalAngle * (nIndex - ptBase.X)) + ptBase.Y);
                            ptVerticalX.X = nIndex;
                            ptVerticalX.Y = nVertY;
                            listPtVert.Add(ptVerticalX);
                        }
                        //// 수직 라인 구하기               
                    }
                    else
                    {
                        OpenCvSharp.Point ptVerticalY = new OpenCvSharp.Point();
                        for (int nIndex = 0; nIndex < nImageHeight; nIndex++)
                        {
                            ptVerticalY.X = ptBase.X;
                            ptVerticalY.Y = nIndex;
                            listPtVert.Add(ptVerticalY);
                        }
                        // Y축에 평행

                    }
                }
                else
                {
                    OpenCvSharp.Point ptVerticalX = new OpenCvSharp.Point();
                    for (int nIndex = 0; nIndex < nImageWidth; nIndex++)
                    {
                        ptVerticalX.X = nIndex;
                        ptVerticalX.Y = ptBase.Y;
                        listPtVert.Add(ptVerticalX);
                    }
                    // X축에 평행
                }
            }
            catch (Exception Desc)
            {
                listPtVert = new List<OpenCvSharp.Point>();
                return;
            }
        }


        public static void GetLineCoef(OpenCvSharp.Point ptStart/*하판 1Point*/, OpenCvSharp.Point ptEnd/*하판 2Point*/, Rect rcRoi, PROJECTION_DIR direction, double dAngle, out List<OpenCvSharp.Point> listPtVertLine)
        {
            int nStartRoiX = rcRoi.X;
            int nStartRoiY = rcRoi.Y;
            int nWidthRoiX = rcRoi.X + rcRoi.Width;
            int nEndRoiY = rcRoi.Y + rcRoi.Height;

            // 포인트 1, 2의 기울기 구함
            int nVertX = 0;
            int nVertY = 0;
            listPtVertLine = new List<OpenCvSharp.Point>();
            OpenCvSharp.Point ptVertLineY = new OpenCvSharp.Point();
            double dLineAngle = dAngle;//(ptEnd.Y - ptStart.Y) / (ptEnd.X - ptStart.X);
                                       //(ptEnd.Y - ptStart.Y) / (ptEnd.X - ptStart.X);
                                       // 직선 A와 직선 B가 수직이면
                                       // 직선 A 기울기 * 직선 B 기울기 == -1
                                       // 그러기 때문에 아래와 같은 공식이 됨
            double dVerticalAngle = -(1.0 / dLineAngle);
            // 수직 라인 구하기
            switch (direction)
            {
                case PROJECTION_DIR.X_LTOR:
                    {
                        for (int nCount = ptStart.X; nCount > nStartRoiX; nCount--)
                        {
                            nVertY = ((int)(dVerticalAngle * (nCount - ptStart.X)) + ptStart.Y);
                            ptVertLineY.X = nCount;
                            ptVertLineY.Y = nVertY;
                            listPtVertLine.Add(ptVertLineY);
                        }
                        // 수직선 구하기 -> 직선의 방정식

                    }
                    break;
                case PROJECTION_DIR.X_RTOL:
                    {
                        for (int nCount = ptStart.X; nCount < nWidthRoiX; nCount++)
                        {
                            nVertY = ((int)(dVerticalAngle * (nCount - ptStart.X)) + ptStart.Y);
                            ptVertLineY.X = nCount;
                            ptVertLineY.Y = nVertY;
                            listPtVertLine.Add(ptVertLineY);
                        }
                    }
                    break;
                case PROJECTION_DIR.Y_TTOB:
                    {
                        for (int nCount = ptStart.Y; nCount > nStartRoiY; nCount--)
                        {
                            nVertX = ((int)((nCount - ptStart.Y) / dVerticalAngle) + ptStart.X);
                            ptVertLineY.X = nVertX;
                            ptVertLineY.Y = nCount;
                            listPtVertLine.Add(ptVertLineY);
                        }
                    }
                    break;
                case PROJECTION_DIR.Y_BTOT:
                    {
                        for (int nCount = ptStart.Y; nCount < nEndRoiY; nCount++)
                        {
                            nVertX = ((int)((nCount - ptStart.Y) / dVerticalAngle) + ptStart.X);
                            ptVertLineY.X = nVertX;
                            ptVertLineY.Y = nCount;
                            listPtVertLine.Add(ptVertLineY);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 세 점을을 이용한 각도 구하기
        /// </summary>
        /// <param name="ptBase"></param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public static double threePointAngle(OpenCvSharp.Point ptBase/*ROI와 수직점이 교차한 포인트*/, OpenCvSharp.Point pt1/*하판점*/, OpenCvSharp.Point pt2/*ROI점*/)
        {
            OpenCvSharp.Point ptVectorVerticalToBottom = new OpenCvSharp.Point();
            OpenCvSharp.Point ptVectorVerticalToRoi = new OpenCvSharp.Point();
            // B(PtBase) -> A(하판) 방향으로 가는 벡터1 생성
            ptVectorVerticalToBottom.X = pt1.X - ptBase.X;
            ptVectorVerticalToBottom.Y = pt1.Y - ptBase.Y;
            // B(PtBase) -> C(ROI) 방향으로 가는 벡터2 생성
            ptVectorVerticalToRoi.X = pt2.X - ptBase.X;
            ptVectorVerticalToRoi.Y = pt2.Y - ptBase.Y;

            // 벡터1과 벡터2의 내적
            int nMoleculatr = (ptVectorVerticalToBottom.X * ptVectorVerticalToRoi.X) + (ptVectorVerticalToBottom.Y * ptVectorVerticalToRoi.Y);

            // 벡터1과 벡터2의 Scalr값
            double dDistanceVerticalToBottom = Math.Sqrt(Math.Pow(ptVectorVerticalToBottom.X, 2) + Math.Pow(ptVectorVerticalToBottom.Y, 2));
            double dDistanceVerticalToRoi = Math.Sqrt(Math.Pow(ptVectorVerticalToRoi.X, 2) + Math.Pow(ptVectorVerticalToRoi.Y, 2));

            double nDenominator = dDistanceVerticalToBottom * dDistanceVerticalToRoi;
            // 각도 구하기 (내적 / Sclar값)
            double dAngle = Math.Acos(nMoleculatr / nDenominator) * (180 / Math.PI);

            return dAngle;
        }

        /// <summary>
        /// 중심점 기준 원근 변환 실행
        /// </summary>
        /// <param name="src"></param>
        /// <param name="squares"></param>
        /// <returns></returns>
        public static Mat PerspectiveTransform(Mat src, OpenCvSharp.Point[] squares)
        {
            Mat dst = new Mat();
            Moments moments = Cv2.Moments(squares);
            double cX = moments.M10 / moments.M00;
            double cY = moments.M01 / moments.M00;

            Point2f[] src_pts = new Point2f[4];
            for (int i = 0; i < squares.Length; i++)
            {
                if (cX > squares[i].X && cY > squares[i].Y) src_pts[0] = squares[i];
                if (cX > squares[i].X && cY < squares[i].Y) src_pts[1] = squares[i];
                if (cX < squares[i].X && cY > squares[i].Y) src_pts[2] = squares[i];
                if (cX < squares[i].X && cY < squares[i].Y) src_pts[3] = squares[i];
            }

            Point2f[] dst_pts = new Point2f[4];
            dst_pts[0] = new Point2f(0, 0);
            dst_pts[1] = new Point2f(0, src.Height);
            dst_pts[2] = new Point2f(src.Width, 0);
            dst_pts[3] = new Point2f(src.Width, src.Height);


            Mat matrix = Cv2.GetPerspectiveTransform(src_pts, dst_pts);
            Cv2.WarpPerspective(src, dst, matrix, new OpenCvSharp.Size(src.Width, src.Height));
            return dst;
        }

        /// <summary>
        /// Area(Width * Height) Return
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        public static int AreaofRect(OpenCvSharp.Size sz) => sz.Width * sz.Height;

        public static double Angle(OpenCvSharp.Point ptfrom, OpenCvSharp.Point ptto) => Math.Atan2(ptto.Y - ptfrom.Y, ptto.X - ptfrom.X) * 180.0D / Math.PI;    
        public static double Angle(System.Drawing.Point ptfrom, System.Drawing.Point ptto) => Math.Atan2(ptto.Y - ptfrom.Y, ptto.X - ptfrom.X) * 180.0D / Math.PI;    
        public static double CalculateAngle360(OpenCvSharp.Point ptfrom, OpenCvSharp.Point ptto)
        {
            double angle = Math.Atan2(ptto.Y - ptfrom.Y, ptto.X - ptfrom.X) * 180.0D / Math.PI;
            if (angle < 0) { angle += 360; }
            return angle;
        }

        public static double CalculateAngle360(System.Drawing.Point ptfrom, System.Drawing.Point ptto)
        {
            double angle = Math.Atan2(ptto.Y - ptfrom.Y, ptto.X - ptfrom.X) * 180.0D / Math.PI;
            if (angle < 0) { angle += 360; }
            return angle;
        }

        public static double CalculateAngle360(PointF ptFrom, PointF ptTo)
        {
            double angle = Math.Atan2(ptTo.Y - ptFrom.Y, ptTo.X - ptFrom.X) * 180.0 / Math.PI;
            if (angle < 0) { angle += 360; }
            return angle;
        }

        public static double RoiAngle(CLine BaseLien, System.Drawing.Point ptCenter)
        {
            double d1 = 0;
            double d2 = 0;
            if (BaseLien.Start.X > ptCenter.X)
            {
                d1 = Math.Atan((BaseLien.Start.Y - ptCenter.Y) / (BaseLien.Start.X - ptCenter.X));
                d2 = Math.Atan((BaseLien.End.Y - ptCenter.Y) / (BaseLien.End.X - ptCenter.X));
            }
            else
            {
                d1 = Math.Atan((ptCenter.Y - BaseLien.Start.Y) / (ptCenter.X - BaseLien.Start.X));
                d2 = Math.Atan((ptCenter.Y - BaseLien.End.Y) / (ptCenter.X - BaseLien.End.X));
            }


            double dAngle = Math.Abs((d2 - d1) * 180 / Math.PI);
            return dAngle;
        }

        public static double DegreeToRadian(double Angle) { return Math.PI * Angle / 180.0; }

        public static double RadianToDegree(double Angle) { return Angle * (180.0 / Math.PI); }

        #region 내적 구하기 - GetDotProduct(x1, y1, x2, y2, x3, y3)

        /// <summary>
        /// 내적 구하기
        /// </summary>
        /// <param name="x1">X 1</param>
        /// <param name="y1">Y 1</param>
        /// <param name="x2">X 2</param>
        /// <param name="y2">Y 2</param>
        /// <param name="x3">X 3</param>
        /// <param name="y3">Y 3</param>
        /// <returns>내적</returns>
        public static float GetDotProduct(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            float dx12 = x1 - x2;
            float dy12 = y1 - y2;
            float dx32 = x3 - x2;
            float dy32 = y3 - y2;

            return (dx12 * dx32 + dy12 * dy32);
        }

        #endregion
        #region 외적 구하기 - GetCrossProduct(x1, y1, x2, y2, x3, y3)

        /// <summary>
        /// 외적 구하기
        /// </summary>
        /// <param name="x1">X 1</param>
        /// <param name="y1">Y 1</param>
        /// <param name="x2">X 2</param>
        /// <param name="y2">Y 2</param>
        /// <param name="x3">X 3</param>
        /// <param name="y3">Y 3</param>
        /// <returns>외적</returns>
        public static float GetCrossProduct(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            float dx12 = x1 - x2;
            float dy12 = y1 - y2;
            float dx32 = x3 - x2;
            float dy32 = y3 - y2;

            return (dx12 * dy32 - dy12 * dx32);
        }

        #endregion
        #region 각도 구하기 - GetAngle(x1, y1, x2, y2, x3, y3)

        /// <summary>
        /// 각도 구하기
        /// </summary>
        /// <param name="x1">X 1</param>
        /// <param name="y1">Y 1</param>
        /// <param name="x2">X 2</param>
        /// <param name="y2">Y 2</param>
        /// <param name="x3">X 3</param>
        /// <param name="y3">Y 3</param>
        /// <returns>각도</returns>
        public static float GetAngle(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            float dotProduct = GetDotProduct(x1, y1, x2, y2, x3, y3);

            float crossProduct = GetCrossProduct(x1, y1, x2, y2, x3, y3);

            return (float)Math.Atan2(crossProduct, dotProduct);
        }

        #endregion
        #region 다각형 내 포인트 여부 구하기 - IsPointInPolygon(x, y, polygonPointList)

        /// <summary>
        /// 다각형 내 포인트 여부 구하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="polygonPointList">다각형 포인트 리스트</param>
        /// <returns>다각형 내 포인트 여부</returns>
        public static bool IsPointInPolygon(float x, float y, PointF[] polygonPointList)
        {
            int pointCount = polygonPointList.Length - 1;

            float totalAngle = GetAngle
            (
                polygonPointList[pointCount].X,
                polygonPointList[pointCount].Y,
                x,
                y,
                polygonPointList[0].X,
                polygonPointList[0].Y
            );

            for (int i = 0; i < pointCount; i++)
            {
                totalAngle += GetAngle
                (
                    polygonPointList[i].X,
                    polygonPointList[i].Y,
                    x,
                    y,
                    polygonPointList[i + 1].X,
                    polygonPointList[i + 1].Y
                );
            }

            return (Math.Abs(totalAngle) > 0.000001);
        }

        #endregion
        #region 교차 포인트 찾기 - FindIntersectionPoint(point1, point2, point3, point4, lineIntersect, segmentIntersect, intersectionPoint, closePoint1, closePoint2, t1, t2)

        /// <summary>
        /// 교차 포인트 찾기
        /// </summary>
        /// <param name="point1">포인트 1</param>
        /// <param name="point2">포인트 2</param>
        /// <param name="point3">포인트 3</param>
        /// <param name="point4">포인트 41</param>
        /// <param name="lineIntersect">직선 교차 여부</param>
        /// <param name="segmentIntersect">세그먼트 교차 여부</param>
        /// <param name="intersectionPoint">교차 포인트</param>
        /// <param name="closePoint1">근접 포인트 1</param>
        /// <param name="closePoint2">근접 포인트 2</param>
        /// <param name="t1">T1</param>
        /// <param name="t2">T2</param>
        public static void FindIntersectionPoint(PointF point1, PointF point2, PointF point3, PointF point4, out bool lineIntersect, out bool segmentIntersect, out PointF intersectionPoint, out PointF closePoint1, out PointF closePoint2, out float t1, out float t2)
        {
            float dx12 = point2.X - point1.X;
            float dy12 = point2.Y - point1.Y;
            float dx34 = point4.X - point3.X;
            float dy34 = point4.Y - point3.Y;

            float denominator = (dy12 * dx34 - dx12 * dy34);

            t1 = ((point1.X - point3.X) * dy34 + (point3.Y - point1.Y) * dx34) / denominator;

            if (float.IsInfinity(t1))
            {
                lineIntersect = false;
                segmentIntersect = false;

                intersectionPoint = new PointF(float.NaN, float.NaN);

                closePoint1 = new PointF(float.NaN, float.NaN);
                closePoint2 = new PointF(float.NaN, float.NaN);

                t2 = float.PositiveInfinity;

                return;
            }

            lineIntersect = true;

            t2 = ((point3.X - point1.X) * dy12 + (point1.Y - point3.Y) * dx12) / -denominator;

            intersectionPoint = new PointF(point1.X + dx12 * t1, point1.Y + dy12 * t1);

            segmentIntersect = ((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1));

            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            closePoint1 = new PointF(point1.X + dx12 * t1, point1.Y + dy12 * t1);
            closePoint2 = new PointF(point3.X + dx34 * t2, point3.Y + dy34 * t2);
        }

        #endregion
        #region 클리핑 포인트 배열 구하기 - GetClippingPointArray(startOutsidePolygon, point1, point2, polygonPointList)

        /// <summary>
        /// 클리핑 포인트 배열 구하기
        /// </summary>
        /// <param name="startOutsidePolygon">다각형 외부 시작 여부</param>
        /// <param name="point1">포인트 1</param>
        /// <param name="point2">포인트 2</param>
        /// <param name="polygonPointList">다각형 포인트 리스트</param>
        /// <returns>포인트 배열</returns>
        public static PointF[] GetClippingPointArray(out bool startOutsidePolygon, PointF point1, PointF point2, List<PointF> polygonPointList)
        {
            List<PointF> intersectionPointList = new List<PointF>();

            List<float> valueList = new List<float>();

            intersectionPointList.Add(point1);

            valueList.Add(0f);

            startOutsidePolygon = !IsPointInPolygon(point1.X, point1.Y, polygonPointList.ToArray());

            for (int i = 0; i < polygonPointList.Count; i++)
            {
                int j = (i + 1) % polygonPointList.Count;

                bool lineIntersect;
                bool segmentIntersect;

                PointF intersectionPoint;
                PointF closePoint1;
                PointF closePoint2;
                float value1;
                float value2;

                FindIntersectionPoint
                (
                    point1,
                    point2,
                    polygonPointList[i],
                    polygonPointList[j],
                    out lineIntersect,
                    out segmentIntersect,
                    out intersectionPoint,
                    out closePoint1,
                    out closePoint2,
                    out value1,
                    out value2
                );

                if (segmentIntersect)
                {
                    intersectionPointList.Add(intersectionPoint);

                    valueList.Add(value1);

                    break;
                }
            }

            //intersectionPointList.Add(point2);

            //valueList.Add(1f);

            PointF[] intersectionPointArray = intersectionPointList.ToArray();

            float[] valueArray = valueList.ToArray();

            Array.Sort(valueArray, intersectionPointArray);

            return intersectionPointArray;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="pt3"></param>
        /// <param name="pt4"></param>
        /// <returns></returns>
        public static bool CCW(OpenCvSharp.Point pt1, OpenCvSharp.Point pt2, OpenCvSharp.Point pt3, OpenCvSharp.Point pt4)
        {
            double dFactor = 0;

            double d1 = (pt2.X - pt1.X) * (pt3.Y - pt1.Y) - (pt2.Y - pt1.Y) * (pt3.X - pt1.X);
            double d2 = (pt2.X - pt1.X) * (pt4.Y - pt1.Y) - (pt2.Y - pt1.Y) * (pt4.X - pt1.X);

            dFactor = d1 * d2;

            if (dFactor < 0) { return true; }

            else { return false; }
        }

        /// <summary>
        /// 4개의 포인트로 서로 교차 여부를 확인 합니다.
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="pt3"></param>
        /// <param name="pt4"></param>
        /// <returns></returns>
        public static bool CrossCheck(OpenCvSharp.Point pt1, OpenCvSharp.Point pt2, OpenCvSharp.Point pt3, OpenCvSharp.Point pt4)
        {
            bool bIsDivideLine1 = CCW(pt1, pt2, pt3, pt4);
            bool bIsDivideLine2 = CCW(pt3, pt4, pt1, pt2);

            if (bIsDivideLine1 && bIsDivideLine2) { return true; }
            else { return false; }
        }

        /// <summary>
        /// 지정한 엣지 개수만큼 각도를 구해서 선을 생성합니다.
        /// </summary>
        /// <param name="Edge">총 엣지 리스트</param>
        /// <param name="ImageW">이미지의 Width(생성한 선이 Width만큼 길어짐)</param>
        /// <param name="ImageH">이미지의 Height(생성한 선이 Height만큼 길어짐)</param>
        /// <param name="POINT_RANGE">POINT_RANGE마다 각도를 산출</param>
        /// <param name=""></param>
        /// <returns></returns>
        public static List<CLine> GetVerticalLines(List<OpenCvSharp.Point> Edge, int ImageW, int ImageH, int POINT_RANGE, PROJECTION_DIR VER_PRJ_DIR)
        {
            // 수직선 라인들(직선의 방정식으로 만들어진)
            List<CLine> ver_lines = new List<CLine>();
            // 검출된 엣지의 숫자만큼 반복
            for (int Cnt = 0; Cnt < Edge.Count; Cnt++)
            {
                OpenCvSharp.Point Start; OpenCvSharp.Point End;
                List<OpenCvSharp.Point> VerLine = new List<OpenCvSharp.Point>();
                if (Cnt == Edge.Count - 1)
                {
                    Start = Edge[Cnt];
                    End = Edge[(Cnt)];
                }
                else
                {
                    Start = Edge[Cnt];
                    End = Edge[(Cnt + 1)];
                }

                OpenCvSharp.Line2D Line = Cv2.FitLine(GetRangePoint(Cnt, POINT_RANGE, Edge), DistanceTypes.L2, 0, 0.01, 0.01);
                double T = Math.Tan(Line.GetVectorRadian());
                GetLineCoef(Start, End, new OpenCvSharp.Rect(0, 0, ImageW, ImageH), VER_PRJ_DIR, T, out VerLine);

                System.Drawing.Point ptStart = new System.Drawing.Point(VerLine[0].X, VerLine[0].Y);
                System.Drawing.Point ptEnd = new System.Drawing.Point(VerLine[VerLine.Count - 1].X, VerLine[VerLine.Count - 1].Y);
                ver_lines.Add(new CLine(CConverter.PointToCVPoint(ptStart), CConverter.PointToCVPoint(ptEnd)));
            }
            return ver_lines;
        }

        /// <summary>
        /// 지정한 각도로 선을 생성합니다.
        /// </summary>
        /// <param name="Edge">총 엣지 리스트</param>
        /// <param name="ImageW">이미지의 Width(생성한 선이 Width만큼 길어짐)</param>
        /// <param name="ImageH">이미지의 Height(생성한 선이 Height만큼 길어짐)</param>
        /// <param name="MANUAL_ANGLE_VALUE">지정한 각도로 선을 생성</param>
        /// <param name=""></param>
        /// <returns></returns>
        public static List<CLine> GetVerticalLinesManual(List<OpenCvSharp.Point> Edge, int ImageW, int ImageH, double MANUAL_ANGLE_VALUE, PROJECTION_DIR VER_PRJ_DIR)
        {
            // 수직선 라인들(직선의 방정식으로 만들어진)
            List<CLine> ver_lines = new List<CLine>();
            // 검출된 엣지의 숫자만큼 반복
            for (int Cnt = 0; Cnt < Edge.Count; Cnt++)
            {
                OpenCvSharp.Point Start; OpenCvSharp.Point End;
                List<OpenCvSharp.Point> VerLine = new List<OpenCvSharp.Point>();
                if (Cnt == Edge.Count - 1)
                {
                    Start = Edge[Cnt];
                    End = Edge[(Cnt)];
                }
                else
                {
                    Start = Edge[Cnt];
                    End = Edge[(Cnt + 1)];
                }

                double result = Math.Tan(MANUAL_ANGLE_VALUE * (Math.PI / 180));
                GetLineCoef(Start, End, new OpenCvSharp.Rect(0, 0, ImageW, ImageH), VER_PRJ_DIR, result, out VerLine);

                System.Drawing.Point ptStart = new System.Drawing.Point(VerLine[0].X, VerLine[0].Y);
                System.Drawing.Point ptEnd = new System.Drawing.Point(VerLine[VerLine.Count - 1].X, VerLine[VerLine.Count - 1].Y);
                ver_lines.Add(new CLine(CConverter.PointToCVPoint(ptStart), CConverter.PointToCVPoint(ptEnd)));
            }
            return ver_lines;
        }

        private static List<OpenCvSharp.Point> GetRangePoint(int Cnt, int Range, List<OpenCvSharp.Point> Edge)
        {
            List<OpenCvSharp.Point> Point = new List<OpenCvSharp.Point>();
            // 레인지만큼 포인트를 가져다가 수직선을 만듬
            if (Cnt + Range > Edge.Count)
            {
                if (Edge.Count < Range)
                {
                    int Diff = Edge.Count - (Cnt + Range);
                    if (Diff > 0) { Point = Edge.GetRange(Cnt - Diff, Range); }
                    else { Point = Edge.GetRange(0, Edge.Count); }
                }
                else
                {
                    int Diff = Math.Abs(Edge.Count - (Cnt + Range));
                    Point = Edge.GetRange(Cnt - Diff, Range);
                }
            }
            else { Point = Edge.GetRange(Cnt, Range); }

            return Point;
        }

        public static List<CLine> GetIntersectionLines(List<CLine> ver_Lines, List<OpenCvSharp.Point> Edges)
        {
            List<CLine> intersectionLines = new List<CLine>();
            if (Edges.Count == 0)
            {
                Debug.WriteLine($"Not Exists Edge, Check Parameter");                
                return intersectionLines;
            }

            List<PointF> Points_R = Edges.ConvertAll(new Converter<OpenCvSharp.Point, PointF>(CConverter.CVPointToPointF));

            for (int i = 0; i < ver_Lines.Count; i++)
            {
                CLine verLine = ver_Lines[i];

                bool draw = true;

                PointF start = new PointF(ver_Lines[i].Start.X, ver_Lines[i].Start.Y);
                PointF end = new PointF(ver_Lines[i].End.X, ver_Lines[i].End.Y);


                PointF[] intersectionPointArray = GetClippingPointArray
                (
                    out draw,
                    start,
                    end,
                    Points_R
                );
                if (intersectionPointArray.Length > 1) { intersectionLines.Add(new CLine(intersectionPointArray[0], intersectionPointArray[0 + 1])); }
            }

            return intersectionLines;
        }

        public static OpenCvSharp.Point GetIntersectionLines(CLine Lines_L, CLine Lines_R)
        {
            OpenCvSharp.Point intersection = new OpenCvSharp.Point();
            bool bInterSection = CrossCheck(Lines_L.Start, Lines_L.End, Lines_R.Start, Lines_R.End);
            if (bInterSection)
            {
                FindIntersection(Lines_L, Lines_R, out intersection);
            }

            return intersection;
        }

        public static (System.Drawing.Point, System.Drawing.Point) ExtendLength(CLine FitLine, double T, int length)
        {
            System.Drawing.Point start = new System.Drawing.Point();
            System.Drawing.Point end = new System.Drawing.Point();

            /*
             * [산수 메모] 한점과 기울기를 알때 일정거리만큼 떨어진 점의 좌표
             한점(x,y)
             기울기 m
             일정거리 t
             새점.x = x + (t * Cos(m));
             새점.y = y + (t * Sin(m));  
            */
            start.X = (int)(FitLine.Start.X - (length * Math.Cos(DegreeToRadian(T))));
            start.Y = (int)(FitLine.Start.Y - (length * Math.Sin(DegreeToRadian(T))));

            end.X = (int)(FitLine.End.X + (length * Math.Cos(DegreeToRadian(T))));
            end.Y = (int)(FitLine.End.Y + (length * Math.Sin(DegreeToRadian(T))));

            return (start, end);
        }

        public static CLine GetFitLine(List<OpenCvSharp.Point> Edges, PROJECTION_DIR PRJ_DIR)
        {
            if (Edges.Count == 0)
            {
                Debug.WriteLine($"Not Exists Edge, Check Parameter");                
                return new CLine();
            }

            CLineFitting cLineFitting = new CLineFitting();
            System.Drawing.Point start = new System.Drawing.Point();
            System.Drawing.Point end = new System.Drawing.Point();

            switch (PRJ_DIR)
            {
                case PROJECTION_DIR.X_RTOL:
                case PROJECTION_DIR.X_LTOR:
                    (start, end) = cLineFitting.LineFitY(Edges);
                    break;
                case PROJECTION_DIR.Y_TTOB:
                case PROJECTION_DIR.Y_BTOT:
                    (start, end) = cLineFitting.LineFitX(Edges);
                    break;
            }

            double T = Angle(start, end);
            (start, end) = ExtendLength(new CLine(start, end), T, 1500);
            return new CLine(start, end);
        }
    }
}
