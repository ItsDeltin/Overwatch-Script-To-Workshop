from mathutils import Matrix

coordinate_system_translation = Matrix([
    [-1, 0, 0],
    [0, 0, 1],
    [0, 1, 0]
])

class Vector:
    def __init__(self, original):
        self.x = original.x
        self.y = original.y
        try:
            self.z = original.z
        except:
            pass
        try:
            self.w = original.w
        except:
            pass

def get_matrix(original): return [original[0][0], original[0][1], original[0][2], original[1][0], original[1][1], original[1][2], original[2][0], original[2][1], original[2][2]]

def get_accumulative_local_matrix(bone):
    if (bone.parent == None):
        return coordinate_system_translation @ bone.matrix
    else:
        return bone.matrix