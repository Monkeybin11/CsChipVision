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
using Emgu.CV.Util;
using System.Windows.Controls;
using System.Windows.Media;
using System.Drawing;


namespace LedChipPassFail_first.Func
{
    public enum ThresholdMode
    {
        Auto,
        Manual
    }

    public static class ImgPFunc
    {
         /* can partial application */
        public static Dictionary<string , Func<dynamic>> ReaderDict( System.IO.Stream stream )
        {
            Dictionary < string, Func <dynamic>> output = new Dictionary<string, Func< dynamic>>();

            Func <byte[],byte[]> readbytearr =  input  =>
            {
                stream.Read(input,0,input.Length);
                return input;
            };

            Func <dynamic> ToByte     = () => (byte)stream.ReadByte();
            Func <dynamic> ToInt      = () => BitConverter.ToInt32 ( readbytearr(new byte[4]),0 );
            Func <dynamic> ToLong     = () => BitConverter.ToInt64 ( readbytearr(new byte[8]),0 );
            Func <dynamic> ToFloat    = () => BitConverter.ToSingle( readbytearr(new byte[4]),0 );
            //Func <dynamic> CutHeader  = () => readbytearr(new byte[],0); 
            byte[] tempman = new byte[2];

            output.Add( "byte" , ToByte );
            output.Add( "int" , ToInt );
            output.Add( "long" , ToLong );
            output.Add( "float" , ToFloat );

            return output;
        }

        public static Action<int , double , double , Canvas> FnActZoom( double zoomin , double zoomMax , double zoomMin , double zoomspeed )
        {
            var output = act(  (int delta ,double x, double y, Canvas canvas) =>
            {
                double zoom = zoomin + zoomspeed * delta;
                if ( zoom < zoomMin ) { zoom = zoomMin; }
                if ( zoom > zoomMax ) { zoom = zoomMax; }
                if ( zoom > 1 ){canvas.RenderTransform = new ScaleTransform( zoom , zoom , x , y ); }
                else{canvas.RenderTransform = new ScaleTransform( zoom , zoom ); }
            });
            return output;
        }

        public static Func<int[] , double[]> FnCalcRealPos( int[] canvasL , int[] realImgL )
        {
            // canvasL , realImgL = [ Len(W) , Len(H) ] 
            var calcPos = fun(( int[] pos )=> {
                var result = from c in canvasL
                             select from r in realImgL
                                    select (double)r/(double)c;
                return result.Cast<double>().ToArray();
            } );
            return calcPos;
        }
        
        public static Func<int[],double[]> FnMapImg2Canv( double[] canvasL , int[] realImgL )
        {
            // canvasL , realImgL = [ Len(W) , Len(H) ] 
            var mapCanv2Img = fun((int[] imgPoint)=> {
                return new double[2]{ ( (double)imgPoint[0] * canvasL[0] )/(double)realImgL[0] ,
                                      ( (double)imgPoint[1] * canvasL[1] )/(double)realImgL[1] };
            } );
            return mapCanv2Img;
        }

        public static Func<double[] , double[]> FnMapCanv2Img( double[] canvasL , int[] realImgL )
        {
            // canvasL , realImgL = [ Len(W) , Len(H) ] 
            var mapImg2Canv = fun((double[] canvPoint)=> {
                return new double[2]{ (canvPoint[0] * realImgL[0] )/canvasL[0] ,
                                      (canvPoint[1] * realImgL[1] )/canvasL[1] };
            } );
            return mapImg2Canv;
        }

        public static Func<Image<Gray , byte> , Image<Gray , byte>> FnCropImg( int xS , int yS , int width , int height )
        {
            var cropimg = fun( (Image<Gray , byte> img)=> {
                img.ROI = new System.Drawing.Rectangle( xS,yS,width,height );
                var tempimg = img.Copy();
                img.ROI = System.Drawing.Rectangle.Empty;
                return tempimg;
            } );
            return cropimg;
        }

        public static Func<double, double, double[,,]> FnCreateEstedChipPos(double realImgH, double realImgW , double hoffset , double woffset )
        {
            var createEsted = fun( (double hChipN,double wChipN) => {
                double[,,] output = new double[(int)hChipN , (int)wChipN,2];
                for (int j = 0; j < hChipN; j++)
                {
                    for (int i = 0; i < wChipN; i++)
                    {
                        output[j,i,0] = realImgW / (wChipN-1) * i + woffset;
                        output[j,i,1] = realImgH / (hChipN-1) * j + hoffset;
                    }
                }
                return output;
            } );
            return createEsted;
        }
       
