
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

        FileStream _replayFile;
        BinaryWriter _replayWriter;

        public void NewReplay()
        {
            if (_replayFile != null) { StopReplay(); }
            LoggerInstance.Msg("Recording Started");
            _replayFile = File.Create($"replays/{Path.GetRandomFileName()}.br"); // TODO Better filename system

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
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Recording = false;
            if (sceneName == "Gym" || sceneName == "Map0" || sceneName == "Map1") //remember to replace Gym with Park when everything works
            {
                // Setup Pools Into our PoolObjects array
                _poolObjects[0] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolBall();
                _poolObjects[1] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolBoulderBall();
                _poolObjects[2] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolCube();
                _poolObjects[3] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolDisc();
                _poolObjects[4] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolLargeRock();
                _poolObjects[5] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolPillar();
                _poolObjects[6] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolWall();
                _poolObjects[7] = RumbleModdingAPI.Calls.Pools.Structures.GetPoolSmallRock();

                
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

                NewReplay();
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

                        partialFrame.Add(((byte)poolIndex)); //Structure Type
                        partialFrame.Add(((byte)i)); // Object Index


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
                    _writebuffer.Add(((byte)partialFrame.Count));
                    _writebuffer.Add(0); // padding/Identifes as a object update frame
                    _writebuffer.AddRange(BitConverter.GetBytes(FrameCounter));
                    _writebuffer.AddRange(partialFrame);
                }

                FrameCounter++;



                if (_writebuffer.Count > 1000) //1kb of replay
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
