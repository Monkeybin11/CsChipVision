using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using Emgu.CV.Util;
using LedChipPassFail_first.Data;
using LedChipPassFail_first.Func;
using System.Diagnostics;
using Accord.Math.Metrics;
using static LanguageExt.Prelude;

namespace LedChipPassFail_first
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        MainCore Core = new MainCore();
        HistogramBox HistoBox;
        DenseHistogram[] HistogramList;
        WindowsFormsHost WinHost;
        public readonly int BoxLimtiAreaUP = 20;
        public readonly int BoxLimtiAreaDW = 3;
        bool ProcessDone = false;

        #region Init
        public MainWindow()
        {
            InitializeComponent();
            Core.InitClass();
            //InitControlsmall();
            //InitControlbig();
            InitControlTestSample();
        }

        // init //
        void InitControlsmall()
        {
            nudImgHeight.Value = 54;
            nudImgWidth.Value = 54;
            nudCWNum.Value = 9;
            nudCHNum.Value = 5;
            nudCWSize.Value = 4;
            nudCHSize.Value = 3;
            nudThresh.Value = 120;
            nudAreaUpLimit.Value = 109;
            nudAreaDWLimit.Value = 5;
        }
        void InitControlbig()
        {
            nudImgHeight.Value = 218;
            nudImgWidth.Value = 186;
            nudCWNum.Value = 30;
            nudCHNum.Value = 20;
            nudCWSize.Value = 4;
            nudCHSize.Value = 3;
            nudThresh.Value = 80;
            nudAreaUpLimit.Value = 109;
            nudAreaDWLimit.Value = 5;
        }
        void InitControlTestSample()
        {
            nudImgHeight.Value = 310;
            nudImgWidth.Value = 310;
            nudCWNum.Value = 50;
            nudCHNum.Value = 28;
            nudCWSize.Value = 4;
            nudCHSize.Value = 3;
            nudThresh.Value = 40;
            nudAreaUpLimit.Value = 109;
            nudAreaDWLimit.Value = 5;
        }
        void ClearLRFrame()
        {
            canvasLT.Children.Clear();
            canvasRB.Children.Clear();
            canvasProced.Children.Clear();
        }
       
        void SetInitImg( Image<Gray , byte> img , Canvas origin , Canvas Pro , Canvas Lt , Canvas Rb )
        {
            List<Rectangle> rectList = new List<Rectangle>();

            /*Canvas Setting*/
            double[] canvXYLen = Core.MapImg2Canv( new int[2] { 35 , 35 } );

            for ( int i = 0 ; i < 2 ; i++ )
            {
                rectList.Add( new Rectangle() );
                rectList[i].Width = canvXYLen[0];
                rectList[i].Height = canvXYLen[1];
                rectList[i].StrokeThickness = 2;
                rectList[i].Stroke = new SolidColorBrush( Colors.BlueViolet );
            }

            Canvas.SetLeft( rectList[0] , 0 );
            Canvas.SetTop( rectList[0] , 0 );
            Canvas.SetRight( rectList[1] , 0 );
            Canvas.SetBottom( rectList[1] , 0 );

            /*image setting*/
            RenderOptions.SetBitmapScalingMode( origin , BitmapScalingMode.NearestNeighbor );
            RenderOptions.SetBitmapScalingMode( Pro , BitmapScalingMode.NearestNeighbor );
            RenderOptions.SetBitmapScalingMode( Lt , BitmapScalingMode.NearestNeighbor );
            RenderOptions.SetBitmapScalingMode( Rb , BitmapScalingMode.NearestNeighbor );

            imgOri.ImageSource = BitmapSourceConvert.ToBitmapSource( Core.OriginImg );
            imgLT.ImageSource = BitmapSourceConvert.ToBitmapSource( Core.CropImgLT( Core.OriginImg ) );
            imgRB.ImageSource = BitmapSourceConvert.ToBitmapSource( Core.CropImgRB( Core.OriginImg ) );

            /*Event*/
            Lt.MouseLeftButtonUp += new MouseButtonEventHandler( LTClickEvt );
            Rb.MouseLeftButtonUp += new MouseButtonEventHandler( RBClickEvt );

            /*att */
            origin.Children.Add( rectList[0] );
            origin.Children.Add( rectList[1] );
        }
        void LTClickEvt( object ob , MouseButtonEventArgs ev )
        {
            while ( canvasLT.Children.Count > 0 ) { canvasLT.Children.RemoveAt( canvasLT.Children.Count - 1 ); }

            double px = ev.GetPosition( this.canvasLT ).X - 4   ;
            double py = ev.GetPosition( this.canvasLT ).Y - 4   ;

            Core.PData.StrImgPos = Core.MapCanv2ImgLTRB( new double[2] { px , py } );
            Core.PData.StrCanvPos = new double[2] { px , py };
            Rectangle rect =  StartEndDot(px-4,py-4);
            canvasLT.Children.Add( rect );
        }
        void RBClickEvt( object ob , MouseButtonEventArgs ev )
        {
            while ( canvasRB.Children.Count > 0 ) { canvasRB.Children.RemoveAt( canvasRB.Children.Count - 1 ); }

            double px = ev.GetPosition( this.canvasRB ).X ;
            double py = ev.GetPosition( this.canvasRB ).Y ;
            double[] endtemp = Core.MapCanv2ImgLTRB( new double[2] { px,py } );

            Core.PData.EndImgPos = new double[2] { endtemp[0] - Core.LTRBPixelNumberW + Core.OriginImg.Width , endtemp[1] - Core.LTRBPixelNumberH + Core.OriginImg.Height };
            Core.PData.EndCanvPos = new double[2] { px , py };
            Rectangle rect =  StartEndDot(px-4,py-4);
            canvasRB.Children.Add( rect );
        }
        Rectangle StartEndDot( double px , double py )
        {
            Rectangle rect = new Rectangle();
            rect.Width = 10;
            rect.Height = 10;
            rect.StrokeThickness = 2;
            rect.Fill = new SolidColorBrush( Colors.OrangeRed );
            rect.Stroke = new SolidColorBrush( Colors.OrangeRed );
            Canvas.SetLeft( rect , px );
            Canvas.SetTop( rect , py );
            return rect;
        }
        #endregion


        #region MainFunction Button Evt

        private async void btnLoad_Click( object sender , RoutedEventArgs e )
        {
            ClearLRFrame();

            OpenFileDialog ofd = new OpenFileDialog();
            if ( ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                TestFileSavePath.Setting(ofd.FileName);
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                //await Task.Run( () => {
                //    Mat graymat  = new Mat(ofd.FileName,LoadImageType.Grayscale);
                //    Mat colormat = new Mat(ofd.FileName,LoadImageType.Color);
                //
                //    Core.OriginImg   = graymat.ToImage<Gray,byte>( false );
                //    Core.ColorOriImg = colormat.ToImage<Bgr , byte>( false );
                //} );
                Mat graymat  = new Mat(ofd.FileName,LoadImageType.Grayscale);
                Mat colormat = new Mat(ofd.FileName,LoadImageType.Color);

                Core.OriginImg = graymat.ToImage<Gray , byte>( false );
                Core.ColorOriImg = colormat.ToImage<Bgr , byte>( false );

                Core.InitFunc(canvas);
                SetInitImg( Core.OriginImg , canvas , canvasProced , canvasLT , canvasRB );
                Core.PData.SetFrame(canvas.ActualHeight,canvas.ActualWidth,Core.OriginImg.Height,Core.OriginImg.Width);
                Mouse.OverrideCursor = null;
            }
        }
        void AfterLoad()
        {
            btnStartProcssing.IsEnabled = false;
        }
        private async void btnStartProcssing_Click( object sender , RoutedEventArgs e )
        {
            ReadyProc();
            //await PorcessingStep1();
            PorcessingStep1();
            Console.WriteLine( "TaskOut" );
            DisplayResultHisto(Core.PResult);
        }
        private void btnSaveData_Click( object sender , RoutedEventArgs e )
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if ( sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK )
            {
                Core.SaveData( Core.PResult , sfd.FileName + ".csv" );
            }
        }
        private void btnSaveImg_Click( object sender , RoutedEventArgs e )
        {
            try
            {

                SaveFileDialog sfd = new SaveFileDialog();
                if ( sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK )
                {
                    var resizedimg = Core.IndexViewImg.Resize( Core.ProcedImg.Width , Core.ProcedImg.Height , Inter.Nearest );
                    Core.SaveImg( Core.IndexViewImg , sfd.FileName + "_OverView_Point2Chip.png" );
                    Core.SaveImg( resizedimg , sfd.FileName + "_OverView_SameSize.png" );
                    Core.SaveImg( Core.ProcedImg , sfd.FileName + "_Proced.png" );

                    HistogramList[0]?.Save( sfd.FileName + "_Histogram1.png" );
                    HistogramList[1]?.Save( sfd.FileName + "_Histogram2.png" );
                }

            }
            catch ( Exception )
            {
            }
        }

        #endregion



        #region Process Ready
        void ReadyProc()
        {
            if ( Core.PData.EndImgPos == null || Core.PData.StrImgPos == null )
            {
                System.Windows.Forms.MessageBox.Show( "Set First and Last Chip Position First" );
                return;
            }
            SetProcessingData();
            ChangeFront2ImgProcStep();
            
        }
        
        void SetProcessingData()
        {
            Core.PData.ImgRealH = Core.OriginImg.Height;
            Core.PData.ImgRealW = Core.OriginImg.Width;
            Core.PData.CanvasH = ( int ) canvas.ActualHeight;
            Core.PData.CanvasW = ( int ) canvas.ActualWidth;

            Core.PData.ChipWNum = ( int ) nudCWNum.Value;
            Core.PData.ChipHNum = ( int ) nudCHNum.Value;
            Core.PData.ChipWSize = ( int ) (nudCWSize.Value/2.33333);
            Core.PData.ChipHSize = ( int ) (nudCHSize.Value/2.33333);

            Core.PData.ThresholdV = ( int ) nudThresh.Value;
            Core.PData.UPAreaLimit = ( int ) ( nudAreaUpLimit.Value / Math.Pow( 2.33333 , 2 ) );
            Core.PData.DWAreaLimit = ( int ) ( nudAreaDWLimit.Value / Math.Pow( 2.33333 , 2 ) );
        }
        void ChangeFront2ImgProcStep()
        {
            

            btnStartProcssing.IsEnabled = true;
            Removeevent( canvasLT , canvasRB );
            ClearLRFrame();
            while ( canvas.Children.Count > 0 ) { canvas.Children.RemoveAt( canvas.Children.Count - 1 ); } // delect rect
            titleRB.Text = "Histogram";
            titleLT.Text = "Indexing View";

            Core.EstedChipPos = ImgPFunc.FnCreateEstedChipPos(  ( Core.PData.EndImgPos[1] - Core.PData.StrImgPos[1] ), 
                                                                ( Core.PData.EndImgPos[0] - Core.PData.StrImgPos[0] ),  
                                                                  Core.PData.StrImgPos[1] , Core.PData.StrImgPos[0] );
            Core.IndexViewImg = new Image<Bgr , byte>( Core.PData.ChipWNum , Core.PData.ChipHNum );
            Core.IndexViewImg.Data = MatPattern( Core.PData.ChipHNum , Core.PData.ChipWNum , 3 );
            imgLT.ImageSource = BitmapSourceConvert.ToBitmapSource( Core.IndexViewImg );
            imgRB.ImageSource = null;


            //var passfun = curry(HistoFromImage(Core.BinSize))(Core.OriginImg); // Passing Form

            WinHost = CreateWinHost(canvasLT);
            HistoBox = new HistogramBox();
            canvasRB.Children.Clear();
            WinHost.Child = HistoBox;
            canvasRB.Children.Add( WinHost );
        }
        void CreateFuncofProc()
        {
            double thres =  Core.PData.ThresholdV ;
            double areaup = Core.PData.UPAreaLimit;
            double areadw = Core.PData.DWAreaLimit;
            double cHnum =  Core.PData.ChipHNum   ;
            double cWnum =  Core.PData.ChipWNum   ;
            ThresholdMode mode = ckbThresMode.IsChecked.Value ? ThresholdMode.Auto : ThresholdMode.Manual;
            Core.SumAreaPoint    = ImgPFunc.FnSumAreaPoint( (int)Core.PData.ChipWSize , ( int ) Core.PData.ChipHSize , Core.OriginImg );
            Core.FindPassContour = ImgPFunc.FnFindPassContour( thres , areaup , areadw , mode );
            Core.ApplyBox        = ImgPFunc.FnApplyBox( Core.PData.UPBoxLimit , Core.PData.DWBoxLimit );
        }
        
        System.Drawing.Rectangle CenterDotForDrawing( double px , double py )
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle();
            rect.Width = 2;
            rect.Height = 2;
            return rect;
        }
        #endregion

        #region After Setting Function
        void PorcessingStep1()
        {
            CreateFuncofProc();

            //return Task.Run( () =>
            //{
                try
                {
                    this.BeginInvoke( ()=>Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait );

                    var temp = Core.PResult;

                    Core.PResult = new ImgPResult();

                        /* For Simple Notation */
                    double cHnum =  Core.PData.ChipHNum   ;
                    double cWnum =  Core.PData.ChipWNum   ;
                    Image<Bgr,byte> targetimg = null;

                        /* Create Data */
                    byte[,,] failchipDisplayData = MatPattern((int)cHnum, (int)cWnum , 3);
                    byte[,,] passfailPosData     = new byte[(int)cHnum, (int)cWnum , 1];
                    double[,,] estedChipP        = Core.EstedChipPos( cHnum, cWnum );
                    var passContours  = Core.FindPassContour(Core.OriginImg);
                    Core.passChipList = new List<System.Drawing.PointF>();
                    Core.failChipList = new List<System.Drawing.PointF>();
                    targetimg = Core.ColorOriImg.Clone();

                    /* Draw Contour and Save */
                    //targetimg = DrawContour( targetimg , passContours );
                    //targetimg.Save( TestFileSavePath.ContourName );

                    passContours = ImgPFunc.FnSortcontours( passContours )();
                    var boxlist  = Core.ApplyBox(passContours);
                    targetimg = DrawBox( targetimg , boxlist );
                    //targetimg.Save( TestFileSavePath.BoxName );

                    // Draw EstedPoint on Image and Cavnas 
                    targetimg = DrawCenterPoint( targetimg , estedChipP );

                    #region check pass fail
                    var boximg = Core.ColorOriImg.Clone();
                    for ( int j = 0 ; j < estedChipP.GetLength( 1 ) ; j++ ) // col
                    {
                        for ( int i = 0 ; i < estedChipP.GetLength( 0 ) ; i++ ) // row
                        {
                            bool isFail = true;
                            for ( int k = 0 ; k < boxlist.Count ; k++ )
                            {
                                var inbox = ImgPFunc.FnInBox(boxlist[k],1);

                                if ( inbox( estedChipP[i , j , 0] , estedChipP[i , j , 1] ) )
                                {
                                    Core.PResult.OutData.Add( new ExResult( j , i , true , Core.SumBox( boxlist[k] ) , CvInvoke.ContourArea( passContours[k] ) ) );
                                    Core.passChipList.Add( new System.Drawing.PointF( ( float ) estedChipP[i , j , 0] , ( float ) estedChipP[i , j , 1] ) );
                                    isFail = false;
                                    break;
                                }
                                if ( isFail )
                                {
                                    double failboxInten = Core.SumAreaPoint( (int)estedChipP[i , j , 0] ,  (int)estedChipP[i , j , 1]);
                                    Core.PResult.OutData.Add( new ExResult( j , i , false , failboxInten , 0 ) );
                                    Core.failChipList.Add( new System.Drawing.PointF( ( float ) estedChipP[i , j , 0] , ( float ) estedChipP[i , j , 1] ) );
                                    failchipDisplayData[i , j , 0] = ( byte ) ( failchipDisplayData[i , j , 0] * 0.3 );
                                    failchipDisplayData[i , j , 1] = ( byte ) ( failchipDisplayData[i , j , 1] * 0.5 );
                                    failchipDisplayData[i , j , 2] = 200;
                                }
                            }
                        }
                        Core.IndexViewImg.Data = failchipDisplayData;
                        Core.ProcedImg = targetimg;

                        lblPassChipnum.BeginInvoke( () => lblPassChipnum.Content = Core.passChipList.Count() );
                        lblFailChipnum.BeginInvoke( () => lblFailChipnum.Content = Core.failChipList.Count() );

                        Core.PResult.ChipPassCount = Core.passChipList.Count();
                        Core.PResult.ChipFailCount = Core.failChipList.Count();

                        #endregion

                        this.BeginInvoke( () =>
                        {
                            imgPro.ImageSource = BitmapSourceConvert.ToBitmapSource( targetimg );
                            imgLT.ImageSource = BitmapSourceConvert.ToBitmapSource( Core.IndexViewImg );
                            Mouse.OverrideCursor = null;
                        } );
                    }
                }
                catch ( Exception er )
                {
                    System.Windows.Forms.MessageBox.Show( er.ToString() );
                }
            //} );
        }

        Image<Bgr,byte> DrawContour(Image<Bgr,byte> img,VectorOfVectorOfPoint contr) {
            for ( int i = 0 ; i < contr.Size ; i++ )
            {
                CvInvoke.DrawContours( img , contr , i , new MCvScalar( 0 , 255 , 0 ) );
            }
            return img;
        }

        Image<Bgr , byte> DrawCenterPoint( Image<Bgr , byte> img , double[,,] centrPoint )
        {
            Parallel.For( 0 , centrPoint.GetLength( 0 ) , i =>
            {
                Parallel.For( 0 , centrPoint.GetLength( 1 ) , j =>
                {
                    img.Data[( int ) centrPoint[i , j , 1] , ( int ) centrPoint[i , j , 0] , 0] = 0;
                    img.Data[( int ) centrPoint[i , j , 1] , ( int ) centrPoint[i , j , 0] , 1] = 0;
                    img.Data[( int ) centrPoint[i , j , 1] , ( int ) centrPoint[i , j , 0] , 2] = 255;
                } );
            } );
            return img;
        }

        Image<Bgr , byte> DrawBox( Image<Bgr , byte> img , List<System.Drawing.Rectangle> rclist )
        {
            Parallel.For( 0 , rclist.Count , i =>
            {
                img.Draw( rclist[i] , new Bgr( 40 , 165 , 5 ) , 1 );
            } );
            return img;
        }

        void Removeevent(Canvas lt,Canvas rb)
        {
            lt.MouseLeftButtonUp -= (MouseButtonEventHandler)LTClickEvt;
            rb.MouseLeftButtonUp -= (MouseButtonEventHandler)RBClickEvt;
        }

        byte[,,] MatZeros( int channal1 , int channal2 , int channal3 )
        {
            byte[,,] output = new byte[channal1,channal2,channal3];
            for ( int i = 0 ; i < channal1 ; i++ )
            {
                for ( int j = 0 ; j < channal2 ; j++ )
                {
                    for ( int k = 0 ; k < channal3 ; k++ )
                    {
                        output[i , j , k] = 150;
                    }
                }
            }
            return output;
        }

        byte[,,] MatPattern( int channal1 , int channal2 , int channal3 )
        {
            byte[,,] output = new byte[channal1,channal2,channal3];

            Parallel.For( 0 , channal1 , i => {
                Parallel.For( 0 , channal2 , j => {

                    if ( i % 2 == 0 ) {
                        if ( j % 2 == 0 )
                        {
                            output[i , j , 0] = 250;
                            output[i , j , 1] = 250;
                            output[i , j , 2] = 250;
                        }
                        else
                        {
                            output[i , j , 0] = 150;
                            output[i , j , 1] = 150;
                            output[i , j , 2] = 150;
                        }
                    }
                    else if ( j%2 == 0) {
                        output[i , j , 0] = 200;
                        output[i , j , 1] = 200;
                        output[i , j , 2] = 200;
                    }
                    else
                    {
                        output[i , j , 0] = 100;
                        output[i , j , 1] = 100;
                        output[i , j , 2] = 100;
                    }

                } );
            } );

            
            return output;
        }
        #endregion



        #region Histogram

        void DisplayResultHisto(ImgPResult data)
        {
            try
            {
                HistoBox.ClearHistogram();
                var passfun = HistoFromResult(data); // Passing Form
                AddHist2Box( HistoBox , HistogramList , passfun );
                HistoBox.Refresh();
                WinHost.Child = HistoBox;
                canvasRB.Children.Clear();
                canvasRB.Children.Add( WinHost );
            }
            catch ( Exception )
            {
            }
        }

        void AddHist2Box( HistogramBox box , DenseHistogram[] histogramArr,dynamic createhist)
        {
            histogramArr = createhist();
            float histmax = (float)(Core.PData.ChipHSize*Core.PData.ChipWSize*255);
            var temp = HistogramList;
            for ( int i = 0 ; i < histogramArr.GetLength(0) ; i++ )
            {
                if ( histogramArr[i] != null ) {
                    box.AddHistogram( i == 0?"Intensity":"Size" , System.Drawing.Color.Black , histogramArr[i] , Core.BinSize , new float[] {0,histmax} );
                }
            }
        }

        #region Helper
        WindowsFormsHost CreateWinHost( Canvas targcanv )
        {
            WindowsFormsHost wh = new WindowsFormsHost();
            wh.Width = targcanv.Width;
            wh.Height = targcanv.Height;
            wh.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            wh.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            return wh;
        }

        DenseHistogram CreateHisto( ImgPResult result , Func<List<ExResult> , int[]> func )
        {
            List<int> temp = new List<int>();
            var item = func(result.OutData);

            DenseHistogram hist = new DenseHistogram(20,new RangeF((float)item.Min(),(float)item.Max()));
            Matrix<float> farr = new Matrix<float>(1,item.GetLength(0));
            for ( int i = 0 ; i < item.GetLength( 0 ) ; i++ )
            {
                farr.Data[0 , i] = item[i];
            }
            Matrix<float>[] histData = new Matrix<float>[1] { farr };
            hist.Calculate( histData , true , null );
            return hist;
        }

        Func<Image<Gray , byte> , float , float , DenseHistogram[]> HistoFromImage( int binsize )
        {
            var fromimg = fun( ( Image<Gray , byte> img  , float dw , float up ) =>
            {
                DenseHistogram[] hist = new DenseHistogram[] { };
                hist = new DenseHistogram[1];
                hist[0] = new DenseHistogram( binsize , new RangeF( dw , up ) );
                hist[0].Calculate<byte>( new Image<Gray , byte>[] { img } , true , null );
                return hist;
            } );
            return fromimg;
        }

        Func<DenseHistogram[]> HistoFromResult( ImgPResult result )
        {
            var fromresult = fun(()=>
            {
                var item = result.OutData.Select( i => ( int ) i.Intensity ).ToArray();
                DenseHistogram histIntes = CreateHisto(Core.PResult, new Func<List<ExResult>,int[]>( j => j.Select(i => ( int ) i.Intensity).ToArray() ));
                DenseHistogram histSize  = CreateHisto(Core.PResult, new Func<List<ExResult>,int[]>( j => j.Select(i => ( int ) i.ContourSize).ToArray() ));
                return new DenseHistogram[2] { histIntes , histSize };
            } );
            return fromresult;
        }

        
        #endregion
        
        void RefreshHistogram()
        {
            try
            {
                if ( !ProcessDone && HistogramList != null )
                {
                   //var passfun = curry(HistoFromImage(Core.BinSize))(Core.OriginImg);
                   //
                   //HistoBox.ClearHistogram();
                   //AddHist2Box( HistoBox , HistogramList, passfun ,
                   //        ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistDW.Text ) : 0 ,
                   //        ( bool ) ckbSetHistRange.IsChecked ? float.Parse( nudHistUP.Text ) : 255 );

                }
            }
            catch ( Exception )
            {
                System.Windows.Forms.MessageBox.Show( "Please Input only Number on Histogram Range" );
            }
        }
        #endregion
    }
}
