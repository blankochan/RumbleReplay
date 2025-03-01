#include <std/io>

enum FrameType: u8{
ObjectUpdate,
PlayerUpdate=2,
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
StructureType StructureType;
u8 Index;
Position;
Rotation;
};

struct Hand{
Position;
Rotation;
};

struct Foot{
Position;
Rotation;
};

struct PlayerUpdate{
u8 PlayerIndex;
Position HeadPosition;
Rotation HeadRotation;

Position SpinePosition;
Rotation SpineRotation;

Foot feet[2];
Hand Hands[2];
};

struct FrameCasing{
 u16 FrameLength;
 u16 FrameCounter;
 FrameType Type;
 match (Type){
 (FrameType::ObjectUpdate): ObjectUpdate updates[FrameLength/sizeof(ObjectUpdate)];
 (FrameType::PlayerUpdate): PlayerUpdate updates[FrameLength /sizeof(PlayerUpdate)];
 (_): char UnsupportedUpdate[FrameLength];
 }
};

char MagicBytes[2] @ 0x0;
u16 JsonLength @0x00+2;
char JsonString[JsonLength] @0x0+4;
FrameCasing Frames[11034] @ (JsonLength+4);