Visual Studio 2005
==================

pythonnet contains a new solution file for Visual Studio 2005: pythonnet.sln
It should make development under Windows much easier since you don't have to
install MSys or Cygwin to run the makefile.

The solution file should work with the free VS .NET Express Edition.

Available configurations
------------------------

Every configuration copies the dll, pdf and exe files to the root directory
of the project.

 * Release
   Builds Python.Runtime, Python.Tests, clr.pyd and python.exe. The console
   project starts a Python console
   
 * Debug
   Builds the same files as Release bug the console project runs the unit
   test suite inside a console.
   
 * EmbeddingTest
   Builds Python.EmbeddingTests and its dependencies. The configuration
  requires the NUunit framework.


Thanks to Virgil Duprasfor his original VS howto!

Christian 'Tiran' Heimes
