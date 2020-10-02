class Vector:
    def __init__(self, original):
        self.x = original.x
        self.y = original.y
        self.z = original.z
        try:
            self.w = original.w
        except:
            pass