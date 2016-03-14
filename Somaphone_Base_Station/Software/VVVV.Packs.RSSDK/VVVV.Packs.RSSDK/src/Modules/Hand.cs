/*
 * Created by Antony Rayzhekov a.k.a Zeos
 * User: Intel
 * Date: 7/19/2015
 * Time: 5:47 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
 
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
	[PluginInfo(Name = "HandTracker", Category="RSSDK", AutoEvaluate = true)]
    public class RSSDKHandTrackerNode : IPluginEvaluate
    {
        [Input("Context")]
        public ISpread<RSSDKContext> FContext;
        
        [Input("Smooth")]
        public IDiffSpread<float> FSmooth;
        
        [Input("Stabilizer")]
        public IDiffSpread<bool> FStabilizer;
        
        [Input("Track Joints")]
        public IDiffSpread<bool> FEnableTrackedJoints;
        
        [Input("Normalized Joints")]
        public IDiffSpread<bool> FNormalizedJoints;
        
        [Input("Segmentation Image")]
        public IDiffSpread<bool> FEnableSegmentationImage;
        
        [Input("Contours", IsSingle = true)]
        public IDiffSpread<bool> FEnableContours;
        
        [Input("Mode")]
        public IDiffSpread<PXCMHandData.TrackingModeType> FTrackingMode;
        
        [Input("Tracking Bounds")]
        public IDiffSpread<Vector4D> FTrackingBoundsIn;
        
        [Input("Reset Tracking", IsBang = true, IsSingle = true)]
        public IDiffSpread<bool> FResetTracking;
        
        [Input("Reset", IsBang = true, IsSingle = true)]
        public IDiffSpread<bool> FReset;
        
//--OUTPUTS
        [Output("NumHands")]
        public ISpread<int> FNumHands;
        
        [Output("ID")]
        public ISpread<int> FID;
        
        //--status
        
        [Output("isCalibrated")]
        public ISpread<bool> FIsCalibrated;
        
        [Output("isTracked")]
        public ISpread<bool>FTracked;
        
        [Output("isMovingTooFast")]
        public ISpread<bool>FMovingTooFast;
        
        [Output("isOutsideFOV")]
        public ISpread<bool>FOutsideFOV;
        
        [Output("isOutsideDepthRane")]
        public ISpread<bool>FOutsideDepthRange;
        
        [Output("isPointingFingers")]
        public ISpread<bool> FPointingFingers;
        
        [Output("BodySide")]
        public ISpread<string> FSide;
        
        [Output("IsOpen")]
        public ISpread<double> FOpen;
        
        [Output("Tracking Bounds")]
        public ISpread<Vector4D> FTrackingBoundsOut;
        
        [Output("BoundingBox")]
        public ISpread<Vector4D> FBBox;
        
        [Output("MassCenterImage")]
        public ISpread<Vector2D> FMassCenterImage;
        
        [Output("MassCenterWorld")]
        public ISpread<Vector3D> FMassCenterWorld;
        
        [Output("PalmOrientation")]
        public ISpread<Vector3D> FPalmOrientation;
        
        [Output("PalmRadiusImage")]
        public ISpread<double> FPalmRadiusImage;
        
        [Output("PalmRadiusWorld")]
        public ISpread<double> FPalmRadiusWorld;
        
        [Output("Contour")]
        public ISpread<ISpread<Vector2D>>FContour;
        
        //JOINTS
        
        [Output("HasTrackedJoints")]
        public ISpread<bool> FHasTrackedJoints;
        
        [Output("Joints Confidence")]
        public ISpread<ISpread<double>>FJointsConfidence;
        
        [Output("Joints(Image)")]
        public ISpread<ISpread<Vector3D>> FJointsPositionImage;
        
        [Output("Joints(World)")]
        public ISpread<ISpread<Vector3D>> FJointsPositionWorld;
        
        [Output("Joints(Orientation)")]
        public ISpread<ISpread<Vector3D>> FJointsOrientation;
        
        //IMAGE
         
        [Output("HasSegmentationImage")]
        public ISpread<bool> FHasSegmentationImage; 
        
     	[Output("Image")]
        public ISpread<RSSDKImage> FImage;
        
        [Import()]
		public ILogger FLogger;
        
		private bool updateActiveConfig = false;
        
        //Hand TrackingBounds frustum 
        private Single nearTrackingDistance, farTrackingDistance, nearTrackingWidth, nearTrackingHeight;
        private PXCMImage image;
        
     	public void Evaluate(int SpreadMax)
        {
        	if( (FContext.SliceCount > 0) && (FContext[0].Configured) && FContext[0].HandEnabled)
        	{
        		
        	
        		processConfig();
        		
            	/*
        		if (FContext[0].handImage != null)
                {
                    if (FContext[0].handImage.Source != null)
                    {
                        image = FContext[0].handImage.Source;
                    }
        		}
        		*/
        		
        		try
                {
        			FID.SliceCount = 0;
        			
        			int numHands = FContext[0].handData.QueryNumberOfHands();
        			FNumHands[0] = numHands;
        			
        			FJointsPositionImage.SliceCount = numHands;
        			FJointsPositionWorld.SliceCount = numHands;
        			FJointsOrientation.SliceCount = numHands;
        			FJointsConfidence.SliceCount = numHands;
        			
        			FIsCalibrated.SliceCount = numHands;
        			FTracked.SliceCount = numHands;
        			FOutsideFOV.SliceCount = numHands;
        			FMovingTooFast.SliceCount = numHands;
        			FOutsideDepthRange.SliceCount = numHands;
        			FPointingFingers.SliceCount = numHands;
        			
        			FOpen.SliceCount = numHands;
        			FSide.SliceCount = numHands;
        			
        			FPalmOrientation.SliceCount = numHands;
        			FPalmRadiusImage.SliceCount = numHands;
        			FPalmRadiusWorld.SliceCount = numHands;
        			
        			FBBox.SliceCount = numHands;
        			FMassCenterImage.SliceCount = numHands;
        			FMassCenterWorld.SliceCount = numHands;
        			FHasSegmentationImage.SliceCount = numHands; //!!!
        			
        			PXCMRotation rotation;
                    FContext[0].Session.CreateImpl<PXCMRotation>(out rotation);
        			
                    
                    PXCMCapture.Sample HandSample = FContext[0].sm.QueryHandSample();
                    if (HandSample != null && HandSample.depth != null)
                    {
                    	image = HandSample.depth;
                    	if(FImage[0] == null)
                    	{
                    		FImage.SliceCount = 1;
                    		FImage[0] = new RSSDKImage();
                    	}
                    } 
                    else image = null;
        		
                    
                    for (int j = 0; j < numHands; j++)
                    {
                        int id;
                       
						FContext[0].handData.QueryHandId(PXCMHandData.AccessOrderType.ACCESS_ORDER_FIXED, j, out id);
                        FID.Add(id);
                        
                        //Get hand by time of appearance
                        PXCMHandData.IHand handInfo;
                        FContext[0].handData.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_FIXED, j, out handInfo);
                        
                        if(handInfo != null)
                        {
                        	
                        	if(FEnableContours[0])
                        	{
	                        	//Extract Hand Contours
	                        	int nContours = handInfo.QueryNumberOfContours();
	                        	FContour.SliceCount = nContours;
	                        	
	                        	for(int contourIndex = 0; contourIndex < nContours; contourIndex++)
	                        	{
	                        		PXCMHandData.IContour contourData;
	                        		handInfo.QueryContour(contourIndex, out contourData);
	                        		
	                        		int contourSize = contourData.QuerySize();
	                        		if(contourData.IsOuter() && (contourSize>0))
	                        		{
	                        			PXCMPointI32[] ContourPoints = new PXCMPointI32[contourSize];
	                        			if(contourData.QueryPoints(out ContourPoints) == pxcmStatus.PXCM_STATUS_NO_ERROR)
	                        			{
	                        				FContour[contourIndex].ResizeAndDismiss(ContourPoints.Length);
		                               		for(int ContourPointIndex = 0; ContourPointIndex < ContourPoints.Length; ContourPointIndex++) 
		                               		{
		                               			FContour[contourIndex][ContourPointIndex] = new Vector2D(ContourPoints[ContourPointIndex].x / (float)FContext[0].DepthImageSize.x * 2.0f - 1.0f, 1.0f - ContourPoints[ContourPointIndex].y / (float)FContext[0].DepthImageSize.y * 2.0f);
		                               		}
	                        			}
	                        		}
	                        		
	                        	}
                        	}
                        	
                            FIsCalibrated[j] = handInfo.IsCalibrated();
                            FHasSegmentationImage[j] = handInfo.HasSegmentationImage();
                            var HasTrackedJoints = handInfo.HasTrackedJoints();
                            FHasTrackedJoints[j] = HasTrackedJoints;
                        	//check status
                            PXCMHandData.TrackingStatusType HandTrackingStatus = (PXCMHandData.TrackingStatusType)handInfo.QueryTrackingStatus();
                            FTracked[j] = HandTrackingStatus.HasFlag(PXCMHandData.TrackingStatusType.TRACKING_STATUS_GOOD);
                            FOutsideFOV[j] = HandTrackingStatus.HasFlag(PXCMHandData.TrackingStatusType.TRACKING_STATUS_OUT_OF_FOV);
                            FMovingTooFast[j] = HandTrackingStatus.HasFlag(PXCMHandData.TrackingStatusType.TRACKING_STATUS_HIGH_SPEED);
                            FOutsideDepthRange[j] = HandTrackingStatus.HasFlag(PXCMHandData.TrackingStatusType.TRACKING_STATUS_OUT_OF_RANGE);
                            FPointingFingers[j] = HandTrackingStatus.HasFlag(PXCMHandData.TrackingStatusType.TRACKING_STATUS_POINTING_FINGERS);
                            
                            FOpen[j] 			=  (float) handInfo.QueryOpenness() / 100.0;
                            FSide[j] 			= handInfo.QueryBodySide().ToString();
                            
                            rotation.SetFromQuaternion(handInfo.QueryPalmOrientation());     
                            FPalmOrientation[j] = rotation.QueryEulerAngles().ToVector3D();
                            
                            FPalmRadiusImage[j] = handInfo.QueryPalmRadiusImage();
                            FPalmRadiusWorld[j] = handInfo.QueryPalmRadiusWorld();
                            FBBox[j] 			= handInfo.QueryBoundingBoxImage().BBox2DtoVVVV(FContext[0].DepthImageSize);
                            FMassCenterImage[j] = handInfo.QueryMassCenterImage().ToVector2D().Vector2DtoVVVV(FContext[0].DepthImageSize);
                            FMassCenterWorld[j] = handInfo.QueryMassCenterWorld().ToVector3D();
                           
                            if( HasTrackedJoints)
                            {
	                            FJointsPositionImage[j].SliceCount = 0x20;
	                            FJointsPositionWorld[j].SliceCount = 0x20;
	                            FJointsOrientation[j].SliceCount = 0x20;
	                            FJointsConfidence[j].SliceCount = 0x20;
	                            //Iterate Joints
	                        	for (int n = 0; n < 0x20; n++)
	                        	{
	                           	 	PXCMHandData.JointData jointData;
	                           	 	
	                            	handInfo.QueryTrackedJoint((PXCMHandData.JointType)n, out jointData);
	                            		
	                            	FJointsPositionImage[j][n] 	= jointData.positionImage.ToVector3D().Vector3DtoVVVV(FContext[0].DepthImageSize);
	                            	FJointsConfidence[j][n]		= jointData.confidence / 100;
	                            	FJointsPositionWorld[j][n] 	= jointData.positionWorld.ToVector3D();
	                            	
	                            	
               						rotation.SetFromQuaternion(jointData.globalOrientation);        
									FJointsOrientation[j][n] 	= rotation.QueryEulerAngles().ToVector3D();
	
	                        	} // end iterating over joints
                            }
                            else
                            {
                            	FJointsPositionImage[j].SliceCount = 0;
	                            FJointsPositionWorld[j].SliceCount = 0;
	                            FJointsOrientation[j].SliceCount = 0;
                            }
                           
                           // blend images with different colors!!!
 							PXCMImage.ImageData data;
 							if ( (image != null) && (FHasSegmentationImage[j]) && (handInfo.QuerySegmentationImage(out image) >= pxcmStatus.PXCM_STATUS_NO_ERROR))
 							{
 								//FImage.SliceCount++;
	                        	if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out data) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
	                            {
	                        		if(j == 0)
	                            	{
	                            		FImage[0].Source = image;
	                            		FImage[0].Data = image.GetY8Pixels();
	                            		FImage[0].Source = image;
		                                FImage[0].Width = image.info.width;
		                                FImage[0].Height = image.info.height;
		                                FImage[0].Format = PXCMImage.PixelFormat.PIXEL_FORMAT_Y8;
	                            	}
	                            	else
	                            	{
	                            		var d = image.GetY8Pixels();
	                            		for(int e = 0; e < d.Length; e++) FImage[0].Data[e] += d[e];
	                            	}
	                            	image.ReleaseAccess(data); 
	                            	 
	                            }
 							}
 							
 							
                        }
                    }
                    
                   
                   // if (image != null)
                   // {
                    //    image.Dispose();
                    //}
                }
                catch (Exception e)
                {
                    
                	FLogger.Log(LogType.Debug, "Hand Tracker: " + e.Message.ToString());
                }
            	

        	}
        	
    	}
     	
     	private void processConfig()
     	{
     		//config the hand tracker
    		if(FStabilizer.IsChanged) 
    		{
    			FContext[0].handConfig.EnableStabilizer(FStabilizer[0]);
    			updateActiveConfig = true;
    		}
        	
        	if(FNormalizedJoints.IsChanged) 
        	{
        		FContext[0].handConfig.EnableNormalizedJoints(FNormalizedJoints[0]);
        		updateActiveConfig = true;
        	}
        	
        	if(FTrackingMode.IsChanged) 
        	{
        		FContext[0].handConfig.SetTrackingMode(FTrackingMode[0]); //PXCMHandData.TrackingModeType.TRACKING_MODE_FULL_HAND
        		updateActiveConfig = true;
        	}
        	
        	if(FEnableSegmentationImage.IsChanged) 
        	{
        		FContext[0].handConfig.EnableSegmentationImage(FEnableSegmentationImage[0]);
        		updateActiveConfig = true;
        	}
        	
        	//pxcmStatus EnableJointSpeed(JointType label, JointSpeedType speed, Int32 time);
        	//FContext[0].handConfig.EnableJointSpeed(PXCMHandData.JointType)
        	//FContext[0].handConfig.DisableJointSpeed
        	
			if(FTrackingBoundsIn.IsChanged)
        	{
				if(FTrackingBoundsIn[0].Length < 0.000001)
        		{
        			FContext[0].handConfig.QueryTrackingBounds(out nearTrackingDistance, out farTrackingDistance, out nearTrackingWidth, out nearTrackingHeight);
        			FLogger.Log(LogType.Debug, "Using current bounds.");
				}
        		else
        		{
        			nearTrackingDistance 	= (float)FTrackingBoundsIn[0].x;
        			farTrackingDistance 	= (float)FTrackingBoundsIn[0].y;
        			nearTrackingWidth 		= (float)FTrackingBoundsIn[0].z;
        			nearTrackingHeight		= (float)FTrackingBoundsIn[0].w;
        			
        			FContext[0].handConfig.SetTrackingBounds(nearTrackingDistance, farTrackingDistance, nearTrackingWidth, nearTrackingHeight);
					updateActiveConfig = true;
        		}
        	}
			
			if(FReset.IsChanged && FReset[0])
        	{
				FContext[0].handConfig.RestoreDefaults();
				updateActiveConfig = true;
			}

        	if(FSmooth.IsChanged) 
        	{
        		FContext[0].handConfig.SetSmoothingValue(FSmooth[0]);
        		updateActiveConfig = true;
        	}
        	
        	if(FEnableTrackedJoints.IsChanged) 
        	{
        		FContext[0].handConfig.EnableTrackedJoints(FEnableTrackedJoints[0]);
        		updateActiveConfig = true;
        	}
        	
        	if(FResetTracking.IsChanged && FResetTracking[0])
        	{
        		FContext[0].handConfig.ResetTracking();
        		updateActiveConfig = true;
        	}
        	
    		if(updateActiveConfig == true) 
        	{
        		FContext[0].handConfig.ApplyChanges();
        		FContext[0].handConfig.Update();
        		updateActiveConfig = false;
        		
        		FContext[0].handConfig.QueryTrackingBounds(out nearTrackingDistance, out farTrackingDistance, out nearTrackingWidth, out nearTrackingHeight);
        		FTrackingBoundsOut[0] = new Vector4D(nearTrackingDistance, farTrackingDistance, nearTrackingWidth, nearTrackingHeight);
        	}
     	}

    }
    
}

