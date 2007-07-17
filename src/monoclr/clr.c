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

#include <mono/jit/jit.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <Python.h>

#define MONO_VERSION "v2.0.50727"
#define MONO_DOMAIN "Python.Runtime"
#define PR_ASSEMBLY "Python.Runtime.dll"

#warning ***************************
#warning  XXX: clr.so does NOT work
#warning ***************************

int error(char *msg);
static int InitializePythonNet(void);
static void main_thread_handler(gpointer user_data);

typedef struct {
    MonoDomain *domain;
    MonoAssembly *pr_assm;
    MonoMethod *initext;
    char *pr_file;
    char *error;
} PR_MainThreadArgs;

int error(char *msg) {
    // XXX set python exception
    printf(msg);
    return -1;
}

static int InitializePythonNet(void) {
    MonoDomain *domain;
    PR_MainThreadArgs main_args; 

    /*
     * Load the default Mono configuration file, this is needed
     * if you are planning on using the dllmaps defined on the
     * system configuration
     */
    mono_config_parse(NULL);

    domain = mono_jit_init_version(MONO_DOMAIN, MONO_VERSION);
    main_args.domain = domain;
    main_args.pr_file = PR_ASSEMBLY;
    main_args.error = NULL;

    mono_runtime_exec_managed_code (domain, main_thread_handler,
                                    &main_args);
    if (main_args.error) 
        return error(main_args.error);
    return 0;
} 

static void main_thread_handler (gpointer user_data) {
    PR_MainThreadArgs *main_args=(PR_MainThreadArgs *)user_data;
    MonoMethodDesc *initext_desc;
    MonoImage *pr_image;
    MonoClass *pythonengine;
    MonoObject *exception = NULL;

    main_args->pr_assm = mono_domain_assembly_open(main_args->domain, main_args->pr_file);
    if (!main_args->pr_assm) {
        main_args->error = "Unable to load assembly";
        return;
    }

    pr_image = mono_assembly_get_image(main_args->pr_assm);
    if (!pr_image) {
        main_args->error = "Unable to get image";
	return;
    }

    pythonengine = mono_class_from_name(pr_image, "Python.Runtime", "PythonEngine");
    if (!pythonengine) {
        main_args->error = "Unable to load class PythonEngine from Python.Runtime";
	return;
    }

    initext_desc = mono_method_desc_new("Python.Runtime:InitExt()", 1);

    main_args->initext = mono_method_desc_search_in_class(initext_desc, pythonengine);
    if (!main_args->initext) {
        main_args->error = "Unable to fetch InitExt() from PythonEngine";
	return;
    }

    mono_runtime_invoke(main_args->initext, NULL, NULL, &exception);

    if (exception) {
        main_args->error = "An exception was raised";
	return;
    }
}

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

