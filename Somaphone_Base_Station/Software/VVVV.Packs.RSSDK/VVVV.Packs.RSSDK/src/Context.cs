/*
 * Created by Antony Rayzhekov a.k.a. Zeos
 * User: Intel
 * Date: 7/19/2015
 * Time: 4:07 PM
 * 
 */
 
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using VVVV.Hosting;
using VVVV.Utils.VMath;
using VVVV.Core.Collections;
using VVVV.Core.Logging;
using libpxcclr;
using libpxcclr.cs;

namespace VVVV.Nodes.RSSDK
{
	
	public enum RSSDKAccurancy {
		
		FINEST,
		MEDIAN,
		COARSE
	}
	
	public enum RSSDKFilterOption {
		
		Skeleton,
		Raw,
		RawGradient,
		VeryCloseRange,
		CloseRange,
		MidRange,
		FarRange,
		VeryFarRange
	}
	
	
	public class RSSDKContext : IDisposable
    {
        public PXCMSession Session;
        public PXCMSenseManager sm;
        public PXCMSenseManager.Handler frameready_cb = null;
        public PXCMCaptureManager cm;
        public PXCMCapture capture;
        public PXCMCapture.Device device = null;
        public pxcmStatus Status = pxcmStatus.PXCM_STATUS_NO_ERROR;
        public Vector2D DepthImageSize;
        
		public Dictionary<PXCMCapture.StreamType, RSSDKImage> Streams = new Dictionary<PXCMCapture.StreamType, RSSDKImage>();
        public Dictionary<PXCMCapture.StreamType, Dictionary<string, PXCMCapture.Device.StreamProfile>> StreamProfiles = new Dictionary<PXCMCapture.StreamType, Dictionary<string, PXCMCapture.Device.StreamProfile>>();
        public PXCMCapture.DeviceModel DeviceModel = PXCMCapture.DeviceModel.DEVICE_MODEL_GENERIC;
        public Dictionary<int, RSSDKFace> Faces = new Dictionary<int, RSSDKFace>();
        
        public RSSDKFaceConfig FaceConfig;
        
        public bool HandEnabled = false;
        public int numHands = 0;
        public PXCMHandModule handAnalysis;
        public PXCMHandConfiguration handConfig;
        public PXCMHandData handData;
        
        public bool BlobEnabled;
        public PXCMBlobModule blobModule;
        public PXCMBlobConfiguration blobConfig;
        public PXCMBlobData blobData;
        
        public ulong frame = 0;
        public bool IsSynchronized = false;
        public bool Configured = false;
        public bool StreamProfilesInitialized = false;
        public string debugtext;
        
       	public RSSDKContext()
        {
           	this.sm = PXCMSenseManager.CreateInstance();
           	this.Session = sm.QuerySession();
           	if(this.Session == null) this.Session  = PXCMSession.CreateInstance();
           	this.cm = sm.QueryCaptureManager();
            if(this.cm  == null) this.cm = this.Session.CreateCaptureManager();
            
        }

        public void Dispose()
        {
        	
        	if(handData != null) 
        	{
        		handData.Dispose();
        		handData = null;
        	}

        	if(blobData != null)
        	{
        		blobData.Dispose();
        		blobData = null;
        	}
        	
        	if(this.device != null) 
        	{
        		this.device.Dispose();
        	}
        	if(this.cm != null) 
        	{
        		this.cm.Dispose();
        	}
        	if(this.sm != null)
        	{
        		this.sm.Close();
        		this.sm.Dispose();
        	}
        	
        	this.Session.Dispose();
        }
        
        public  pxcmStatus OnConnect(PXCMCapture.Device dev, bool connected) 
        {
        	Console.WriteLine("RSSDK(OnConnect): Device connected status is " + connected.ToString());
   			return pxcmStatus.PXCM_STATUS_DEVICE_FAILED;
		}
 
		public  pxcmStatus OnModuleProcessFrame(int mid, PXCMBase obj, PXCMCapture.Sample sample) 
		{
			if(mid == PXCMHandModule.CUID)
        	{
        		if(HandEnabled && (handData != null))
	        	{
	        		handData.Update();
	        		numHands = handData.QueryNumberOfHands();
	        	}
        	}
        	
        	if(BlobEnabled && (blobData != null))
        	{
        		blobData.Update();
        	}
        	
			//PXCMBase m = this.sm.QueryModule(mid);
			//Console.WriteLine("RSSDK(ModuleProcessFrame): " + " - " +  mid.ToString());
   			return pxcmStatus.PXCM_STATUS_NO_ERROR;
   		}
		
