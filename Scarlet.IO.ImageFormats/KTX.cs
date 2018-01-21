﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.IO.ImageFormats
{
    /* https://www.khronos.org/opengles/sdk/tools/KTX/file_format_spec/ */

    // TODO: InternalFormat/BaseInternalFormat, OnGetBitmap, anything else broken or missing...

    public enum GlType : uint
    {
        None = 0x0,
        GL_UNSIGNED_BYTE = 0x1401,
        GL_BYTE = 0x1400,
        GL_UNSIGNED_SHORT = 0x1403,
        GL_SHORT = 0x1402,
        GL_UNSIGNED_INT = 0x1405,
        GL_INT = 0x1404,
        GL_HALF_FLOAT = 0x140B,
        GL_FLOAT = 0x1406,
        GL_UNSIGNED_BYTE_3_3_2 = 0x8032,
        GL_UNSIGNED_BYTE_2_3_3_REV = 0x8362,
        GL_UNSIGNED_SHORT_5_6_5 = 0x8363,
        GL_UNSIGNED_SHORT_5_6_5_REV = 0x8364,
        GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033,
        GL_UNSIGNED_SHORT_4_4_4_4_REV = 0x8365,
        GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034,
        GL_UNSIGNED_SHORT_1_5_5_5_REV = 0x8366,
        GL_UNSIGNED_INT_8_8_8_8 = 0x8035,
        GL_UNSIGNED_INT_8_8_8_8_REV = 0x8367,
        GL_UNSIGNED_INT_10_10_10_2 = 0x8036,
        GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368,
        GL_UNSIGNED_INT_24_8 = 0x84FA,
        GL_UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B,
        GL_UNSIGNED_INT_5_9_9_9_REV = 0x8C3E,
        GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD
    }

    public enum GlFormat : uint
    {
        None = 0x0,
        GL_STENCIL_INDEX = 0x1901,
        GL_DEPTH_COMPONENT = 0x1902,
        GL_DEPTH_STENCIL = 0x84F9,
        GL_RED = 0x1903,
        GL_GREEN = 0x1904,
        GL_BLUE = 0x1905,
        GL_RG = 0x8227,
        GL_RGB = 0x1907,
        GL_RGBA = 0x1908,
        GL_BGR = 0x80E0,
        GL_BGRA = 0x80E1,
        GL_RED_INTEGER = 0x8D94,
        GL_GREEN_INTEGER = 0x8D95,
        GL_BLUE_INTEGER = 0x8D96,
        GL_RG_INTEGER = 0x8228,
        GL_RGB_INTEGER = 0x8D98,
        GL_RGBA_INTEGER = 0x8D99,
        GL_BGR_INTEGER = 0x8D9A,
        GL_BGRA_INTEGER = 0x8D9B,
    }

    public enum GlInternalFormat : uint
    {
        None = 0x0,
        GL_STENCIL_INDEX = 0x1901,
        GL_DEPTH_COMPONENT = 0x1902,
        GL_DEPTH_STENCIL = 0x84F9,
        GL_RED = 0x1903,
        GL_GREEN = 0x1904,
        GL_BLUE = 0x1905,
        GL_RG = 0x8227,
        GL_RGB = 0x1907,
        GL_RGBA = 0x1908,
        GL_BGR = 0x80E0,
        GL_BGRA = 0x80E1,
        GL_RED_INTEGER = 0x8D94,
        GL_GREEN_INTEGER = 0x8D95,
        GL_BLUE_INTEGER = 0x8D96,
        GL_RG_INTEGER = 0x8228,
        GL_RGB_INTEGER = 0x8D98,
        GL_RGBA_INTEGER = 0x8D99,
        GL_BGR_INTEGER = 0x8D9A,
        GL_BGRA_INTEGER = 0x8D9B,
    }

    public class KTXDictionary : Dictionary<string, byte[]>
    {
        public string GetValueString(string key)
        {
            return Encoding.UTF8.GetString(this[key]).TrimEnd('\0');
        }
    }

    [MagicNumber(new byte[] { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A }, 0x00)]  /* "«KTX 11»\r\n\x1A\n" */
    public class KTX : ImageFormat
    {
        public static readonly uint ExpectedEndianness = 0x04030201;

        public byte[] Identifier { get; private set; }
        public uint Endianness { get; private set; }
        public GlType GlType { get; private set; }
        public uint GlTypeSize { get; private set; }
        public GlFormat GlFormat { get; private set; }
        public uint GlInternalFormat { get; private set; }
        public uint GlBaseInternalFormat { get; private set; }
        public uint PixelWidth { get; private set; }
        public uint PixelHeight { get; private set; }
        public uint PixelDepth { get; private set; }
        public uint NumberOfArrayElements { get; private set; }
        public uint NumberOfFaces { get; private set; }
        public uint NumberOfMipmapLevels { get; private set; }
        public uint BytesOfKeyValueData { get; private set; }

        public KTXDictionary Dictionary { get; private set; }

        List<MipmapLevel> mipmapLevels;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            Identifier = reader.ReadBytes(12);
            Endianness = reader.ReadUInt32();
            if (Endianness != ExpectedEndianness)
            {
                if (reader.Endianness == Endian.BigEndian)
                    reader.Endianness = Endian.LittleEndian;
                else
                    reader.Endianness = Endian.BigEndian;
            }

            GlType = (GlType)reader.ReadUInt32();
            GlTypeSize = reader.ReadUInt32();
            GlFormat = (GlFormat)reader.ReadUInt32();
            GlInternalFormat = reader.ReadUInt32();
            GlBaseInternalFormat = reader.ReadUInt32();
            PixelWidth = reader.ReadUInt32();
            PixelHeight = reader.ReadUInt32();
            PixelDepth = reader.ReadUInt32();
            NumberOfArrayElements = reader.ReadUInt32();
            NumberOfFaces = reader.ReadUInt32();
            NumberOfMipmapLevels = reader.ReadUInt32();
            BytesOfKeyValueData = reader.ReadUInt32();

            Dictionary = new KTXDictionary();

            long keyValueDataEnd = (reader.BaseStream.Position + BytesOfKeyValueData);
            while (reader.BaseStream.Position < keyValueDataEnd)
            {
                uint keyAndValueByteSize = reader.ReadUInt32();

                long keyStartPosition = reader.BaseStream.Position;
                string key = reader.ReadNullTerminatedString();
                byte[] value = reader.ReadBytes((int)(keyAndValueByteSize - (reader.BaseStream.Position - keyStartPosition)));
                Dictionary.Add(key, value);

                for (int i = 0; i < (3 - ((keyAndValueByteSize + 3) % 4)); i++)
                    reader.ReadByte();
            }

            int numMipmapLevelsTrue = (int)Math.Min(1, NumberOfMipmapLevels);
            int numArrayElementsTrue = (int)Math.Min(1, NumberOfArrayElements);

            mipmapLevels = new List<MipmapLevel>();

            int mipmapWidth = (int)PixelWidth, mipmapHeight = (int)PixelHeight;

            for (int mipmapLevel = 0; mipmapLevel < numMipmapLevelsTrue; mipmapLevel++)
            {
                uint imageSize = reader.ReadUInt32();

                mipmapLevels.Add(new MipmapLevel(mipmapWidth, mipmapHeight, reader.ReadBytes((int)imageSize)));
                mipmapWidth >>= 1;
                mipmapHeight >>= 1;

                for (int i = 0; i < (3 - ((imageSize + 3) % 4)); i++)
                    reader.ReadByte();
            }

            //
        }

        public override int GetImageCount()
        {
            return mipmapLevels.Count;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            throw new NotImplementedException();
        }
    }
}
