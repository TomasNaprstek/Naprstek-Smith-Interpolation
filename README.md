# Naprstek Smith Interpolation Method ("Multi-trend gridding")
Last updated November 2020

This project holds the code for an interpolation method designed for use on aeromagnetic data. Its primary strength lies in its ability to strongly interpolate thin, linear features across flight lines, even when they are at acute angles (normally an issue for most other interpolation methods). It can be used as a general interpolation method as well, leading to results similar to minimum curvature.

This is the code associated with the paper, Tomas Naprstek and Richard S. Smith, (2019), "A new method for interpolating linear features in aeromagnetic data," GEOPHYSICS 84: JM15-JM24. https://library.seg.org/doi/10.1190/geo2018-0156.1

If you use this interpolation method for any publications, please cite the above paper, thanks! If you have any questions or comments, feel free to email me at tomnaprstek@gmail.com

%%%%%%%%
**STAND-ALONE USAGE**
%%%%%%%%

Download all files in the "StandAlone" folder. Edit the "MTG_config.txt" to define user variables (see "User Variables.txt"), and run NSI_V7.exe to execute. All source C# code is included.

A test file, BaseModel1-250mInterval-1nTNoise.txt is included in the GitHub repository. If the executable or C# code is run as is, it will run on the BaseModel file. This synthetic dataset was developed using PyGMI: Cole, P., 2015, PyGMI, https://github.com/Patrick-Cole/pygmi, accessed 20 October 2015. For comparison, the "AllValues" version contains all data points of the synthetic dataset, before the 250m flight line subsampling was applied.

%%%%%%%%
**GEOSOFT DLL USAGE**
%%%%%%%%

**NOTE: As of Oasis Montaj Version 9.9, Multi-trend gridding is now one of the standard interpolation methods within Oasis Montaj. If you have access to it, I recommend using the built-in version rather than this custom dll version (they are the same, but the built-in version is more integrated into Oasis Montaj).**

Also included in the GitHub repository is a dll and omn file for running the method as a custom menu in Oasis Montaj. Files tested and working with Geosoft's Oasis Montaj V9 (will not work on previous versions!). Follow the instructions given on Geosoft’s support site for importing custom menus, using the NaprstekSmithV18.dll and MTG.omn files. This will allow the creation of a new menu inside of Oasis Montaj. Once run, the plugin will output a .txt file that can be used in the “Import ASCII Grid…” option under the “Grid and Image”, “Utilities” menu. Information regarding the importing process is written at the top of the output file.
