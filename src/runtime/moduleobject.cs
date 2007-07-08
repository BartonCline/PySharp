// ==========================================================================
// This software is subject to the provisions of the Zope Public License,
// Version 2.0 (ZPL).  A copy of the ZPL should accompany this distribution.
// THIS SOFTWARE IS PROVIDED "AS IS" AND ANY AND ALL EXPRESS OR IMPLIED
// WARRANTIES ARE DISCLAIMED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF TITLE, MERCHANTABILITY, AGAINST INFRINGEMENT, AND FITNESS
// FOR A PARTICULAR PURPOSE.
// ==========================================================================

using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

namespace Python.Runtime {

    //========================================================================
    // Implements a Python type that provides access to CLR namespaces. The 
    // type behaves like a Python module, and can contain other sub-modules.
    //========================================================================

    internal class ModuleObject : ExtensionType {

	Dictionary<string, ManagedType> cache;
	internal string moduleName;
	internal IntPtr dict;
	protected string _namespace;

	public ModuleObject(string name) : base() {
            if (name == String.Empty)
            {
                throw new ArgumentException("Name must not be empty!");
            }
	    moduleName = name;
	    cache = new Dictionary<string, ManagedType>();
	    _namespace = name;

	    dict = Runtime.PyDict_New();
	    IntPtr pyname = Runtime.PyString_FromString(moduleName);
	    Runtime.PyDict_SetItemString(dict, "__name__", pyname);
	    Runtime.PyDict_SetItemString(dict, "__file__", Runtime.PyNone);
	    Runtime.PyDict_SetItemString(dict, "__doc__", Runtime.PyNone);
	    Runtime.Decref(pyname);

	    Marshal.WriteIntPtr(this.pyHandle, ObjectOffset.ob_dict, dict);
	}


	//===================================================================
	// Returns a ClassBase object representing a type that appears in
	// this module's namespace or a ModuleObject representing a child 
	// namespace (or null if the name is not found). This method does
	// not increment the Python refcount of the returned object.
	//===================================================================

	public ManagedType GetAttribute(string name, bool guess) {
	    ManagedType cached = null;
	    this.cache.TryGetValue(name, out cached);
	    if (cached != null) {
		return cached;
	    }

	    string qname = (_namespace == String.Empty) ? name : 
		            _namespace + "." + name;

	    ModuleObject m;
	    ClassBase c;

	    // If the fully-qualified name of the requested attribute is 
	    // a namespace exported by a currently loaded assembly, return 
	    // a new ModuleObject representing that namespace.

	    if (AssemblyManager.IsValidNamespace(qname)) {
		m = new ModuleObject(qname);
		StoreAttribute(name, m);
		return (ManagedType) m;
	    }

	    // Look for a type in the current namespace. Note that this 
	    // includes types, delegates, enums, interfaces and structs.
	    // Only public namespace members are exposed to Python.

	    Type type = AssemblyManager.LookupType(qname);
	    if (type != null) {
		if (!type.IsPublic) {
		    return null;
		}
		c = ClassManager.GetClass(type);
		StoreAttribute(name, c);
		return (ManagedType) c;
	    }

	    // This is a little repetitive, but it ensures that the right
	    // thing happens with implicit assembly loading at a reasonable
	    // cost. Ask the AssemblyManager to do implicit loading for each 
	    // of the steps in the qualified name, then try it again.

	    if (AssemblyManager.LoadImplicit(qname)) {
		if (AssemblyManager.IsValidNamespace(qname)) {
		    m = new ModuleObject(qname);
		    StoreAttribute(name, m);
		    return (ManagedType) m;
		}

		type = AssemblyManager.LookupType(qname);
		if (type != null) {
		    if (!type.IsPublic) {
			return null;
		    }
		    c = ClassManager.GetClass(type);
		    StoreAttribute(name, c);
		    return (ManagedType) c;
		}
	    }

	    // We didn't find the name, so we may need to see if there is a 
	    // generic type with this base name. If so, we'll go ahead and
	    // return it. Note that we store the mapping of the unmangled
	    // name to generic type -  it is technically possible that some
	    // future assembly load could contribute a non-generic type to
	    // the current namespace with the given basename, but unlikely
	    // enough to complicate the implementation for now.

	    if (guess) {
		string gname = GenericUtil.GenericNameForBaseName(
					      _namespace, name);
		if (gname != null) {
		    ManagedType o = GetAttribute(gname, false);
		    if (o != null) {
			StoreAttribute(name, o);
			return o;
		    }
		}
	    }

	    return null;
	}


	//===================================================================
	// Stores an attribute in the instance dict for future lookups.
 	//===================================================================

	private void StoreAttribute(string name, ManagedType ob) {
	    Runtime.PyDict_SetItemString(dict, name, ob.pyHandle);
	    cache[name] = ob;
	}


	//===================================================================
	// Preloads all currently-known names for the module namespace. This
	// can be called multiple times, to add names from assemblies that
	// may have been loaded since the last call to the method.
 	//===================================================================

	public void LoadNames() {
	    ManagedType m = null;
	    foreach (string name in AssemblyManager.GetNames(_namespace)) {
		this.cache.TryGetValue(name, out m);
		if (m == null) {
		    ManagedType attr = this.GetAttribute(name, true);
		    if (Runtime.wrap_exceptions) {
			if (attr is ClassBase) {
			    ClassBase c = attr as ClassBase;
			    if (c.is_exception) {
			      IntPtr p = attr.pyHandle;
			      IntPtr r =Exceptions.GetExceptionClassWrapper(p);
			      Runtime.PyDict_SetItemString(dict, name, r);
			      Runtime.Incref(r);

			    }
			}
		    }
		}
	    }
	}


