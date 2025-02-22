
import bpy
from bpy_extras.io_utils import ImportHelper
from bpy.types import Operator
from mathutils import Quaternion,Euler
import math
from enum import Enum
from typing import Tuple
from io import BufferedReader
import struct
import json
## dear reader im sorry for this dogshit addon i wrote;
## it gets the job done -blank
bl_info = {
    "name": "RumbleReplay",
    "category": "Object",
}


class StructureTypeEnum(Enum):
    Ball = 0
    BoulderBall = 1
    Cube = 2
    Disc = 3
    LargeRock = 4
    Pillar = 5
    Wall = 6
    SmallRock = 7


class FrameTypeEnum(Enum): # when we have other types this will be helpful for readablity
    ObjectUpdate = 0
    BasicPlayerUpdate = 1

class VIEW3D_PT_rumblereplay(bpy.types.Panel): #side panel
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_label = "RumbleReplay"
    def draw(self,context):
        """define layout of our panel"""
        Select = self.layout.row()
        Generate = self.layout.row()
        Setup = self.layout.row()

        Select.operator("rumblereplay.select",text="Select File")
        Generate.operator("rumblereplay.animate",text="Generate Animation")
        Setup.operator("rumblereplay.scenesetup",text="Setup Scene")



class GenerateAnim(bpy.types.Operator):
    """Generate the animation"""
    bl_idname = "rumblereplay.animate"
    bl_label = "Generate Animation"
    def execute(self,context):
        if context.scene.SelectedFile == "": self.report({'ERROR'},"No file selected"); return {'FINISHED'}
        try: bpy.data.objects["World"]
        except: self.report({'ERROR'},"Please Press Setup Scene"); return {'FINISHED'}
        f = open(context.scene.SelectedFile,"rb")
        print("generating animation")
        magicBytes = f.read(2)
        if not all(Byte == 0x52 for Byte in magicBytes):
            print(f"Not RumbleReplay:{magicBytes}")
            f.close()
            self.report({'ERROR'},"Not a valid RumbleReplay file.")
            return {'FINISHED'}
        headerLength:int = f.read(1)[0]
        Header:dict = json.loads(f.read(headerLength))
        print(Header)
        if Header["Version"] != "1.0.0": self.report({'WARNINGZ'},"Mismatched replayfile version, should be fine")
        generateAnimation(f)

        f.close()
        return {'FINISHED'}
    

class SetupScene(bpy.types.Operator):
    """Setup the scene"""
    bl_idname = "rumblereplay.scenesetup"
    bl_label = "Setup Scene"
    def execute(self,context:bpy.types.Context):
        print("Setting Up Scene")

        if bpy.data.collections.get("Structures") == None: self.report({'ERROR'},"Please link or append a collection with the Structures"); return {'FINISHED'}
        StructureCollection:bpy.types.Collection = bpy.data.collections.get("Structures")
        Pool:bpy.types.Collection = bpy.data.collections.new("RumbleReplay Pool",)
        context.scene.collection.children.link(Pool)

        bpy.ops.object.empty_add(type='PLAIN_AXES')
        WorldParent:bpy.types.Object = bpy.data.objects["Empty"]
        WorldParent.name = "World"
        WorldParent.rotation_euler = Euler((math.radians(90),0,0))


        for i in range(5):
            bpy.ops.object.empty_add(type='CUBE')
            Player:bpy.types.Object = bpy.data.objects["Empty"]
            Player.name = f"Player.{str(i).zfill(3)}"
            Player.scale = [0.2,0.2,0.2]

            bpy.ops.object.empty_add(type='CUBE')
            handLeft:bpy.types.Object = bpy.data.objects["Empty"]
            handLeft.name = f"Hand.{str(i).zfill(3)}.L"
            handLeft.scale = [0.1,0.1,0.2]

            bpy.ops.object.empty_add(type='CUBE')
            handRight:bpy.types.Object = bpy.data.objects["Empty"]
            handRight.name = f"Hand.{str(i).zfill(3)}.R"
            handRight.scale = [0.1,0.1,0.2]

            Pool.objects.link(Player)

            Pool.objects.link(handLeft)
            Pool.objects.link(handRight)

            handLeft.parent = WorldParent
            handRight.parent = WorldParent
            Player.parent = WorldParent

        #Pool.objects.link(ArrowParent)

        for obj in StructureCollection.objects:
            for _ in range(25): # should be enough in most cases, we delete unused objects after animation generation anyway
                new_obj = obj.copy()
                new_obj.data = obj.data.copy() 
                Pool.objects.link(new_obj)
                new_obj.parent = WorldParent
        return {'FINISHED'}
    
