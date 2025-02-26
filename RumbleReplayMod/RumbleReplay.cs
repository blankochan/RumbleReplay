using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppRUMBLE.Players;
using MelonLoader;
using Newtonsoft.Json;
using RumbleModdingAPI;
using UnityEngine;

namespace RumbleReplay
{
    public sealed class RumbleReplayModClass : MelonMod
    {
        GameObject[] _poolObjects = new GameObject[8]; // Global to the class, easier to keep track of 
        readonly int[][] _cullers = new int[8][];
        List<Byte> _writebuffer = new List<Byte>();


        private MelonPreferences_Category _rumbleReplayPreferences;
        private MelonPreferences_Entry<int> _basicPlayerUpdateInterval;
        private MelonPreferences_Entry<int> _basicStructureUpdateInterval;
        private MelonPreferences_Entry<bool> _enabled;
        
        
        public bool Recording;
        public Int16 FrameCounter;
        internal string CurrentScene;
        FileStream _replayFile;
        BinaryWriter _replayWriter;
        public sealed class ReplayHeader //ignore the warnings about unused variables it gets serialized by JsonConvert.SerializeObject
        {
            public readonly string Version = "1.0.2";
            public string EnemyName = "Unknown";
            public string LocalName = "Unknown";
            public string MapName = "Unknown";
            public readonly string Date = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
        }

        private static List<Byte> SerializeTransform(Transform transform,bool includeRotation=true,bool includePosition=true ,bool includeScale=false)
        {
            List<Byte> buffer = new List<Byte>();
            if (includePosition){
                buffer.AddRange(BitConverter.GetBytes(transform.position.x)); // Position X
                buffer.AddRange(BitConverter.GetBytes(transform.position.y)); // Position Y
                buffer.AddRange(BitConverter.GetBytes(transform.position.z)); // Position Z
            }

            if (includeRotation)
            {
                buffer.AddRange(BitConverter.GetBytes(transform.rotation.w)); // Rotation W
                buffer.AddRange(BitConverter.GetBytes(transform.rotation.y)); // Rotation Y
                buffer.AddRange(BitConverter.GetBytes(transform.rotation.x)); // Rotation X
                buffer.AddRange(BitConverter.GetBytes(transform.rotation.z)); // Rotation Z
            }

            if (includeScale)
            {
                buffer.AddRange(BitConverter.GetBytes(transform.localScale.x)); // Scale X
                buffer.AddRange(BitConverter.GetBytes(transform.localScale.y)); // Scale Y
                buffer.AddRange(BitConverter.GetBytes(transform.localScale.z)); // Scale Z
            }

            return buffer;
        }
        public void NewReplay(string scene,string localPlayerName = "", string remotePlayerName = "")
        {
            if (!_enabled.Value){return;}
            
            
            ReplayHeader replayHeader = new ReplayHeader
            {
                EnemyName = remotePlayerName,
                LocalName = localPlayerName,
                MapName = scene
            };
            string header = JsonConvert.SerializeObject(replayHeader);
            if (_replayFile != null) { StopReplay(); }
            LoggerInstance.Msg("Recording Started");
            if (!Directory.Exists("UserData/Replays")){ Directory.CreateDirectory("UserData/Replays"); }
            _replayFile = File.Create($"UserData/Replays/{localPlayerName}-Vs-{remotePlayerName} On {scene}-{Path.GetRandomFileName()}.rr"); 

            _replayWriter = new BinaryWriter(_replayFile);
            byte[] magicBytes = { 0x52, 0x52 }; // 'RR'

            _replayWriter.Write(magicBytes);
            _replayWriter.Write((byte)header.Length);
            _replayWriter.Write(Encoding.UTF8.GetBytes(header)); // json header
            
            Recording = true; //should get ModUI support for starting/stopping at some point 
        }
        public void StopReplay()
        {
            if (_replayFile == null) { return; }
            _replayWriter.Write(_writebuffer.ToArray());
            _writebuffer.Clear(); // Write Any Pending Data
            
            Recording = false; //should get ModUI support for starting/stopping at some point
            FrameCounter = 0;
            LoggerInstance.Msg("Recording Stopped");
            _replayFile.Close();
            _replayFile = null;
            _replayWriter = null;


        }
        public override void OnLateInitializeMelon()
        {
            Calls.onMapInitialized += MapReady;
        }

