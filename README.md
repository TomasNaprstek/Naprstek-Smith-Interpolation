# Naprstek-Smith Interpolation Method ("Multi-trend interpolation")
Interpolation method developed by Tomas Naprstek and Richard Smith.

Last updated February 2019

Associated source code for the paper (currently pre-print): Tomas Naprstek and Richard S. Smith A new method for interpolating linear features in aeromagnetic data. GEOPHYSICS. https://library.seg.org/doi/10.1190/geo2018-0156.1

Run NSI_V5.cs in a C# compiler (Visual Studio recommended) to use the method as is. All source code is in this file. Comments in code and description of method in paper should suffice to be able to run, however if any other help is needed, or for further information contact: tnaprstek@laurentian.ca

A test file, BaseModel1-250mInterval-1nTNoise.txt is included in the GitHub repository. If the C# code is run as is, it will run on the BaseModel file. This synthetic dataset was developed using PyGMI: Cole, P., 2015, PyGMI, https://github.com/Patrick-Cole/pygmi, accessed 20 October 2015.

Also included in the GitHub repository is a dll and omn file for running the method as a custom menu in Oasis Montaj. Files tested and working with Geosoft's Oasis Montaj V9. Follow the instructions given on Geosoft’s support site for importing custom menus, using the NaprstekSmithV15.dll and NSI.omn files. This will allow the creation of a new menu inside of Oasis Montaj. Once run, the plugin will output a .txt file that can be used in the “Import ASCII Grid…” option under the “Grid and Image”, “Utilities” menu. Information regarding the importing process is written at the top of the output file.
