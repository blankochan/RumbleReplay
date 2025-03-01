using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppRUMBLE.Players;
using Il2CppSystem.Text;
using MelonLoader;
using Newtonsoft.Json;
using RumbleModdingAPI;
using UnityEngine;

namespace RumbleReplay
{
    public sealed class RumbleReplayModClass : MelonMod
    {
        readonly GameObject[] _poolObjects = new GameObject[8]; // Global to the class, easier to keep track of 
        private readonly int[][] _cullers = new int[8][];
        private readonly List<Byte> _writebuffer = new List<Byte>();


        private MelonPreferences_Category _rumbleReplayPreferences;
        private MelonPreferences_Entry<int> _basicPlayerUpdateInterval;
        private MelonPreferences_Entry<int> _basicStructureUpdateInterval;
        private MelonPreferences_Entry<bool> _enabled;
        
        
        public bool Recording;
        public Int16 FrameCounter;
        private string _currentScene;
        FileStream _replayFile;
        BinaryWriter _replayWriter;
        public sealed class ReplayPlayerData
        {
            public string Name = "Unknown";
            public int Battlepoints;
            public string PlayfabID = "Unknown";
            public string Cosmetics = PlayerVisualData.DefaultFemale.ToPlayfabDataString(); // TODO find where Bucket head stores the enums for cosmetics so this string can be handy in places that arent rumble
        }
        public sealed class ReplayHeader //ignore the warnings about unused variables it gets serialized by JsonConvert.SerializeObject
        {
            public readonly string Version = "1.1.0";
            public ReplayPlayerData LocalPlayer = new ReplayPlayerData();
            public ReplayPlayerData RemotePlayer = new ReplayPlayerData();
            public string Scene = "Unknown"; // TODO Integrate with The custom map mod (idr its name) to include the map name
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
        public void NewReplay(string scene,ReplayPlayerData localPlayer, ReplayPlayerData remotePlayer)
        {
            if (!_enabled.Value){return;}
            
            
            ReplayHeader replayHeader = new ReplayHeader
            {
                LocalPlayer = localPlayer,
                RemotePlayer = remotePlayer,
                Scene = scene
            };
            string header = JsonConvert.SerializeObject(replayHeader);
            if (_replayFile != null) { StopReplay(); }
            LoggerInstance.Msg("Recording Started");
            if (!Directory.Exists("UserData/Replays")){ Directory.CreateDirectory("UserData/Replays"); }
            _replayFile = File.Create($"UserData/Replays/{localPlayer.Name}-Vs-{remotePlayer.Name} On {scene}-{Path.GetRandomFileName()}.rr"); 

            _replayWriter = new BinaryWriter(_replayFile);
            byte[] magicBytes = { 0x52, 0x52 }; // 'RR'

            _replayWriter.Write(magicBytes);
            _replayWriter.Write((short)header.Length);
            _replayWriter.Write(Encoding.UTF8.GetBytes(header)); // json header
            
            Recording = true; //should get ModUI support for starting/stopping at some point 
        }
        public void StopReplay()
        {
            if (_replayFile == null)
            {
                LoggerInstance.Warning("StopReplay with null replay file");
                return;
            }
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
            _playerUpdateInterval = _rumbleReplayPreferences.CreateEntry("PlayerUpdate_Interval", 4,description:"The interval we create updates for the players Hands and Head");
            _basicStructureUpdateInterval = _rumbleReplayPreferences.CreateEntry("BasicStructureUpdate_Interval", 1,description:"the interval structure positions and rotations are updated (Leave at 1 for 1 update every physics frame, 2 for one update every 2 and so on)");
            _enabled = _rumbleReplayPreferences.CreateEntry("RecordingEnabled", true);
            _rumbleReplayPreferences.SaveToFile();
            
            LoggerInstance.Msg($"PlayerUpdate_Interval={_playerUpdateInterval.Value}");
            LoggerInstance.Msg($"BasicStructureUpdate_Interval={_basicStructureUpdateInterval.Value}");
            LoggerInstance.Msg($"Enabled {_enabled.Value}");
        }
        