		public void OnStatus(Int32 mid, pxcmStatus status)
		{
			//PXCMBase m = this.sm.QueryModule(mid);
			//Console.WriteLine("RSSDK(Status): " +  mid.ToString() + " - " + status.ToString());
		}
		
		public pxcmStatus OnModuleSetProfile(Int32 mid, PXCMBase obj)
		{
			//PXCMBase m = this.sm.QueryModule(mid);
			//Console.WriteLine("RSSDK(ModuleSetProfile): " + " - "+ mid.ToString());
			return pxcmStatus.PXCM_STATUS_NO_ERROR; 
		}
        
        public pxcmStatus OnNewSample(int mid, PXCMCapture.Sample sample)
		{
        	if (sample != null)
           	{
				frame++;
				foreach (RSSDKImage img in Streams.Values)
                {
                    switch (img.Type)
                    {
                        case PXCMCapture.StreamType.STREAM_TYPE_COLOR:
                            if (sample.color != null)
                            {
                            	img.Source = sample.color;
                                switch(img.Format)
                                {                                	
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32 : img.Data = sample.color.GetRGB32Pixels();
                                	break;
                                	/*
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2: img.Data = null;
                       				break;
                                	*/
                                	default:
                                	break;
                                	
                                }
                                img.Width =  sample.color.info.width;
                                img.Height = sample.color.info.height;
                                img.Format = sample.color.info.format;
                            }
                            break;

                        case PXCMCapture.StreamType.STREAM_TYPE_DEPTH:
                        {
                            if (sample.depth != null)
                            {
                            	img.Source = sample.depth;
                            	switch(img.Format)
                                {                                	
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH : img.Data = sample.depth.GetDepthPixels();
                                	break;
                                	
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_F32 : img.Data = sample.depth.GetDepthF32Pixels();
                                	break;
                                	
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_RAW : img.Data = sample.depth.GetDepthRawPixels();
                                	break;
                                	
                                	default:
                                	break;
                                }
                                img.Height = sample.depth.info.height;
                                img.Width = sample.depth.info.width;
                                img.Format = sample.depth.info.format;
                            }
                        }
                        break;

                        case PXCMCapture.StreamType.STREAM_TYPE_IR:
                        {
                            if (sample.ir != null)
                            {
                                img.Source = sample.ir;
                                switch(img.Format)
                                {                                	
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_Y8 : img.Data = sample.ir.GetY8Pixels();
                                	break;
                                	
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_Y8_IR_RELATIVE : img.Data = sample.ir.GetY8IrRelativePixels();
                                	break;
                                	
                                	case PXCMImage.PixelFormat.PIXEL_FORMAT_Y16: img.Data = sample.ir.GetY16Pixels();
                                	break;
                                	
                                	default:
                                	break;
                                }
                                img.Width = sample.ir.info.width;
                                img.Height = sample.ir.info.height;
                                img.Format = sample.ir.info.format;
                            }
                        }
                        break;

                        default: break;
                    }
                }
               //--
		   	} 
			 return pxcmStatus.PXCM_STATUS_NO_ERROR;
			//return pxcmStatus.PXCM_STATUS_PROCESS_FAILED;
		}
    }
	
	[PluginInfo(Name = "Context", Category="RSSDK", AutoEvaluate = true)]
    public class RSSDKContextNode : IPluginEvaluate, IPartImportsSatisfiedNotification, IPluginConnections
    {
       
        [Input("DeviceInfo", IsSingle = true)]
        public Pin<PXCMCapture.DeviceInfo> FDeviceInfo;
        
        [Input("Filename", IsSingle = true)]
        public IDiffSpread<string> FFilename;
        	
        [Input("Record", IsSingle = true)]
        public IDiffSpread<bool> FRecord;
        
        [Input("Pause", IsSingle = true)]
        public IDiffSpread<bool> FPause;
        
        // -- Settings
        [Input("Mirror", IsSingle = true)]
       	public IDiffSpread<bool> FMirror;
       	
       	[Input("Realtime", IsSingle = true)]
       	public IDiffSpread<bool> FRealtime;
        
