#region usings
using System;
using System.Net;
using System.Globalization;
using System.Threading; 
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using VVVV.Hosting; 
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
using VVVV.Utils.OSC;
using VVVV.Core.Logging;
using VVVV.Core;
#endregion usings

namespace VVVV.Nodes.AbletonLiveBridge
{
	public delegate void LogDebugDelegate(string msg);  
	//public delegate void NotifyRefreshEventHandler(object sender, EventArgs e);
	
	class AbletonLiveSet {
	
		public int ID; // Index  
		public double Length;
		public double BPM;
		public int Sig_Denom;
		public int Sig_Nom;
		
		public bool isPlaying;
		public bool isRecording;
		
		public bool isLooped;
		public float Loop_Start;
		public float Loop_Length;
		public float Loop_End;
		
		//timecode (position)
		public double timecode_beats;
		public string timecode_SMPTE;
		public int timecode_bar;
		public int timecode_beat;
		public int timecode_tick;
		
		//monitoring
		public double MasterLevel_L;
		public double MasterLevel_R;
		public double MasterLevel;
		
		private OSCReceiver FOSCReceiver;
		private bool FListening;
		private Thread FThread;
		public List<OSCMessage> MessageQueue = new List<OSCMessage>();
		
		private OSCTransmitter FOSCTransmitter;
		private IPAddress FIP;
		
		private string LastError = "";
		public bool Debug = false; 
		
		public event EventHandler NotifyRefresh;
		
		//tracks & cues
		private Dictionary<int, AbletonLiveTrack> FTrack = new Dictionary<int, AbletonLiveTrack>();
		
		//private Map<int, string> FCueIDs;
		private Dictionary<string, AbletonLiveCue> FCue = new Dictionary<string, AbletonLiveCue>();
		
		private LogDebugDelegate LogDebug;
		
		public AbletonLiveSet(LogDebugDelegate ld)
		{
			//FCue.Add("None", new AbletonLiveCue(0, "None", 0));
			//FTrack.Add(0, new AbletonLiveTrack(0, "None"));
			LogDebug = ld;
		}
		    
		public void RaiseNotifyRefresh()
	    {
	        EventHandler handler = NotifyRefresh;
	        if (handler != null)
	        {
	            handler(this, EventArgs.Empty);
	        }
	    }
		/*
		public void RaiseNotifyRefresh()
		{
			if (NotifyRefresh != null) NotifyRefresh(this, EventArgs.Empty);
		}	
		*/
		
		public void Clear()
		{
			string[] Empty = new string[] {};
			
			EnumManager.UpdateEnum("LiveSetCues", "", Empty);
			EnumManager.UpdateEnum("LiveSetTracks", "", Empty);
			
			FTrack.Clear();
			FCue.Clear();
		}
		
		public string GetLastError() 
		{
			return LastError;
		}
		
		#region Network
		public void SendToLive(string addr, object[] args)
		{
			if( (FOSCTransmitter != null) && (addr != "") )
			{
				var bundle = new OSCBundle();
				var message = new OSCMessage(addr);
				
				for (int i = 0; i < args.Length; i++)
					message.Append(args[i]);
				
				bundle.Append(message);

				try
				{
					FOSCTransmitter.Send(bundle);
					if(Debug) LogDebug(message.ToString());
				}
				catch (Exception ex)
				{
					LastError = ex.Message.ToString();
				}
			}
		}
		
		public void SendToLive(string addr, object arg)
		{
			if( (FOSCTransmitter != null) && (addr != "") )
			{
				var bundle = new OSCBundle();
				var message = new OSCMessage(addr);
				
				//message.Append(arg);
				if(arg is float) message.Append((float)arg);
				else 
					if(arg is double) message.Append((double)arg);
				else 
					if(arg is int) message.Append((int)arg);
				else 
					if(arg is bool) message.Append((bool)arg ? 1 : 0);
				else
					message.Append(arg.ToString());
				
				bundle.Append(message);
				
				try
				{
					FOSCTransmitter.Send(bundle);
					if(Debug) LogDebug(message.ToString());
				}
				catch (Exception ex)
				{
					LastError = ex.Message.ToString();
				}
			} else LastError = "ERROR: FOSCTransmitter is null!";
		}
		
		public void SendToLiveTrack(string addr, string prop, object arg)
		{
			if( (FOSCTransmitter != null) && (addr != "") )
			{
				var bundle = new OSCBundle();
				var message = new OSCMessage(addr);
				
				message.Append(prop);
				
				if(arg is float) message.Append((float)arg);
				else 
					if(arg is double) message.Append((float)arg);
				else 
					if(arg is int) message.Append((int)arg);
				else 
					if(arg is bool) message.Append((bool)arg ? 1 : 0);
				else
					message.Append(arg.ToString());
				
				bundle.Append(message);
				
				try
				{
					FOSCTransmitter.Send(bundle);
					if(Debug) LogDebug(message.ToString());
				}
				catch (Exception ex)
				{
					LastError = ex.Message.ToString();
				}
			} else LastError = "ERROR: FOSCTransmitter is null!";
		}
		
