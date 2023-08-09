# Land

Procedural land generation.

## LOD

The basic assumption here and also easier to work with will be a MxM quadratic map. Therefore, hieght will be equal to width.  
  
So Let's for example say that our map width is 9, so `w=9`. There are now 4 ways to create vertices:

1. By visiting every point in our map. So a step of `i-1`, which would itterate through all points `w[0] - w[8]`.
2. By visiting every *second* point. So a step of `i=2`, which will itterate through 5 points `w[0], w[2], w[4], w[6], w[8]`. This will yield a less detailed mesh.
3. By visiting every *forth* point. So a step of `i=4`, which will itterate through 3 points `w[0], w[4], w[8]`. This will yield an even less detailed mesh.
4. By visiting every *eights* point. So a step of `i=8`, which will itterate through 2 points `w[0], w[8]`. This will yield the least detailed mesh.

Trying a step incrementation of `i=3` would not work since we would still have points past our last iterrable points. So, `w[0], w[3], w[6]` would be possible but there would remain points `w[7] and w[8]` and this would not work. Because of this, we say that `i` must be a factor of `(w-1)`.
  
So the general formula for calculating the total number of vertices will be:
  
`v = (w - 1) / i + 1`
  
The Unity limit for the number of vertices per mesh is `v <= 255^2` or `65025` vertices. Because of this, the mesh width in a square mesh should be `w <= 255`. So a good width for a mesh is `w = 241`, since we said that  `i` must be a factor of `(w-1)`. This would give a value which will be divisible by 6 numbers. I.e. `i` can be `2, 4, 6, 8, 10 or 12`. We will from now on call `w` the **map chunk size**.