        [Input("Sync Streams", IsSingle = true)]
        public IDiffSpread<bool> FSync;
       
        [Input("Color", EnumName="RealSenseStreamMode_COLOR", DefaultEnumEntry = "RGB32 1920x1080@30")]
		public IDiffSpread<EnumEntry> FStreamColorMode;
		
		[Input("Depth", EnumName="RealSenseStreamMode_DEPTH", DefaultEnumEntry = "DEPTH 640x480@60")]
		public IDiffSpread<EnumEntry> FStreamDepthMode;
		
		[Input("IR", EnumName="RealSenseStreamMode_IR", DefaultEnumEntry = "Y8 640x480@60")]
		public IDiffSpread<EnumEntry> FStreamIRMode;
		
		//modules
        
		[Input("Enable Face", IsSingle = true)]
        public IDiffSpread<bool> FEnableFace;
        
        [Input("Enable Blob", IsSingle = true)]
        public IDiffSpread<bool> FEnableBlob;
        
        [Input("Enable Hand", IsSingle = true)]
        public IDiffSpread<bool> FEnableHand;
        
        [Input("Enable 3DSeg", IsSingle = true)]
        public ISpread<bool> FEnable3DSeg;
        
        [Input("Enabled", IsSingle = true, DefaultValue = 0)]
        public IDiffSpread<bool> FEnabled;
        
       //-- OUTPUT
        
        [Output("Context")]
        public ISpread<RSSDKContext> FContext;
        
        [Output("AllowProfileChange")]
        public ISpread<bool> FAllowProfileChangeOut;
        
        [Output("Status", DefaultString = "")]
        public ISpread<string> FStatus;
        
        [Output("Connected", IsSingle = true)]
        public ISpread<bool> FConnected;
       
       	[Import()]
		public ILogger FLogger;
		
		public RSSDKContext SenseSession;
		
		#region ContextConnect
        bool FContextChanged = false;
      
		public void ConnectPin(IPluginIO pin)
		{
			if (pin == FDeviceInfo.PluginIO) 
			{
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got connected");
			}
		}

		public void DisconnectPin(IPluginIO pin)
		{
			if (pin == FDeviceInfo.PluginIO) 
			{
				FContextChanged = true;
				FLogger.Log(LogType.Debug, pin.Name + " got disconnected");
			}
		}
		#endregion
		
        public void OnImportsSatisfied()
        {
            this.SenseSession = new RSSDKContext();
            FLogger.Log(LogType.Debug, "RSSDK: Context created.");
        }
      
        public void Evaluate(int SpreadMax)
        {
        	if(FContextChanged && (FDeviceInfo != null) && (FEnabled[0]) )
        	{
        		FStatus[0] = createContext(FDeviceInfo[0]);
        		//SenseSession.device.SetDeviceAllowProfileChange(FAllowProfileChangeIn[0]);
        		FConnected[0] = SenseSession.sm.IsConnected();
        		
        		FContext.SliceCount = 1;
        		FContext[0] = SenseSession;
        		FAllowProfileChangeOut[0] = SenseSession.device.QueryDeviceAllowProfileChange();
        		
        		FStatus[0] += Start(FRealtime[0]);
        		
        		SenseSession.DepthImageSize = SenseSession.cm.QueryImageSize(PXCMCapture.StreamType.STREAM_TYPE_DEPTH).ToVector2D();
        		if(FMirror.IsChanged) SenseSession.cm.device.SetMirrorMode((FMirror[0]) ? PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL  : PXCMCapture.Device.MirrorMode.MIRROR_MODE_DISABLED);
	    		
        		FContextChanged = false;
        		
        		if(FEnableHand[0]) 
        		{
					SenseSession.handData = SenseSession.handAnalysis.CreateOutput();
	        			
        			if(SenseSession.handData  != null)
        			{
        				SenseSession.handConfig= SenseSession.handAnalysis.CreateActiveConfiguration();
        				SenseSession.handConfig.DisableAllGestures();
        				SenseSession.handConfig.ApplyChanges();
        				
        				SenseSession.HandEnabled = true; 
        				FLogger.Log(LogType.Debug, "RSSDK: Hand tracker started.");
        				FStatus[0] += "RSSDK: Hand tracker started!\n\r";
        			}
        			else FLogger.Log(LogType.Debug, "RSSDK(Error): Can not initialize the Hand tracker module.");
        			
				}
    			
    			if(FEnableBlob.IsChanged && FEnableBlob[0]) 
    			{
    				SenseSession.blobData = SenseSession.blobModule.CreateOutput();
    				if(SenseSession.blobData  != null)
        			{
    					SenseSession.blobConfig = SenseSession.blobModule.CreateActiveConfiguration();
    					FLogger.Log(LogType.Debug, "RSSDK: Blob tracker started.");
        				FStatus[0] += "RSSDK: Blob tracker started!\n\r";
        				SenseSession.BlobEnabled = true; 
    				}
    				else  FLogger.Log(LogType.Debug, "RSSDK(Error): Can not initialize the Blob tracker module.");
    			}
        	}
        		
        	if(FEnabled.IsChanged && SenseSession.Configured)
        	{
        		//if (FEnabled[0] == false) SenseSession.cm.CloseStreams();
	        	//else SenseSession.sm.StreamFrames(false);
        	}
        	
        	//realtime settings
        	if((SenseSession != null) && SenseSession.sm.IsConnected() && (SenseSession.device != null) && SenseSession.Configured)
    		{
        		if(FEnableFace.IsChanged && FEnableFace[0]) SenseSession.sm.EnableFace();
	        	
	        	//if(FAllowProfileChangeIn.IsChanged) SenseSession.device.SetDeviceAllowProfileChange(FAllowProfileChangeIn[0]);
	        	
	        	if( (FFilename[0] != "") && (FRecord.IsChanged) ) SenseSession.sm.captureManager.SetFileName(FFilename[0], FRecord[0]);
	    		   
    		}
        	//FFrame[0] = SenseSession.frame;
        }
        
