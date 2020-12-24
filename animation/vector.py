from mathutils import Matrix

coordinate_system_translation = Matrix([
    [-1, 0, 0],
    [0, 0, 1],
    [0, 1, 0]
])

class Vector:
    def __init__(self, original, adjustWorld):
        if not adjustWorld:
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
        else:
            self.x = -original.x
            try:
                self.z = original.y
                self.y = original.z
            except:
                self.y = original.y

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