        public override void OnSceneWasLoaded(int _, string sceneName)
        {
            if (Recording) StopReplay();    
            _currentScene = sceneName;
        }
        private void MapReady()
        {
            if (_currentScene != "Loader" && _currentScene != "Park" && _currentScene != "Gym")
            {
                LoggerInstance.Msg($"Loaded scene: {_currentScene}");
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
                var localPlayer = new ReplayPlayerData()
                {
                    Name = Regex.Replace(Calls.Managers.GetPlayerManager().LocalPlayer?.Data.GeneralData.PublicUsername ?? "Unknown","<.*?>|[^a-zA-Z0-9_ ]",""),
                    Battlepoints = Calls.Managers.GetPlayerManager().LocalPlayer?.Data.GeneralData.BattlePoints ?? 0,
                    PlayfabID = Calls.Managers.GetPlayerManager().LocalPlayer?.Data.GeneralData.PlayFabMasterId ?? "Unknown",
                    Cosmetics = Calls.Managers.GetPlayerManager().LocalPlayer?.Data.visualData.ToPlayfabDataString() ?? PlayerVisualData.DefaultFemale.ToPlayfabDataString(),
                };
                var remotePlayer = new ReplayPlayerData()
                {
                    Name = Regex.Replace(Calls.Players.GetEnemyPlayers().FirstOrDefault()?.Data.GeneralData.PublicUsername ?? "Unknown","<.*?>|[^a-zA-Z0-9_ ]",""),
                    Battlepoints = Calls.Players.GetEnemyPlayers().FirstOrDefault()?.Data.GeneralData.BattlePoints ?? 0,
                    PlayfabID = Calls.Players.GetEnemyPlayers().FirstOrDefault()?.Data.GeneralData.PlayFabMasterId ?? "Unknown",
                    Cosmetics = Calls.Players.GetEnemyPlayers().FirstOrDefault()?.Data.visualData.ToPlayfabDataString() ?? PlayerVisualData.DefaultFemale.ToPlayfabDataString(),
                };
                
                LoggerInstance.Msg(localPlayer.Name);
                LoggerInstance.Msg(remotePlayer.Name);
                
                NewReplay(_currentScene,localPlayer,remotePlayer);  
            }
        }

        public override void OnApplicationQuit()
        {
            if (Recording)
            {
                StopReplay();
            }
        }

        private static List<Byte> _createPlayerUpdate()
        {
                        int index = 0;
                        List<Byte> frame = new List<Byte>();
                        foreach (Player player in Calls.Managers.GetPlayerManager().AllPlayers) // worth noting this doesn't instantly update so you can Null Reference despite IDE's saying its known not to be null
                        {
                            // remote player hitboxes start at 6 instead of 5 and ALlPlayers always starts at 0 for the local
                            Transform head = player?.Controller.transform.GetChild(index > 0 ? 6 : 5).GetChild(4).transform ?? new Transform(); // head hitbox,
                            // I don't fully remember why im using the hitbox, I think it was something along the lines of the quaternion rotation being broken with headset offset

                            Transform spine = player?.Controller.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(4).transform ?? new Transform(); // Spine Bone; I fucking hate working with bones

                            Transform leftHand = player?.Controller.transform.GetChild(1).GetChild(1).transform ?? new Transform(); // Left Controller
                            Transform rightHand = player?.Controller.transform.GetChild(1).GetChild(2).transform ?? new Transform(); // Right Controller   


                            // Actually toebones because its more helpful, because foot/heel will be inferred by IK if its wanted
                            Transform leftFoot = player?.Controller.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetChild(0).transform ?? new Transform();
                            Transform rightFoot = player?.Controller.transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(0).transform ?? new Transform();


                            frame.Add(((byte)index)); // PlayerId
                            frame.AddRange(SerializeTransform(head));

                            frame.AddRange(SerializeTransform(spine));

                            // Hands
                            frame.AddRange(SerializeTransform(leftHand));
                            frame.AddRange(SerializeTransform(rightHand));

                            // Feet
                            frame.AddRange(SerializeTransform(leftFoot));
                            frame.AddRange(SerializeTransform(rightFoot));
                            index++;
                        }
                        return frame;
        }

        private List<Byte> _createBasicStructureUpdate()
        {
            List<Byte> frame = new List<Byte>();
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

                    frame.Add(((byte)poolIndex)); // Structure Type
                    frame.Add(((byte)i)); // Object Index, there might be space savings here, but I don't know how to do that and its nicer looking this way.
                    Transform transform = structure.transform; 
                    if (!structure.active) // rumble when it breaks a structure disables it (or when it's not spawned)
                    {
                        transform.position = new Vector3(0,-300,0); // arbitrary, allows for a parser to see -300 and just mark it as destroyed without making the format overly complex
                    } 
                        
                    frame.AddRange(SerializeTransform(transform));
                        
                }
            }
            return frame;
        }
        
        public override void OnFixedUpdate()
        {
            if ( Recording )
            {
                
                List<byte> playerUpdateFrame = new List<byte>();
                if (FrameCounter % _playerUpdateInterval.Value == 0) // my hack fix for every other frame
                {
                         playerUpdateFrame.AddRange(_createPlayerUpdate());
                }
                List<Byte> objectUpdatePartialFrame = new List<Byte>();
                if (FrameCounter % _basicStructureUpdateInterval.Value == 0){ 
                    objectUpdatePartialFrame.AddRange(_createBasicStructureUpdate());
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
                if (playerUpdateFrame.Count != 0)
                {
                    _writebuffer.AddRange(BitConverter.GetBytes(((short)playerUpdateFrame.Count))); // short in the event it's ever longer than 256 bytes, if a single frame takes 65,536 bytes something is probably wrong
                    _writebuffer.AddRange(BitConverter.GetBytes(FrameCounter));
                    _writebuffer.Add(2); // PlayerUpdate
                    _writebuffer.AddRange(playerUpdateFrame);
                }
                FrameCounter++;



                if (_writebuffer.Count >= 1000) //1kb of replay, arbitrary, TODO replace with more complicated more time based logic
                {
                    _replayWriter.Write(_writebuffer.ToArray());
                    _writebuffer.Clear();
                   
                }

            }
        }
    }
}
