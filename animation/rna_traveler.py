from vector import Vector

class Rna_traveler:
    def __init__(self, value):
        self.value = value
    
    # If the current value starts with the specified string, it will be consumed then True will be returned.
    def current_is(self, path: str, skip = 0):
        if self.startswith(path):
            self.consume(len(path) + skip)
            return True
        return False
    
    # Gets the current array value.
    def array_value(self):
        if not self.startswith('['):
            return None
        self.consume()

        # string
        if self.current_is('"'):
            value = ''
            while not self.startswith('"'):
                value += self.consume()
            self.consume(2) # " and ]
            self.current_is('.')
            return value
        # Unknown array type
        raise ValueError('Unknown array type in RNA value.')
    
    # Consumes a certain number of characters in the current value.
    # The consumed characters will be returned.
    def consume(self, count = 1):
        consumed = self.value[:count]
        self.value = self.value[count:]
        return consumed
        
    # Returns true if the current value contains the specified string.
    def startswith(self, value):
        return self.value.startswith(value)
    
    # Determines if the end of the current value was reached.
    # This is done by determining if the current value is empty.
    def end_reached(self):
        return len(self.value) == 0
    
    def scan(self):
        # Pose
        if self.current_is('pose', 1):
            # Bone
            if self.current_is('bones'):
                # Get array value.
                bone_name = self.array_value()
                # Get the element.
                if self.current_is('rotation_quaternion'):
                    return 1, bone_name
                
                elif self.current_is('location'):
                    return 2, bone_name
        
        # Location
        if self.current_is('location'):
            return 0, None
        
        return -1, None