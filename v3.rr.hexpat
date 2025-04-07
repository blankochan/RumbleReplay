// RR Protocol v3.0.0
#pragma magic [52 52] @ 0x0
// Aims to improve Forwards/Backwards-Compatibility over v2;
// whilst being more Maintainable.

// Worth specifying it is completely incompatible with prior versions.


// v3 relies on this concept of "Updates With Elements", instead of "Frame Types";
// to give itself Higher Future Proofing, whilst making smaller updates possible.

//Generic Structs
struct Vector3 {
  float x, y, z;
};

struct Quaternion {
  float w, x, y, z;
};

enum Structure: u8{

    Ball,
    BoulderBall,
    Cube,
    Disc,
    LargeRock,
    Pillar,
    SmallRock,
    Wall,
};

enum UpdateType: u8{

    Structure,
    Player,
};

bitfield Modifiers { // Equivalent to [Flags] in C#

    bool Explode : 1;
    bool Flick : 1;
    bool Grounded : 1;
    bool Hold : 1;
    bool Kick : 1;
    bool Parry : 1;
    bool Straight : 1;
    bool Uppercut : 1;
    bool Volatile : 1;
    
   
    // 7 more slots before this needs reconsidering
};

enum ElementType: u8{

    DUMMY, 
    Position,
    Rotation,
    Structure,
    Health,
    Modifiers,
};

struct Element{ 

    ElementType Type; 
    u8 Length; // allows a parser to skip unrecognized ElementType's
    
        match (Type) {
        (ElementType::DUMMY): u8 Dummy;
        (ElementType::Position): Vector3 Position;
        (ElementType::Rotation): Quaternion Position;
        (ElementType::Structure): u8 Index;
        (ElementType::Health): u8 Health;
        (ElementType::Modifiers): Modifiers;
        
        (_): char Data[Length]; // Unknown
    }  
};

struct Update{    

    UpdateType Type; // Basically Frame Type from v2, Analogous to an Class
    u16 Length; // Use for skip bytes if unsupported UpdateType
    
    u8 ElementCount; // if you seriously have more than 255 Elements seek help 
    Element Elements[ElementCount];
};

struct Frame{

    u32 FrameCounter; // so incredibly overkill :sob:
    
    u8 UpdateCount;
    Update Updates[UpdateCount];
 };
 
#include <std/mem>
struct ReplayFile{
    char Magic[2];
    u16 HeaderLength;
    char Header[HeaderLength]; //Json Header
    
    Frame Frames[while(!std::mem::eof())]; // Everything after Header is Frames until EOF
};

ReplayFile Replay @ 0x0;