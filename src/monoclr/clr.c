#include <mono/jit/jit.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <Python.h>

#define MONO_VERSION "v2.0.50727"
#define MONO_DOMAIN "Python.Runtime"
#define PR_ASSEMBLY "Python.Runtime.dll"

int error(char *msg) {
    // set python exception
    printf(msg);
    return -1;
}

int InitializePythonNet(void) {
    MonoDomain *domain;
    MonoAssembly *pyruntime;
    MonoMethod *initext;
    MonoMethodDesc *initext_desc;
    MonoImage *pr_image;
    MonoClass *pythonengine;
    MonoObject *exception = NULL;

    /*
     * Load the default Mono configuration file, this is needed
     * if you are planning on using the dllmaps defined on the
     * system configuration
     */
    mono_config_parse(NULL);

    domain = mono_jit_init_version(MONO_DOMAIN, MONO_VERSION);

    pyruntime = mono_domain_assembly_open(domain, PR_ASSEMBLY);
    if (!pyruntime)
        return error("Unable to load assembly");
 
    pr_image = mono_assembly_get_image(pyruntime);
    if (!pr_image) 
        return error("Unable to get image");

    pythonengine = mono_class_from_name(pr_image, "Python.Runtime", "PythonEngine");
    if (!pythonengine)
        return error("Unable to load class PythonEngine from Python.Runtime");

    initext_desc = mono_method_desc_new("Python.Runtime:InitExt()", 1);
 
    initext = mono_method_desc_search_in_class(initext_desc, pythonengine);
    if (!initext)
        return error("Unable to fetch InitExt() from PythonEngine");
    mono_runtime_invoke(initext, NULL, NULL, &exception); 

    if (exception)
        return error("An exception was raised");
    
    // XXX more
    return 0;
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