        internal string Start(bool realtime)
        {
        	string Status = "RSSDK: Starting...";
       		if(SenseSession.Configured)
			{
       			try
       			{
					//start streaming
        			this.SenseSession.Status = SenseSession.sm.StreamFrames(false);
        		}
       			catch (Exception e)
       			{
       				FLogger.Log(LogType.Debug, "RSSDK(Exception):" + e.Message.ToString());
       			}
       			
        		if(SenseSession.Status == pxcmStatus.PXCM_STATUS_NO_ERROR) 
        		{
        			Status += "done\n\r";
        		}
        		else
        		{
        			Status += "failed\n\r";
        			FLogger.Log(LogType.Debug," Can not start streaming. Status: " + SenseSession.Status.ToString());
        		}
        		
			}
       		return Status;
        }
        
        internal string createContext(PXCMCapture.DeviceInfo devInfo)
        {
        	string Status = "RSSDK: Configuring...\n\r";
       		if(!this.SenseSession.Configured)
			{
       			if(SenseSession == null) this.SenseSession = new RSSDKContext();
       			
				if(SetupCaptureDevice(devInfo) == pxcmStatus.PXCM_STATUS_NO_ERROR)
				{
					Status += "RSSDK: Device created\n\r";
					Status += "RSSDK: Setup streams\n\r";
					Status += SetupStreams();
					if(this.SenseSession.Status != pxcmStatus.PXCM_STATUS_NO_ERROR) Status += "RSSDK: Can not setup streams\n\r";
					
					Status += "RSSDK: Setup modules\n\r";
					/****************************************************************************************************
					 * setup modules
					 ***************************************************************************************************/
					if(FEnableHand[0]) 
		        	{
		        		SenseSession.Status = SenseSession.sm.EnableHand();
		        		if(SenseSession.Status != pxcmStatus.PXCM_STATUS_NO_ERROR) FLogger.Log(LogType.Debug, "RSSDK(Error): Can not enable the hand tracker");
		        		{
		        			SenseSession.handAnalysis = SenseSession.sm.QueryHand();
		        			if(SenseSession.handAnalysis == null) FLogger.Log(LogType.Debug, "RSSDK(Error): Can not initialize the hand analysis module");
		        		}
	       			}
					
					if(FEnableBlob[0]) 
		        	{
						SenseSession.Status = SenseSession.sm.EnableBlob();
    					if(SenseSession.Status != pxcmStatus.PXCM_STATUS_NO_ERROR) FLogger.Log(LogType.Debug, "RSSDK(Error): Can not enable the blob tracker");
		        		{
		        			SenseSession.blobModule = SenseSession.sm.QueryBlob();
		        			if(SenseSession.blobModule == null) FLogger.Log(LogType.Debug, "RSSDK(Error): Can not initialize the blob analysis module");
		        		}
    				}
					
					
    				if(this.SenseSession.frameready_cb == null) 
					{
		        		this.SenseSession.frameready_cb = new PXCMSenseManager.Handler();
		        		this.SenseSession.frameready_cb.onNewSample = this.SenseSession.OnNewSample;
		        		//this.SenseSession.frameready_cb.onConnect = this.SenseSession.OnConnect;
		        		//this.SenseSession.frameready_cb.onStatus = this.SenseSession.OnStatus;
		        		//this.SenseSession.frameready_cb.onModuleSetProfile = this.SenseSession.OnModuleSetProfile;
		        		this.SenseSession.frameready_cb.onModuleProcessedFrame = this.SenseSession.OnModuleProcessFrame;
    				}
				
    				if(FFilename[0] != "") SenseSession.sm.captureManager.SetFileName(FFilename[0], FRecord[0]);
	    		
    				
    				this.SenseSession.cm.SetRealtime(FRealtime[0]);
	        		Status += "RSSDK: Init...";
					this.SenseSession.Status = this.SenseSession.sm.Init(this.SenseSession.frameready_cb);
    				
					if(SenseSession.Status == pxcmStatus.PXCM_STATUS_NO_ERROR)
	        		{
		        		
		        		FLogger.Log(LogType.Debug,"done.");
	        			//Thread.Sleep(100);
    					this.SenseSession.Configured = true;
    					Status += "done.\n\r";
    					
        			}
    				else FLogger.Log(LogType.Debug, "RSSDK: Can not Configured context!");
				}
    		}
       		return Status;
        }
        
