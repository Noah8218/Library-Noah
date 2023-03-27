using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Common
{
    public static class CImageProcessing
    {
        public static async Task<Bitmap> CropAtRect(Bitmap orgImg, Rectangle sRect)
        {
            Rectangle destRect = new Rectangle(System.Drawing.Point.Empty, sRect.Size);

            var cropImage = new Bitmap(destRect.Width, destRect.Height);
            using (var graphics = Graphics.FromImage(cropImage))
            {
                graphics.DrawImage(orgImg, destRect, sRect, GraphicsUnit.Pixel);
            }
            return await Task.FromResult(cropImage);
        }


        /// <summary>
        /// 이미지를 Overlay 합니다.
        /// </summary>
        /// <param name="bottom">원본이미지</param>
        /// <param name="overlay">원본 이미지 위에 Overlay 할 이미지</param>
        /// <param name="pLeft">Overlay 시작 포인트: Left Point</param>
        /// <param name="pTop">Overlay 시작 포인트 : Top Point</param>
        /// <returns>Overlay 된 이미지 반환</returns>
        public static Bitmap OverlayImage(System.Drawing.Image bottom, Bitmap overlay, int pLeft, int pTop)
        {
            Bitmap result = new Bitmap(bottom.Width, bottom.Height);
            Graphics g = Graphics.FromImage((System.Drawing.Image)result);

            g.DrawImage(bottom, 0, 0, bottom.Width, bottom.Height);
            g.DrawImage(overlay, pLeft, pTop, overlay.Width, overlay.Height);
            g.Dispose();

            return result;
        }
    }
}
