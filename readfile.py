f = open('fft', 'r', encoding = 'ascii')
fp= open('X','w',encoding = 'ascii')
a = []
b = []

for i in range(0,608):
	c = f.readline().split("\t")
	a.append(float(c[0]))
	b.append(float(c[1]))
print (a)
print (b)