    	internal pxcmStatus SetupCaptureDevice(PXCMCapture.DeviceInfo devinfo)
   	 	{
    		if(SenseSession == null) 
    		{
    			this.SenseSession = new RSSDKContext();
    		}
    		
    		if(SenseSession.sm.QueryCaptureManager().QueryCapture() != null)
    		{
    			SenseSession.capture = SenseSession.sm.QueryCaptureManager().capture;
    			SenseSession.Status = pxcmStatus.PXCM_STATUS_NO_ERROR;
    		}
    		else
    		{
	    		PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
				desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
				desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;
		    	desc.iuid = devinfo.duid;
				desc.cuids[0] = PXCMCapture.CUID;
				SenseSession.Status = SenseSession.Session.CreateImpl<PXCMCapture>(desc, out SenseSession.capture);
    		}
    		
    		
			if (SenseSession.Status == pxcmStatus.PXCM_STATUS_NO_ERROR)
			{
				FLogger.Log(LogType.Debug, "RSSDK: Capture created.");
				
				if(SenseSession.cm.QueryDevice() != null)
				{
					SenseSession.device = SenseSession.cm.QueryDevice();
				}
				else
				{
					SenseSession.device = SenseSession.capture.CreateDevice(FDeviceInfo[0].didx);
					
					if(SenseSession.device == null) return pxcmStatus.PXCM_STATUS_INIT_FAILED;
					else FLogger.Log(LogType.Debug, "RSSDK: Device created.");
				}
			
				//if(!this.SenseSession.StreamProfilesInitialized) 
				{
					this.SenseSession.StreamProfilesInitialized = InitStreamProfiles(this.SenseSession.device);
						
					if(!this.SenseSession.StreamProfilesInitialized) return pxcmStatus.PXCM_STATUS_DATA_NOT_INITIALIZED;
					else FLogger.Log(LogType.Debug, "RSSDK: Device profiles Initialized.");
				} 
			
				this.SenseSession.cm.FilterByDeviceInfo(FDeviceInfo[0]);
					
				FLogger.Log(LogType.Debug, "RSSDK: Device ready for configuration.");
				return pxcmStatus.PXCM_STATUS_NO_ERROR;
			} 
			else 
            {
            	FLogger.Log(LogType.Debug, "RSSDK: Can not create capture.");
            	return pxcmStatus.PXCM_STATUS_INIT_FAILED;
            }
    	}
    	
