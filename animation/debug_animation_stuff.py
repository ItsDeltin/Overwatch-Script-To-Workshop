def print_matrix(m):
    print('[' + get_matrix_row(m[0]))
    print(' ' + get_matrix_row(m[1]))
    print(' ' + get_matrix_row(m[2]) + ']')

def get_matrix_row(mr):
    return format(mr[0]) + ", " + format(mr[1]) + ", " + format(mr[2])

def print_quaternion(q):
    print('[' + format(q[0]) + ', ' + format(q[1]) + ', ' + format(q[2]) + ", " + format(q[3]) + ']')

def print_vector(q):
    print('[' + format(q[0]) + ', ' + format(q[1]) + ', ' + format(q[2]) + ']')
    
def format(v):
    return '{:f}'.format(v)