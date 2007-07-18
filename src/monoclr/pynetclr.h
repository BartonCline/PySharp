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

#ifndef PYNET_CLR_H
#define PYNET_CLR_H

#include <mono/jit/jit.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <Python.h>

#define MONO_VERSION "v2.0.50727"
#define MONO_DOMAIN "Python.Runtime"
#define PR_ASSEMBLY "Python.Runtime.dll"

int error(char *msg);
int InitializePythonNet(void);
void main_thread_handler(gpointer user_data);

typedef struct {
    MonoDomain *domain;
    MonoAssembly *pr_assm;
    MonoMethod *initext;
    char *pr_file;
    char *error;
} PR_MainThreadArgs;

#endif // PYNET_CLR_H

