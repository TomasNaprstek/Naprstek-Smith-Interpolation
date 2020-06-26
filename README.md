# Naprstek Smith 2019 Interpolation Method ("Multi-trend gridding")
Interpolation method developed by Tomas Naprstek and Richard Smith.

Last updated June 2020

This is the code associated with the paper, Tomas Naprstek and Richard S. Smith, (2019), "A new method for interpolating linear features in aeromagnetic data," GEOPHYSICS 84: JM15-JM24. https://library.seg.org/doi/10.1190/geo2018-0156.1

If you use this interpolation method for any work, please cite the above paper! Thanks!

%%%%%%%%
**USAGE**
%%%%%%%%

Run NSI_V6.exe to use the method, using the "MTG_config.txt" to define user variables. All source code is included. Comments in code and description of method in paper should suffice to be able to run, however if any other help is needed, or for further information, feel free to contact me.

A test file, BaseModel1-250mInterval-1nTNoise.txt is included in the GitHub repository. If the C# code is run as is, it will run on the BaseModel file. This synthetic dataset was developed using PyGMI: Cole, P., 2015, PyGMI, https://github.com/Patrick-Cole/pygmi, accessed 20 October 2015.

Also included in the GitHub repository is a dll and omn file for running the method as a custom menu in Oasis Montaj. Files tested and working with Geosoft's Oasis Montaj V9 (will not work on previous versions!). Follow the instructions given on Geosoft’s support site for importing custom menus, using the NaprstekSmithV17.dll and MTG.omn files. This will allow the creation of a new menu inside of Oasis Montaj. Once run, the plugin will output a .txt file that can be used in the “Import ASCII Grid…” option under the “Grid and Image”, “Utilities” menu. Information regarding the importing process is written at the top of the output file.
