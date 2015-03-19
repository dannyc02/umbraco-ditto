﻿namespace Our.Umbraco.Ditto
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// The argument handler for handling arguments in methods.
    /// </summary>
    internal class ArgumentHandler
    {
        /// <summary>
        /// Pushes the array of arguments.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="il">The <see cref="ILGenerator"/>.</param>
        /// <param name="isStatic">Whether the property or method is static.</param>
        public void PushArguments(ParameterInfo[] parameters, ILGenerator il, bool isStatic)
        {
            int parameterCount = parameters == null ? 0 : parameters.Length;

            // object[] args = new object[size];
            il.Emit(OpCodes.Ldc_I4, parameterCount);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc_S, 0);

            if (parameterCount == 0)
            {
                il.Emit(OpCodes.Ldloc_S, 0);
                return;
            }

            // Populate the object array with the list of arguments
            int index = 0;
            int argumentPosition = 1;
            if (parameters != null)
            {
                foreach (ParameterInfo param in parameters)
                {
                    Type parameterType = param.ParameterType.IsByRef
                        ? param.ParameterType.GetElementType()
                        : param.ParameterType;

                    // args[N] = argumentN (pseudocode)
                    il.Emit(OpCodes.Ldloc_S, 0);
                    il.Emit(OpCodes.Ldc_I4, index);

                    // Zero out the [out] parameters
                    if (param.IsOut)
                    {
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Stelem_Ref);
                        argumentPosition++;
                        index++;
                        continue;
                    }

                    il.Emit(OpCodes.Ldarg, argumentPosition);

                    bool isGeneric = parameterType.IsGenericParameter;

                    if (param.ParameterType.IsByRef)
                    {
                        string typeName = param.ParameterType.Name;
                        OpCode referenceInstruction = OpCodes.Ldind_Ref;
                        Dictionary<string, OpCode> map = new Dictionary<string, OpCode>();

                        map["Bool&"] = OpCodes.Ldind_I1;
                        map["Int8&"] = OpCodes.Ldind_I1;
                        map["Uint8&"] = OpCodes.Ldind_I1;

                        map["Int16&"] = OpCodes.Ldind_I2;
                        map["Uint16&"] = OpCodes.Ldind_I2;

                        map["Uint32&"] = OpCodes.Ldind_I4;
                        map["Int32&"] = OpCodes.Ldind_I4;

                        map["IntPtr"] = OpCodes.Ldind_I4;
                        map["Uint64&"] = OpCodes.Ldind_I8;
                        map["Int64&"] = OpCodes.Ldind_I8;
                        map["Float32&"] = OpCodes.Ldind_R4;
                        map["Float64&"] = OpCodes.Ldind_R8;

                        if (map.ContainsKey(typeName))
                        {
                            referenceInstruction = map[typeName];
                        }

                        il.Emit(referenceInstruction);
                    }

                    if (parameterType.IsValueType || parameterType.IsByRef || isGeneric)
                    {
                        il.Emit(OpCodes.Box, parameterType);
                    }

                    il.Emit(OpCodes.Stelem_Ref);

                    index++;
                    argumentPosition++;
                }
            }

            il.Emit(OpCodes.Ldloc_S, 0);
        }
    }
}
