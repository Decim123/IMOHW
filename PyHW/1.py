import sys

input = sys.stdin.readline

n_line = input().strip()
n = int(n_line)
parent = {}

for _ in range(n - 1):
    child, par = input().split()
    parent[child] = par

def is_ancestor(a, b):
    cur = b
    while cur in parent:
        cur = parent[cur]
        if cur == a:
            return True
    return False

out = []
for line in sys.stdin:
    line = line.strip()
    if not line:
        continue
    x, y = line.split()
    if is_ancestor(x, y):
        out.append("1")
    elif is_ancestor(y, x):
        out.append("2")
    else:
        out.append("0")

sys.stdout.write(" ".join(out) + (" " if out else ""))
