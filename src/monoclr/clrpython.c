// ==========================================================================
// This software is subject to the provisions of the Zope Public License,
// Version 2.1 (ZPL).  A copy of the ZPL should accompany this distribution.
// THIS SOFTWARE IS PROVIDED "AS IS" AND ANY AND ALL EXPRESS OR IMPLIED
// WARRANTIES ARE DISCLAIMED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF TITLE, MERCHANTABILITY, AGAINST INFRINGEMENT, AND FITNESS
// FOR A PARTICULAR PURPOSE.
// ==========================================================================
//
// Example how to integrate Python, PythonNet and Mono into a C application
// It provides a command prompt equal to PythonNet's console but using a
// different path.
//
// Author: Christian Heimes <christian(at)cheimes(dot)de>
//

#include "pynetclr.h"

int main(int argc, char *argv[]) {
    Py_Initialize();
    InitializePythonNet();
    int rc = Py_Main(argc, argv);
    Py_Finalize();
    return rc;
}

