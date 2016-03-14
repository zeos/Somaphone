/*
 * Created by SharpDevelop.
 * User: Intel
 * Date: 7/19/2015
 * Time: 11:04 PM
 * 
 * https://software.intel.com/sites/landingpage/realsense/camera-sdk/2014gold/documentation/html/index.html?manuals_extracting_blob_data.html
 */

// Get an instance of PXCMBlobModule
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
	[PluginInfo(Name = "BlobTracker", Category="RSSDK", AutoEvaluate = true)]
    public class RSSDKBlobTrackerNode : IPluginEvaluate
    {
        [Input("Context")]
        public ISpread<RSSDKContext> FContext;
        
        [Input("Stabilizer", DefaultValue = 1)]
        public IDiffSpread<bool> FStabilizerIn;
        
        [Input("Smooth", DefaultValue = 1, MinValue = 0, MaxValue = 1)]
        public IDiffSpread<double> FSmoothIn;
        
        [Input("Max Blobs", DefaultValue = 1, MinValue = 0, MaxValue = 4)]
        public IDiffSpread<int> FMaxBlobsIn;

 		[Input("Max Distance", DefaultValue = 550)]
        public IDiffSpread<double> FMaxDistanceIn;
        
        [Input("Max Object Depth", DefaultValue = 300)]
        public IDiffSpread<double> FMaxObjectDepthIn;
        
        [Input("Min Contour Size", DefaultValue = 30)]
        public IDiffSpread<int> FMinContourSizeIn;
        
        [Input("Contours", DefaultValue = 1)]
        public IDiffSpread<bool> FContoursIn;
        
        [Input("Segmentation Image", DefaultValue = 0)]
        public IDiffSpread<bool> FSegmentationImageIn;
        
        [Input("Segmentation Smoothing", DefaultValue = 0, MinValue = 0, MaxValue = 1)]
        public IDiffSpread<double> FSegmentationSmoothingIn;
        
        [Input("Reset", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FReset;
        
        
   // OUTPUTS  
 		[Output("Blobs Count")]
        public ISpread<int> FBlobsCount;
        
        [Output("CenterOfMass")]
        public ISpread<Vector3D> FCenter;
        
        [Output("Closest Point")]
        public ISpread<Vector3D> FClosest;
        
    	[Output("Outer Contours")]
        public ISpread<ISpread<Vector2D>> FOuterPoints;
        
        [Output("Inner Contours")]
        public ISpread<ISpread<Vector2D>> FInnerPoints;
        
        [Output("HasSegmentationImage")]
        public ISpread<bool> FHasSegmentationImage; 
        
     	[Output("Image")]
        public ISpread<RSSDKImage> FImage;
        
        [Import()]
		public ILogger FLogger;
		
      	private bool updateActiveConfig = false;
      
        private void processConfig()
        {
        	// Configure the blob module
        	
        	//FContext[0].blobConfig.
        		
			if(FSmoothIn.IsChanged)
        	{
        		FContext[0].blobConfig.SetContourSmoothing((float)FSmoothIn[0]);
				updateActiveConfig = true;
        	}
        	
        	if(FMaxDistanceIn.IsChanged)
        	{
        		FContext[0].blobConfig.SetMaxDistance((float)FMaxDistanceIn[0]);
				updateActiveConfig = true;
        	}
        	
        	if(FMaxBlobsIn.IsChanged)
        	{
        		FContext[0].blobConfig.SetMaxBlobs(FMaxBlobsIn[0]);
				updateActiveConfig = true;
        	}
        	
        	if(FSegmentationImageIn.IsChanged)
        	{
        		FContext[0].blobConfig.EnableSegmentationImage(FSegmentationImageIn[0]);
        		FHasSegmentationImage[0] = FSegmentationImageIn[0]; //!!!
				updateActiveConfig = true;
        	}
        	
        	if(FContoursIn.IsChanged)
        	{
        		FContext[0].blobConfig.EnableContourExtraction(FContoursIn[0]);
				updateActiveConfig = true;
        	}
        	
        	if(FStabilizerIn.IsChanged)
        	{
        		FContext[0].blobConfig.EnableStabilizer(FStabilizerIn[0]);
				updateActiveConfig = true;
        	}
				
			if(FSegmentationSmoothingIn.IsChanged)
        	{
				FContext[0].blobConfig.SetSegmentationSmoothing((float)FSegmentationSmoothingIn[0]);
				updateActiveConfig = true;
        	}
			
			if(FMaxObjectDepthIn.IsChanged)
        	{
				FContext[0].blobConfig.SetMaxObjectDepth((float)FMaxObjectDepthIn[0]);
				updateActiveConfig = true;
        	}
			
			if(FMinContourSizeIn.IsChanged)
        	{
				FContext[0].blobConfig.SetMinContourSize(FMinContourSizeIn[0]);
				updateActiveConfig = true;
        	}
			
			if(FReset.IsChanged && FReset[0])
			{
				FContext[0].blobConfig.RestoreDefaults();
			}
			
			// Apply the new configuration values
			if(updateActiveConfig)
			{
				FContext[0].blobConfig.ApplyChanges();
				FContext[0].blobConfig.Update();
				updateActiveConfig = false;
			}
        		
        }
        
        public void Evaluate(int SpreadMax)
        {
        	if( (FContext.SliceCount > 0) && (FContext[0].Configured) )
        	{
        		processConfig();
        		
        		// Iterate over blobs from right to left
				try
				{
			   		Int32 iBlobsNum = FContext[0].blobData.QueryNumberOfBlobs();
			   		
			   		FCenter.SliceCount = iBlobsNum;
			   		FOuterPoints.SliceCount = iBlobsNum;
			   		FInnerPoints.SliceCount = iBlobsNum;
			   		FClosest.SliceCount = iBlobsNum;
			   		FBlobsCount[0] = iBlobsNum;
			   		var ImageSize =  FContext[0].DepthImageSize;
			   		FImage.SliceCount = iBlobsNum;
			   		
					PXCMBlobData.IBlob[] blobList = new PXCMBlobData.IBlob[iBlobsNum];
            			
			   		for (int i = 0; i < iBlobsNum; i++) 
			   		{
			   			FContext[0].blobData.QueryBlob(i, 
			   			                               PXCMBlobData.SegmentationImageType.SEGMENTATION_IMAGE_DEPTH,
			   			                               PXCMBlobData.AccessOrderType.ACCESS_ORDER_NEAR_TO_FAR, 
			   			                               out blobList[i]);
			   			
			   			// handle extracted blob data
						Int32 nContours = blobList[i].QueryNumberOfContours();
			       		if (nContours > 0)
                        {
			       				
			       			//contourPts
							for (int k = 0; k < nContours; ++k)
                            {
								PXCMBlobData.IContour ContourData;
								blobList[i].QueryContour(k, out ContourData);
								
								int contourSize = ContourData.QuerySize();
								
								PXCMPointI32[] ContourPoints = new PXCMPointI32[contourSize];
           		 				
            					ContourData.QueryPoints(out ContourPoints);
                               	
                               	if(ContourPoints.Length > 0)
                               	{
                               		if( (ContourData.IsOuter() == true)  )
	                               	{
	                               		FOuterPoints[i].SliceCount = ContourPoints.Length;	
	                               		for(int index = 0; index < ContourPoints.Length; index++) FOuterPoints[i][index] = new Vector2D(ContourPoints[index].x / (float)ImageSize.x * 2.0f - 1.0f, 1.0f - ContourPoints[index].y / (float)ImageSize.y * 2.0f); 
	                               		//Parallel.For (0, ContourPoints.Length, index => { FOuterPoints[i][index] = new Vector2D(ContourPoints[index].x / (float)ImageSize.x * 2.0f - 1.0f, 1.0f - ContourPoints[index].y / (float)ImageSize.y * 2.0f); );
	                               	}
                               		/*
	                               	else
	                               	{
	                               		FInnerPoints[i].SliceCount = ContourPoints.Length;	
	                               		for (int p = 0; p < ContourPoints.Length; p++)
										{
	                               			Parallel.For (0, ContourPoints.Length, 
	                               		              	  index => { FInnerPoints[i][index] = new Vector2D(ContourPoints[index].x / (float)ImageSize.x * 2.0f - 1.0f, 1.0f - ContourPoints[index].y / (float)ImageSize.y * 2.0f); }
	                               		             );
											
										}
	                               	}
                               		*/
                               	}
                               
                            }
							
							PXCMImage BlobImage;
							if (FContext[0].blobConfig.IsSegmentationImageEnabled() && blobList[i].QuerySegmentationImage(out BlobImage) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
							{
								
								PXCMImage.ImageData BlobImageData;
								if (BlobImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out BlobImageData) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
	                            {
	                                FImage[i] = new RSSDKImage();
	                            	FImage[i].Source = BlobImage;
	                            	FImage[i].Data = BlobImage.GetY8Pixels();
	                            	FImage[i].Source = BlobImage;
	                                FImage[i].Width = BlobImage.info.width;
	                                FImage[i].Height = BlobImage.info.height;
	                                FImage[i].Format = PXCMImage.PixelFormat.PIXEL_FORMAT_Y8;
	                                
	                                BlobImage.ReleaseAccess(BlobImageData); 
	                            	 
	                            }
								
			       			}
			       		
			       		}
			       		
			       		FCenter[i] 	= blobList[i].QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_CENTER).ToVector3D();
			       		FClosest[i] = blobList[i].QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_CLOSEST).ToVector3D();
			       		
			   		}
			   		
			   		
			
			   	}
				catch (Exception)
                {
					//Flogger.Log();
				}
        	}
        }
    }
}

