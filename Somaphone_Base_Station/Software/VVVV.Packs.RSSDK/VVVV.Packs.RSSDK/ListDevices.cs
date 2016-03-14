/*
 * Created by Antony Rayzhekov a.k.a. Zeos
 * User: Intelf
 * Date: 7/19/2015
 * Time: 4:07 PM
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
	[PluginInfo(Name = "ListDevices", Category="RSSDK", AutoEvaluate = false)]
    public class RSSDKListDevicesNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Input("Refresh", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FRefresh;
        
        [Output("DeviceName")]
        public ISpread<string> FDeviceName;
        
        [Output("DeviceInfo")]
        public ISpread<PXCMCapture.DeviceInfo> FDeviceInfo;
        
        [Output("Module IUID")]
        public ISpread<int> FModuleIUID;
        
        [Output("Module Description")]
        public ISpread<string> FModuleInfo;
        
        [Import()]
		public ILogger FLogger;
       	
       	public void OnImportsSatisfied()
        {
          	FDeviceInfo.SliceCount = 0;
        	FDeviceName.SliceCount = 0;
        	FModuleInfo.SliceCount = 0;
        	FModuleIUID.SliceCount = 0;
        }
       	
        public void Evaluate(int SpreadMax)
        {
        	if(FRefresh.IsChanged && FRefresh[0])
        	{
        		FDeviceName.SliceCount = 0;
        		FDeviceInfo.SliceCount = 0;
        		FModuleInfo.SliceCount = 0;
        		FModuleIUID.SliceCount = 0;
        	
        		var tmp_session = PXCMSession.CreateInstance();
        		
        		var DDevices = new Dictionary<string, PXCMCapture.DeviceInfo>();
        		
        		//Enumerate devices
        		PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
        		desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
        		desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;
            
            	for (int i = 0; ; i++)
            	{
                	PXCMSession.ImplDesc desc1;
                	if (tmp_session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                	
                	PXCMCapture capture;
                	if (tmp_session.CreateImpl<PXCMCapture>(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;
                	
                	for (int j = 0; ; j++)
                	{
                    	PXCMCapture.DeviceInfo dinfo;
                    	if (capture.QueryDeviceInfo(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) 
                    	{
                    		
                    		break;
                    	}
                    	
                    	string name = dinfo.name;
                    	if (DDevices.ContainsKey(dinfo.name))
                    	{
                        	name += j;
                    	}
                    	
                    	DDevices.Add(name,dinfo);
                    	PXCMCapture.DeviceInfo devInfo = dinfo;
                    	
                    	FDeviceName.Add(name);
                    	FDeviceInfo.Add(devInfo);
                    	
               	 	}
                	
                	DDevices.Clear();
                	capture.Dispose();
                	
            	} //end for
            	
            	//enumerate Modules
            	FModuleIUID.SliceCount = 0;
            	FModuleInfo.SliceCount = 0;
            	
            	desc.group = PXCMSession.ImplGroup.IMPL_GROUP_ANY;
            	desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_ANY;
            	
            	for (int i=0;;i++) 
            	{
					PXCMSession.ImplDesc mdesc;
					if (tmp_session.QueryImpl(desc,  i, out mdesc)<pxcmStatus.PXCM_STATUS_NO_ERROR) break;
					
					FModuleIUID.Add(mdesc.iuid);
					FModuleInfo.Add(String.Format("{0} version={1}.{2}",mdesc.friendlyName, mdesc.version.major, mdesc.version.minor));
				}
            	
            	
            	tmp_session.Dispose();

        	}
        	
    	}
       
    }
    
    [PluginInfo(Name = "F200 Settings", Category="RSSDK", AutoEvaluate = false)]
    public class RSSDKF200Node : IPluginEvaluate
    {
       
        [Input("Context", IsSingle = true)]
        public Pin<RSSDKContext> FContext;
        
        //F200 Settings
		[Input("Accurancy", IsSingle = true)]
        public IDiffSpread<PXCMCapture.Device.IVCAMAccuracy> FAccurancyIn;
        
        [Input("Filter", IsSingle = true)]
        public IDiffSpread<RSSDKFilterOption> FFilterIn;
        
        [Input("LaserPower", IsSingle = true, MinValue=0, MaxValue=16)]
        public IDiffSpread<int> FLaserPowerIn;
        
        [Input("MotionTradeOff", IsSingle = true, MinValue=0, MaxValue=100)]
        public IDiffSpread<int> FMotionTradeOffIn;
        
        [Output("Accurancy")]
        public ISpread<PXCMCapture.Device.IVCAMAccuracy> FAccurancyOut;
        
        [Output("Filter")]
        public ISpread<RSSDKFilterOption> FFilterOut;
        
        [Output("LaserPower")]
        public ISpread<int> FLaserPowerOut;
        
        [Output("MotionTradeOff")]
        public ISpread<int> FMotionTradeOffOut;
        
        public void Evaluate(int SpreadMax)
        {
        	//realtime settings
        	if((FContext != null) && (FContext[0].sm != null) && FContext[0].sm.IsConnected() && (FContext[0].device != null))
    		{
	        	if(FAccurancyIn.IsChanged)
	    		{
	        		FContext[0].device.SetIVCAMAccuracy(FAccurancyIn[0]);
	    			FAccurancyOut[0] = FContext[0].device.QueryIVCAMAccuracy();
	    		}
	    		
	    		if(FFilterIn.IsChanged)
	    		{
	    			FContext[0].device.SetIVCAMFilterOption((int)FFilterIn[0]);
	    			FFilterOut[0] = (RSSDKFilterOption) FContext[0].device.QueryIVCAMFilterOption();
	    		}
	    		
	    		if(FLaserPowerIn.IsChanged)
	    		{
	    			FContext[0].device.SetIVCAMLaserPower(FLaserPowerIn[0]);
	    			FLaserPowerOut[0] = FContext[0].device.QueryIVCAMLaserPower();
	    		}
	    		
	    		if(FMotionTradeOffIn.IsChanged)
	    		{
	    			FContext[0].device.SetIVCAMMotionRangeTradeOff(FMotionTradeOffIn[0]);
	    			FMotionTradeOffOut[0] =  FContext[0].device.QueryIVCAMMotionRangeTradeOff();
	    		}
        	}
        }
    }
    
   	[PluginInfo(Name = "DeviceInfo", Category="RSSDK", AutoEvaluate = false)]
    public class RSSDKDeviceInfoNode : IPluginEvaluate
    {
        [Input("DeviceInfo")]
        public IDiffSpread<PXCMCapture.DeviceInfo> FDeviceInfo;
        
        [Output("ID")]
        public ISpread<string> FID;
        
        [Output("Index")]
        public ISpread<int> FIndex;
        
        [Output("UID")]
        public ISpread<string> FUID;
        
        [Output("Firmware")]
        public ISpread<string> FFirmware;
        
        [Output("Location")]
        public ISpread<Vector2D> FLocation;
        
        [Output("Model")]
        public ISpread<string> FModel;
        
        [Output("Name")]
        public ISpread<string> FName;
        
        [Output("Orientation")]
        public ISpread<string> FOrientation;
        
        [Output("Serial")]
        public ISpread<string> FSerial;
        
        [Output("Streams")]
        public ISpread<ISpread<string>> FStreams;
        
        [Import()]
		public ILogger FLogger;
        
        public void Evaluate(int SpreadMax)
        {
        	if(FDeviceInfo.IsChanged && (FDeviceInfo.SliceCount>0) && (SpreadMax>0) )
        	{
        		string[] StreamsListSeparator = {", "};
        		
        		for (int i = 0; i < FDeviceInfo.SliceCount; i++)
        		{
        			FID[i] = FDeviceInfo[i].did;
        			FIndex[i] = FDeviceInfo[i].didx;
        			FUID[i] = FDeviceInfo[i].duid.ToString();
        			FFirmware[i] = String.Join(".",FDeviceInfo[i].firmware); //join as string separated by "."
        			FLocation[i] = VectorConverter.ToVector2D(FDeviceInfo[i].location);
        			FModel[i] = FDeviceInfo[i].model.ToString();
        			FName[i] = FDeviceInfo[i].name;
        			FOrientation[i] = FDeviceInfo[i].orientation.ToString();
        			FSerial[i] = FDeviceInfo[i].serial;
        			var supported_streams = FDeviceInfo[i].streams.ToString().Split(StreamsListSeparator, StringSplitOptions.RemoveEmptyEntries);
        			FStreams[i].AssignFrom(supported_streams);
        			
        		}
        	}
        	
    	}   
    }
}

