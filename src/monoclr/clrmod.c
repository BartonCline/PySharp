// ==========================================================================
// This software is subject to the provisions of the Zope Public License,
// Version 2.1 (ZPL).  A copy of the ZPL should accompany this distribution.
// THIS SOFTWARE IS PROVIDED "AS IS" AND ANY AND ALL EXPRESS OR IMPLIED
// WARRANTIES ARE DISCLAIMED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF TITLE, MERCHANTABILITY, AGAINST INFRINGEMENT, AND FITNESS
// FOR A PARTICULAR PURPOSE.
// ==========================================================================
//
// Author: Christian Heimes <christian(at)cheimes(dot)de>

#include "pynetclr.h" 

#warning ***************************
#warning  XXX: clr.so does NOT work
#warning ***************************

/* List of functions defined in the module */
static PyMethodDef clr_methods[] = {
    {NULL, NULL, 0, NULL}        /* Sentinel */
};

PyDoc_STRVAR(clr_module_doc,
"clr facade module to initialize the CLR. It's later "
"replaced by the real clr module. This module has a facade "
"attribute to make it distinguishable from the real clr module."
);

PyMODINIT_FUNC
initclr(void)
{
        PyObject *m;

        /* Create the module and add the functions */
        m = Py_InitModule3("clr", clr_methods, clr_module_doc);
        if (m == NULL)
                return;
        PyModule_AddObject(m, "facade", Py_True);
        Py_INCREF(Py_True);

        if (InitializePythonNet() != 0) 
                return;
}

