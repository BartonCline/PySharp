

using System;
using System.Collections;
using System.Reflection;

namespace Python.Runtime
{

    //========================================================================
    // Implements a Python type that represents a CLR method. Constructor objects
    // support a .__overloads__[] to allow explicit ctor overload selection.
    //========================================================================
    // TODO: ForbidPythonThreadsAttribute per method info
    /// <summary>
    /// Have ClassManager store an instance in the class's __dict__['__overloads__']
    /// </summary>
    internal class Constructors : ExtensionType
    {
        //MethodBinding unbound;
        internal string name;
        IntPtr pyTypeHndl;
        ConstructorBinder ctorBinder;

        public Constructors(string name, IntPtr pyTypeHndl, ConstructorBinder ctorBinder) : base() {
            this.name = name;
            this.pyTypeHndl = pyTypeHndl;
            this.ctorBinder = ctorBinder;

        }

        /// <summary>
        /// Descriptor __get__ implementation.
        /// </summary>
        /// <param name="self"> PyObject* to a reflected MethodObject </param>
        /// <param name="instance"> the instance that the attribute was accessed through,
        /// or None when the attribute is accessed through the owner </param>
        /// <param name="owner"> always the owner class </param>
        /// <returns> a MethodBinding created one of two ways </returns>
        /// 
        /// <remarks>
        /// Python 2.6.5 docs:
        /// object.__get__(self, instance, owner)
        /// Called to get the attribute of the owner class (class attribute access)
        /// or of an instance of that class (instance attribute access). 
        /// owner is always the owner class, while instance is the instance that
        /// the attribute was accessed through, or None when the attribute is accessed through the owner.
        /// This method should return the (computed) attribute value or raise an AttributeError exception.
        /// </remarks>
        public static IntPtr tp_descr_get(IntPtr self, IntPtr instance, IntPtr owner)
        {
            Constructors _self = (Constructors)GetManagedObject(self);
            CtorMapper mapper;
            if (_self == null) {
                return IntPtr.Zero;
                }

            // Will be accessed through its type (rather than via an instance).
            // We return a .

            /*if (instance == IntPtr.Zero)
            {
                if (_self.unbound == null)
                {
                    _self.unbound = new MethodBinding(_self, IntPtr.Zero);
                }
                binding = _self.unbound;
                Runtime.Incref(binding.pyHandle);
                return binding.pyHandle;
            }

            if (Runtime.PyObject_IsInstance(instance, owner) < 1)
            {
                return Exceptions.RaiseTypeError("invalid argument");
            }*/

            mapper = new CtorMapper(_self.pyTypeHndl, _self.ctorBinder);
            //Runtime.Incref(mapper.pyHandle);
            return mapper.pyHandle;
        }

        //====================================================================
        // ConstructorObject dealloc implementation.
        //====================================================================

        public static new void tp_dealloc(IntPtr ob)
        {
            Constructors self = (Constructors)GetManagedObject(ob);
            ExtensionType.FinalizeObject(self);
        }
    }

    //========================================================================
    // Implements the __overloads__ attribute of class objects. This object
    // supports the [] syntax to explicitly select an overload by signature.
    //========================================================================

    internal class CtorMapper : ExtensionType
    {
        IntPtr pyTypeHndl;
        ConstructorBinder ctorBinder;
        MethodBase ctorInfo;

        public CtorMapper(IntPtr pyTypeHndl, ConstructorBinder ctorBinder) : base()
        {
            this.pyTypeHndl = pyTypeHndl;   // Type handle created by TypeManager
            this.ctorBinder = ctorBinder;
            ctorInfo = null;
        }

        //====================================================================
        // Given a sequence of ConstructorInfo and a sequence of types, return the 
        // ConstructorInfo that matches the signature represented by those types.
        // XXX I'd like to convert MethodBinder.MatchSignature() to use the
        // base class instead of MethodInfo/MethodInfo[]
        // or Type.GetContructor(Type[])
        //====================================================================

         internal static MethodBase MatchSignature(MethodBase[] mi, Type[] tp) {
             int count = tp.Length;
             for (int i = 0; i < mi.Length; i++) {
                 ParameterInfo[] pi = mi[i].GetParameters();
                 if (pi.Length != count) {
                     continue;
                 }
                 int n;
                 for (n = 0; n < pi.Length; n++) {
                     if (tp[n]!= pi[n].ParameterType) {
                         break;
                     }
                 }
                 if (n == (count)) {
                    return mi[i];
                }
             }
             return null;
         }

        //====================================================================
        // Implement explicit overload selection using subscript syntax ([]).
        //====================================================================

        public static IntPtr mp_subscript(IntPtr tp, IntPtr idx)
        {
            CtorMapper self = (CtorMapper)GetManagedObject(tp);

            Type[] types = Runtime.PythonArgsToTypeArray(idx);
            if (types == null) {
                return Exceptions.RaiseTypeError("type(s) expected");
            }

            MethodBase ci = MatchSignature(self.ctorBinder.GetMethods(), types);
            if (ci == null) {
                string msg = "No match found for signature";
                return Exceptions.RaiseTypeError(msg);
            }
            self.ctorInfo = ci;

            /*MethodBinding mb = new MethodBinding(self.methObj, self.target);*/
            Runtime.Incref(self.pyHandle);
            return self.pyHandle;
        }


        //====================================================================
        // Constructors  __call__ implementation.
        //====================================================================

        public static IntPtr tp_call(IntPtr ob, IntPtr args, IntPtr kw)
        {
            CtorMapper self = (CtorMapper)GetManagedObject(ob);
            // Although a call with null ctorInfo 
            if (self.ctorInfo == null) {
                string msg = "Use subscript notation: Class.__overloads__[CLR_Type_list]";
                return Exceptions.RaiseTypeError(msg);
            }
            // Bind using MethodBinder.Bind and invoke the ctor providing a null instancePtr
            // which will fire self.ctorInfo using ConstructorInfo.Invoke().
            Object obj = self.ctorBinder.InvokeRaw(IntPtr.Zero, args, kw, self.ctorInfo);
            if (obj == null) {
                return IntPtr.Zero;
            }
            // Instantiate the python object that reflects the result of the method call
            // and return the PyObject* to it.
            return CLRObject.GetInstHandle(obj, self.pyTypeHndl);
        }

        //====================================================================
        // OverloadMapper dealloc implementation.
        //====================================================================

        public static new void tp_dealloc(IntPtr ob)
        {
            CtorMapper self = (CtorMapper)GetManagedObject(ob);
            ExtensionType.FinalizeObject(self);
        }

    }
}
