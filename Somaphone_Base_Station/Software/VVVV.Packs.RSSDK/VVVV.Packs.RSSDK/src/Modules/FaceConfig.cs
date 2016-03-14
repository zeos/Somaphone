using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
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
    [PluginInfo(Name = "SetConfiguration", Category = "RSSDK", Version = "Face", AutoEvaluate = true)]
    public class RSSDKFaceSetConfigurationNode : IPluginEvaluate
    {
        [Input("Context")]
        public ISpread<RSSDKContext> FSenseWrap;

        [Input("Tracking Mode")]
        public ISpread<PXCMFaceConfiguration.TrackingModeType> FTrackMode;
        [Input("Strategy")]
        public ISpread<PXCMFaceConfiguration.TrackingStrategyType> FStrategy;
        [Input("Smoothing Level")]
        public ISpread<PXCMFaceConfiguration.SmoothingLevelType> FSmoothing;

        [Input("Enable Detection")]
        public ISpread<bool> FEnableDetection;
        [Input("Enable Pose")]
        public ISpread<bool> FEnablePose;
        [Input("Enable Landmarks")]
        public ISpread<bool> FEnableLandmarks;
        [Input("Enable Expressions")]
        public ISpread<bool> FEnableExpressions;

        [Input("Max Tracked Faces")]
        public ISpread<int> FMaxTrackedFaces;

        [Input("Number of Landmarks")]
        public ISpread<int> FNumLandmarks;

        [Input("Set", IsBang = true)]
        public ISpread<bool> FSet;

        public void Evaluate(int SpreadMax)
        {
            if (FSet[0])
            {
                FSenseWrap[0].FaceConfig.FaceConfig.SetTrackingMode(FTrackMode[0]);
                FSenseWrap[0].FaceConfig.FaceConfig.strategy = FStrategy[0];
                FSenseWrap[0].FaceConfig.FaceConfig.detection.smoothingLevel = FSmoothing[0];
                FSenseWrap[0].FaceConfig.FaceConfig.landmarks.smoothingLevel = FSmoothing[0];
                FSenseWrap[0].FaceConfig.FaceConfig.pose.smoothingLevel = FSmoothing[0];

                FSenseWrap[0].FaceConfig.FaceConfig.detection.isEnabled = FEnableDetection[0];
                FSenseWrap[0].FaceConfig.FaceConfig.pose.isEnabled = FEnablePose[0];
                FSenseWrap[0].FaceConfig.FaceConfig.landmarks.isEnabled = FEnableLandmarks[0];
                FSenseWrap[0].FaceConfig.ExpressionConfig.properties.isEnabled = FEnableExpressions[0];

                if (FSenseWrap[0].FaceConfig.ExpressionConfig.properties.isEnabled)
                    FSenseWrap[0].FaceConfig.ExpressionConfig.EnableAllExpressions();

                FSenseWrap[0].FaceConfig.FaceConfig.detection.maxTrackedFaces = FMaxTrackedFaces[0];
                FSenseWrap[0].FaceConfig.FaceConfig.pose.maxTrackedFaces = FMaxTrackedFaces[0];
                FSenseWrap[0].FaceConfig.FaceConfig.landmarks.maxTrackedFaces = FMaxTrackedFaces[0];
                FSenseWrap[0].FaceConfig.ExpressionConfig.properties.maxTrackedFaces = FMaxTrackedFaces[0];

                FSenseWrap[0].FaceConfig.FaceConfig.landmarks.numLandmarks = FNumLandmarks[0];

                FSenseWrap[0].FaceConfig.FaceConfig.ApplyChanges();
            }
        }
    }

    [PluginInfo(Name = "GetConfiguration", Category = "RSSDK", Version = "Face")]
    public class RSSDKFaceGetConfigurationNode : IPluginEvaluate
    {
        [Input("Context")]
        public ISpread<RSSDKContext> FSenseWrap;

        [Output("Tracking Mode")]
        public ISpread<PXCMFaceConfiguration.TrackingModeType> FTrackMode;
        [Output("Strategy")]
        public ISpread<PXCMFaceConfiguration.TrackingStrategyType> FStrategy;
        [Output("Smoothing Level")]
        public ISpread<PXCMFaceConfiguration.SmoothingLevelType> FSmoothing;

        [Output("Detection")]
        public ISpread<bool> FEnableDetection;
        [Output("Pose")]
        public ISpread<bool> FEnablePose;
        [Output("Landmarks")]
        public ISpread<bool> FEnableLandmarks;
        [Output("Expressions")]
        public ISpread<bool> FEnableExpressions;

        [Output("Max Tracked Faces")]
        public ISpread<int> FMaxTrackedFaces;

        [Output("Number of Landmarks")]
        public ISpread<int> FNumLandmarks;

        public void Evaluate(int SpreadMax)
        {
            if (FSenseWrap[0] != null)
            {
                FSmoothing.SliceCount = 3;
                FMaxTrackedFaces.SliceCount = 4;

                FSenseWrap[0].FaceConfig.FaceConfig.Update();

                FTrackMode[0] = FSenseWrap[0].FaceConfig.FaceConfig.GetTrackingMode();
                FStrategy[0] = FSenseWrap[0].FaceConfig.FaceConfig.strategy;

                FSmoothing[0] = FSenseWrap[0].FaceConfig.FaceConfig.detection.smoothingLevel;
                FSmoothing[1] = FSenseWrap[0].FaceConfig.FaceConfig.pose.smoothingLevel;
                FSmoothing[2] = FSenseWrap[0].FaceConfig.FaceConfig.landmarks.smoothingLevel;

                FEnableDetection[0] = FSenseWrap[0].FaceConfig.FaceConfig.detection.isEnabled;
                FEnablePose[0] = FSenseWrap[0].FaceConfig.FaceConfig.pose.isEnabled;
                FEnableLandmarks[0] = FSenseWrap[0].FaceConfig.FaceConfig.landmarks.isEnabled;
                FEnableExpressions[0] = FSenseWrap[0].FaceConfig.ExpressionConfig.properties.isEnabled;

                FMaxTrackedFaces[0] = FSenseWrap[0].FaceConfig.FaceConfig.detection.maxTrackedFaces;
                FMaxTrackedFaces[1] = FSenseWrap[0].FaceConfig.FaceConfig.pose.maxTrackedFaces;
                FMaxTrackedFaces[2] = FSenseWrap[0].FaceConfig.FaceConfig.landmarks.maxTrackedFaces;
                FMaxTrackedFaces[3] = FSenseWrap[0].FaceConfig.ExpressionConfig.properties.maxTrackedFaces;

                FNumLandmarks[0] = FSenseWrap[0].FaceConfig.FaceConfig.landmarks.numLandmarks;
            }
        }
    }
}