class OT_OpenFilebrowser(Operator, ImportHelper):
     bl_idname = "rumblereplay.open_filebrowser"
     bl_label = "Open the file browser (yay)" 
     def execute(self, context): 
        """Do something with the selected file.""" 
        context.scene.SelectedFile = self.filepath
        return {'FINISHED'}
     
class SelectFile(bpy.types.Operator):
    """Generate the animation"""
    bl_idname = "rumblereplay.select"
    bl_label = "Select File"
    def execute(self,context):
        bpy.ops.rumblereplay.open_filebrowser('INVOKE_DEFAULT')
        return {'FINISHED'}     


def generateAnimation(f:BufferedReader):
    FileContinues = True
    
    while FileContinues:
        FrameLength, FrameCounter,FrameType = struct.unpack("<2HB",f.read(5)) # FrameCasing Header
        FileContinues = parseframes(f,FrameLength, FrameCounter,FrameType)

    bpy.context.scene.frame_end = FrameCounter

    for obj in bpy.data.objects["World"].children: # cull anything that didnt end up getting animated
        obj:bpy.types.Object = obj
        bpy.data.objects["World"].scale = (-1,1,1)
        if not obj.animation_data:
            bpy.data.objects.remove(obj, do_unlink=True)
            


def ParseObjectUpdate(f:BufferedReader,FrameLength:int, FrameCounter:int,FrameType:int):
    for _ in range(int(FrameLength/30)): # for ObjectUpdates each update is 30 bytes
        STRUCTURE_TYPE, OBJECT_INDEX = struct.unpack("<2B",f.read(2))
        POSITION:Tuple[float,float,float] = struct.unpack("<3f",f.read(12))
        W,Y,X,Z =struct.unpack("<4f",f.read(16))
        ROTATION:Quaternion = Quaternion((W,X,Y,Z))

        OBJECT_NAME:str = f"{StructureTypeEnum(STRUCTURE_TYPE).name}.{str(OBJECT_INDEX+1).zfill(3)}" #should output something like Wall.009
    # now its time for the fun part!, animating!!!
        try:
            obj:bpy.types.Object = bpy.data.objects[OBJECT_NAME]
        except:
            print(OBJECT_NAME,":UnknownStructure@",f.tell())
            continue # should call if its an object we dont have an object for, isnt a big deal like 99% of time 


        obj.location = POSITION
        obj.rotation_mode = "QUATERNION"
        obj.rotation_quaternion = ROTATION
        HideOnDestory:bool = False
        if POSITION[1] == -300: # Y
            if obj.animation_data and obj.animation_data.action:
                for fcurve in obj.animation_data.action.fcurves:
                    if fcurve.keyframe_points:
                        last_kf = fcurve.keyframe_points[-1]
                        last_kf.interpolation = 'CONSTANT'
            obj.hide_render = HideOnDestory 
            obj.hide_viewport = HideOnDestory    
            obj.keyframe_insert(data_path="hide_viewport",frame=FrameCounter, index=-1) 
            obj.keyframe_insert(data_path="hide_render",frame=FrameCounter, index=-1)
        if POSITION[1] == -300: # repeated so the two keyframes are constant between eachother
            if obj.animation_data and obj.animation_data.action:
                for fcurve in obj.animation_data.action.fcurves:
                    if fcurve.keyframe_points:
                        last_kf = fcurve.keyframe_points[-1]
                        last_kf.interpolation = 'CONSTANT'
        else:
            if obj.hide_render: # nested ew, but stops spamming the timeline with even more shit you dont need to
                obj.hide_render = False 
                obj.hide_viewport = False 
                obj.keyframe_insert(data_path="hide_viewport",frame=FrameCounter, index=-1) 
                obj.keyframe_insert(data_path="hide_render",frame=FrameCounter, index=-1)

        obj.keyframe_insert(data_path="location", frame=FrameCounter, index=-1)
        obj.keyframe_insert(data_path="rotation_quaternion", frame=FrameCounter, index=-1)

