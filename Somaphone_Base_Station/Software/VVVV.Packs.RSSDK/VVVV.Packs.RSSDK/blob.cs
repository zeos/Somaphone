/*
 * Created by SharpDevelop.
 * User: Intel
 * Date: 7/19/2015
 * Time: 11:04 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
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
using libpxcclr;
using libpxcclr.cs;

namespace VVVV.Nodes.RSSDK
{
	[PluginInfo(Name = "BlobTracker", Category="RSSDK", AutoEvaluate = true)]
    public class RSSDKBlobTrackerNode : IPluginEvaluate
    {
        [Input("Sense Wrapper")]
        public ISpread<RSSDKContext> FSenseWrap;
        
        [Output("CenterOfMass")]
        public ISpread<Vector3D> FCenter;
        
        [Output("Contours")]
        public ISpread<ISpread<Vector2D>> FPoints;
        
        public void Evaluate(int SpreadMax)
        {
        	if( (FSenseWrap.SliceCount > 0) && (FSenseWrap[0].Configured) )
        	{
        		var ImageSize = FSenseWrap[0].sm.captureManager.QueryImageSize(PXCMCapture.StreamType.STREAM_TYPE_DEPTH);
        		
        		// Iterate over blobs from right to left
				try
				{
			   		Int32 iBlobsNum = FSenseWrap[0].blobData.QueryNumberOfBlobs();
			   		FCenter.SliceCount = iBlobsNum;
			   		FPoints.SliceCount = iBlobsNum;
			   		
					PXCMBlobData.IBlob[] blobList = new PXCMBlobData.IBlob[iBlobsNum];
            		PXCMPointI32[][] pointOuter = new PXCMPointI32[iBlobsNum][];
           		 	PXCMPointI32[][] pointInner = new PXCMPointI32[iBlobsNum][];
            
			   		for (int i = 0; i < iBlobsNum; i++) 
			   		{
						
			   			FSenseWrap[0].blobData.QueryBlobByAccessOrder(i,PXCMBlobData.AccessOrderType.ACCESS_ORDER_RIGHT_TO_LEFT, out blobList[i]);
						
						// handle extracted blob data
						Int32 nContours = blobList[i].QueryNumberOfContours();
			       		if (nContours > 0)
                        {
			       			//contourPts
							for (int k = 0; k < nContours; ++k)
                            {
                                int contourSize = blobList[i].QueryContourSize(k);
                                
                                if(blobList[i].IsContourOuter(k) == true)
                                {
                                    blobList[i].QueryContourPoints(k, out pointOuter[i]);
                                }
                                else
                                {
                                //    blobList[i].QueryContourPoints(k, out pointInner[i]);
                                }
                               
                            }
							
							//Convert to vector2D, map to vvvv!!!
							if (pointOuter[i] != null && pointOuter[i].Length > 0)
							{
								FPoints[i].SliceCount = pointOuter[i].Length;
								for (int p = 0; p < pointOuter[i].Length; p++)
								{
									var pt = pointOuter[i][p].ToVector2D();
									pt.x = pt.x / (float)ImageSize.width * 2.0f - 1.0f;
									pt.y = 1.0f - pt.y / (float)ImageSize.height * 2.0f;
									
									FPoints[i][p] = pt;
									
								}
							}
			       		}
			       		
			       		FCenter[i] = blobList[i].QueryExtremityPoint(PXCMBlobData.ExtremityType.EXTREMITY_CENTER).ToVector3D() / 100;
			       		
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