    	internal PXCMCapture.Device.StreamProfile getProfile(PXCMCapture.StreamType st, string mode)
    	{
    		if(SenseSession.StreamProfiles.ContainsKey(st))
    		{
    			if(SenseSession.StreamProfiles[st].ContainsKey(mode))
    			{
    				return SenseSession.StreamProfiles[st][mode];
    			}
    		}
    		
    		//default
    		PXCMCapture.Device.StreamProfile profileInfo = new PXCMCapture.Device.StreamProfile();
    		profileInfo.frameRate.max = 0;
    		profileInfo.frameRate.min = 0;
    		profileInfo.imageInfo.height = 0;
    		profileInfo.imageInfo.width = 0;
    		
    		return profileInfo;
    	}
    	
    	internal bool EnableCaptureStream(PXCMCapture.StreamType st, PXCMCapture.Device.StreamProfile profileInfo)
    	{
    		Single fps = profileInfo.frameRate.max;
			SenseSession.Status = SenseSession.sm.EnableStream(st, profileInfo.imageInfo.width, profileInfo.imageInfo.height, fps);
			if(SenseSession.Status == pxcmStatus.PXCM_STATUS_NO_ERROR)  
			{
				RSSDKImage img = new RSSDKImage();
    			img.Type = st;
    			img.Format = profileInfo.imageInfo.format;
    		
    			SenseSession.Streams.Add(st, img);
    			
    			return true;
			}
			return false;
    	}
    	
    	internal string SetupStreams()
    	{
    		PXCMCapture.Device.StreamProfileSet profiles = new PXCMCapture.Device.StreamProfileSet();
    		string Status = ""; //"Setup streams...\n\r";
    		
			//COLOR    		
			if( (FStreamColorMode != null) && (FStreamColorMode[0].Index > 0)&& (FStreamColorMode[0].Name != "Disabled"))
            {
            	var profileInfo = getProfile(PXCMCapture.StreamType.STREAM_TYPE_COLOR, FStreamColorMode[0].Name);
            	if(EnableCaptureStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, profileInfo))
            	{
					profiles[PXCMCapture.StreamType.STREAM_TYPE_COLOR] = profileInfo;
					Status += "RSSDK: Enabled stream " + FStreamColorMode[0].Name + "\n\r";
            	} 
            } 
            
            //DEPTH
			if( (FStreamDepthMode != null) && (FStreamDepthMode[0].Index > 0) && (FStreamDepthMode[0].Name != "Disabled"))
			{
				var profileInfo = getProfile(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, FStreamDepthMode[0].Name);
            	if(EnableCaptureStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, profileInfo))
            	{
					profiles[PXCMCapture.StreamType.STREAM_TYPE_DEPTH] = profileInfo;
					Status += "RSSDK: Enabled stream " + FStreamDepthMode[0].Name + "\n\r";
            	}
			}
			
			//IR
			if( (FStreamIRMode != null) && (FStreamIRMode[0].Index > 0) && (FStreamIRMode[0].Name != "Disabled") )
			{
				var profileInfo = getProfile(PXCMCapture.StreamType.STREAM_TYPE_IR, FStreamIRMode[0].Name);
            	if(EnableCaptureStream(PXCMCapture.StreamType.STREAM_TYPE_IR, profileInfo))
            	{
					profiles[PXCMCapture.StreamType.STREAM_TYPE_IR] = profileInfo;
					Status += "RSSDK: Enabled stream " + FStreamIRMode[0].Name + "\n\r";
            	}
			}
				
			SenseSession.cm.FilterByStreamProfiles(profiles);
			
			
			//validate profileSet
			if(SenseSession.device.IsStreamProfileSetValid(profiles))
			{
				if(SenseSession.device.SetStreamProfileSet(profiles) ==  pxcmStatus.PXCM_STATUS_NO_ERROR)
				{
					Status += "RSSDK: Profileset is valid\n\r";
					SenseSession.Status = pxcmStatus.PXCM_STATUS_NO_ERROR;
					FLogger.Log(LogType.Debug, "RSSDK: Profileset is valid");
				} //SenseSession.Configured 
				else 
				{
					Status += "RSSDK: Profileset can not be set\n\r";
					FLogger.Log(LogType.Debug, "RSSDK: Profileset can not be set");
				}
			}
			else 
			{
				Status += "RSSDK: Profileset is not valid\n\r";
				FLogger.Log(LogType.Debug, "RSSDK: Profileset is not valid");
			}
			
