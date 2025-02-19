
using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BlenderReplayMod
{
    public class BlenderReplayModClass : MelonMod
    {
        GameObject[] _poolObjects = new GameObject[8]; // Global to the class, easier to keep track of 
        readonly int[][] _cullers = new int[8][];
        List<Byte> _writebuffer = new List<Byte>();


        public bool Recording;
        public Int16 FrameCounter;
        internal string CurrentScene;
        FileStream _replayFile;
        BinaryWriter _replayWriter;

        public void NewReplay(string scene,string localPlayerName = "Unspecified", string remotePlayerName = "Unspecified")
        {
            if (_replayFile != null) { StopReplay(); }
            LoggerInstance.Msg("Recording Started");
            _replayFile = File.Create($"replays/{localPlayerName}Vs{remotePlayerName}On{scene}-{Path.GetRandomFileName()}.rr"); 

            _replayWriter = new BinaryWriter(_replayFile);
            Recording = true; //should get ModUI support for starting/stopping at somepoint 
        }
        public void StopReplay()
        {
            if (_replayFile == null) { return; }

            Recording = false; //should get ModUI support for starting/stopping at somepoint
            FrameCounter = 0;
            _replayFile.Close();
            _replayFile = null;
            _replayWriter = null;


        }
        public override void OnLateInitializeMelon()
        {
            Calls.onMapInitialized += MapReady;
        }

        public override void OnSceneWasLoaded(int buildindex, string sceneName)
        {
            CurrentScene = sceneName;
        }
        public void MapReady()
        {
            Recording = false;
            if (CurrentScene != "Loader" && CurrentScene != "Park") //remember to replace Gym with Park when everything works
            {
                MelonLogger.Msg($"Loaded scene: {CurrentScene}");
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
                if (CurrentScene != "Gym")
                {
                    string localPlayer = Calls.Managers.GetPlayerManager().LocalPlayer.Data.GeneralData.PublicUsername;
                    string remotePlayer = Calls.Players.GetEnemyPlayers()[0].Data.GeneralData.PublicUsername;
                    MelonLogger.Msg(localPlayer);
                    MelonLogger.Msg(remotePlayer);
                   NewReplay(CurrentScene,Regex.Replace(localPlayer, "[^a-zA-Z0-9_ ]", ""),Regex.Replace(remotePlayer, "[^a-zA-Z0-9_ ]", ""));  
                }
                else NewReplay(CurrentScene);
            }
            else // put our stop logic here
            {
                StopReplay();
            }
        }
        public override void OnFixedUpdate()
        {
            if ( Recording )
            {
                
                List<Byte> partialFrame = new List<Byte>();
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

                        partialFrame.Add(((byte)poolIndex)); // Structure Type
                        partialFrame.Add(((byte)i)); // Object Index, there might be space savings here, but I don't know how to do that and its nicer looking this way.


                        partialFrame.AddRange(BitConverter.GetBytes(structure.transform.position.x)); // Position X
                        partialFrame.AddRange(BitConverter.GetBytes(structure.transform.position.y)); // Position Y
                        partialFrame.AddRange(BitConverter.GetBytes(structure.transform.position.z)); // Position Z


                        partialFrame.AddRange(BitConverter.GetBytes(structure.transform.rotation.w)); // Rotation W
                        partialFrame.AddRange(BitConverter.GetBytes(structure.transform.rotation.y)); // Position Y
                        partialFrame.AddRange(BitConverter.GetBytes(structure.transform.rotation.x)); // Position X
                        partialFrame.AddRange(BitConverter.GetBytes(structure.transform.rotation.z)); // Position Z
                    }
                }
                //Frame Header
                if (partialFrame.Count != 0)
                {
                    _writebuffer.AddRange(BitConverter.GetBytes(((short)partialFrame.Count))); // short in the event it's ever longer than 256 bytes, if a single frame takes 65,536 bytes something is probably wrong
                    _writebuffer.AddRange(BitConverter.GetBytes(FrameCounter));
                    _writebuffer.Add(0); // UpdateType, currently always 0 because ive yet to program other types, but Type 0 Represents object position/rotation updates 
                    _writebuffer.AddRange(partialFrame);
                }

                FrameCounter++;



                if (_writebuffer.Count >= 1000) //1kb of replay, arbitrary, TODO replace with more complicated more time based logic
                {
                    foreach (var currentByte in _writebuffer)
                    {
                        _replayWriter.Write(currentByte);
                    }
                    LoggerInstance.Msg($"Writing {_writebuffer.Count} bytes, Frame:{FrameCounter}");
                    _writebuffer.Clear();
                   
                }

            }
        }
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                // fill with debugging info or smth
            }
        }
    }
}
