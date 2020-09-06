PMedians.exe -file instance_param.txt -rng 1
PMedians.exe -dflt
PMedians.exe -val 3 10 500 5000 10 500 100 1000 1000 -rng 1 -draw ex1
PMedians.exe -val 3 10 500 5000 10 500 100 1000 1000 -rng 1 -draw ex1-same-seed
PMedians.exe -val 3 10 500 5000 10 500 100 1000 1000 -rng 2 -draw ex2
PMedians.exe -val 3 10 500 5000 10 500 100 1000 1000 -rng 3 -draw ex3
PMedians.exe -val 5 20 1000 5000 20 1000 100 1000 1000 -rng 1 -draw ex4 -runtime 100
PMedians.exe -val 7 30 5000 5000 30 5000 100 1000 1000 -rng 2 -draw ex5 -runtime 100