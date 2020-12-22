bl_info = {
    'name': 'OSTW animation watcher',
    'blender': (2, 80, 0),
    'category': 'todo'
}

import bpy
import time
import re
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class WorkshopLogHandler(FileSystemEventHandler):
    def on_modified(self, event):
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
            
            # Get the object that the animation affects.
            # TODO: Supply the object from the log.
            obj = bpy.context.scene.objects[0]
            tracks = obj.animation_data.nla_tracks
            
            # Mute gross tracks
            for track in tracks:
                track.mute = track.name == '[Action Stash]' or len(track.strips) == 0 or track.strips[0].action.name != animation_name
            
            bpy.ops.screen.animation_cancel() # Stop playing
            bpy.context.scene.frame_set(0)
            bpy.ops.screen.animation_play() # Start playing
        else:
            print('last line did not match')

# todo: make this not a constant
workshop_log_folder = 'C:/Users/Deltin/Documents/Overwatch/Workshop/'

def register():
    log_event_handler = WorkshopLogHandler()
    observer = Observer()
    observer.schedule(log_event_handler, path=workshop_log_folder, recursive=False)
    observer.start()

# This allows you to run the script directly from Blender's Text editor
# to test the add-on without having to install it.
if __name__ == "__main__":
    register()