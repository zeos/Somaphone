using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
	/*
    public class SenseWrapper : IDisposable
    {
        public PXCMSession Session;
        public PXCMSenseManager SenseManager;
        public pxcmStatus Status = pxcmStatus.PXCM_STATUS_NO_ERROR;

        public Dictionary<int, RSSDKFace> Faces = new Dictionary<int, RSSDKFace>();
        public RSSDKFaceConfig FaceConfig;

        public PXCMHandModule handAnalysis;
        public PXCMHandConfiguration handConfig;
        public PXCMHandData HandData;
        
        public PXCMBlobModule blobModule;
        public PXCMBlobConfiguration blobConfig;
        public PXCMBlobData blobData;
        	
        public bool IsSynchronized = false;
        public bool EnabledStream = false;
        public bool EnabledFace = false;
        public bool EnabledHand = false;
        public bool Enabled3DSeg = false;
        public bool EnabledBlob = false;
        public bool Initialized = false;
        public bool Configured = false;

        public Dictionary<PXCMCapture.StreamType, RSSDKImage> Streams = new Dictionary<PXCMCapture.StreamType, RSSDKImage>();
       
        public RSSDKImage handImage = null;
        
        public SenseWrapper()
        {
            this.Session = PXCMSession.CreateInstance();
            this.SenseManager = Session.CreateSenseManager();
        }

        public void Dispose()
        {
        	if(EnabledHand && (handAnalysis != null)) this.handAnalysis.Dispose();
        	
            this.SenseManager.Close();
        }
    }

    [PluginInfo(Name = "Session", Category="RSSDK", AutoEvaluate = true)]
    public class RSSDKSessionNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Input("Enable Stream", IsSingle = true)]
        public ISpread<bool> FEnableStream;
        
        [Input("Stream Settings")]
        public ISpread<RSSDKStreamSettings> FStreamSettings;
        
        [Input("Enable Face", IsSingle = true)]
        public ISpread<bool> FEnableFace;
        
        [Input("Enable Blob", IsSingle = true)]
        public ISpread<bool> FEnableBlob;
        
        [Input("Enable Hand", IsSingle = true)]
        public ISpread<bool> FEnableHand;
        
        [Input("Enable 3DSeg", IsSingle = true)]
        public ISpread<bool> FEnable3DSeg;
        
        [Input("Synchronized", IsSingle = true)]
        public ISpread<bool> FSync;
        
        [Input("Object Expiry", IsSingle = true, DefaultValue = 0.5)]
        public ISpread<double> FExpiry;
        
        [Input("Configure", IsSingle = true, IsBang = true)]
        public ISpread<bool> FConfig;
        
        [Input("Initialize", IsSingle = true, IsBang = true)]
        public ISpread<bool> FInit;
        
        [Input("Close", IsSingle = true, IsBang = true)]
        public ISpread<bool> FClose;
        
        [Output("Sense Wrapper")]
        public ISpread<SenseWrapper> FSenseWrap;
        
       	[Import()]
		public ILogger FLogger;

        public PXCMFaceData.ExpressionsData.FaceExpression[] Expressions =
        {
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_LEFT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_RIGHT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_LEFT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_RIGHT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_LEFT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_RIGHT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_DOWN,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_UP,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_DOWN,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_UP,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_TILT_LEFT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_TILT_RIGHT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_TURN_LEFT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_HEAD_TURN_RIGHT,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_KISS,
            PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_TONGUE_OUT
        };

        public SenseWrapper SenseSession;
        public void OnImportsSatisfied()
        {
            this.SenseSession = new SenseWrapper();
        }

       
        public  static Task t = null;
        public  static CancellationTokenSource ctSource = new CancellationTokenSource();
      	public  static CancellationToken token = ctSource.Token;
      
        //public Thread RSSSDKLoopThread;
        public void RSSDKFrame()
        {
        	FSenseWrap[0].Status = FSenseWrap[0].SenseManager.AcquireFrame(FSync[0]);
            //FSenseWrap[0].FrameStatus = FSenseWrap[0].SenseManager.StreamFrames(false);
			
            if (FEnableStream[0])
            {
                PXCMCapture.Sample sample = FSenseWrap[0].SenseManager.QuerySample();
                if (sample != null)
                {
                    foreach (RSSDKImage img in FSenseWrap[0].Streams.Values)
                    {
                        switch (img.Type)
                        {
                            case PXCMCapture.StreamType.STREAM_TYPE_COLOR:
                                if (sample.color != null)
                                {
                                    img.Source = sample.color;
                                    img.Data = sample.color.GetRGB32Pixels();
                                    img.Width = sample.color.info.width;
                                    img.Height = sample.color.info.height;
                                    img.Format = PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32;
                                }
                                break;

                            case PXCMCapture.StreamType.STREAM_TYPE_DEPTH:
                                if (sample.depth != null)
                                {
                                    img.Source = sample.depth;
                                    img.Data = sample.depth.GetDepthPixels();
                                    img.Width = sample.depth.info.width;
                                    img.Height = sample.depth.info.height;
                                    img.Format = PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH;
                                }
                                break;

                            case PXCMCapture.StreamType.STREAM_TYPE_IR:
                                if (sample.ir != null)
                                {
                                    img.Source = sample.ir;
                                    img.Data = sample.ir.GetY8Pixels();
                                    img.Width = sample.ir.info.width;
                                    img.Height = sample.ir.info.height;
                                    img.Format = PXCMImage.PixelFormat.PIXEL_FORMAT_Y8;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
            
            if(FEnableHand[0])
            {
            	FSenseWrap[0].HandData.Update();
            	/ *
            	PXCMCapture.Sample handSample = FSenseWrap[0].SenseManager.QueryHandSample();
            	if (handSample.depth != null)
                {
                    FSenseWrap[0].handImage.Source = handSample.depth;
                    FSenseWrap[0].handImage.Data = handSample.depth.GetY8Pixels();
                    FSenseWrap[0].handImage.Width = handSample.depth.info.width;
                    FSenseWrap[0].handImage.Height = handSample.depth.info.height;
                    FSenseWrap[0].handImage.Format = PXCMImage.PixelFormat.PIXEL_FORMAT_Y8;
                }
            	* /
            }
            
            if(FEnableBlob[0])
            {
            	PXCMBlobData blobData = FSenseWrap[0].blobData;
            	blobData.Update(); // update to the current blob data
            }
            
            if (FEnableFace[0])
            {
                if (FSenseWrap[0].FaceConfig.FaceData != null)
                {

                    PXCMFaceData FaceData = FSenseWrap[0].FaceConfig.FaceData;
                    FaceData.Update();
                    int FaceCount = FaceData.QueryNumberOfDetectedFaces();
                    for (int i = 0; i < FaceCount; i++)
                    {
                        PXCMFaceData.Face IndividualFace = FaceData.QueryFaceByIndex(i);
                        int userid = IndividualFace.QueryUserID();

                        RSSDKFace FaceBridge;
                        if (FSenseWrap[0].Faces.ContainsKey(userid))
                        {
                            FaceBridge = FSenseWrap[0].Faces[userid];
                            FaceBridge.Expiry.Restart();
                        }
                        else
                        {
                            FaceBridge = new RSSDKFace();
                            FaceBridge.UserID = userid;
                            if (FaceBridge.UserID != -1) FSenseWrap[0].Faces.Add(userid, FaceBridge);
                        }

                        PXCMFaceData.DetectionData ddata = IndividualFace.QueryDetection();
                        if (FSenseWrap[0].FaceConfig.FaceConfig.detection.isEnabled)
                        {
                            if (ddata != null)
                            {
                                PXCMRectI32 rect;
                                ddata.QueryBoundingRect(out rect);
                                FaceBridge.Bounds = rect.ToVector4D();

                                float avgdepth;
                                ddata.QueryFaceAverageDepth(out avgdepth);
                                FaceBridge.AverageDepth = avgdepth;
                            }
                        }

                        PXCMFaceData.PoseData pdata = IndividualFace.QueryPose();
                        if (FSenseWrap[0].FaceConfig.FaceConfig.pose.isEnabled)
                        {
                            if (pdata != null)
                            {
                                PXCMFaceData.HeadPosition hpos;
                                pdata.QueryHeadPosition(out hpos);
                                FaceBridge.WorldPosition = hpos.headCenter.ToVector3D();

                                PXCMFaceData.PoseQuaternion pquat;
                                pdata.QueryPoseQuaternion(out pquat);
                                FaceBridge.Quaternion = pquat.ToVector4D();
                                FaceBridge.YawPitchRoll = pquat.ToYawPitchRoll();
                                FaceBridge.YawPitchRoll.x /= Math.PI * 2;
                                FaceBridge.YawPitchRoll.y /= Math.PI * 2;
                                FaceBridge.YawPitchRoll.z /= Math.PI * 2;
                            }
                        }

                        PXCMFaceData.LandmarksData ldata = IndividualFace.QueryLandmarks();
                        if (FSenseWrap[0].FaceConfig.FaceConfig.landmarks.isEnabled)
                        {
                            if (ldata != null)
                            {
                                PXCMFaceData.LandmarkPoint[] points;
                                ldata.QueryPoints(out points);

                                FaceBridge.Landmarks.SliceCount = points.Length;
                                for (int j = 0; j < points.Length; j++)
                                {
                                    RSSDKLandmark LandmarkBridge;
                                    if (FaceBridge.Landmarks[j] == null)
                                    {
                                        LandmarkBridge = new RSSDKLandmark();
                                        FaceBridge.Landmarks[j] = LandmarkBridge;
                                    }
                                    else
                                        LandmarkBridge = FaceBridge.Landmarks[j];

                                    LandmarkBridge.Index = points[j].source.index;
                                    LandmarkBridge.Type = points[j].source.alias;
                                    LandmarkBridge.ImageConfidence = points[j].confidenceImage;
                                    LandmarkBridge.ImagePosition = points[j].image.ToVector2D();
                                    LandmarkBridge.WorldConfidence = points[j].confidenceWorld;
                                    LandmarkBridge.WorldPosition = points[j].world.ToVector3D();
                                }
                            }
                        }
                        else
                        {
                            FaceBridge.Landmarks.SliceCount = 0;
                        }

                        PXCMFaceData.ExpressionsData edata = IndividualFace.QueryExpressions();
                        if (FSenseWrap[0].FaceConfig.ExpressionConfig.IsEnabled())
                        {
                            if (edata != null)
                            {
                                FaceBridge.Expressions.SliceCount = this.Expressions.Length;
                                for (int j = 0; j < this.Expressions.Length; j++)
                                {
                                    RSSDKExpression ExpressionBridge;
                                    if (FaceBridge.Expressions[j] == null)
                                    {
                                        ExpressionBridge = new RSSDKExpression();
                                        FaceBridge.Expressions[j] = ExpressionBridge;
                                    }
                                    else
                                        ExpressionBridge = FaceBridge.Expressions[j];

                                    PXCMFaceData.ExpressionsData.FaceExpressionResult result;
                                    bool detected = edata.QueryExpression(this.Expressions[j], out result);
                                    ExpressionBridge.Type = this.Expressions[j];
                                    ExpressionBridge.Intensity = result.intensity;
                                    ExpressionBridge.Detected = detected;
                                }
                            }
                        }
                        else
                        {
                            FaceBridge.Expressions.SliceCount = 0;
                        }
                    }
                }
                
                List<int> Removables = new List<int>();
                
                foreach (KeyValuePair<int, RSSDKFace> kvp in FSenseWrap[0].Faces)
                    if (kvp.Value.Expiry.Elapsed.TotalSeconds > FExpiry[0]) Removables.Add(kvp.Key);
                
                foreach (int r in Removables)
                    FSenseWrap[0].Faces.Remove(r);
            }

            FSenseWrap[0].SenseManager.ReleaseFrame();
        }

        public void Evaluate(int SpreadMax)
        {
            FSenseWrap[0] = SenseSession;
            FSenseWrap[0].IsSynchronized = false;

            if (FInit[0] || FConfig[0])
            {
                bool NotConfigured = !FSenseWrap[0].Configured;
                
                 // * Set mirror mode 
                //PXCMCapture.Device.MirrorMode mirror = form.GetMirrorState() ? PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL : PXCMCapture.Device.MirrorMode.MIRROR_MODE_DISABLED;
               // pp.captureManager.device.SetMirrorMode(mirror);


                if (FEnableStream[0] && NotConfigured)
                {
                    for (int i = 0; i < FStreamSettings.SliceCount; i++ )
                    {
                        RSSDKStreamSettings ss = FStreamSettings[i];
                        if (ss != null)
                        {
                            if (!FSenseWrap[0].Streams.ContainsKey(ss.StreamType))
                            {
                                RSSDKImage img = new RSSDKImage();
                                img.Type = ss.StreamType;
                                FSenseWrap[0].Streams.Add(ss.StreamType, img);
                                FSenseWrap[0].StreamSettings.Add(ss.StreamType, ss);
                            }
                        }
                    }
                    foreach (RSSDKStreamSettings ss in FSenseWrap[0].StreamSettings.Values)
                    {
                        FSenseWrap[0].SenseManager.EnableStream(ss.StreamType, ss.Width, ss.Height, ss.FPS);
                    }
                }
                
                if(FEnableHand[0] && NotConfigured)
	            {
                	
                	FSenseWrap[0].handImage = new RSSDKImage();
                		
                	FSenseWrap[0].EnabledHand = true;
                	FSenseWrap[0].Status = FSenseWrap[0].SenseManager.EnableHand();
                	FSenseWrap[0].handAnalysis = FSenseWrap[0].SenseManager.QueryHand();
                	
                	if ( (FSenseWrap[0].Status == pxcmStatus.PXCM_STATUS_NO_ERROR) && (FSenseWrap[0].handAnalysis != null) )
                	{
                		FSenseWrap[0].handConfig= FSenseWrap[0].handAnalysis.CreateActiveConfiguration();
                	
	                	//config the hand tracker
	                	//FSenseWrap[0].handConfig.EnableStabilizer(true);
	                	FSenseWrap[0].handConfig.DisableAllGestures();
	                	FSenseWrap[0].handConfig.EnableNormalizedJoints(true);
	                	FSenseWrap[0].handConfig.SetTrackingMode(PXCMHandData.TrackingModeType.TRACKING_MODE_FULL_HAND);
	                	FSenseWrap[0].handConfig.EnableSegmentationImage(true);
	                	//FSenseWrap[0].handConfig.SetSmoothingValue((float).5);
						
						
						FSenseWrap[0].HandData = FSenseWrap[0].handAnalysis.CreateOutput();
						if (FSenseWrap[0].HandData == null)
			            {
			                FLogger.Log(LogType.Debug, "RSSDK: Failed Create Output");
			                return;
			            }
                	}
		            else
                	{
		               FLogger.Log(LogType.Debug,"RSSDK: Failed Loading Hand Module");
		                
		            }
                	
                	
                	
	            }
                
                if(FEnableBlob[0] && NotConfigured)
	            {
                	FSenseWrap[0].SenseManager.EnableBlob();
                	FSenseWrap[0].EnabledBlob = true;
                	FSenseWrap[0].blobModule = FSenseWrap[0].SenseManager.QueryBlob();
                	FSenseWrap[0].blobConfig = FSenseWrap[0].blobModule.CreateActiveConfiguration();
                	// Configure the blob module
					FSenseWrap[0].blobConfig.SetSegmentationSmoothing(0.1f);
					FSenseWrap[0].blobConfig.EnableContourExtraction(true);
					FSenseWrap[0].blobConfig.EnableSegmentationImage(true);
					FSenseWrap[0].blobConfig.EnableStabilizer(false);
					FSenseWrap[0].blobConfig.SetMaxBlobs(4);
					FSenseWrap[0].blobConfig.SetMaxDistance(10000);
					// Apply the new configuration values
					FSenseWrap[0].blobConfig.ApplyChanges();
					
					// Create an output.

					FSenseWrap[0].blobData = FSenseWrap[0].blobModule.CreateOutput();
	            }
	                
                if (FEnableFace[0] && NotConfigured)
                {
                    FSenseWrap[0].SenseManager.EnableFace();
                    PXCMFaceModule FaceModule = FSenseWrap[0].SenseManager.QueryFace();
                    FSenseWrap[0].FaceConfig = new RSSDKFaceConfig();
                    FSenseWrap[0].FaceConfig.FaceConfig = FaceModule.CreateActiveConfiguration();
                    FSenseWrap[0].FaceConfig.FaceData = FaceModule.CreateOutput();
                    FSenseWrap[0].FaceConfig.ExpressionConfig = FSenseWrap[0].FaceConfig.FaceConfig.QueryExpressions();
                    FSenseWrap[0].FaceConfig.ExpressionConfig.EnableAllExpressions();
                    FSenseWrap[0].FaceConfig.RecognitionConfig = FSenseWrap[0].FaceConfig.FaceConfig.QueryRecognition();
                }
                
                FSenseWrap[0].Configured = true;
                
                if (FInit[0])
                {
                    FSenseWrap[0].Initialized = true;
                    FSenseWrap[0].Status = FSenseWrap[0].SenseManager.Init();
                    if(FSenseWrap[0].Status ==  pxcmStatus.PXCM_STATUS_NO_ERROR)
                    {
                    	if(FSenseWrap[0].EnabledHand)
                    	{
                    		FSenseWrap[0].handConfig.ApplyChanges();
							FSenseWrap[0].handConfig.Update();
                    	}
                    	
                    	//free the reading task
                    	if (t != null)
		            	{
		            		ctSource.Cancel();
		            	}
                    	
                    	//start the reading task
                    	t = Task.Factory.StartNew(() =>
		        		{
		                	while (!token.IsCancellationRequested)
		       				{	
		                		RSSDKFrame();
		                	}
		            	}, token); //, TaskCreationOptions.LongRunning
                    }
                    else
                    {
                    	FLogger.Log(LogType.Debug,"RSSDK: Error on Init - " + FSenseWrap[0].Status.ToString());
                    }
						
                   
	            	
	               	
		                    
	        	}
            }
            
            if(FSenseWrap[0].Initialized) //stop/re-start reading thread!!!
            {
            	
            }
            
            if(FClose[0])
            {
            	ctSource.Cancel();
            }
        }
    }
    	*/
}