def ParseBasicPlayerUpdate(f:BufferedReader,FrameLength:int, FrameCounter:int,FrameType:int):
    for _ in range(int(FrameLength/85)):
        PLAYER_INDEX:int = f.read(1)[0]
        HEAD_POSITION:Tuple[float,float,float] = struct.unpack("<3f",f.read(12))
        w,y,x,z =struct.unpack("<4f",f.read(16))
        HEAD_ROTATION:Quaternion = Quaternion((w,x,y,z))
        
        HAND_LEFT_POSITION:Tuple[float,float,float] = struct.unpack("<3f",f.read(12))
        w,y,x,z =struct.unpack("<4f",f.read(16))
        HAND_LEFT_ROTATION:Quaternion = Quaternion((w,x,y,z))

        HAND_RIGHT_POSITION:Tuple[float,float,float] = struct.unpack("<3f",f.read(12))
        w,y,x,z =struct.unpack("<4f",f.read(16))
        HAND_RIGHT_ROTATION:Quaternion = Quaternion((w,x,y,z))

        

        OBJECT_NAME:str = f"Player.{str(PLAYER_INDEX).zfill(3)}" #should output something like Player.001

        try:
            obj:bpy.types.Object = bpy.data.objects[OBJECT_NAME]
            leftHand:bpy.types.Object = bpy.data.objects[f"Hand.{str(PLAYER_INDEX).zfill(3)}.L"]
            rightHand:bpy.types.Object = bpy.data.objects[f"Hand.{str(PLAYER_INDEX).zfill(3)}.R"]
        except:
            print(OBJECT_NAME,":PlayerNotFound@",f.tell())
            continue # should call if its an object we dont have an object for, isnt a big deal like 99% of time 


        obj.location = HEAD_POSITION
        obj.rotation_mode = "QUATERNION"
        obj.rotation_quaternion = HEAD_ROTATION

        leftHand.location = HAND_LEFT_POSITION
        leftHand.rotation_mode = "QUATERNION"
        leftHand.rotation_quaternion = HAND_LEFT_ROTATION

        rightHand.location = HAND_RIGHT_POSITION
        rightHand.rotation_mode = "QUATERNION"
        rightHand.rotation_quaternion = HAND_RIGHT_ROTATION

        obj.keyframe_insert(data_path="location", frame=FrameCounter, index=-1)
        obj.keyframe_insert(data_path="rotation_quaternion", frame=FrameCounter, index=-1)

        leftHand.keyframe_insert(data_path="location", frame=FrameCounter, index=-1)
        leftHand.keyframe_insert(data_path="rotation_quaternion", frame=FrameCounter, index=-1)

        rightHand.keyframe_insert(data_path="location", frame=FrameCounter, index=-1)
        rightHand.keyframe_insert(data_path="rotation_quaternion", frame=FrameCounter, index=-1)

def parseframes(f:BufferedReader,FrameLength:int, FrameCounter:int,FrameType:int) -> bool:
    match FrameType:
        case FrameTypeEnum.ObjectUpdate.value:
            ParseObjectUpdate(f,FrameLength, FrameCounter,FrameType)
        case FrameTypeEnum.BasicPlayerUpdate.value:
            ParseBasicPlayerUpdate(f,FrameLength, FrameCounter,FrameType)
        case _: # unsupported type
            f.read(FrameLength)
            print(FrameType, ":Unsupported FrameType@",f.tell())
            if not f.peek(5):
                print("EOF?@",f.tell()) 
                return False
            else: return True

    if not f.peek(5):
        print("EOF?@",f.tell()) 
        return False
    else: return True

    


bpy.types.Scene.SelectedFile = bpy.props.StringProperty(name="SelectedFile")
def register():
    bpy.utils.register_class(VIEW3D_PT_rumblereplay)
    bpy.utils.register_class(GenerateAnim)
    bpy.utils.register_class(SelectFile)
    bpy.utils.register_class(SetupScene)
    bpy.utils.register_class(OT_OpenFilebrowser)
def unregister():
    bpy.utils.unregister_class(VIEW3D_PT_rumblereplay)
    bpy.utils.unregister_class(GenerateAnim)
    bpy.utils.unregister_class(SelectFile)
    bpy.utils.unregister_class(SetupScene)
    bpy.utils.unregister_class(OT_OpenFilebrowser)
    del bpy.types.Scene.SelectedFile

if __name__ == "__main__":
    register()