        public override void OnInitializeMelon()
        {
            _rumbleReplayPreferences = MelonPreferences.CreateCategory("RumbleReplaySettings");
            _rumbleReplayPreferences.SetFilePath(@"UserData/RumbleReplay.cfg");
            _basicPlayerUpdateInterval = _rumbleReplayPreferences.CreateEntry("BasicPlayerUpdate_Interval", 4,description:"The interval we create updates for the players Hands and Head (will deprecate when better solution arises)");
            _basicStructureUpdateInterval = _rumbleReplayPreferences.CreateEntry("BasicStructureUpdate_Interval", 1,description:"the interval structure positions and rotations are updated (Leave at 1 for 1 update every physics frame, 2 for one update every 2 and so on)");
            _enabled = _rumbleReplayPreferences.CreateEntry("RecordingEnabled", true);
            _rumbleReplayPreferences.SaveToFile();
            
            LoggerInstance.Msg($"BasicPlayerUpdate_Interval={_basicPlayerUpdateInterval.Value}");
            LoggerInstance.Msg($"BasicStructureUpdate_Interval={_basicStructureUpdateInterval.Value}");
            LoggerInstance.Msg($"Enabled {_enabled.Value}");
        }
        
        public override void OnSceneWasLoaded(int _, string sceneName)
        {
            if (Recording) StopReplay();    
            CurrentScene = sceneName;
        }
        private void MapReady()
        {
            if (CurrentScene != "Loader" && CurrentScene != "Park" && CurrentScene != "Gym")
            {
                LoggerInstance.Msg($"Loaded scene: {CurrentScene}");
                // Setup Pools Into our PoolObjects array
                _poolObjects[0] = Calls.Pools.Structures.GetPoolBall();
                _poolObjects[1] = Calls.Pools.Structures.GetPoolBoulderBall();
                _poolObjects[2] = Calls.Pools.Structures.GetPoolCube();
                _poolObjects[3] = Calls.Pools.Structures.GetPoolDisc();
                _poolObjects[4] = Calls.Pools.Structures.GetPoolLargeRock();
                _poolObjects[5] = Calls.Pools.Structures.GetPoolPillar();
                _poolObjects[6] = Calls.Pools.Structures.GetPoolWall();
                _poolObjects[7] = Calls.Pools.Structures.GetPoolSmallRock();

                
                for (UInt16 poolIndex = 0; poolIndex < _poolObjects.Length; poolIndex++)
                {
                    var pool = _poolObjects[poolIndex];
                    _cullers[poolIndex] = new int[pool.transform.GetChildCount()];
                    LoggerInstance.Msg(_cullers[poolIndex].Length);
                    LoggerInstance.Msg(pool.transform.GetChild(0).name);
                    for (UInt16 i = 0; i < pool.transform.GetChildCount(); i++)
                    {
                        GameObject structure = pool.transform.GetChild(i).gameObject;
                        _cullers[poolIndex][i] = structure.transform.position.GetHashCode();
                        

                    }
                }
                string localPlayer = Calls.Managers.GetPlayerManager().LocalPlayer?.Data.GeneralData.PublicUsername ?? "Unknown"; // should never happen but it might?
                string remotePlayer = Calls.Players.GetEnemyPlayers().FirstOrDefault()?.Data.GeneralData.PublicUsername ?? "Unknown";
                LoggerInstance.Msg(localPlayer);
                LoggerInstance.Msg(remotePlayer);
                NewReplay(CurrentScene,Regex.Replace(localPlayer, "[^a-zA-Z0-9_ ]", ""),Regex.Replace(remotePlayer, "[^a-zA-Z0-9_ ]", ""));  
            }
        }

        public override void OnApplicationQuit()
        {
            if (Recording)
            {
                StopReplay();
            }
        }

        
        
