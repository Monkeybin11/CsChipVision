using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.Util;

namespace LedChipPassFail_first.Data
{
    public class ImgPData
    {
        /*Display Window Data*/
        public double CanvasH;
        public double CanvasW;
        public int ImgRealH;
        public int ImgRealW;

        public double[] StrImgPos;
        public double[] StrCanvPos;
        public double[] EndImgPos;
        public double[] EndCanvPos;

        public int ChipHNum;
        public int ChipWNum;

        // Size Unit = um //
        public double ChipHSize;
        public double ChipWSize;

        /* Img Processing Parameter */
        public int ThresholdV;
        public int UPAreaLimit;
        public int DWAreaLimit;
        public int UPBoxLimit { get { return (int)(ChipHSize*ChipWSize); } }
        public int DWBoxLimit = 1;

        public void SetFrame( double hc , double wc ,int hi,int wi )
        {
            CanvasH  = hc;
            CanvasW  = wc;
            ImgRealH = hi;
            ImgRealW = wi;
        }
    }
}
