#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "QuaternionToEulerYawPitchRoll", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueQuaternionToEulerYawPitchRollNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultValue = 1.0)]
		public ISpread<Vector4D> FInput;

		[Output("Output")]
		public ISpread<Vector3D> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				double pitch, roll, yaw;
				VMath.QuaternionToEulerYawPitchRoll(FInput[i], out pitch, out yaw, out roll);
				FOutput[i] = new Vector3D(pitch, yaw, roll);
			}
			//FLogger.Log(LogType.Debug, "hi tty!");
		}
	}
}