        public override void OnFixedUpdate()
        {
            if ( Recording )
            {
                List<Byte> basicPlayerUpdatePartialFrame = new List<Byte>();
                if (FrameCounter % _basicPlayerUpdateInterval.Value == 0) // my hack fix for every other frame
                {
                    int index = 0;
                    foreach (Player player in Calls.Managers.GetPlayerManager().AllPlayers)
                    {
                        
                        int hitboxOffset = 5;
                        if (index != 0) hitboxOffset = 6; // remote player hitboxes start at 6 instead of 5 and according to ulvak ALlPlayers always starts at 0 for the local
                         Transform headTransform = player.Controller.transform.GetChild(hitboxOffset).GetChild(4).transform; // head position
                         Transform leftHandTransform = player.Controller.transform.GetChild(1).GetChild(1).transform; // Left Hand Transform
                         Transform rightHandTransform = player.Controller.transform.GetChild(1).GetChild(2).transform; // Right Hand Transform
                         
                         basicPlayerUpdatePartialFrame.Add(((byte)index)); // PlayerId
                         
                         // Head
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(headTransform.position.x)); // Position X
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(headTransform.position.y)); // Position Y
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(headTransform.position.z)); // Position Z
                         
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(headTransform.rotation.w)); // Rotation W
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(headTransform.rotation.y)); // Position Y
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(headTransform.rotation.x)); // Position X
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(headTransform.rotation.z)); // Position Z
                         
                         // Hands
                         // Left Hand
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(leftHandTransform.position.x)); // Position X
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(leftHandTransform.position.y)); // Position Y
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(leftHandTransform.position.z)); // Position Z
                         
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(leftHandTransform.rotation.w)); // Rotation W
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(leftHandTransform.rotation.y)); // Position Y
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(leftHandTransform.rotation.x)); // Position X
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(leftHandTransform.rotation.z)); // Position Z
                         
                         // Right Hand
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(rightHandTransform.position.x)); // Position X
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(rightHandTransform.position.y)); // Position Y
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(rightHandTransform.position.z)); // Position Z
                         
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(rightHandTransform.rotation.w)); // Rotation W
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(rightHandTransform.rotation.y)); // Position Y
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(rightHandTransform.rotation.x)); // Position X
                         basicPlayerUpdatePartialFrame.AddRange(BitConverter.GetBytes(rightHandTransform.rotation.z)); // Position Z
                         
                         index++;
                    }
                }
                List<Byte> objectUpdatePartialFrame = new List<Byte>();
                if (FrameCounter % _basicStructureUpdateInterval.Value == 0){ 
                    for (UInt16 poolIndex = 0; poolIndex < _poolObjects.Length; poolIndex++)
                    {
                        var pool = _poolObjects[poolIndex];
                        for (UInt16 i = 0; i < _cullers[poolIndex].Length; i++)
                        {
                            GameObject structure = pool.transform.GetChild(i).gameObject;
                        if (_cullers[poolIndex][i] == structure.transform.position.GetHashCode()) 
                        {
                            continue;
                        } 

                        _cullers[poolIndex][i] = structure.transform.position.GetHashCode();

                        objectUpdatePartialFrame.Add(((byte)poolIndex)); // Structure Type
                        objectUpdatePartialFrame.Add(((byte)i)); // Object Index, there might be space savings here, but I don't know how to do that and its nicer looking this way.
                        Transform transform = structure.transform;
                        var transformPosition = structure.transform.position; // for some reason rider throws an error, if I don't separate it out, visual studio 2022 doesn't, but I use rider so stuck with this
                        if (structure.GetComponent<Rigidbody>().IsSleeping() && structure.transform.position.y <= 0) // rumble when it breaks something sets the Y to a number below 0 (the exact number changes on my system sometimes, but It's always below 0)
                        {
                            transformPosition.y = -300; // arbitrary, allows for a parser to see -300 and just mark it as destroyed without making the format overly complex
                        } 
                        objectUpdatePartialFrame.AddRange(BitConverter.GetBytes(transformPosition.x)); // Position X
                        objectUpdatePartialFrame.AddRange(BitConverter.GetBytes(transformPosition.y)); // Position Y
                        objectUpdatePartialFrame.AddRange(BitConverter.GetBytes(transformPosition.z)); // Position Z

                        
                        objectUpdatePartialFrame.AddRange(BitConverter.GetBytes(transform.rotation.w)); // Rotation W
                        objectUpdatePartialFrame.AddRange(BitConverter.GetBytes(transform.rotation.y)); // Position Y
                        objectUpdatePartialFrame.AddRange(BitConverter.GetBytes(transform.rotation.x)); // Position X
                        objectUpdatePartialFrame.AddRange(BitConverter.GetBytes(transform.rotation.z)); // Position Z
                        }
                    } 
                }
                //Frame Header
                if (objectUpdatePartialFrame.Count != 0)
                {
                    _writebuffer.AddRange(BitConverter.GetBytes(((short)objectUpdatePartialFrame.Count))); // short in the event it's ever longer than 256 bytes, if a single frame takes 65,536 bytes something is probably wrong
                    _writebuffer.AddRange(BitConverter.GetBytes(FrameCounter));
                    _writebuffer.Add(0); // ObjectUpdate
                    _writebuffer.AddRange(objectUpdatePartialFrame);
                }
                //Frame Header
                if (basicPlayerUpdatePartialFrame.Count != 0)
                {
                    _writebuffer.AddRange(BitConverter.GetBytes(((short)basicPlayerUpdatePartialFrame.Count))); // short in the event it's ever longer than 256 bytes, if a single frame takes 65,536 bytes something is probably wrong
                    _writebuffer.AddRange(BitConverter.GetBytes(FrameCounter));
                    _writebuffer.Add(1); // BasicPlayerUpdate
                    _writebuffer.AddRange(basicPlayerUpdatePartialFrame);
                }
                FrameCounter++;



                if (_writebuffer.Count >= 1000) //1kb of replay, arbitrary, TODO replace with more complicated more time based logic
                {
                    _replayWriter.Write(_writebuffer.ToArray());
                    //LoggerInstance.Msg($"Writing {_writebuffer.Count} bytes, Frame:{FrameCounter}");
                    _writebuffer.Clear();
                   
                }

            }
        }
    }
}
