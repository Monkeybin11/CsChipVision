using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using Emgu.CV.Util;
using LedChipPassFail_first.Data;
using LedChipPassFail_first.Func;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO;
using System.Runtime.Serialization.Json;

namespace LedChipPassFail_first
{
    public class MainCore
    {
        #region Data 
        public ImgPData PData;
        public ImgPResult PResult;
        public Image<Gray,byte> OriginImg; 
        public Image<Bgr,byte> ColorOriImg; 
        public Image<Bgr,byte> ProcedImg; 
        public Image<Bgr,byte> IndexViewImg; 
        public double zoomMax = 20;
        public double zoomMin = 0.2;
        public double zoomSpeed = 0.001;
        public double zoom = 1;
        public readonly double LTRBPixelNumberW = 35;
        public readonly double LTRBPixelNumberH = 35;
        
        public readonly float HistogramDwRange = 46;
        public readonly int BinSize = 100;
        public List<System.Drawing.PointF> passChipList;
        public List<System.Drawing.PointF> failChipList;
        #endregion 
       
        #region Functions
        /*Global Function*/
        public Action<int,double,double,Canvas> DoZoom;
        public Func<double[],double[]> MapCanv2Img;
        public Func<double[],double[]> MapCanv2ImgLTRB;
        public Func<int[],double[]>    MapImg2Canv;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgLT;
        public Func<Image<Gray,byte>, Image<Gray , byte>> CropImgRB;
        public Func<System.Drawing.Rectangle,double> SumBox;
        public Func<int,int,double> SumAreaPoint;
        /*Local Function*/
        public Func<double, double, double[,,]> EstedChipPos;
        public Func<Image<Gray,byte>,VectorOfVectorOfPoint> FindPassContour;
        public Func<System.Drawing.PointF , double> InContour;
        public Func<VectorOfVectorOfPoint> Sortcontours;
        public Func<VectorOfVectorOfPoint , List<System.Drawing.Rectangle>> ApplyBox;

        #endregion

        #region Init
        public MainCore()
        {
            PData = new ImgPData();
            PResult = new ImgPResult();
        }

        public void InitClass()
        {
            PData = new ImgPData();
            PResult = new ImgPResult();
        }

        public void InitFunc(Canvas canvas)
        {
            DoZoom          = ImgPFunc.FnActZoom( zoom , zoomMax , zoomMin , zoomSpeed );
            CropImgLT       = ImgPFunc.FnCropImg( 0 , 0 , 35 , 35 );
            CropImgRB       = ImgPFunc.FnCropImg( OriginImg.Width - 35 , OriginImg.Height - 35 , OriginImg.Width , OriginImg.Height );
            MapCanv2Img     = ImgPFunc.FnMapCanv2Img( new double[2] { canvas.Width , canvas.Height } ,
                                                      new int[2] { OriginImg.Width , OriginImg.Height } );
            MapImg2Canv     = ImgPFunc.FnMapImg2Canv( new double[2] { canvas.Width , canvas.Height } ,
                                                      new int[2] { OriginImg.Width , OriginImg.Height } );
            MapCanv2ImgLTRB = ImgPFunc.FnMapCanv2Img( new double[2] { canvas.Width , canvas.Height } ,
                                                      new int[2] { ( int ) LTRBPixelNumberW , ( int ) LTRBPixelNumberH } );
            SumBox          = ImgPFunc.FnSumBox( OriginImg );
            
        }
        #endregion


        #region Save & Load

        public void SaveImg ( Image<Bgr,byte> img , string path )
        {
            img.Save( path );
        }

        public void SaveImg( Image<Gray , byte> img , string path )
        {
            img.Save( path );
        }

        public void SaveData( ImgPResult result , string path) {
            string delimiter = ",";
            StringBuilder csvExport = new StringBuilder(); //
            csvExport.Append( "Pass Number" );
            csvExport.Append( delimiter );
            csvExport.Append( PResult.ChipPassCount.ToString() );
            csvExport.Append( delimiter );
            csvExport.Append( "Fail Number" );
            csvExport.Append( delimiter );
            csvExport.Append( PResult.ChipFailCount.ToString() );
            csvExport.Append( Environment.NewLine );
            csvExport.Append( "Y (Row) " );
            csvExport.Append( delimiter );
            csvExport.Append( "X (Column)" );
            csvExport.Append( delimiter );
            csvExport.Append( "Pass/Fail)" );
            csvExport.Append( delimiter );
            csvExport.Append( "Size" );
            csvExport.Append( delimiter );
            csvExport.Append( "Intensity" );
            csvExport.Append( delimiter );
            csvExport.Append( Environment.NewLine );

            for ( int i = 0 ; i < result.OutData.Count ; i++ )
            {
                csvExport.Append( result.OutData[i].Hindex+1);
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].Windex+1 );
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].PassFail? "Pass":"Fail" );
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].ContourSize );
                csvExport.Append( delimiter );
                csvExport.Append( result.OutData[i].Intensity);
                csvExport.Append( Environment.NewLine );
            }
            System.IO.File.WriteAllText( path , csvExport.ToString() );
        }
        #endregion

        //public void SaveConfig(ConfigData data)
        //{
        //    MemoryStream strm = new MemoryStream();
        //    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ConfigData));
        //    ser.WriteObject( strm , data );
        //
        //
        //
        //}




    }
}
