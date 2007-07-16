#include <mono/jit/jit.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-config.h>
#include <Python.h>

#define MONO_VERSION "v2.0.50727"
#define MONO_DOMAIN "Python.Runtime"
#define PR_ASSEMBLY "Python.Runtime.dll"

void InitializePythonNet(void) {
    MonoDomain *domain;
    MonoAssembly *pyruntime;

    /*
     * Load the default Mono configuration file, this is needed
     * if you are planning on using the dllmaps defined on the
     * system configuration
     */
    mono_config_parse(NULL);

    domain = mono_jit_init_version(MONO_DOMAIN, MONO_VERSION);

    pyruntime = mono_domain_assembly_open(domain, PR_ASSEMBLY);
    if (!pyruntime) {
        // XXX
        printf("Unable to load assembly");
    }
    // XXX more

} 

/* List of functions defined in the module */
static PyMethodDef clr_methods[] = {
    {NULL, NULL, 0, NULL}        /* Sentinel */
};

PyDoc_STRVAR(clr_module_doc,
"clr fascade module to initialize the CLR. It's later "
"replaced by the real clr module");

PyMODINIT_FUNC
initclr(void)
{
        PyObject *m;

        /* Create the module and add the functions */
        m = Py_InitModule3("clr", clr_methods, clr_module_doc);
        if (m == NULL)
                return;
        PyModule_AddObject(m, "fascade", Py_True);
        Py_INCREF(Py_True);

        InitializePythonNet();
}