		public bool InitNetwork(string TargetIP, int TargetPort)
		{
			try
			{
				FIP = IPAddress.Parse(TargetIP);
				if (FIP != null) 
				{
					if (FOSCTransmitter != null) FOSCTransmitter.Close();
				
					FOSCTransmitter = new OSCTransmitter(FIP.ToString(), TargetPort);
				
					FOSCTransmitter.Connect();
				
					return true;
					
				}
			}
			catch (Exception e)
			{
				LastError = e.Message.ToString();
			}
			return false;
		}
		
		public void StartListeningOSC(int UDPPort)
		{
			FOSCReceiver = new OSCReceiver(UDPPort);
			FListening = true;
			FThread = new Thread(new ThreadStart(ListenToOSC));
			FThread.Start();
		}
		
		public void StopListeningOSC()
		{
			if (FThread != null && FThread.IsAlive)
			{
				FListening = false;
				//FOSCReceiver is blocking the thread
				//so waiting would freeze
				//shouldn't be necessary here anyway...
				
				//FThread.Join();
			}

			if (FOSCReceiver != null) FOSCReceiver.Close();
			FOSCReceiver = null;
		}
		
		public void ListenToOSC() //Live -> VVVV
		{
			while(FListening)
			{
				try
				{
					OSCPacket packet = FOSCReceiver.Receive();
					if (packet != null)
					{
						if (packet.IsBundle())
						{
							ArrayList messages = packet.Values;
							lock(MessageQueue)
								for (int i=0; i<messages.Count; i++)
									MessageQueue.Add((OSCMessage)messages[i]);
						}
						else
							lock(MessageQueue)
								MessageQueue.Add((OSCMessage)packet);
					}
				}
				catch (Exception e)
				{
					LastError = e.Message.ToString();
				}
			}
		}
		#endregion Network
		
		//Cues
		#region Cues
		public void addCue(int ID, string Name, float Pos)
		{
			if(!FCue.ContainsKey(Name))
			{
				FCue.Add(Name, new AbletonLiveCue(ID, Name, Pos));
			}
		}
		
		public bool getCueByName(string Name, out AbletonLiveCue Cue)
		{
			if(FCue.ContainsKey(Name))
			{
				Cue = FCue[Name];
				return true;
			}
			Cue = null;
			return false; 
		}
		
		public void ClearCues()
		{
			FCue.Clear();
		}
		
		public int cueCount()
		{
			return FCue.Count;
		}
		
		public string[] getCuesNames() //use keys!!!, use map ID, to ensure unq Cues
		{
			string[] cueNames = new String[FCue.Count];
			int i = 0;
			foreach(var c in FCue.Values)
			{
				cueNames[i] = c.Name;
				i++;
			}
			return cueNames;
		}
		#endregion Cues
		
		//Tracks
		
		public AbletonLiveTrack GetTrackByIndex(int TrackIndex)
		{
			if(TrackIndex == -1) return null;
			
			if(FTrack.ContainsKey(TrackIndex))
			{
				return FTrack[TrackIndex];
			}
			else if(TrackIndex > -1)
			{
				AbletonLiveTrack Track = new AbletonLiveTrack(TrackIndex, "_Track_" + TrackIndex.ToString());
				FTrack.Add(TrackIndex, Track);
				RaiseNotifyRefresh();
				return Track;
				
			}
			
			return null;
		}
		
		public string[] GetTracksNames()
		{
			string[] trackNames = new String[FTrack.Count];
			int i = 0;
			foreach(var t in FTrack.Values)
			{
				trackNames[i] = t.Name;
				i++;
			}
			return trackNames;
		}
		
		public AbletonLiveTrack GetTrackByName(string _Name)
		{
			foreach(var t in FTrack.Values)
			{
				if(t.Name == _Name) return t;
			}
			return null;
		}
		//some methods to convert timings, DB2Lin...
		
	}
	
	class AbletonLiveCue {
		
		public int ID;
		public string Name;
		public float position; //in beats
		
		public AbletonLiveCue(int _ID, string _Name, float _Pos)
		{
			this.ID = _ID;
			this.Name = _Name;
			this.position = _Pos;
		}
		
		public int GetID()
		{
			return this.ID;
		}
	}
	
	class AbletonLiveClip {
	
		public int ID;
		public int Index;
		public string Name;
		public double Length_in_beats;
		public bool is_playing;
		//....
	}
	
	class AbletonLiveTrack {
	
		public int ID = -1;
		public string Name;
		public double Volume;
		public double Level_L;
		public double Level_R;
		public double Level;
		public float Peak;
		public bool isMuted;
		public bool isArmed;
		public bool isSolo;
		public float pan; //(-1..1)
		
