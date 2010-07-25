// ==========================================================================
// This software is subject to the provisions of the Zope Public License,
// Version 2.0 (ZPL).  A copy of the ZPL should accompany this distribution.
// THIS SOFTWARE IS PROVIDED "AS IS" AND ANY AND ALL EXPRESS OR IMPLIED
// WARRANTIES ARE DISCLAIMED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF TITLE, MERCHANTABILITY, AGAINST INFRINGEMENT, AND FITNESS
// FOR A PARTICULAR PURPOSE.
// ==========================================================================

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Python.Runtime {

    /// <summary>
    /// ManagedType instance factory:
    /// GetInstHandle(*) methods create a new instance of a python object which reflects
    /// a subclass of a Python.Runtime.ManagedType object and return the python handle
    /// to the instances.
    /// 
    /// Allocate a new instance of the python type and, if it's a subclass, inherit or
    /// allocate a new dictionary from/for the type.  Keep a reference to the managed Object.
    /// </summary>
    /// <param name="ob"> A managed Object to be reflected by the python type. </param>
    /// <param name="tp"> The python type handle. </param>
    /// <remarks>
    /// This class was provided by the author without any comments
    /// Utility routines return Instantiated python objects for a ManagedType, complete with
    /// the reflected managed part hidden in a magic type slot.
    /// They are generally called by tp_new
    /// but also by PyObject.FromManagedObject, Exceptions and Converter.
    /// 
    /// PyObject.FromManagedObject() comments:
    /// Given an arbitrary managed object, return a Python instance that
    /// reflects the managed object.
    /// </remarks>

    internal class CLRObject : ManagedType {

        internal Object inst;

        internal CLRObject(Object ob, IntPtr tp) : base() {

            IntPtr py = Runtime.PyType_GenericAlloc(tp, 0);

            int flags = (int)Marshal.ReadIntPtr(tp, TypeOffset.tp_flags);
            // TypeFlags.Subclass is a PythonNET specific flag...
            if ((flags & TypeFlags.Subclass) != 0) {
                IntPtr dict = Marshal.ReadIntPtr(py, ObjectOffset.ob_dict);
                if (dict == IntPtr.Zero) {
                    dict = Runtime.PyDict_New();
                    Marshal.WriteIntPtr(py, ObjectOffset.ob_dict, dict);
                }
            }

            // Hide the gchandle of the implementation in a magic type slot.
            GCHandle gc = GCHandle.Alloc(this);
            Marshal.WriteIntPtr(py, ObjectOffset.magic(), (IntPtr)gc);
            this.tpHandle = tp;
            this.pyHandle = py;
            this.gcHandle = gc;
            inst = ob;
        }

        /// <summary>
        /// CreatePyInstance(Object ob, IntPtr pyType)
        /// equivalent to: new CLRObject(Object ob, IntPtr pyType)
        /// </summary>
        /// <param name="ob"> an arbitrary instance of a subtype of ManagedType. </param>
        /// <param name="pyType"> PyObject* to the python type </param>
        /// <returns> basic subtype of ManagedType </returns>
        /// <remarks>
        /// Only called by utility routines.
        /// </remarks>
        internal static CLRObject CreatePyInstance(Object ob, IntPtr pyType) {
            return new CLRObject(ob, pyType);
        }

        /// <summary>
        /// CreatePyInstance(Object ob)
        /// Supplies the python type handle for CreatePyInstance(Object ob, IntPtr pyType)
        /// by calling ClassManager.GetClass(ob.GetType()) to get the ClassBase of ob.
        /// </summary>
        /// <param name="ob"> an arbitrary instance of a subtype of ManagedType. </param>
        /// <returns> basic subtype of ManagedType </returns>
        internal static CLRObject CreatePyInstance(Object ob) {
            ClassBase cc = ClassManager.GetClass(ob.GetType());
            return CreatePyInstance(ob, cc.tpHandle);
        }

        /// <summary>
        /// GetInstHandle(Object ob, IntPtr pyType)
        /// equivalent to: pyInst = new CLRObject(Object ob, IntPtr pyType).pyHandle
        /// </summary>
        /// <param name="ob"> an arbitrary instance of a subtype of ManagedType. </param>
        /// <param name="pyType"> PyObject* to the python type </param>
        /// <returns> ManagedType.pyHandle </returns>
        internal static IntPtr GetInstHandle(Object ob, IntPtr pyType) {
            CLRObject co = CreatePyInstance(ob, pyType);
            return co.pyHandle;
        }

        /// <summary>
        /// GetInstHandle(Object ob, Type type)
        /// Convert the CLI Type to a python type handle for CreatePyInstance(Object ob, IntPtr pyType)
        /// </summary>
        /// <param name="ob"> an arbitrary instance of a subtype of ManagedType. </param>
        /// <param name="type"> the CLI Type to convert </param>
        /// <returns> ManagedType.pyHandle </returns>
        internal static IntPtr GetInstHandle(Object ob, Type type) {
            ClassBase cc = ClassManager.GetClass(type);
            CLRObject co = CreatePyInstance(ob, cc.tpHandle);
            return co.pyHandle;
        }

        /// <summary>
        /// GetInstHandle(Object ob)
        /// Supplies the python type handle for CreatePyInstance(Object ob, IntPtr pyType)
        /// by calling CreatePyInstance(ob) to get a new ClassBase.
        /// </summary>
        /// <param name="ob"> an arbitrary instance of a subtype of ManagedType. </param>
        /// <returns> ManagedType.pyHandle </returns>
        internal static IntPtr GetInstHandle(Object ob) {
            CLRObject co = CreatePyInstance(ob);
            return co.pyHandle;
        }


    }


}

