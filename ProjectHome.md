# XNA Raytracer #
Written in C# with XNA, but does not rely heavily on XNA features. The main reason for using XNA is the content pipeline.

Here are some videos rendered by the XNA Raytracer

(2011-10-17) http://www.youtube.com/watch?v=UPVU6lSzmS8

(2011-10-24) http://www.youtube.com/watch?v=iGsERNdZB6w

I occasionally upload screenshots to my Flickr account (http://www.flickr.com/photos/emilnorden/) when I have some progress to show.


### Features ###
  * Textured and non-textured surfaces.
  * Bilinear and point texture filtering.
  * Adaptive supersampling for high quality anti-aliasing.
  * Reflective surfaces.
  * Spotlights, pointlights and directional lights.
  * Shadows.
  * Incorporates the AviFile library for possibility to create AVI files. Using this C# wrapper: http://www.codeproject.com/KB/audio-video/avifilewrapper.aspx

### To be done ###
  * Major performance improvements:
    * Fix octree implementation.
    * Fix content processor to tag triangles as "part of convex geometry".
    * Write wrapper for Intels _Advanced Vector Extensions_ (AVX), which could potentially speed up calculations a lot.
  * Radiosity.
  * A different way to handle materials. Currently a model is restricted to one material.
  * Refraction