		public int ActiveClipID;
		public string ActiveClipName;
		
		public int playing_slot_index;
		public int fired_slot_index;
		
		public SortedDictionary<int, AbletonLiveClip> FClip = new SortedDictionary<int, AbletonLiveClip>();
		
		public AbletonLiveTrack(int _TrackID, string _Name)
		{
			this.ID = _TrackID;
			this.Name = _Name;
		}
		
		public int ClipCount()
		{
			return FClip.Count;
		}
		
		public string getAddr(string prop = "")
		{
			if(prop != "") prop = "/" + prop; 
			
			if(ID > -1)
				return "/live_set/tracks/" + ID.ToString() + prop;
			else
				return "";
		}
		
		public AbletonLiveClip AddClip(int ClipIndex, string ClipName, int ClipID)
		{
			if(ClipIndex == -1) return null;
			
			if(FClip.ContainsKey(ClipIndex))
			{
				return FClip[ClipIndex];
			}
			else if(ClipIndex > -1)
			{
				AbletonLiveClip Clip = new AbletonLiveClip();
				
				Clip.Index 	= ClipIndex;
				Clip.Name 	= ClipName;
				//Clip.ID 	= ClipID;
				
				FClip.Add(ClipIndex, Clip);
				return Clip;
			}
			
			return null;
		}
		
		public int GetClips(out double[] ClipLength, out string[] ClipNames)
		{
			ClipNames = new string[FClip.Count];
			ClipLength = new double[FClip.Count];
			
			int i = 0;
			
			foreach(KeyValuePair<int, AbletonLiveClip> entry in FClip)
			{
				ClipLength[i] = entry.Value.Length_in_beats;
				ClipNames[i] = entry.Value.Name;
				i++;
			}
			return FClip.Count;
		}
		
		public int GetClipSlotByIndex(int Idx)
		{
			var el = FClip.ElementAt(Idx % FClip.Count);
			return el.Key;
			
		}
		
		public AbletonLiveClip GetClipBySlotIndex(int SlotIndex)
		{
			
			if(FClip.ContainsKey(SlotIndex))
				return FClip[SlotIndex];
			else
				return null;
		}
		
	}
	
	#region PluginInfo
	[PluginInfo(Name = "Live", 
				Category = "Live", 
				Help = "Live <-> VVVV over OSC", 
				Tags = "live, osc, ableton, control",
				AutoEvaluate = true
	)]
	#endregion PluginInfo
	
	public class LiveNode : IPluginEvaluate, IDisposable
	{
		
		#region fields & pins
		#pragma warning disable 0649
		
		[Input("IP", IsSingle = true, DefaultString = "127.0.0.1")]
        IDiffSpread<string> FUDPIP;
		
		[Input("Port", IsSingle = true, DefaultValue = 9013)]
        IDiffSpread<int> FUDPPort;
		
		[Input("Feedback IP", IsSingle = true, DefaultString = "127.0.0.1")]
        IDiffSpread<string> FTargetIP;

        [Input("Feedback Port", IsSingle = true, DefaultValue = 9014)]
        IDiffSpread<int> FTargetPort;
		
		//CONTROL
		
		[Input("Play", IsSingle = true, IsBang=true)]
        IDiffSpread<bool> FPlay;
		
		[Input("Stop", IsSingle = true, IsBang=true)]
        IDiffSpread<bool> FStop;
		
		[Input("Continue", IsSingle = true, IsBang=true)]
        IDiffSpread<bool> FContinue;
		
		[Input("StopAllClips", IsSingle = true, IsBang=true)]
        IDiffSpread<bool> FStopAllClips;
		
		[Input("Record", IsSingle = true, IsBang=false)]
        IDiffSpread<bool> FRecord;
		
		[Input("Volume")]
        IDiffSpread<double> FVolume;
		
		[Input("Init", IsSingle = true, IsBang=true)]
        IDiffSpread<bool> FReset;
		
		[Input("Enabled", IsSingle = true)]
        IDiffSpread<bool> FEnabled;
		
		/*************************************************************************************************************************/
		//[Output("LiveSet")]
       	//ISpread<AbletonLiveSet> FLiveSet;
		[Output("LiveSet", IsSingle = true)]
		Pin<AbletonLiveSet> FLiveSet;
		
		[Output("BPM")]
        ISpread<float> FBPM;
		
		[Output("Beats per measure")]
        ISpread<int> FSigNom;
		
		[Output("Beat Type")]
        ISpread<int> FSigDenom;
		
		[Output("Playing")]
        ISpread<bool> FPlaying;
		
		[Output("Recording")]
        ISpread<bool> FRecording;
		
		[Output("Looped")]
        ISpread<bool>FLooped;

		[Output("LoopStart")]
        ISpread<float>FLoopStart;
		
		[Output("LoopLength")]
        ISpread<float> FLoopLength;
		
		[Output("Length")]
        ISpread<double> FLength;
		