			//setup sync streams
			if (FSync[0])
    		{
    			SenseSession.IsSynchronized = true;
    			
    			PXCMVideoModule.DataDesc ddesc=new PXCMVideoModule.DataDesc();
    			if(FStreamColorMode[0].Name != "Disabled") ddesc.deviceInfo.streams|= PXCMCapture.StreamType.STREAM_TYPE_COLOR;
    			if(FStreamDepthMode[0].Name != "Disabled") ddesc.deviceInfo.streams|= PXCMCapture.StreamType.STREAM_TYPE_DEPTH;
    			if(FStreamIRMode[0].Name != "Disabled") ddesc.deviceInfo.streams|= PXCMCapture.StreamType.STREAM_TYPE_IR;
    			SenseSession.Status = SenseSession.sm.EnableStreams(ddesc);
    		} 
			
			return Status;
    	}
    	 
        private bool InitStreamProfiles(PXCMCapture.Device device)
        {
        	uint ProfileCount = 0;
        	if (device !=null) 
		    {		
		    	PXCMCapture.Device.StreamProfileSet profile = new PXCMCapture.Device.StreamProfileSet();
		
            	for (int s = 0; s < PXCMCapture.STREAM_LIMIT; s++)
            	{
            		PXCMCapture.StreamType st = PXCMCapture.StreamTypeFromIndex(s);
                	
                	PXCMCapture.DeviceInfo dinfo;
                	device.QueryDeviceInfo(out dinfo);
                	
                	if (((int)dinfo.streams & (int)st) != 0)
                	{
                    	
                    	int num = device.QueryStreamProfileSetNum(st);
                    	
                    	for (int p = 0; p < num; p++)
                    	{
                        	if (device.QueryStreamProfileSet(st, p, out profile) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                        
                        	PXCMCapture.Device.StreamProfile sprofile = profile[st];
                        	
                        	
                        	//filter out all Color non RGB32 stream modes out
                        	if((st == PXCMCapture.StreamType.STREAM_TYPE_COLOR) && (sprofile.imageInfo.format != PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32 ))
                        	continue;
                        	
                        	if(!SenseSession.StreamProfiles.ContainsKey(st)) 
                        	{
                        		SenseSession.StreamProfiles.Add(st, new Dictionary<string, PXCMCapture.Device.StreamProfile>());
                        	}
                        	
                        	var StreamTypeProfileSet = SenseSession.StreamProfiles[st];
                        	var ProfileDescr = ProfileToString(st, sprofile);
                        	
                        	if(!StreamTypeProfileSet.ContainsKey(ProfileDescr))
                        	{
                        		StreamTypeProfileSet.Add(ProfileDescr, sprofile);
                        		ProfileCount++;
                        	}
                    		
                		}
                    	
                    	//add to enums
                    	string[] defaultMode = {"Disabled"};
                    	string[] streamModes = defaultMode.Concat(SenseSession.StreamProfiles[st].Keys.ToArray()).ToArray();
                    	EnumManager.UpdateEnum("RealSenseStreamMode_" + st.ToString().Substring(12), "Disabled", streamModes);
                    	
            		}
                	else if (((int)dinfo.streams & (int)st) == 0)
                	{
                		
                	}
            	}
            	
        	} 
        	else FLogger.Log(LogType.Debug,"Device is null");
        	
        	return ProfileCount > 0;
        }
        
        private string ProfileToString(PXCMCapture.StreamType st, PXCMCapture.Device.StreamProfile pinfo)
        {
        	
        	string line = ""; // + ":"; //remove STREAM_TYPE_
            
            if (Enum.IsDefined(typeof(PXCMImage.PixelFormat), pinfo.imageInfo.format))
            {
            	
            	line += pinfo.imageInfo.format.ToString().Substring(13)+" "+pinfo.imageInfo.width+"x"+pinfo.imageInfo.height+"@";
            }
            else
                line += pinfo.imageInfo.width + "x" + pinfo.imageInfo.height + "@";
            
            if (pinfo.frameRate.min != pinfo.frameRate.max) {
                line += (float)pinfo.frameRate.min + "-" +
                      (float)pinfo.frameRate.max;
            } else {
                float fps = (pinfo.frameRate.min!=0)?pinfo.frameRate.min:pinfo.frameRate.max;
                line += fps;
            }
            
            return line;
        }

    }
    
   	
}