	//====================================================================
	// ModuleObject __getattribute__ implementation. Module attributes
	// are always either classes or sub-modules representing subordinate 
	// namespaces. CLR modules implement a lazy pattern - the sub-modules
	// and classes are created when accessed and cached for future use.
	//====================================================================

	public static IntPtr tp_getattro(IntPtr ob, IntPtr key) {
	    ModuleObject self = (ModuleObject)GetManagedObject(ob);

	    if (!Runtime.PyString_Check(key)) {
		Exceptions.SetError(Exceptions.TypeError, "string expected");
		return IntPtr.Zero;
	    }

	    IntPtr op = Runtime.PyDict_GetItem(self.dict, key);
	    if (op != IntPtr.Zero) {
		Runtime.Incref(op);
		return op;
	    }
 
	    string name = Runtime.GetManagedString(key);
	    if (name == "__dict__") {
		Runtime.Incref(self.dict);
		return self.dict;
	    }

	    ManagedType attr = self.GetAttribute(name, true);

	    if (attr == null) {
		Exceptions.SetError(Exceptions.AttributeError, name);
		return IntPtr.Zero;		
	    }

	    // XXX - hack required to recognize exception types. These types
	    // may need to be wrapped in old-style class wrappers in versions
	    // of Python where new-style classes cannot be used as exceptions.

	    if (Runtime.wrap_exceptions) {
		if (attr is ClassBase) {
		    ClassBase c = attr as ClassBase;
		    if (c.is_exception) {
			IntPtr p = attr.pyHandle;
			IntPtr r = Exceptions.GetExceptionClassWrapper(p);
			Runtime.PyDict_SetItemString(self.dict, name, r);
			Runtime.Incref(r);
			return r;
		    }
		}
	    }

	    Runtime.Incref(attr.pyHandle);
	    return attr.pyHandle;
	}

	//====================================================================
	// ModuleObject __repr__ implementation.
	//====================================================================

	public static IntPtr tp_repr(IntPtr ob) {
	    ModuleObject self = (ModuleObject)GetManagedObject(ob);
	    string s = String.Format("<module '{0}'>", self.moduleName);
	    return Runtime.PyString_FromString(s);
	}



    }

    /// <summary>
    /// The CLR module is the root handler used by the magic import hook
    /// to import assemblies. It has a fixed module name "clr" and doesn't
    /// provide a namespace.
    /// </summary>
    internal class CLRModule : ModuleObject
    {
        protected static bool hacked = false;
        protected static bool interactive_preload = true;
        public bool preload {
            get {
                IntPtr pybool = Runtime.PyDict_GetItemString(dict, "preload");
                if (pybool == IntPtr.Zero) {
                    Exceptions.Clear();
                    return false;
                }
                return (Runtime.PyObject_IsTrue(pybool) == 1);
            }
            set {
                IntPtr pybool;
                if (value) {
                    pybool = Runtime.PyTrue;
                } else {
                    pybool = Runtime.PyFalse;
                }
                IntPtr oldval = Runtime.PyDict_GetItemString(dict, "preload");
                Runtime.Decref(oldval);
                Runtime.Incref(pybool);
                Runtime.PyDict_SetItemString(dict, "preload", pybool);
            }

        }

        public CLRModule() : base("clr") {
            _namespace = String.Empty;
            preload = false;
            if (interactive_preload) {
                if (Runtime.PySys_GetObject("ps1") != IntPtr.Zero) {
                    preload = true;
                } else {
                    Exceptions.Clear();
                    preload = false;
                }
            }
            
            // This hackery is required in order to allow a plain Python to
            // import the managed runtime via the CLR bootstrapper module. 
            // The standard Python machinery in control at the time of the
            // import requires the module to pass PyModule_Check. :(
            if (!hacked)
            {
                IntPtr type = this.tpHandle;
                IntPtr mro = Marshal.ReadIntPtr(type, TypeOffset.tp_mro);
                IntPtr ext = Runtime.ExtendTuple(mro, Runtime.PyModuleType);
                Marshal.WriteIntPtr(type, TypeOffset.tp_mro, ext);
                Runtime.Decref(mro);
                hacked = true;
            }
        }

        //====================================================================
        // Type __setattr__ implementation.
        //====================================================================
        public static new int tp_setattro(IntPtr ob, IntPtr key, IntPtr val)
        {
            string name = Runtime.GetManagedString(key);
            if (name != "preload") {
                return ExtensionType.tp_setattro(ob, key, val);
            } else {
                IntPtr dict = Runtime.PyModule_GetDict(ob);
                Exceptions.ErrorCheck(dict);
                
                IntPtr oldval = Runtime.PyDict_GetItemString(dict, "preload");
                Runtime.Decref(oldval);

                IntPtr newval;
                if ((val != IntPtr.Zero) && (Runtime.PyObject_IsTrue(val) == 1)) {
                    newval = Runtime.PyTrue;
                } else {
                    newval = Runtime.PyFalse;
                }
                Runtime.PyDict_SetItemString(dict, "preload", newval);
                return 0;
            }
        }
    }

}
