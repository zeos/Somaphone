using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using VVVV.Hosting;
using VVVV.Utils.VMath;
using VVVV.Core.Collections;
using VVVV.Core.Logging;
using libpxcclr;
using libpxcclr.cs;

using SlimDX.Direct3D11;
using SlimDX;

using VVVV.DX11;
using VVVV.DX11.Lib;
using FeralTic.DX11.Resources;
using FeralTic.DX11;

namespace VVVV.Nodes.RSSDK
{
    public class RSSDKImage
    {
        public PXCMImage Source;
        public byte[] Data;
        public int Width = -1;
        public int Height = -1;
        public PXCMCapture.StreamType Type = PXCMCapture.StreamType.STREAM_TYPE_ANY;
        public PXCMImage.PixelFormat Format = PXCMImage.PixelFormat.PIXEL_FORMAT_ANY;

        public RSSDKImage()
        {
            //
        }
    }

    [PluginInfo(Name = "Streams", Category = "RSSDK", AutoEvaluate = true)]
    public class RSSDKSampleStreamNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Input("Context")]
        public ISpread<RSSDKContext> FContext;

        [Output("Color")]
        public ISpread<RSSDKImage> FColor;
       
        [Output("Depth")]
        public ISpread<RSSDKImage> FDepth;

        [Output("IR")]
        public ISpread<RSSDKImage> FIR;

        [Output("Frame Changed")]
        public ISpread<bool> FSampleChanged;
        
        [Output("Frame Status")]
        public ISpread<pxcmStatus> FSampleStatus;
      	
        public void OnImportsSatisfied()
        {
           	FColor.SliceCount = 0;
        	FDepth.SliceCount = 0;
        	FIR.SliceCount = 0;
        }
        
        [Import()]
		public ILogger FLogger;

        public void Evaluate(int SpreadMax)
        {
        	if((FContext != null) && (FContext.SliceCount > 0) && (FContext[0].Configured) ) // && FContext[0].sm.IsConnected()
            {
                if (FContext[0].Streams.ContainsKey(PXCMCapture.StreamType.STREAM_TYPE_COLOR))
                {
                    if (FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR].Source != null)
                    {
                        FColor.SliceCount = 1;
                        FColor[0] = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR];
                    }
                }
                
                if (FContext[0].Streams.ContainsKey(PXCMCapture.StreamType.STREAM_TYPE_DEPTH))
                {
                    if (FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH].Source != null)
                    {
                        FDepth.SliceCount = 1;
                        FDepth[0] = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH];
                       // FLogger.Log(LogType.Debug,"FORMAT" + FDepth[0].Format.ToString());
                    }
                     
                }
                

                if (FContext[0].Streams.ContainsKey(PXCMCapture.StreamType.STREAM_TYPE_IR))
                {
                    if (FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_IR].Source != null)
                    {
                        FIR.SliceCount = 1;
                        FIR[0] = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_IR];
                    }
                     
                }
                
            }
        }
    }
    
    [PluginInfo(Name = "Depth", Category = "RSSDK, depth", AutoEvaluate = true)]
    public class RSSDKSampleDepthStreamNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IPluginConnections
    {
        [Input("Context")]
        public Pin<RSSDKContext> FContext;

        [Input("ConfidenceThreshold", DefaultValue = 6)]
        public IDiffSpread<int> FConfidenceThresholdIn;
        
       	[Input("DepthUnit", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<Single> FDepthUnitIn;
        
        [Input("SetSettings", IsSingle = true)]
        public ISpread<bool> FSet;
        
        [Input("Reset", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FReset;
        
        [Input("Enabled", Order = 99)]
        public IDiffSpread<bool> FEnabled;
        
        //Output
      	[Output("Depth")]
        public ISpread<RSSDKImage> FDepth;
        
        [Output("Format")]
        public ISpread<string> FFormatOut;

        [Output("ConfidenceThreshold")]
        public ISpread<int> FConfidenceThresholdOut;
        
        [Output("FOV")]
        public ISpread<Vector2D> FFOVOut;
        
        [Output("FocalLength")]
        public ISpread<Vector2D> FFocalLengthOut;
        
        [Output("PrincipalPoint")]
        public ISpread<Vector2D> FPrincipalPointOut;
        
        [Output("SensorRange")]
        public ISpread<Vector2D> FSensorRangeOut;
        
        [Output("LowConfidenceValue")]
        public ISpread<int> FLowConfidenceValueOut;
        
        [Output("DepthUnit")]
        public ISpread<float> FDepthUnitOut;
       
        [Output("Frame Status")]
        public ISpread<pxcmStatus> FSampleStatus;
        
        #region ContextConnect
        bool FContextChanged = false;
      
		public void ConnectPin(IPluginIO pin)
		{
			if (pin == FContext.PluginIO) {
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got connected");
			}
		}

		public void DisconnectPin(IPluginIO pin)
		{
			if (pin == FContext.PluginIO) {
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got disconnected");
			}
		}
		#endregion
		
		internal void SetupNode()
		{
			if(
           		(FContext != null) &&
				(FContext.SliceCount > 0) && 
				FContext[0].Configured &&
           	  	(FContext[0].sm != null) && 
           	   	FContext[0].sm.IsConnected() &&
           	    (FContext[0].Streams.Count > 0) &&
           	    FContext[0].Streams.ContainsKey(PXCMCapture.StreamType.STREAM_TYPE_DEPTH) 
           	  )
            {
				Image = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH];
           		
           		FFocalLengthOut[0] = FContext[0].device.QueryDepthFocalLength().ToVector2D();
				FFOVOut[0] = FContext[0].device.QueryDepthFieldOfView().ToVector2D();
				FPrincipalPointOut[0] =  FContext[0].device.QueryDepthPrincipalPoint().ToVector2D();
				
				FSensorRangeOut[0] = FContext[0].device.QueryDepthSensorRange().ToVector2D(); //mm
				FDepthUnitOut[0] = FContext[0].device.QueryDepthUnit(); //uint
				
				FLowConfidenceValueOut[0] = FContext[0].device.QueryDepthLowConfidenceValue();
				FConfidenceThresholdOut[0] = FContext[0].device.QueryDepthConfidenceThreshold();
				
				FFormatOut[0] = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_DEPTH].Format.ToString();
			}
			else
			{
				Image = null;
				FDepth.SliceCount = 0;
			}
		}
		
		public void OnImportsSatisfied()
        {
           	FDepth.SliceCount = 0;
        }
        
       	RSSDKImage Image = null;
        
        [Import()]
		public ILogger FLogger;

        public void Evaluate(int SpreadMax)
        {
        	if(FContextChanged)
        	{
        		SetupNode();
        		FContextChanged = false;
        	}
        	
        	if(Image != null)
            {
        		FDepth.SliceCount = 1;
                FDepth[0] = Image;
                FFormatOut[0] = FDepth[0].Format.ToString();
            }
            else 
            {
            	FLogger.Log(LogType.Debug,"No STREAM_TYPE_DEPTH found");
            	FDepth.SliceCount = 0;
            }
            
            if(FConfidenceThresholdIn.IsChanged)
            {
            	FContext[0].device.SetDepthConfidenceThreshold((ushort)FConfidenceThresholdIn[0]);
            	FConfidenceThresholdOut[0] = FContext[0].device.QueryDepthConfidenceThreshold();
            	
            }
            
           	if(FReset.IsChanged)
        	{
        		FContext[0].device.ResetProperties(Image.Type);
        	}
             //settings
        }
    }
    
    [PluginInfo(Name = "IR", Category = "RSSDK", AutoEvaluate = true)]
    public class RSSDKSampleIRStreamNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IPluginConnections
    {
        [Input("Context")]
        public Pin<RSSDKContext> FContext;
        
        [Input("Reset", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FReset;
        
        [Input("Enabled", Order = 99)]
        public IDiffSpread<bool> FEnabled;
      
       	[Output("IR")]
        public ISpread<RSSDKImage> FIR;
        
        [Output("Format")]
        public ISpread<string> FFormatOut;

        [Output("Frame Status")]
        public ISpread<pxcmStatus> FSampleStatus;
        
       	
        RSSDKImage Image = null;
        bool FContextChanged = false;
         
        #region ContextConnect
        
		public void ConnectPin(IPluginIO pin)
		{
			if (pin == FContext.PluginIO) {
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got connected");
			}
		}

		public void DisconnectPin(IPluginIO pin)
		{
			if (pin == FContext.PluginIO) {
				Image = null;
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got disconnected");
			}
		}
		#endregion
        
		internal void SetupNode()
		{
			if(FContext != null)
			{
				if( FContext[0].Configured &&
	           	    (FContext[0].sm != null) && 
	           	   	FContext[0].sm.IsConnected()
	           	  )
	            {
					if(FContext[0].Streams.ContainsKey(PXCMCapture.StreamType.STREAM_TYPE_IR))
					{
						Image = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_IR];
						FIR.SliceCount = 1;
	            	   	FIR[0] = Image;
						FFormatOut.SliceCount = 1;
						FFormatOut[0] = Image.Format.ToString();
					} else FLogger.Log(LogType.Debug,"IR: No STREAM_TYPE_IR found");
				}
				else
				{
					Image = null;
					FIR.SliceCount = 0;
					FFormatOut.SliceCount  = 0;
				}
			}
			else
			{
				Image = null;
				FIR.SliceCount = 0;
				FFormatOut.SliceCount  = 0;
			}
		}
		
        public void OnImportsSatisfied()
        {
           	FIR.SliceCount = 0;
           	FFormatOut.SliceCount = 0;
        }
        
        [Import()]
		public ILogger FLogger;

        public void Evaluate(int SpreadMax)
        {
        	if(FContextChanged)
        	{
        		SetupNode();
        		FContextChanged = false;
        	}
        	
        	if(Image != null)
            {
        		//FIR.SliceCount = 1;
               	FIR[0] = Image;
               
               	if(FReset.IsChanged)
	        	{
	        		FContext[0].device.ResetProperties(Image.Type);
	        		FContextChanged = true;
	        	}
                
            } 
        	
        }
    }
    
    [PluginInfo(Name = "RGB", Category = "RSSDK", AutoEvaluate = true)]
    public class RSSDKSampleColorNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IPluginConnections
    {
        [Input("Context")]
        public Pin<RSSDKContext> FContext;
        
        [Input("AutoExposure", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> FAutoExposureIn;
        
        [Input("AutoWhiteBalance", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> FAutoWhiteBalanceIn;
        
        [Input("BackLightCompensation", MinValue = 0, Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FBackLightCompensationIn;
        
        [Input("Brightness", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FColorBrightnessIn;
        
        [Input("Contrast", MinValue = 0, MaxValue = 10000, Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FContrastIn;
        
        [Input("Exposure", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FExposureIn;
        
        [Input("Hue", MinValue = -180, MaxValue = 180, Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FHueIn;
        
        [Input("Gamma", MinValue = 1, MaxValue = 500, Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FGammaIn;
        
        [Input("Gain", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FColorGainIn;
        
       	[Input("PowerLineFrequency", Visibility = PinVisibility.OnlyInspector)]
       	public IDiffSpread<PXCMCapture.Device.PowerLineFrequency> FPowerLineFrequencyIn;
        
        [Input("Saturation", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FSaturationIn;
        
        [Input("Sharpness", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FSharpnessIn;
        
        [Input("WhiteBalance", MinValue = 0, Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> FWhiteBalanceIn;
        
        [Input("SetSettings", IsSingle = true)]
        public ISpread<bool> FSet;
        
        [Input("Reset", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FReset;
        
        [Input("Enabled", Order = 99)]
        public IDiffSpread<bool> FEnabled;
        
        //-- OUTPUT

        [Output("Color")]
        public ISpread<RSSDKImage> FColor;
        
        [Output("Format")]
        public ISpread<string> FFormatOut;
        
        [Output("Frame Status")]
        public ISpread<pxcmStatus> FSampleStatus;
        
        [Output("AutoExposure", Visibility = PinVisibility.OnlyInspector, DefaultBoolean = true)]
        public ISpread<bool> FAutoExposureOut;
        
        [Output("Exposure", Visibility = PinVisibility.OnlyInspector, DefaultValue = -5 )]
        public ISpread<int> FExposureOut;
        
        [Output("Auto White Balance", Visibility = PinVisibility.OnlyInspector, DefaultBoolean = true)]
        public ISpread<bool> FAutoWhiteBalanceOut;
        
        [Output("White Balance", MinValue = 0, Visibility = PinVisibility.OnlyInspector)]
        public ISpread<int> FWhiteBalanceOut;
        
        [Output("Back Light Compensation", MinValue = 0, MaxValue = 4, DefaultValue = 1)]
        public ISpread<int> FBackLightCompensationOut;
        
        [Output("Brightness", Visibility = PinVisibility.OnlyInspector, DefaultValue=55)]
        public ISpread<int> FColorBrightnessOut;
        
        [Output("Contrast", MinValue = 16, MaxValue = 64, Visibility = PinVisibility.OnlyInspector, DefaultValue=32)]
        public ISpread<int> FContrastOut;
        
        [Output("Hue", MinValue = -180, MaxValue = 180, Visibility = PinVisibility.OnlyInspector, DefaultValue=0)]
        public ISpread<int> FHueOut;
        
        [Output("Gamma", MinValue = 100, MaxValue = 280, Visibility = PinVisibility.OnlyInspector, DefaultValue=220)]
        public ISpread<int> FGammaOut;
        
        [Output("Gain",MinValue = 64, MaxValue = 2540, Visibility = PinVisibility.OnlyInspector, DefaultValue=64)]
        public ISpread<int> FColorGainOut;
        
        [Output("Power Line Frequency", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<PXCMCapture.Device.PowerLineFrequency> FPowerLineFrequencyOut;
        
        [Output("Saturation",MinValue = 0, MaxValue = 255,  Visibility = PinVisibility.OnlyInspector, DefaultValue=128)]
        public ISpread<int> FSaturationOut;
        
        [Output("Sharpness", MinValue = 0, MaxValue = 7,  Visibility = PinVisibility.OnlyInspector, DefaultValue=0)]
        public ISpread<int> FSharpnessOut;
       
        [Output("FOV", Visibility = PinVisibility.True)] //!!! degrees -> radians
        public ISpread<Vector2D> FFOVOut;
        
        [Output("Focal Length", Visibility = PinVisibility.True)] //!!! degrees -> radians
        public ISpread<Vector2D> FFocalLengthOut;
        
        [Output("Principal Point", Visibility = PinVisibility.OnlyInspector)] //!!! degrees -> radians
        public ISpread<Vector2D> FPrincipalPointOut;
        
        RSSDKImage Image = null;
        #region ContextConnect
            
        bool FContextChanged = false;
      
		public void ConnectPin(IPluginIO pin)
		{
			if (pin == FContext.PluginIO) {
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got connected");
			}
		}

		public void DisconnectPin(IPluginIO pin)
		{
			if (pin == FContext.PluginIO) {
				Image = null;
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got disconnected");
			}
		}
		#endregion
		
		internal void SetupNode()
		{
			if((FContext != null) &&
				(FContext.SliceCount > 0) && 
				FContext[0].Configured &&
           	    (FContext[0].sm != null) && 
           	   	FContext[0].sm.IsConnected() )
			{
				if(!FContext[0].Streams.ContainsKey(PXCMCapture.StreamType.STREAM_TYPE_COLOR) )
           	  	{
					FContext[0].cm.CloseStreams();
					FContext[0].Status = FContext[0].sm.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 0,0,0);
					if(FContext[0].Status == pxcmStatus.PXCM_STATUS_NO_ERROR)  FLogger.Log(LogType.Debug, "Enabled RGB stream\n\r");
       		 	
					RSSDKImage img = new RSSDKImage();
        			img.Type = PXCMCapture.StreamType.STREAM_TYPE_COLOR;
        			img.Format = PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32;
        		
        			FContext[0].Streams.Add(PXCMCapture.StreamType.STREAM_TYPE_COLOR, img);
        		
        			//FContext[0].sm.StreamFrames(false);
				} //???
           		
				Image = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR];
           		
		    	FFocalLengthOut[0] = FContext[0].device.QueryColorFocalLength().ToVector2D();
				FFOVOut[0] = FContext[0].device.QueryColorFieldOfView().ToVector2D();
				FPrincipalPointOut[0] =  FContext[0].device.QueryColorPrincipalPoint().ToVector2D();
				FFormatOut[0] = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR].Format.ToString();
				FExposureOut[0] = FContext[0].device.QueryColorExposure();
				FWhiteBalanceOut[0] = FContext[0].device.QueryColorWhiteBalance();
				FBackLightCompensationOut[0] = FContext[0].device.QueryColorBackLightCompensation();
				FColorBrightnessOut[0] = FContext[0].device.QueryColorBrightness();
				FContrastOut[0] = FContext[0].device.QueryColorContrast();
				FColorGainOut[0] = FContext[0].device.QueryColorGain();
				FGammaOut[0] = FContext[0].device.QueryColorGamma();
				FHueOut[0] = FContext[0].device.QueryColorHue();
				FPowerLineFrequencyOut[0] = FContext[0].device.QueryColorPowerLineFrequency();
				FSaturationOut[0] = FContext[0].device.QueryColorSaturation();
		    	FSharpnessOut[0] = FContext[0].device.QueryColorSharpness();
		    	FFormatOut[0] = FContext[0].Streams[PXCMCapture.StreamType.STREAM_TYPE_COLOR].Format.ToString();
		    	FAutoExposureOut[0] = FContext[0].device.QueryColorAutoExposure();
				FAutoWhiteBalanceOut[0] = FContext[0].device.QueryColorAutoWhiteBalance();
				
				//query properties info
				var pInfoExposure = FContext[0].device.QueryColorExposureInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("Exposure", pInfoExposure));
            	
    			var pInfoWhiteBalance = FContext[0].device.QueryColorWhiteBalanceInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("WhiteBalance", pInfoWhiteBalance));
            	
            	
    			var pInfoBackLightCompensation = FContext[0].device.QueryColorBackLightCompensationInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("BackLightCompensation", pInfoBackLightCompensation));
            	
    			var pInfoBrightness = FContext[0].device.QueryColorBrightnessInfo();
    			FLogger.Log(LogType.Debug, PropertyInfoToString("Brightness", pInfoBrightness));
            	
    			var pInfoContrast = FContext[0].device.QueryColorContrastInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("Contrast", pInfoContrast));
            	
    			var pInfoGain = FContext[0].device.QueryColorGainInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("Gain", pInfoGain));
            	
    			var pInfoGamma = FContext[0].device.QueryColorGammaInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("Gamma", pInfoGamma));
            	
    			var pInfoHue = FContext[0].device.QueryColorHueInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("Hue", pInfoHue));
            	
    			var pInfoSaturation = FContext[0].device.QueryColorSaturationInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("Saturation", pInfoSaturation));
            	
    			var pInfoSharpness = FContext[0].device.QueryColorSharpnessInfo();
            	FLogger.Log(LogType.Debug, PropertyInfoToString("Sharpness", pInfoSharpness));
           	} 
			
		}
        
      	public void OnImportsSatisfied()
        {
           	FColor.SliceCount = 0;
        }
       
        [Import()]
		public ILogger FLogger;

        public void Evaluate(int SpreadMax)
        {
        	if(FContextChanged)
        	{
        		SetupNode();
        		FContextChanged = false;
        	}
        	
        	if(Image != null) 
        	{
        		//output the stream
               	FColor.SliceCount = 1;
               	FColor[0] = Image; 
                
               	if(FReset.IsChanged && FReset[0])
        		{
        			FContext[0].device.ResetProperties(PXCMCapture.StreamType.STREAM_TYPE_COLOR);
        		}
                
                //settings
                if(FSet[0])
                {
                	if(FAutoExposureIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorAutoExposure(FAutoExposureIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FAutoExposureOut[0] = FContext[0].device.QueryColorAutoExposure();
	                }
	                
	                if(FExposureIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorExposure(FExposureIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FExposureOut[0] = FContext[0].device.QueryColorExposure();
	                } 	 				
	
	                if(FAutoWhiteBalanceIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorAutoWhiteBalance(FAutoWhiteBalanceIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FAutoWhiteBalanceOut[0] = FContext[0].device.QueryColorAutoWhiteBalance();
	                }
	                
	                if(FWhiteBalanceIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorSharpness(FWhiteBalanceIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FWhiteBalanceOut[0] = FContext[0].device.QueryColorWhiteBalance();
	                }
	                
	 				if(FBackLightCompensationIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorBackLightCompensation(FBackLightCompensationIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FBackLightCompensationOut[0] = FContext[0].device.QueryColorBackLightCompensation();
	                }  
	 				
	 				if(FColorBrightnessIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorBrightness(FColorBrightnessIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FColorBrightnessOut[0] = FContext[0].device.QueryColorBrightness();
	                }  
	 				
	 				if(FContrastIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorContrast(FContrastIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FContrastOut[0] = FContext[0].device.QueryColorContrast();
	                } 
	 				
					
		 			if(FColorGainIn.IsChanged)
	                {
		 				if(FContext[0].device.SetColorGain(FColorGainIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FColorGainOut[0] = FContext[0].device.QueryColorGain();
	                } 
	
					if(FGammaIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorGain(FGammaIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FGammaOut[0] = FContext[0].device.QueryColorGamma();
	                } 
					
					if(FHueIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorHue(FHueIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FHueOut[0] = FContext[0].device.QueryColorHue();
	                } 
	
					if(FPowerLineFrequencyIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorPowerLineFrequency(FPowerLineFrequencyIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FPowerLineFrequencyOut[0] = FContext[0].device.QueryColorPowerLineFrequency();
	                }
	
					if(FSaturationIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorSaturation(FSaturationIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FSaturationOut[0] = FContext[0].device.QueryColorSaturation();
	                }
	
					if(FSharpnessIn.IsChanged)
	                {
	                	if(FContext[0].device.SetColorSharpness(FSharpnessIn[0]) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                	FSharpnessOut[0] = FContext[0].device.QueryColorSharpness();
	                }
                }
				
            }
        }
        
        internal string PropertyInfoToString(String pName, PXCMCapture.Device.PropertyInfo pInfo)
        {
        	return  pName + " \t" + "auto:" + pInfo.automatic.ToString() + " default:" + pInfo.defaultValue.ToString() + " range:" + pInfo.range.min.ToString() + ".." + pInfo.range.max.ToString() + " step:" + pInfo.step.ToString();
                	
        }
    }

    [PluginInfo(Name = "AsTexture", Category = "DX11.Texture", Version = "RSSDK")]
    public unsafe class DynamicTexture2DNode : IPluginEvaluate, IDX11ResourceProvider, IDisposable
    {
        [Input("Input", DefaultValue = 1, AutoValidate = false)]
        protected ISpread<RSSDKImage> FInImage;
      
        [Input("Apply", IsBang = true, DefaultValue = 1)]
        protected ISpread<bool> FApply;

        [Output("Texture Out")]
        protected Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;
        
        [Output("Texture Format")]
        protected ISpread<SlimDX.DXGI.Format> FTexFormat;

        [Output("Valid")]
        protected ISpread<bool> FValid;
        
        [Import()]
		public ILogger FLogger;

        private bool FInvalidate;

        private float[] data = new float[0];

        public void Evaluate(int SpreadMax)
        {
        	
        	if (this.FApply[0] && (this.FInImage != null))
            {
            	
                this.FInImage.Sync();
                this.FInvalidate = true;
            }

            if ((this.FInImage.SliceCount == 0) || (this.FInImage[0] == null))
            {
                if (this.FTextureOutput.SliceCount == 1)
                {
                    if (this.FTextureOutput[0] != null) { this.FTextureOutput[0].Dispose(); }
                    this.FTextureOutput.SliceCount = 0;
                }
            }
            else
            {
                this.FTextureOutput.SliceCount = 1;
                if (this.FTextureOutput[0] == null) { this.FTextureOutput[0] = new DX11Resource<DX11DynamicTexture2D>(); }
            }
        }

        public unsafe void Update(IPluginIO pin, DX11RenderContext context)
        {
            if (this.FTextureOutput.SliceCount == 0) { return; }

            if (this.FInvalidate || !this.FTextureOutput[0].Contains(context))
            {
            	//Check & convert dynamicly
                SlimDX.DXGI.Format fmt;
                switch (this.FInImage[0].Format)
                {
					//DEPTH STREAM
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH:
					fmt = SlimDX.DXGI.Format.R16_UInt; //B8G8R8X8_UNorm;
                        break;
                    
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_F32:
                		fmt = SlimDX.DXGI.Format.R32_Float;
                        break;
                    
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_RAW:
                		fmt = SlimDX.DXGI.Format.R16_UInt;
                        break;
                    
                   
                    //COLOR STREAM    
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32:
                        fmt = SlimDX.DXGI.Format.B8G8R8X8_UNorm;
                        break;
                      
                 //--not working  !!! 
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24:
                        fmt = SlimDX.DXGI.Format.B8G8R8X8_UNorm;
                        break;
                        
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2:
                        fmt = SlimDX.DXGI.Format.G8R8_G8B8_UNorm;
                        break;
                    
					case PXCMImage.PixelFormat.PIXEL_FORMAT_NV12:
                        fmt = SlimDX.DXGI.Format.R8G8_UNorm;
                        break;
                        
                    //IR STREAM    
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_Y8:
                        fmt = SlimDX.DXGI.Format.R8_UNorm;
                        break;
                        
                    case PXCMImage.PixelFormat.PIXEL_FORMAT_Y8_IR_RELATIVE:
                        fmt = SlimDX.DXGI.Format.R8_UNorm;
                        break;
                        
                     case PXCMImage.PixelFormat.PIXEL_FORMAT_Y16:
                		fmt = SlimDX.DXGI.Format.R16_UInt;
                        break;
                        
                    default:
                        fmt = SlimDX.DXGI.Format.Unknown;
                        break;
                }
                FTexFormat[0] = fmt;

                Texture2DDescription desc;
				
                if (this.FTextureOutput[0].Contains(context))
                {
                    desc = this.FTextureOutput[0][context].Resource.Description;

                    if (desc.Width != this.FInImage[0].Width || desc.Height != this.FInImage[0].Height || desc.Format != fmt)
                    {
                        this.FTextureOutput[0].Dispose(context);
                        this.FTextureOutput[0][context] = new DX11DynamicTexture2D(context, this.FInImage[0].Width, this.FInImage[0].Height, fmt);
                    }
                }
                else
                {
                    this.FTextureOutput[0][context] = new DX11DynamicTexture2D(context, this.FInImage[0].Width, this.FInImage[0].Height, fmt);
#if DEBUG
                    this.FTextureOutput[0][context].Resource.DebugName = "DynamicTexture";
#endif
                }

                desc = this.FTextureOutput[0][context].Resource.Description;

                var t = this.FTextureOutput[0][context];
                byte[] imgData = this.FInImage[0].Data;
                t.WriteData(imgData);
                this.FInvalidate = false;
            }

        }

        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {

            this.FTextureOutput[0].Dispose(context);
        }


        #region IDisposable Members
        public void Dispose()
        {
            if (this.FTextureOutput.SliceCount > 0)
            {
                if (this.FTextureOutput[0] != null)
                {
                    this.FTextureOutput[0].Dispose();
                }
            }

        }
        #endregion
    }
}