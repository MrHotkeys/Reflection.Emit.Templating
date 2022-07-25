namespace MrHotkeys.Reflection.Emit.Templating
{
    internal enum OpCodeName : short
    {
        /// <summary>
        /// nop
        /// </summary>
        Nop = 0x00,

        /// <summary>
        /// break
        /// </summary>
        Break = 0x01,

        /// <summary>
        /// ldarg.0
        /// </summary>
        Ldarg_0 = 0x02,

        /// <summary>
        /// ldarg.1
        /// </summary>
        Ldarg_1 = 0x03,

        /// <summary>
        /// ldarg.2
        /// </summary>
        Ldarg_2 = 0x04,

        /// <summary>
        /// ldarg.3
        /// </summary>
        Ldarg_3 = 0x05,

        /// <summary>
        /// ldloc.0
        /// </summary>
        Ldloc_0 = 0x06,

        /// <summary>
        /// ldloc.1
        /// </summary>
        Ldloc_1 = 0x07,

        /// <summary>
        /// ldloc.2
        /// </summary>
        Ldloc_2 = 0x08,

        /// <summary>
        /// ldloc.3
        /// </summary>
        Ldloc_3 = 0x09,

        /// <summary>
        /// stloc.0
        /// </summary>
        Stloc_0 = 0x0A,

        /// <summary>
        /// stloc.1
        /// </summary>
        Stloc_1 = 0x0B,

        /// <summary>
        /// stloc.2
        /// </summary>
        Stloc_2 = 0x0C,

        /// <summary>
        /// stloc.3
        /// </summary>
        Stloc_3 = 0x0D,

        /// <summary>
        /// ldarg.s
        /// </summary>
        Ldarg_S = 0x0E,

        /// <summary>
        /// ldarga.s
        /// </summary>
        Ldarga_S = 0x0F,

        /// <summary>
        /// starg.s
        /// </summary>
        Starg_S = 0x10,

        /// <summary>
        /// ldloc.s
        /// </summary>
        Ldloc_S = 0x11,

        /// <summary>
        /// ldloca.s
        /// </summary>
        Ldloca_S = 0x12,

        /// <summary>
        /// stloc.s
        /// </summary>
        Stloc_S = 0x13,

        /// <summary>
        /// ldnull
        /// </summary>
        Ldnull = 0x14,

        /// <summary>
        /// ldc.i4.m1
        /// </summary>
        Ldc_I4_M1 = 0x15,

        /// <summary>
        /// ldc.i4.0
        /// </summary>
        Ldc_I4_0 = 0x16,

        /// <summary>
        /// ldc.i4.1
        /// </summary>
        Ldc_I4_1 = 0x17,

        /// <summary>
        /// ldc.i4.2
        /// </summary>
        Ldc_I4_2 = 0x18,

        /// <summary>
        /// ldc.i4.3
        /// </summary>
        Ldc_I4_3 = 0x19,

        /// <summary>
        /// ldc.i4.4
        /// </summary>
        Ldc_I4_4 = 0x1A,

        /// <summary>
        /// ldc.i4.5
        /// </summary>
        Ldc_I4_5 = 0x1B,

        /// <summary>
        /// ldc.i4.6
        /// </summary>
        Ldc_I4_6 = 0x1C,

        /// <summary>
        /// ldc.i4.7
        /// </summary>
        Ldc_I4_7 = 0x1D,

        /// <summary>
        /// ldc.i4.8
        /// </summary>
        Ldc_I4_8 = 0x1E,

        /// <summary>
        /// ldc.i4.s
        /// </summary>
        Ldc_I4_S = 0x1F,

        /// <summary>
        /// ldc.i4
        /// </summary>
        Ldc_I4 = 0x20,

        /// <summary>
        /// ldc.i8
        /// </summary>
        Ldc_I8 = 0x21,

        /// <summary>
        /// ldc.r4
        /// </summary>
        Ldc_R4 = 0x22,

        /// <summary>
        /// ldc.r8
        /// </summary>
        Ldc_R8 = 0x23,

        /// <summary>
        /// dup
        /// </summary>
        Dup = 0x25,

        /// <summary>
        /// pop
        /// </summary>
        Pop = 0x26,

        /// <summary>
        /// jmp
        /// </summary>
        Jmp = 0x27,

        /// <summary>
        /// call
        /// </summary>
        Call = 0x28,

        /// <summary>
        /// calli
        /// </summary>
        Calli = 0x29,

        /// <summary>
        /// ret
        /// </summary>
        Ret = 0x2A,

        /// <summary>
        /// br.s
        /// </summary>
        Br_S = 0x2B,

        /// <summary>
        /// brfalse.s
        /// </summary>
        Brfalse_S = 0x2C,

        /// <summary>
        /// brtrue.s
        /// </summary>
        Brtrue_S = 0x2D,

        /// <summary>
        /// beq.s
        /// </summary>
        Beq_S = 0x2E,

        /// <summary>
        /// bge.s
        /// </summary>
        Bge_S = 0x2F,

        /// <summary>
        /// bgt.s
        /// </summary>
        Bgt_S = 0x30,

        /// <summary>
        /// ble.s
        /// </summary>
        Ble_S = 0x31,

        /// <summary>
        /// blt.s
        /// </summary>
        Blt_S = 0x32,

        /// <summary>
        /// bne.un.s
        /// </summary>
        Bne_Un_S = 0x33,

        /// <summary>
        /// bge.un.s
        /// </summary>
        Bge_Un_S = 0x34,

        /// <summary>
        /// bgt.un.s
        /// </summary>
        Bgt_Un_S = 0x35,

        /// <summary>
        /// ble.un.s
        /// </summary>
        Ble_Un_S = 0x36,

        /// <summary>
        /// blt.un.s
        /// </summary>
        Blt_Un_S = 0x37,

        /// <summary>
        /// br
        /// </summary>
        Br = 0x38,

        /// <summary>
        /// brfalse
        /// </summary>
        Brfalse = 0x39,

        /// <summary>
        /// brtrue
        /// </summary>
        Brtrue = 0x3A,

        /// <summary>
        /// beq
        /// </summary>
        Beq = 0x3B,

        /// <summary>
        /// bge
        /// </summary>
        Bge = 0x3C,

        /// <summary>
        /// bgt
        /// </summary>
        Bgt = 0x3D,

        /// <summary>
        /// ble
        /// </summary>
        Ble = 0x3E,

        /// <summary>
        /// blt
        /// </summary>
        Blt = 0x3F,

        /// <summary>
        /// bne.un
        /// </summary>
        Bne_Un = 0x40,

        /// <summary>
        /// bge.un
        /// </summary>
        Bge_Un = 0x41,

        /// <summary>
        /// bgt.un
        /// </summary>
        Bgt_Un = 0x42,

        /// <summary>
        /// ble.un
        /// </summary>
        Ble_Un = 0x43,

        /// <summary>
        /// blt.un
        /// </summary>
        Blt_Un = 0x44,

        /// <summary>
        /// switch
        /// </summary>
        Switch = 0x45,

        /// <summary>
        /// ldind.i1
        /// </summary>
        Ldind_I1 = 0x46,

        /// <summary>
        /// ldind.u1
        /// </summary>
        Ldind_U1 = 0x47,

        /// <summary>
        /// ldind.i2
        /// </summary>
        Ldind_I2 = 0x48,

        /// <summary>
        /// ldind.u2
        /// </summary>
        Ldind_U2 = 0x49,

        /// <summary>
        /// ldind.i4
        /// </summary>
        Ldind_I4 = 0x4A,

        /// <summary>
        /// ldind.u4
        /// </summary>
        Ldind_U4 = 0x4B,

        /// <summary>
        /// ldind.i8
        /// </summary>
        Ldind_I8 = 0x4C,

        /// <summary>
        /// ldind.i
        /// </summary>
        Ldind_I = 0x4D,

        /// <summary>
        /// ldind.r4
        /// </summary>
        Ldind_R4 = 0x4E,

        /// <summary>
        /// ldind.r8
        /// </summary>
        Ldind_R8 = 0x4F,

        /// <summary>
        /// ldind.ref
        /// </summary>
        Ldind_Ref = 0x50,

        /// <summary>
        /// stind.ref
        /// </summary>
        Stind_Ref = 0x51,

        /// <summary>
        /// stind.i1
        /// </summary>
        Stind_I1 = 0x52,

        /// <summary>
        /// stind.i2
        /// </summary>
        Stind_I2 = 0x53,

        /// <summary>
        /// stind.i4
        /// </summary>
        Stind_I4 = 0x54,

        /// <summary>
        /// stind.i8
        /// </summary>
        Stind_I8 = 0x55,

        /// <summary>
        /// stind.r4
        /// </summary>
        Stind_R4 = 0x56,

        /// <summary>
        /// stind.r8
        /// </summary>
        Stind_R8 = 0x57,

        /// <summary>
        /// add
        /// </summary>
        Add = 0x58,

        /// <summary>
        /// sub
        /// </summary>
        Sub = 0x59,

        /// <summary>
        /// mul
        /// </summary>
        Mul = 0x5A,

        /// <summary>
        /// div
        /// </summary>
        Div = 0x5B,

        /// <summary>
        /// div.un
        /// </summary>
        Div_Un = 0x5C,

        /// <summary>
        /// rem
        /// </summary>
        Rem = 0x5D,

        /// <summary>
        /// rem.un
        /// </summary>
        Rem_Un = 0x5E,

        /// <summary>
        /// and
        /// </summary>
        And = 0x5F,

        /// <summary>
        /// or
        /// </summary>
        Or = 0x60,

        /// <summary>
        /// xor
        /// </summary>
        Xor = 0x61,

        /// <summary>
        /// shl
        /// </summary>
        Shl = 0x62,

        /// <summary>
        /// shr
        /// </summary>
        Shr = 0x63,

        /// <summary>
        /// shr.un
        /// </summary>
        Shr_Un = 0x64,

        /// <summary>
        /// neg
        /// </summary>
        Neg = 0x65,

        /// <summary>
        /// not
        /// </summary>
        Not = 0x66,

        /// <summary>
        /// conv.i1
        /// </summary>
        Conv_I1 = 0x67,

        /// <summary>
        /// conv.i2
        /// </summary>
        Conv_I2 = 0x68,

        /// <summary>
        /// conv.i4
        /// </summary>
        Conv_I4 = 0x69,

        /// <summary>
        /// conv.i8
        /// </summary>
        Conv_I8 = 0x6A,

        /// <summary>
        /// conv.r4
        /// </summary>
        Conv_R4 = 0x6B,

        /// <summary>
        /// conv.r8
        /// </summary>
        Conv_R8 = 0x6C,

        /// <summary>
        /// conv.u4
        /// </summary>
        Conv_U4 = 0x6D,

        /// <summary>
        /// conv.u8
        /// </summary>
        Conv_U8 = 0x6E,

        /// <summary>
        /// callvirt
        /// </summary>
        Callvirt = 0x6F,

        /// <summary>
        /// cpobj
        /// </summary>
        Cpobj = 0x70,

        /// <summary>
        /// ldobj
        /// </summary>
        Ldobj = 0x71,

        /// <summary>
        /// ldstr
        /// </summary>
        Ldstr = 0x72,

        /// <summary>
        /// newobj
        /// </summary>
        Newobj = 0x73,

        /// <summary>
        /// castclass
        /// </summary>
        Castclass = 0x74,

        /// <summary>
        /// isinst
        /// </summary>
        Isinst = 0x75,

        /// <summary>
        /// conv.r.un
        /// </summary>
        Conv_R_Un = 0x76,

        /// <summary>
        /// unbox
        /// </summary>
        Unbox = 0x79,

        /// <summary>
        /// throw
        /// </summary>
        Throw = 0x7A,

        /// <summary>
        /// ldfld
        /// </summary>
        Ldfld = 0x7B,

        /// <summary>
        /// ldflda
        /// </summary>
        Ldflda = 0x7C,

        /// <summary>
        /// stfld
        /// </summary>
        Stfld = 0x7D,

        /// <summary>
        /// ldsfld
        /// </summary>
        Ldsfld = 0x7E,

        /// <summary>
        /// ldsflda
        /// </summary>
        Ldsflda = 0x7F,

        /// <summary>
        /// stsfld
        /// </summary>
        Stsfld = 0x80,

        /// <summary>
        /// stobj
        /// </summary>
        Stobj = 0x81,

        /// <summary>
        /// conv.ovf.i1.un
        /// </summary>
        Conv_Ovf_I1_Un = 0x82,

        /// <summary>
        /// conv.ovf.i2.un
        /// </summary>
        Conv_Ovf_I2_Un = 0x83,

        /// <summary>
        /// conv.ovf.i4.un
        /// </summary>
        Conv_Ovf_I4_Un = 0x84,

        /// <summary>
        /// conv.ovf.i8.un
        /// </summary>
        Conv_Ovf_I8_Un = 0x85,

        /// <summary>
        /// conv.ovf.u1.un
        /// </summary>
        Conv_Ovf_U1_Un = 0x86,

        /// <summary>
        /// conv.ovf.u2.un
        /// </summary>
        Conv_Ovf_U2_Un = 0x87,

        /// <summary>
        /// conv.ovf.u4.un
        /// </summary>
        Conv_Ovf_U4_Un = 0x88,

        /// <summary>
        /// conv.ovf.u8.un
        /// </summary>
        Conv_Ovf_U8_Un = 0x89,

        /// <summary>
        /// conv.ovf.i.un
        /// </summary>
        Conv_Ovf_I_Un = 0x8A,

        /// <summary>
        /// conv.ovf.u.un
        /// </summary>
        Conv_Ovf_U_Un = 0x8B,

        /// <summary>
        /// box
        /// </summary>
        Box = 0x8C,

        /// <summary>
        /// newarr
        /// </summary>
        Newarr = 0x8D,

        /// <summary>
        /// ldlen
        /// </summary>
        Ldlen = 0x8E,

        /// <summary>
        /// ldelema
        /// </summary>
        Ldelema = 0x8F,

        /// <summary>
        /// ldelem.i1
        /// </summary>
        Ldelem_I1 = 0x90,

        /// <summary>
        /// ldelem.u1
        /// </summary>
        Ldelem_U1 = 0x91,

        /// <summary>
        /// ldelem.i2
        /// </summary>
        Ldelem_I2 = 0x92,

        /// <summary>
        /// ldelem.u2
        /// </summary>
        Ldelem_U2 = 0x93,

        /// <summary>
        /// ldelem.i4
        /// </summary>
        Ldelem_I4 = 0x94,

        /// <summary>
        /// ldelem.u4
        /// </summary>
        Ldelem_U4 = 0x95,

        /// <summary>
        /// ldelem.i8
        /// </summary>
        Ldelem_I8 = 0x96,

        /// <summary>
        /// ldelem.i
        /// </summary>
        Ldelem_I = 0x97,

        /// <summary>
        /// ldelem.r4
        /// </summary>
        Ldelem_R4 = 0x98,

        /// <summary>
        /// ldelem.r8
        /// </summary>
        Ldelem_R8 = 0x99,

        /// <summary>
        /// ldelem.ref
        /// </summary>
        Ldelem_Ref = 0x9A,

        /// <summary>
        /// stelem.i
        /// </summary>
        Stelem_I = 0x9B,

        /// <summary>
        /// stelem.i1
        /// </summary>
        Stelem_I1 = 0x9C,

        /// <summary>
        /// stelem.i2
        /// </summary>
        Stelem_I2 = 0x9D,

        /// <summary>
        /// stelem.i4
        /// </summary>
        Stelem_I4 = 0x9E,

        /// <summary>
        /// stelem.i8
        /// </summary>
        Stelem_I8 = 0x9F,

        /// <summary>
        /// stelem.r4
        /// </summary>
        Stelem_R4 = 0xA0,

        /// <summary>
        /// stelem.r8
        /// </summary>
        Stelem_R8 = 0xA1,

        /// <summary>
        /// stelem.ref
        /// </summary>
        Stelem_Ref = 0xA2,

        /// <summary>
        /// ldelem
        /// </summary>
        Ldelem = 0xA3,

        /// <summary>
        /// stelem
        /// </summary>
        Stelem = 0xA4,

        /// <summary>
        /// unbox.any
        /// </summary>
        Unbox_Any = 0xA5,

        /// <summary>
        /// conv.ovf.i1
        /// </summary>
        Conv_Ovf_I1 = 0xB3,

        /// <summary>
        /// conv.ovf.u1
        /// </summary>
        Conv_Ovf_U1 = 0xB4,

        /// <summary>
        /// conv.ovf.i2
        /// </summary>
        Conv_Ovf_I2 = 0xB5,

        /// <summary>
        /// conv.ovf.u2
        /// </summary>
        Conv_Ovf_U2 = 0xB6,

        /// <summary>
        /// conv.ovf.i4
        /// </summary>
        Conv_Ovf_I4 = 0xB7,

        /// <summary>
        /// conv.ovf.u4
        /// </summary>
        Conv_Ovf_U4 = 0xB8,

        /// <summary>
        /// conv.ovf.i8
        /// </summary>
        Conv_Ovf_I8 = 0xB9,

        /// <summary>
        /// conv.ovf.u8
        /// </summary>
        Conv_Ovf_U8 = 0xBA,

        /// <summary>
        /// refanyval
        /// </summary>
        Refanyval = 0xC2,

        /// <summary>
        /// ckfinite
        /// </summary>
        Ckfinite = 0xC3,

        /// <summary>
        /// mkrefany
        /// </summary>
        Mkrefany = 0xC6,

        /// <summary>
        /// ldtoken
        /// </summary>
        Ldtoken = 0xD0,

        /// <summary>
        /// conv.u2
        /// </summary>
        Conv_U2 = 0xD1,

        /// <summary>
        /// conv.u1
        /// </summary>
        Conv_U1 = 0xD2,

        /// <summary>
        /// conv.i
        /// </summary>
        Conv_I = 0xD3,

        /// <summary>
        /// conv.ovf.i
        /// </summary>
        Conv_Ovf_I = 0xD4,

        /// <summary>
        /// conv.ovf.u
        /// </summary>
        Conv_Ovf_U = 0xD5,

        /// <summary>
        /// add.ovf
        /// </summary>
        Add_Ovf = 0xD6,

        /// <summary>
        /// add.ovf.un
        /// </summary>
        Add_Ovf_Un = 0xD7,

        /// <summary>
        /// mul.ovf
        /// </summary>
        Mul_Ovf = 0xD8,

        /// <summary>
        /// mul.ovf.un
        /// </summary>
        Mul_Ovf_Un = 0xD9,

        /// <summary>
        /// sub.ovf
        /// </summary>
        Sub_Ovf = 0xDA,

        /// <summary>
        /// sub.ovf.un
        /// </summary>
        Sub_Ovf_Un = 0xDB,

        /// <summary>
        /// endfinally
        /// </summary>
        Endfinally = 0xDC,

        /// <summary>
        /// leave
        /// </summary>
        Leave = 0xDD,

        /// <summary>
        /// leave.s
        /// </summary>
        Leave_S = 0xDE,

        /// <summary>
        /// stind.i
        /// </summary>
        Stind_I = 0xDF,

        /// <summary>
        /// conv.u
        /// </summary>
        Conv_U = 0xE0,

        /// <summary>
        /// prefix7
        /// </summary>
        Prefix7 = 0xF8,

        /// <summary>
        /// prefix6
        /// </summary>
        Prefix6 = 0xF9,

        /// <summary>
        /// prefix5
        /// </summary>
        Prefix5 = 0xFA,

        /// <summary>
        /// prefix4
        /// </summary>
        Prefix4 = 0xFB,

        /// <summary>
        /// prefix3
        /// </summary>
        Prefix3 = 0xFC,

        /// <summary>
        /// prefix2
        /// </summary>
        Prefix2 = 0xFD,

        /// <summary>
        /// prefix1
        /// </summary>
        Prefix1 = 0xFE,

        /// <summary>
        /// prefixref
        /// </summary>
        Prefixref = 0xFF,

        /// <summary>
        /// arglist
        /// </summary>
        Arglist = unchecked((short)0xFE_00),

        /// <summary>
        /// ceq
        /// </summary>
        Ceq = unchecked((short)0xFE_01),

        /// <summary>
        /// cgt
        /// </summary>
        Cgt = unchecked((short)0xFE_02),

        /// <summary>
        /// cgt.un
        /// </summary>
        Cgt_Un = unchecked((short)0xFE_03),

        /// <summary>
        /// clt
        /// </summary>
        Clt = unchecked((short)0xFE_04),

        /// <summary>
        /// clt.un
        /// </summary>
        Clt_Un = unchecked((short)0xFE_05),

        /// <summary>
        /// ldftn
        /// </summary>
        Ldftn = unchecked((short)0xFE_06),

        /// <summary>
        /// ldvirtftn
        /// </summary>
        Ldvirtftn = unchecked((short)0xFE_07),

        /// <summary>
        /// ldarg
        /// </summary>
        Ldarg = unchecked((short)0xFE_09),

        /// <summary>
        /// ldarga
        /// </summary>
        Ldarga = unchecked((short)0xFE_0A),

        /// <summary>
        /// starg
        /// </summary>
        Starg = unchecked((short)0xFE_0B),

        /// <summary>
        /// ldloc
        /// </summary>
        Ldloc = unchecked((short)0xFE_0C),

        /// <summary>
        /// ldloca
        /// </summary>
        Ldloca = unchecked((short)0xFE_0D),

        /// <summary>
        /// stloc
        /// </summary>
        Stloc = unchecked((short)0xFE_0E),

        /// <summary>
        /// localloc
        /// </summary>
        Localloc = unchecked((short)0xFE_0F),

        /// <summary>
        /// endfilter
        /// </summary>
        Endfilter = unchecked((short)0xFE_11),

        /// <summary>
        /// unaligned.
        /// </summary>
        Unaligned = unchecked((short)0xFE_12),

        /// <summary>
        /// volatile.
        /// </summary>
        Volatile = unchecked((short)0xFE_13),

        /// <summary>
        /// tail.
        /// </summary>
        Tailcall = unchecked((short)0xFE_14),

        /// <summary>
        /// initobj
        /// </summary>
        Initobj = unchecked((short)0xFE_15),

        /// <summary>
        /// constrained.
        /// </summary>
        Constrained = unchecked((short)0xFE_16),

        /// <summary>
        /// cpblk
        /// </summary>
        Cpblk = unchecked((short)0xFE_17),

        /// <summary>
        /// initblk
        /// </summary>
        Initblk = unchecked((short)0xFE_18),

        /// <summary>
        /// rethrow
        /// </summary>
        Rethrow = unchecked((short)0xFE_1A),

        /// <summary>
        /// sizeof
        /// </summary>
        Sizeof = unchecked((short)0xFE_1C),

        /// <summary>
        /// refanytype
        /// </summary>
        Refanytype = unchecked((short)0xFE_1D),

        /// <summary>
        /// readonly.
        /// </summary>
        Readonly = unchecked((short)0xFE_1E),
    }
}