		[Output("Tracks")]
        ISpread<string> FTracks;

		[Import()]
        ILogger FLogger;

        [Import()] 
#pragma warning restore
		
		private IHDEHost FHDEHost;
		//private List<OSCMessage> FMessageQueue = null;
		private AbletonLiveSet LiveSet = null;
		
		//private bool FDebug = true;
		private bool FDisposed;
		#endregion fields & pins
		
		#region constructor/destructor
		[ImportingConstructor]
		public LiveNode(IHDEHost host)
		{
			FHDEHost = host;
			//FMessageQueue = new List<OSCMessage>();
			LiveSet = new AbletonLiveSet(new LogDebugDelegate(mylog));
			
		}
		
		~LiveNode()
		{
			Dispose(false);
		}
		
		public void Dispose()
		{
			Dispose(true);
		}
		
		protected void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if(!FDisposed)
			{
				if(disposing)
				{
					// Dispose managed resources.
					
					LiveSet.StopListeningOSC();
				}
				// Release unmanaged resources. If disposing is false,
				// only the following code is executed.
				
			}
			FDisposed = true;
		}
		#endregion
		
		public void mylog(string msg)
		{
			FLogger.Log(LogType.Debug, "Live4VVV:" + msg);
		}
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FUDPPort.IsChanged)
			{
				LiveSet.StopListeningOSC();
				LiveSet.StartListeningOSC(FUDPPort[0]);
			}
			
			//re/init udp
			if (FTargetIP.IsChanged || FTargetPort.IsChanged)
			{
				bool connected = LiveSet.InitNetwork(FTargetIP[0], FTargetPort[0]);
				FLiveSet[0] = LiveSet;
				LiveSet.SendToLive("/live/refresh",1);
			}
			
			if(FEnabled[0])
			{
				//process messagequeue
				lock(LiveSet.MessageQueue)
				{
					foreach (var message in LiveSet.MessageQueue) ProcessOSCMessage(message);
					LiveSet.MessageQueue.Clear();
				}
				
				if(FPlay.IsChanged && FPlay[0]) LiveSet.SendToLive("/live/play", 1);
					
				if(FStop.IsChanged && FStop[0]) LiveSet.SendToLive("/live/stop", 1);
				
				if(FContinue.IsChanged & FContinue[0]) LiveSet.SendToLive("/live/continue",1);
				
				if(FStopAllClips.IsChanged & FStopAllClips[0]) LiveSet.SendToLive("/live/stopallclips",1);
				
				if(FVolume.IsChanged) LiveSet.SendToLive("/live/volume",(float)FVolume[0]);
				
				
				
				if(FRecord.IsChanged) 
				{
					int r = (FRecord[0] == true) ? 1 : 0;
					LiveSet.SendToLive("/live/record", r);
				}
				
				if(FReset.IsChanged && FReset[0]) 
				{
					LiveSet.StopListeningOSC();
					LiveSet.Clear();
					LiveSet.StartListeningOSC(FUDPPort[0]);
					LiveSet.SendToLive("/live/refresh",1);
					//FLiveSet[0] = LiveSet;
					
					LiveSet.RaiseNotifyRefresh();
				}
				
				FTracks.AssignFrom(LiveSet.GetTracksNames());
				
				//errors?
				if(LiveSet.GetLastError() != "")FLogger.Log(LogType.Debug, LiveSet.GetLastError());
			}
		}
		
		private void ProcessTrackOSCMessage(OSCMessage message) 
		{
			char[] Sep = {'/'}; 
			
			// FLogger.Log(LogType.Debug, "OSC In: " + message.Address);
			
			string Prop = ""; 
			int TrackIndex = -1;
			AbletonLiveTrack track = null;
			
			var a = message.Address.Split(Sep);
			if(a.Length < 4) return;
			
			try
			{
				TrackIndex = Convert.ToInt32(a[3]); //get track ID
				Prop = a[4]; //get track Property
				track = LiveSet.GetTrackByIndex(TrackIndex); //get V4 track object
			
			}
			catch(Exception e)
			{
				FLogger.Log(LogType.Debug,e.Message);
				return;
			}
			/*
			FLogger.Log(LogType.Debug, "LIVE-TRACK: " 
						+ message.Address 
						+ " Index:" + TrackIndex.ToString() 
						+ " Property: " + Prop 
						//+ " Data: " + message.Values[0].ToString()
				);
			
			*/
			//process
			if((track != null) && (message.Values.Count>0) ) switch (Prop)
			{
				case "name":
				{
					if (message.Values[0] is string)
					{
						track.Name = (string)message.Values[0];
							
						//update tracks ENUM
						int cnt = EnumManager.GetEnumEntryCount("LiveSetTracks");
						bool found = false;
						for(int i = 0; i < cnt; i++)
						{
							var name = EnumManager.GetEnumEntry("LiveSetTracks", i);
							if(name == track.Name) { found = true; break; }
						}
						if(!found) EnumManager.AddEntry("LiveSetTracks", track.Name);
						//	EnumManager.UpdateEnum("LiveSetTracks", "None", LiveSet.GetTracksNames());
					}
					
				}
				break;
				
				case "level":
				{
					if(message.Values.Count == 2)
					{
						track.Level_L = (float)message.Values[0];
						track.Level_R = (float)message.Values[1];
						track.Level = (track.Level_L + track.Level_R) / 2;
					}
					//peak detection
					//peak on beat detection
					//max peak
					//accumulator
					//fft?
				}
				break;
				
				case "output":
				{
					track.Peak = (float)message.Values[0];
				}
				break;
				
				case "mute":
				{
					track.isMuted = ((int)message.Values[0] == 1) ? true : false;
				}
				break;
				
				case "solo":
				{
					track.isSolo = ((int)message.Values[0] == 1) ? true : false;
				}
				break;
				
				case "arm":
				{
					track.isArmed = ((int)message.Values[0] == 1) ? true : false;
				}
				break;
				
				case "volume":
				{
					if(message.Values[0] is float) track.Volume = (float)message.Values[0];
				}
				break;
				
				case "pan":
				{
					if(message.Values[0] is float) track.pan = (float)message.Values[0];
				}
				break;
				
				
				case "clip":
				{
					if(message.Values.Count == 2)
					{
						track.ActiveClipID = (int)message.Values[0];
						track.ActiveClipName = (string)message.Values[1];
						
					}
				}
				break; 
				
				case "clip_slot":
				{
					/*
					FLogger.Log(LogType.Debug, "clip_slot(" + TrackIndex.ToString() + ")  " +
						message.Values[0].ToString() + ": " + 
						message.Values[1].ToString()
					);
					*/
					if(message.Values.Count == 3)
					{
						var clip = track.AddClip((int)message.Values[0], message.Values[1].ToString(), -1);
						clip.Length_in_beats = (int)message.Values[2];
						LiveSet.RaiseNotifyRefresh();
					}
				}
				break; 
				
				
				default:
				break;
			}
		}
		
		private void ProcessOSCMessage(OSCMessage message)
		{
			
			if(message.Address.StartsWith("/live_set/tracks/") && (message.Values.Count > 0))
			{
				ProcessTrackOSCMessage(message);
			}
			else switch (message.Address)
			{
				case "/live/song_length":
				{
					LiveSet.Length = (float) message.Values[0];
					FLength[0] = LiveSet.Length;
				}
				break;
				
				case "/live/master/level":
				{
					LiveSet.MasterLevel_L = (float)message.Values[0];
					LiveSet.MasterLevel_R = (float)message.Values[1];
				}
				break;
				
				case "/live/timecode":
				{
					LiveSet.timecode_beats = (float) message.Values[0];
					string bbu = (string)(message.Values[1]);
					string[] BBU = bbu.Split(' ');
					if(BBU.Length > 2)
					{
						Int32.TryParse(BBU[0], out LiveSet.timecode_bar);
						Int32.TryParse(BBU[1], out LiveSet.timecode_beat);
						Int32.TryParse(BBU[2], out LiveSet.timecode_tick);
					}
					LiveSet.timecode_SMPTE = (string)message.Values[2];
					
					bool state = ((int)message.Values[3] == 1) ? true : false;
					LiveSet.isPlaying = state;
					FPlaying[0] = state;
				}
				break;
				
				case "/live/record":
				{
					bool state = ((int)message.Values[0] == 1) ? true : false;
					LiveSet.isRecording = state;
					FRecording[0] = state;
				}
				break;
				
				case "/live/loop/active":
				{
					bool state = ((int)message.Values[0] == 1) ? true : false;
					LiveSet.isLooped = state;
					FLooped[0] = state;
				}
				break;
				
				case "/live/loop/start":
				{
					float pos = (float)message.Values[0];
					LiveSet.Loop_Start = pos;
					FLoopStart[0] = pos;
					LiveSet.Loop_End = LiveSet.Loop_Start + LiveSet.Loop_Length;
				}
				break;
				
				case "/live/loop/length":
				{
					float len = (float)message.Values[0];
					LiveSet.Loop_Length = len;
					FLoopLength[0] = len;
					LiveSet.Loop_End = LiveSet.Loop_Start + LiveSet.Loop_Length;
				}
				break;
				
				case "/live/signature/denominator":
				{
					int val = (int)message.Values[0];
					LiveSet.Sig_Denom = val;
					FSigDenom[0] = val;
				}
				break;
				
				case "/live/signature/numerator":
				{
					int val = (int)message.Values[0];
					LiveSet.Sig_Nom = val;
					FSigNom[0] = val;
				}
				break;
				
				case "/live/bpm":
				{
					float bpm = (float)message.Values[0];
					LiveSet.BPM = bpm;
					FBPM[0] = bpm;
				}
				break;
				
				//cues
				case "/live/cue":
				{
					//LiveSet.ClearCues();
					
					int cueID = (int)message.Values[0];
					string cueName = (string)message.Values[1];
					float cueTime  = (float)message.Values[2];
					
					LiveSet.addCue(cueID, cueName, cueTime);
					EnumManager.UpdateEnum("LiveSetCues", "None", LiveSet.getCuesNames());
				}
				break;
				
				default:
				break;
				
			}

		}
		
	}
	
	
	
	//
	#region PluginInfo
	[PluginInfo(Name = "Timecode", 
				Category = "Live", 
				Help = "Live <-> VVVV over OSC", 
				Tags = "live, osc, ableton, control",
				AutoEvaluate = true
	)]
	#endregion PluginInfo
	
	public class LiveNodeTimecode : IPluginEvaluate
	{
		[Input("LiveSet")]
        ISpread<AbletonLiveSet> FLiveSet;
		
		[Output("Beats")]
        ISpread<double> FBeats;
		
		[Output("Bar")]
        ISpread<int> FBar;
		
		[Output("Beat")]
        ISpread<int> FBeat;
		
		[Output("Tick")]
        ISpread<int> FTick;
		
		[Output("SMPTE")]
        ISpread<string> FSMPTE;
		
		public void Evaluate(int SpreadMax)
		{
			if( (FLiveSet.SliceCount>0) && (FLiveSet[0] != null) )
			{
				FBeats[0] = FLiveSet[0].timecode_beats;
				FBar[0] = FLiveSet[0].timecode_bar;
				FBeat[0] = FLiveSet[0].timecode_beat;
				FTick[0] = FLiveSet[0].timecode_tick;
				FSMPTE[0] = FLiveSet[0].timecode_SMPTE;
			}
			
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "Cue", 
				Category = "Live", 
				Help = "Live <-> VVVV over OSC", 
				Tags = "live, osc, ableton, control",
				AutoEvaluate = true
	)]
	#endregion PluginInfo
	
	public class LiveNodeCue : IPluginEvaluate
	{
		[Input("LiveSet")]
        ISpread<AbletonLiveSet> FLiveSet;
		
		[Input("Cue", EnumName="LiveSetCues", DefaultEnumEntry = "None")]
        ISpread<EnumEntry> FCue;
		
		[Input("Go")]
        IDiffSpread<bool> FJumpTo;
		
		[Input("Prev")]
        IDiffSpread<bool> FPrev;
		
		[Input("Next")]
        IDiffSpread<bool> FNext;
		
		public void Evaluate(int SpreadMax)
		{
			if( (FLiveSet.SliceCount>0) && (FLiveSet[0] != null) )
			{
				if(FJumpTo.IsChanged && FJumpTo[0])
				{
					AbletonLiveCue Cue;
					if(FLiveSet[0].getCueByName(FCue[0].Name, out Cue))
					{
						if(Cue.Name != "None") 
						{
							FLiveSet[0].SendToLive("/live/goto/cue",Cue.GetID());
						}
					} 
					
				}
				
				if(FPrev.IsChanged && FPrev[0]) FLiveSet[0].SendToLive("/live/goto/prev",1);
				
				if(FNext.IsChanged && FNext[0]) FLiveSet[0].SendToLive("/live/goto/next",1);
				//FCues.AssignFrom(FLiveSet[0].getCueNames());
			}
			
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "Track", 
				Category = "Live", 
				Help = "Live <-> VVVV over OSC", 
				Tags = "live, osc, ableton, control",
				AutoEvaluate = true
	)]
	#endregion PluginInfo
	
	public class LiveNodeTrack : IPluginEvaluate, IDisposable, IPluginConnections
	{
		[Input("LiveSet", IsSingle = true)]
        Pin<AbletonLiveSet> FLiveSet;
		
		[Input("Track", EnumName="LiveSetTracks", DefaultEnumEntry = "None")]
        IDiffSpread<EnumEntry> FTrack;
		
		[Input("Volume")]
        IDiffSpread<double> FVolumeIn;
		
		[Input("Pan", MinValue = -1, MaxValue = 1, DefaultValue = 0)]
        IDiffSpread<double> FPanIn;
		
		[Input("Muted")]
        IDiffSpread<bool> FMutedIn;
		
		[Input("Solo")]
        IDiffSpread<bool> FSoloIn;
		
		[Input("Armed")]
        IDiffSpread<bool> FArmedIn;
	
		//stop all
		
		//OUTPUTS
		
		[Output("Peak")]
        ISpread<double> FPeak;
		
		[Output("L")]
        ISpread<double> FLeft;
		
		[Output("R")]
        ISpread<double> FRight;
		
		[Output("Volume")]
        ISpread<double> FVolumeOut;
		
		[Output("Pan")]
        ISpread<double> FPanOut;
		
		[Output("Muted")]
        ISpread<bool> FMutedOut;
		
		[Output("Solo")]
        ISpread<bool> FSoloOut;
		
		[Output("Armed")]
        ISpread<bool> FArmedOut;
		
		[Import()]
        ILogger FLogger;
		
		private AbletonLiveTrack track = null;
		private bool FRefresh = true;
		private bool FRefreshEventInited = false;
		
		#region ContextConnect      
		public void ConnectPin(IPluginIO pin)
		{
			if (pin == FLiveSet.PluginIO) 
			{
				FRefresh = true;
			}
		}

		public void DisconnectPin(IPluginIO pin)
		{
			if (pin == FLiveSet.PluginIO) 
			{
				FRefresh = true;
				if(FLiveSet[0] != null) FLiveSet[0].NotifyRefresh -= onRefresh;
			}
		}
		#endregion
		
		public void Evaluate(int SpreadMax)
		{
			if( (FLiveSet != null) )
			{
				if( (FTrack.IsChanged || FRefresh) || ((track == null)&& (FTrack[0] != null)) )//update track
				{
					if(FRefreshEventInited)
					{
						if(FLiveSet[0] != null) FLiveSet[0].NotifyRefresh -= onRefresh;
					}
					
					if(FLiveSet[0] != null) 
					{
						FLiveSet[0].NotifyRefresh += onRefresh;
						FRefreshEventInited = true;
					}
					
				 	track = FLiveSet[0].GetTrackByName(FTrack[0].Name);
					
					FRefresh = false; 
				}
				
				if(track != null) //read the outputs
				{
					//set inputs
					if(FVolumeIn.IsChanged) FLiveSet[0].SendToLive(track.getAddr("volume"), (float) FVolumeIn[0]);
					if(FPanIn.IsChanged) 	FLiveSet[0].SendToLive(track.getAddr("pan"), (float) FPanIn[0]);
					
					if(FMutedIn.IsChanged) 	FLiveSet[0].SendToLiveTrack(track.getAddr("set"), "mute", FMutedIn[0] ? 1 : 0  );
					if(FSoloIn.IsChanged) 	FLiveSet[0].SendToLiveTrack(track.getAddr("set"), "solo", FSoloIn[0]  ? 1 : 0  );
					if(FArmedIn.IsChanged) 	FLiveSet[0].SendToLiveTrack(track.getAddr("set"), "arm",  FArmedIn[0] ? 1 : 0  );
					
					//outputs 
					FPeak[0]			= track.Peak;
					FLeft[0] 			= track.Level_L;
					FRight[0] 			= track.Level_R;
					
					FVolumeOut[0] 		= track.Volume;
					FPanOut[0] 			= track.pan;
					
					FMutedOut[0] 		= track.isMuted;
					FSoloOut[0] 		= track.isSolo;
					FArmedOut[0] 		= track.isArmed;
				} 
			}
		}
		
		public void Dispose()
		{
			if(FLiveSet[0] != null) FLiveSet[0].NotifyRefresh -= onRefresh;
		}
    

	    private void onRefresh(object sender, EventArgs e)
	    {
	        FRefresh = true;
	    }
	}
	
	#region PluginInfo
	[PluginInfo(Name = "Clip", 
				Category = "Live", 
				Help = "Live <-> VVVV over OSC", 
				Tags = "live, osc, ableton, control",
				AutoEvaluate = true
	)]
	#endregion PluginInfo
	public class LiveNodeClip : IPluginEvaluate, IDisposable, IPluginConnections
	{
		[Input("LiveSet", IsSingle = true)]
        Pin<AbletonLiveSet> FLiveSet;
		
		[Input("Track", EnumName="LiveSetTracks", DefaultEnumEntry = "None")]
        IDiffSpread<EnumEntry> FTrack;
		
		[Input("Clip")]
        ISpread<int> FClip;
		
		[Input("Fire Clip", IsBang = true)]
        IDiffSpread<bool> FFireClipBang;
		
		[Input("Stop", IsBang = true)]
        IDiffSpread<bool> FStop;
		
		[Input("Scrub", MinValue = 0, MaxValue = 1)]
        IDiffSpread<double> FScrub;
		
		[Input("Pitch")]
        IDiffSpread<double> FPitch;
		
		
		//OUTPUTS
		[Output("Length")]
		ISpread<double> FLength;
		
		[Output("Clip Name")]
		ISpread<string> FClipName;
		
		[Output("Active Clip Slot")]
        ISpread<int>FActiveClipSlot;
		
		[Output("Active Clip Name")]
        ISpread<string>FActiveClipName;
		
		[Import()]
        ILogger FLogger;
		
		private AbletonLiveTrack track = null;
		private bool FRefresh = true;
		private bool FRefreshEventInited = false;
		
		#region ContextConnect
		public void ConnectPin(IPluginIO pin)
		{
			if (pin == FLiveSet.PluginIO) 
			{
				FRefresh = true;
			}
		}

		public void DisconnectPin(IPluginIO pin)
		{
			if (pin == FLiveSet.PluginIO) 
			{
				if(FLiveSet[0] != null) FLiveSet[0].NotifyRefresh -= onRefresh;
				FRefresh = true;
			}
		}
		#endregion
		
		public void Evaluate(int SpreadMax)
		{
			if(FLiveSet != null)
			{
				if( (FTrack.IsChanged || FRefresh) || ( (track == null) && (FTrack[0] != null) ) )//update track
				{
					
					if(FRefreshEventInited)
					{
						if(FLiveSet[0] != null) FLiveSet[0].NotifyRefresh -= onRefresh;
					}
					
					if(FLiveSet[0] != null) 
					{
						FLiveSet[0].NotifyRefresh += onRefresh;
						FRefreshEventInited = true;
					}
					
					track = FLiveSet[0].GetTrackByName(FTrack[0].Name);
					
					if(track != null)
					{
						//clips
						double[] ClipLength;
						string[] ClipNames;
						var cnt = track.GetClips(out ClipLength, out ClipNames);
						FLength.AssignFrom(ClipLength);
						FClipName.AssignFrom(ClipNames);
					}
					//clear event
					FRefresh = false; 
				}
				//read the outputs
				if(track != null) 
				{
					if(FFireClipBang.IsChanged && FFireClipBang[0]) 
						FLiveSet[0].SendToLive(track.getAddr("fire_clip"), track.GetClipSlotByIndex(FClip[0]));
					
					if(FStop.IsChanged && FStop[0]) 
						FLiveSet[0].SendToLive(track.getAddr("stop_clip"), -1); //track.GetClipSlotByIndex(FClip[0])
					
					if(FScrub.IsChanged)
					{
						var clip =  track.GetClipBySlotIndex(track.playing_slot_index);
						if(clip != null)
						{
							float new_pos = (float) (FScrub[0] * clip.Length_in_beats);
							FLiveSet[0].SendToLive(track.getAddr("scrub_clip"), new_pos);
						}
						
					}
					
					if(FPitch.IsChanged) 
					{
						int pitch_coarse = (int) FPitch[0];
						float pitch_fine = (float) (FPitch[0] - pitch_coarse);
						
						FLiveSet[0].SendToLive(track.getAddr("pitch_clip"), pitch_coarse);
					}
					//if(FStopAll.IsChnaged && FStop[0]) FLiveSet[0].SendToLive(track.getAddr("stop_all_clips"), track.GetClipSlotByIndex(FClip[0]));
					
					FActiveClipSlot[0] 	= track.ActiveClipID;
					FActiveClipName[0] 	= track.ActiveClipName;
				} 
			}
		}
		
		public void Dispose()
		{
			if(FLiveSet[0] != null) 
			{
				FLiveSet[0].NotifyRefresh -= onRefresh;
			}
		}
    

	    private void onRefresh(object sender, EventArgs e)
	    {
	        FRefresh = true;
	    }
	}
	
		
	#region PluginInfo
	[PluginInfo(Name = "Master", 
				Category = "Live", 
				Help = "Live <-> VVVV over OSC", 
				Tags = "live, osc, ableton, control",
				AutoEvaluate = true
	)]
	#endregion PluginInfo
	public class LiveNodeMasterLevel : IPluginEvaluate
	{
		[Input("LiveSet")]
        ISpread<AbletonLiveSet> FLiveSet;
		
		[Output("L")]
        ISpread<double> FLeft;
		
		[Output("R")]
        ISpread<double> FRight;
		
		public void Evaluate(int SpreadMax)
		{
			if( (FLiveSet.SliceCount>0) && (FLiveSet[0] != null) )
			{
				FLeft[0] = FLiveSet[0].MasterLevel_L;
				FRight[01] = FLiveSet[0].MasterLevel_R;
			}
			
		}
	}
	
	#region PluginInfo
	[PluginInfo(Name = "Param", 
				Category = "Live", 
				Help = "Live <-> VVVV over OSC", 
				Tags = "live, osc, ableton, control",
				AutoEvaluate = true
	)]
	#endregion PluginInfo
	public class LiveNodeParam : IPluginEvaluate
	{
		[Input("LiveSet")]
        ISpread<AbletonLiveSet> FLiveSet;
		
		[Input("Address")]
        ISpread<string> FAddress;
		
		[Input("Value")]
        IDiffSpread<double> FValueIn;
		
		[Output("Value")]
        ISpread<double> FValueOut;
		
		public void Evaluate(int SpreadMax)
		{
			if( (FLiveSet.SliceCount>0) && (FLiveSet[0] != null) )
			{
				if(FValueIn.IsChanged) 
				{
					FLiveSet[0].SendToLive(FAddress[0], (float)FValueIn[0]);
					
				}
				FValueOut[0] = 0;
			}
			else FValueOut[0] = -1;
		}
	}
}
