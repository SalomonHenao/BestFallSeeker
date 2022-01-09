This repository contains a solution to the following challenge.
It was developed using .Net Core 3.1.

<img src="https://matildastorage.blob.core.windows.net/matildaservices/bestFallSeekerLoading.png" height="auto" width="300px">
<img src="https://matildastorage.blob.core.windows.net/matildaservices/bestFallSeekerResult.png" height="300px" width="auto">

Let’s say you hopped on a flight to the Kitzbühel ski resort in Austria. Being a software engineer you 
can’t help but value efficiency, so naturally you want to ski as long as possible and as fast as possible 
without having to ride back up on the ski lift. So you take a look at the map of the mountain and try 
to find the longest ski run down.

Each number of the data matriz represents the elevation of that area of the mountain.
From each area (i.e. box) in the grid you can go north, south, east, west - but only if 
the elevation of the area you are going into is less than the one you are in. I.e. you can only ski 
downhill. You can start anywhere on the map and you are looking for a starting point with the 
longest possible path down as measured by the number of boxes you visit. And if there are several 
paths down of the same length, you want to take the one with the steepest vertical drop, i.e. the 
largest difference between your starting elevation and your ending elevation.

Your challenge is to write a program to find the longest (and then steepest) path on the specified 
map in the format above. It’s 1000x1000 in size, and all the numbers on it are between 0 and 1500.