        public static Func<System.Drawing.PointF,double> FnInContour(VectorOfPoint contour )
        {
            var incontour = fun( (System.Drawing.PointF pt) =>
            {
                float ceilX = (float)Math.Ceiling( (double) pt.X);
                float ceilY = (float)Math.Ceiling( (double) pt.Y);
                float trunX = (float)Math.Truncate( (double) pt.X);
                float trunY = (float)Math.Truncate( (double) pt.Y);

                List<double> outlist = new List<double>();
                System.Drawing.PointF[] ptArr = new System.Drawing.PointF[4];

                ptArr[0] = new System.Drawing.PointF(ceilX,ceilY);
                ptArr[1] = new System.Drawing.PointF(trunX,ceilY);
                ptArr[2] = new System.Drawing.PointF(ceilX,trunY);
                ptArr[3] = new System.Drawing.PointF(trunX,trunY);

                for (int i = 0; i < ptArr.GetLength(0) ; i++)
                {
                    outlist.Add(CvInvoke.PointPolygonTest( contour , pt, false));
                }
                return outlist.Max();
            } );
            return incontour;
        } 

        public static Func<Image<Gray,byte>,VectorOfVectorOfPoint> FnFindPassContour(double threshold,double areaUP,double areaDW, ThresholdMode mode )
        {
            var findpasscntr = fun((Image<Gray,byte> imgori) => {
                var thresedimg = mode == ThresholdMode.Auto ? imgori.ThresholdAdaptive(new Gray(255),AdaptiveThresholdType.MeanC,ThresholdType.Binary,13,new Gray(3))
                                                            : imgori.ThresholdBinary(new Gray(threshold),new Gray(255));
                //var thresedimg = imgori.ThresholdBinary(new Gray(threshold),new Gray(255));
                thresedimg.Save(@"D:\1612vision\Lg\LGSample_blue\Mapping_Image\testImage\Lt\Threshold.png");
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                VectorOfVectorOfPoint passcontours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours( thresedimg , contours , null , RetrType.List , ChainApproxMethod.ChainApproxNone );

                for ( int i = 0 ; i < contours.Size ; i++ )
                {
                    double areaSize = CvInvoke.ContourArea( contours[i],false);  //  Find the area of contour
                    if ( areaSize >= areaDW && areaSize <= areaUP )
                    {
                        passcontours.Push( contours[i] );
                    }
                }
                return passcontours;
            } );
            return findpasscntr;
        }

        public static Func<double , double , bool> FnInBox( Rectangle box,int margin ) {
            var inbox = fun((double x,double y)=> {
                if(x > box.X + box.Width + margin || x < box.X -margin || y > box.Y + box.Height + margin || y < box.Y - margin ) {
                    return false; }
                return true;
            } );
            return inbox;
        }

        public static Func<Rectangle,double> FnSumBox( Image<Gray , byte> src ) {
            var sumbox = fun((Rectangle box)=>
            {
                double sum = 0;
                for (int i = box.X; i < box.X + box.Width; i++)
                {
                    for (int j = box.Y; j < box.Y + box.Height; j++)
                    {
                        sum += src.Data[j,i,0];
                    }
                }
                return sum;
            } );
            return sumbox;
        }

        public static Func<VectorOfVectorOfPoint> FnSortcontours( VectorOfVectorOfPoint inputContours )
        {
            var sort = fun(()=>
            {
                var temp = inputContours.ToArrayOfArray();
                var sorted = temp.OrderBy( p => p[0].Y ).ThenBy( p => p[0].X ).ToArray();
                return new VectorOfVectorOfPoint( sorted );
            } );
            return sort;
        }

        public static Func<VectorOfVectorOfPoint , List<Rectangle>> FnApplyBox(int upLimit,int dwLimit ) {
            var applybox = fun((VectorOfVectorOfPoint contr)=>
            {
                List<System.Drawing.Rectangle> PassBoxArr = new List<System.Drawing.Rectangle>();
                for ( int i = 0 ; i < contr.Size ; i++ )
                {
                    System.Drawing.Rectangle rc = CvInvoke.BoundingRectangle(contr[i]);
                    PassBoxArr.Add( rc );
                    //if ( rc.Width * rc.Height <= upLimit && rc.Width * rc.Height >= dwLimit )
                    //{
                    //    PassBoxArr.Add( rc ); // box pass
                    //}
                    //else
                    //{
                    //    PassBoxArr.Add( rc );
                    //}
                }
                return PassBoxArr;
            } );
            return applybox;
        }

        public static Func<int , int , double> FnSumAreaPoint( int width,int height , Image<Gray,byte> img ) {
            var sumareap = fun((int x, int y)=>
            {
                double output = 0;
                for (int i = x; i < x+width; i++)
                {
                    for (int j = y; j < y+height; j++)
                    {
                        output += img.Data[ j, i, 0 ];
                    }
                }
                return output;
            } );
            return sumareap;
        }
    }
}
