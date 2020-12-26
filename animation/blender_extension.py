bl_info = {
    'name': 'OSTW animation watcher',
    'blender': (2, 80, 0),
    'category': 'todo'
}

import bpy
import time
import re
import glob, os
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

playing = None

class WorkshopLogHandler(FileSystemEventHandler):
    def on_modified(self, event):
        global playing
        print('Workshop log file \'' + event.src_path + '\' updated!')
        
        # Get the last line in the
        last_line = None 
        with open(event.src_path) as f:
            for line in f:
                if line.strip():
                    last_line = line.strip()
        
        if not last_line:
            print('empty data')
            return
        
        print('Last line: \'' + last_line + '\'')
        match = re.search(r'\[\d+:\d+:\d+\]\s*Run_Animation:(.+)', last_line)
        
        if match:
            # Get the name of the animation being played.
            animation_name = match.group(1).strip()
            
            print('Animation name: \'' + animation_name + '\'')
            playing = animation_name
        else:
            print('last line did not match')

# todo: make this not a constant
workshop_log_folder = 'C:/Users/Deltin/Documents/Overwatch/Workshop/'
stop_loop = False

def check():
    global playing
    global stop_loop
    global workshop_log_folder
    
    for file in os.listdir(workshop_log_folder):
        try:
            f = open(os.path.join(workshop_log_folder, file), "r")
            f.close()
        except Exception as e:
            print(e)
            pass
    
    if playing == None:
        return 0.1
    
    print('playing = ' + playing)
    
    # Get the object that the animation affects.
    # TODO: Supply the object from the log.
    obj = bpy.context.scene.objects['Armature']
    tracks = obj.animation_data.nla_tracks
    
    # Mute gross tracks
    for track in tracks:
        # bpy.context.scene.objects['Armature'].animation_data.nla_tracks[0].is_solo = True
        if track.strips[0].action.name == playing:
            track.is_solo = True
            bpy.context.scene.frame_end = track.strips[0].frame_end
            break
        # track.mute = track.name == '[Action Stash]' or len(track.strips) == 0 or track.strips[0].action.name != playing
    
    # Play 
    bpy.ops.screen.animation_cancel() # Stop playing
    bpy.context.scene.frame_set(0)
    bpy.ops.screen.animation_play() # Start playing
    
    stop_loop = True
    playing = None
    return 0.1

def stop_playback(scene):
    global stop_loop
    if stop_loop and scene.frame_current == scene.frame_end:
        stop_loop = False
        bpy.ops.screen.animation_cancel(restore_frame=False)

def register():
    log_event_handler = WorkshopLogHandler()
    observer = Observer()
    observer.schedule(log_event_handler, path=workshop_log_folder, recursive=False)
    observer.start()
    
    print('checking')
    
    bpy.app.timers.register(check)
    bpy.app.handlers.frame_change_pre.append(stop_playback)

# This allows you to run the script directly from Blender's Text editor
# to test the add-on without having to install it.
if __name__ == "__main__":
    print('STARTED OSTW');
    register()