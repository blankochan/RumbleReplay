#include <std/io>

enum FrameType: u8{
ObjectUpdate,
BasicPlayerUpdate,
};

struct Position{
float X;
float Y;
float Z;
};

struct Rotation{
float W;
float Y;
float X;
float Z;
};

enum StructureType: u8{
    Ball,
    BoulderBall,
    Cube,
    Disc,
    LargeRock,
    Pillar,
    Wall,
    SmallRock,
};
struct ObjectUpdate{
StructureType Type;
u8 Index;
Position;
Rotation;
};

struct Hand{
Position;
Rotation;
};

struct BasicPlayerUpdate{
u8 PlayerIndex;
Position HeadPosition;
Rotation HeadRotation;
Hand Hands[2];
};

struct FrameCasing{
 u16 FrameLength;
 u16 FrameCounter;
 FrameType Type;
 match (Type){
 (FrameType::ObjectUpdate): ObjectUpdate updates[FrameLength/sizeof(ObjectUpdate)];
 (FrameType::BasicPlayerUpdate): BasicPlayerUpdate updates[FrameLength /sizeof(BasicPlayerUpdate)];
 (_): char UnsupportedUpdate[FrameLength];
 }
};

char MagicBytes[2] @ 0x0;
u8 JsonLength @0x00+2;
char JsonString[JsonLength] @0x0+3;
FrameCasing Frames[3000] @ (JsonLength+3);