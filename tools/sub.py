import subprocess

proc = subprocess.Popen(["./lomb", "A.txt"], stdout = subprocess.PIPE, stderr = subprocess.STDOUT)
for line in proc.stdout:
    print(line)

proc.wait()
