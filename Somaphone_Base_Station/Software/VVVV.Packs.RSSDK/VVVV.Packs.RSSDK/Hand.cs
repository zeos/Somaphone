/*
 * Created by SharpDevelop.
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
using libpxcclr;
using libpxcclr.cs;

namespace VVVV.Nodes.RSSDK
{
	[PluginInfo(Name = "HandTracker", Category="RSSDK", AutoEvaluate = true)]
    public class RSSDKHandTrackerNode : IPluginEvaluate
    {
        [Input("Sense Wrapper")]
        public ISpread<RSSDKContext> FSenseWrap;
        
        [Output("ID")]
        public ISpread<int> FID;
        
        [Output("IsCalibrated")]
        public ISpread<bool> FIsCalibrated;
         
        [Output("HasSegmentationImage")]
        public ISpread<bool> FHasSegmentationImage; 
        
        [Output("HasTrackedJoints")]
        public ISpread<bool> FHasTrackedJoints;
        
        [Output("Tracked")]
        public ISpread<string>FTrackingStatus;
         
        [Output("BoundingBox")]
        public ISpread<Vector4D> FBBox;
        
        [Output("MassCenterImage")]
        public ISpread<Vector2D> FMassCenterImage;
        
        [Output("MassCenterWorld")]
        public ISpread<Vector3D> FMassCenterWorld;
        
        [Output("Joints(Image)")]
        public ISpread<ISpread<Vector3D>> FJointsPositionImage;
        
        [Output("Joints(World)")]
        public ISpread<ISpread<Vector3D>> FJointsPositionWorld;
        
        [Output("Joints(Orientation)")]
        public ISpread<ISpread<Vector4D>> FJointsOrientation;
         
        
     	[Output("Image")]
        public ISpread<RSSDKImage> FImage;
        
     	public void Evaluate(int SpreadMax)
        {
        	if( (FSenseWrap.SliceCount > 0) && (FSenseWrap[0].Configured) )
        	{
        		
        		PXCMImage image = null;
        		/*
        		if (FSenseWrap[0].handImage != null)
                {
                    if (FSenseWrap[0].handImage.Source != null)
                    {
                        image = FSenseWrap[0].handImage.Source;
                    }
        		}
        		*/
        		try
                {
        			FID.SliceCount = 0;
        			FImage.SliceCount = 0;
        			int numHands = FSenseWrap[0].HandData.QueryNumberOfHands();
        				
        			FJointsPositionImage.SliceCount = numHands;
        			FJointsPositionWorld.SliceCount = numHands;
        			FJointsOrientation.SliceCount = numHands;
        			
                    for (int j = 0; j < numHands; j++)
                    {
                        int id;
                        

                        FSenseWrap[0].HandData.QueryHandId(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, j, out id);
                        FID.Add(id);
                        
                        //Get hand by time of appearance
                        PXCMHandData.IHand handInfo;
                        FSenseWrap[0].HandData.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, j, out handInfo);
                        
                        if (handInfo != null)
                        {
                            FIsCalibrated[j] = handInfo.IsCalibrated();
                            FHasSegmentationImage[j] = handInfo.HasSegmentationImage();
                            FHasTrackedJoints[j] = handInfo.HasTrackedJoints();
                            FTrackingStatus[j] = handInfo.QueryTrackingStatus().ToString();
                            FBBox[j] = handInfo.QueryBoundingBoxImage().ToVector4D();
                            FMassCenterImage[j] = handInfo.QueryMassCenterImage().ToVector2D();
                            FMassCenterWorld[j] = handInfo.QueryMassCenterWorld().ToVector3D();
                           
                            FJointsPositionImage[j].SliceCount = 0x20;
                            FJointsPositionWorld[j].SliceCount = 0x20;
                            FJointsOrientation[j].SliceCount = 0x20;
                            
                            //Iterate Joints
                        	for (int n = 0; n < 0x20; n++)
                        	{
                           	 	PXCMHandData.JointData jointData;
                            	handInfo.QueryTrackedJoint((PXCMHandData.JointType)n, out jointData);
                            	FJointsPositionImage[j][n] 	= jointData.positionImage.ToVector3D();
                            	FJointsPositionWorld[j][n] 	= jointData.positionWorld.ToVector3D();
                            	FJointsOrientation[j][n] 	= jointData.globalOrientation.ToVector4D();

                        	} // end iterating over joints
                        	
                            /*  
 							PXCMImage.ImageData data;
 							if (handData != null)	&& (handData.QuerySegmentationImage(out image) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                        	if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out data) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                            {
                               
                            	
                            	FImage[j] = new RSSDKImage();
                            	FImage[j].Source = image;
                            	FImage[j].Data = image.GetY8Pixels();
                            	FImage[j].Source = image;
                                FImage[j].Width = image.info.width;
                                FImage[j].Height = image.info.height;
                                FImage[j].Format = PXCMImage.PixelFormat.PIXEL_FORMAT_Y8;
                                
                                image.ReleaseAccess(data); 
                            	 
                            }
                            */
                        }
                    }
                    
                   
                    if (image != null)
                    {
                        image.Dispose();
                    }
                }
                catch (Exception)
                {
                    
                    if (image != null)
                    {
                        image.Dispose();
                    }
                }
            	

        	}
        	
    	}

    }
}

