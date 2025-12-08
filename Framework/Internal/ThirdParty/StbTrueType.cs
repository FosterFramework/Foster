using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// This is a modified version of StbTrueTypeSharp:
/// https://github.com/StbSharp/StbTrueTypeSharp
///
/// Which itself is a C# port of stb_truetype:
/// https://github.com/nothings/stb/blob/master/stb_truetype.h
///
/// This source was modified to remove all unused code, 
/// and to simplify how Foster uses it.
/// </summary>
internal static unsafe class StbTrueType
{
	#region Public (internal) API

	public static unsafe nint Create(nint data)
	{
		if (stbtt_GetNumberOfFonts((byte*)data) <= 0)
			return nint.Zero;

		stbtt_fontinfo* info = (stbtt_fontinfo*)Marshal.AllocHGlobal(sizeof(stbtt_fontinfo));

		if (stbtt_InitFont(info, (byte*)data, 0) == 0)
		{
			Marshal.FreeHGlobal((nint)info);
			return nint.Zero;
		}

		return (nint)info;
	}

	public static unsafe void GetMetrics(nint font, out int ascent, out int descent, out int linegap)
	{
		int a, d, l;
		stbtt_GetFontVMetrics((stbtt_fontinfo*)font, &a, &d, &l);
		ascent = a;
		descent = d;
		linegap = l;
	}

	public static unsafe int GetGlyphIndex(nint font, int codepoint)
		=> stbtt_FindGlyphIndex((stbtt_fontinfo*)font, codepoint);

	public static unsafe float GetScale(nint font, float size)
		=> stbtt_ScaleForMappingEmToPixels((stbtt_fontinfo*)font, size);

	public static unsafe float GetKerning(nint font, int glyph1, int glyph2, float scale)
		=> stbtt_GetGlyphKernAdvance((stbtt_fontinfo*)font, glyph1, glyph2) * scale;

	public static unsafe void GetCharacter(nint font, int glyph, float scale, out int width, out int height, out float advance, out float offsetX, out float offsetY, out int visible)
	{
		stbtt_fontinfo* info = (stbtt_fontinfo*)font;

		int adv, ox, x0, y0, x1, y1;

		stbtt_GetGlyphHMetrics(info, glyph, &adv, &ox);
		stbtt_GetGlyphBitmapBox(info, glyph, scale, scale, &x0, &y0, &x1, &y1);

		width = (x1 - x0);
		height = (y1 - y0);
		advance = adv * scale;
		offsetX = ox * scale;
		offsetY = (float)y0;
		visible = (width > 0 && height > 0 && stbtt_IsGlyphEmpty(info, glyph) == 0) ? 1 : 0;
	}

	public static unsafe void GetPixels(nint font, nint dest, int glyph, int width, int height, float scale)
	{
		byte* dst = (byte*)dest;
		stbtt_fontinfo* info = (stbtt_fontinfo*)font;

		// parse it directly into the dest buffer
		stbtt_MakeGlyphBitmapSubpixel(info, dst, width, height, width, scale, scale, 0, 0, glyph);

		// convert the buffer to RGBA data by working backwards, overwriting data
		int len = width * height;
		for (int a = (len - 1) * 4, b = (len - 1); b >= 0; a -= 4, b -= 1)
		{
			dst[a + 0] = dst[b];
			dst[a + 1] = dst[b];
			dst[a + 2] = dst[b];
			dst[a + 3] = dst[b];
		}
	}

	public static unsafe void Destroy(nint font)
	{
		Marshal.FreeHGlobal(font);
	}

	#endregion

	#region Internal Utility

	static void* Malloc(ulong size)
		=> Marshal.AllocHGlobal((int)size).ToPointer();

	static void Free(void* a)
	{
		if (a != null)
			Marshal.FreeHGlobal((nint)a);
	}

	static void MemCpy(void* a, void* b, ulong size)
		=> Buffer.MemoryCopy(a, b, size, size);

	static void Zero(void* ptr, ulong size)
	{
		var bptr = (byte*)ptr;
		for (ulong i = 0; i < size; ++i)
			*bptr++ = 0;
	}

	#endregion

	#region Definitions

	const int STBTT_MS_EID_UNICODE_BMP = 1;
	const int STBTT_MS_EID_UNICODE_FULL = 10;
	const int STBTT_PLATFORM_ID_MICROSOFT = 3;
	const int STBTT_PLATFORM_ID_UNICODE = 0;

	const int STBTT_vmove = 1;
	const int STBTT_vline = 2;
	const int STBTT_vcurve = 3;
	const int STBTT_vcubic = 4;

	struct stbtt_fontinfo
	{
		public stbtt__buf cff;
		public stbtt__buf charstrings;
		public byte* data;
		public stbtt__buf fdselect;
		public stbtt__buf fontdicts;
		public int fontstart;
		public int glyf;
		public int gpos;
		public stbtt__buf gsubrs;
		public int head;
		public int hhea;
		public int hmtx;
		public int index_map;
		public int indexToLocFormat;
		public int kern;
		public int loca;
		public int numGlyphs;
		public stbtt__buf subrs;
		public int svg;
	}

	struct stbtt_vertex
	{
		public short x;
		public short y;
		public short cx;
		public short cy;
		public short cx1;
		public short cy1;
		public byte type;
		public byte padding;
	}

	struct stbtt__csctx
	{
		public int bounds;
		public int started;
		public float first_x;
		public float first_y;
		public float x;
		public float y;
		public int min_x;
		public int max_x;
		public int min_y;
		public int max_y;
		public stbtt_vertex* pvertices;
		public int num_vertices;
	}

	struct stbtt__bitmap
	{
		public int w;
		public int h;
		public int stride;
		public byte* pixels;
	}

	struct stbtt__point
	{
		public float x;
		public float y;
	}

	struct stbtt__edge
	{
		public float x0;
		public float y0;
		public float x1;
		public float y1;
		public int invert;
	}

	struct stbtt__active_edge
	{
		public stbtt__active_edge* next;
		public float fx;
		public float fdx;
		public float fdy;
		public float direction;
		public float sy;
		public float ey;
	}

	struct stbtt__hheap
	{
		public stbtt__hheap_chunk* head;
		public void* first_free;
		public int num_remaining_in_head_chunk;
	}

	struct stbtt__hheap_chunk
	{
		public stbtt__hheap_chunk* next;
	}

	struct stbtt__buf
	{
		public byte* data;
		public int cursor;
		public int size;
	}

	#endregion

	#region Implementation

	static int ttLONG(byte* p)
		=> (p[0] << 24) + (p[1] << 16) + (p[2] << 8) + p[3];

	static short ttSHORT(byte* p)
		=> (short)(p[0] * 256 + p[1]);

	static uint ttULONG(byte* p)
		=> (uint)((p[0] << 24) + (p[1] << 16) + (p[2] << 8) + p[3]);

	static ushort ttUSHORT(byte* p)
		=> (ushort)(p[0] * 256 + p[1]);

	static int equal(float* a, float* b)
		=> a[0] == b[0] && a[1] == b[1] ? 1 : 0;

	static int stbtt__isfont(byte* font)
	{
		if (font[0] == 49 && font[1] == 0 && font[2] == 0 && font[3] == 0)
			return 1;
		if (font[0] == "typ1"[0] && font[1] == "typ1"[1] && font[2] == "typ1"[2] && font[3] == "typ1"[3])
			return 1;
		if (font[0] == "OTTO"[0] && font[1] == "OTTO"[1] && font[2] == "OTTO"[2] && font[3] == "OTTO"[3])
			return 1;
		if (font[0] == 0 && font[1] == 1 && font[2] == 0 && font[3] == 0)
			return 1;
		if (font[0] == "true"[0] && font[1] == "true"[1] && font[2] == "true"[2] && font[3] == "true"[3])
			return 1;
		return 0;
	}

	static uint stbtt__find_table(byte* data, uint fontstart, string tag)
	{
		int num_tables = ttUSHORT(data + fontstart + 4);
		var tabledir = fontstart + 12;
		int i;
		for (i = 0; i < num_tables; ++i)
		{
			var loc = (uint)(tabledir + 16 * i);
			if ((data + loc + 0)[0] == tag[0] && (data + loc + 0)[1] == tag[1] &&
				(data + loc + 0)[2] == tag[2] && (data + loc + 0)[3] == tag[3])
				return ttULONG(data + loc + 8);
		}

		return 0;
	}

	static int stbtt_GetNumberOfFonts(byte* data)
	{
		return stbtt_GetNumberOfFonts_internal(data);
	}

	static int stbtt_GetNumberOfFonts_internal(byte* font_collection)
	{
		if (stbtt__isfont(font_collection) != 0)
			return 1;
		if (font_collection[0] == "ttcf"[0] && font_collection[1] == "ttcf"[1] && font_collection[2] == "ttcf"[2] &&
			font_collection[3] == "ttcf"[3])
			if (ttULONG(font_collection + 4) == 0x00010000 || ttULONG(font_collection + 4) == 0x00020000)
				return ttLONG(font_collection + 8);

		return 0;
	}

	static int stbtt_InitFont(stbtt_fontinfo* info, byte* data, int offset)
	{
		return stbtt_InitFont_internal(info, data, offset);
	}

	static int stbtt_InitFont_internal(stbtt_fontinfo* info, byte* data, int fontstart)
	{
		uint cmap = 0;
		uint t = 0;
		var i = 0;
		var numTables = 0;
		info->data = data;
		info->fontstart = fontstart;
		info->cff = stbtt__new_buf(null, 0);
		cmap = stbtt__find_table(data, (uint)fontstart, "cmap");
		info->loca = (int)stbtt__find_table(data, (uint)fontstart, "loca");
		info->head = (int)stbtt__find_table(data, (uint)fontstart, "head");
		info->glyf = (int)stbtt__find_table(data, (uint)fontstart, "glyf");
		info->hhea = (int)stbtt__find_table(data, (uint)fontstart, "hhea");
		info->hmtx = (int)stbtt__find_table(data, (uint)fontstart, "hmtx");
		info->kern = (int)stbtt__find_table(data, (uint)fontstart, "kern");
		info->gpos = (int)stbtt__find_table(data, (uint)fontstart, "GPOS");
		if (cmap == 0 || info->head == 0 || info->hhea == 0 || info->hmtx == 0)
			return 0;
		if (info->glyf != 0)
		{
			if (info->loca == 0)
				return 0;
		}
		else
		{
			var b = new stbtt__buf();
			var topdict = new stbtt__buf();
			var topdictidx = new stbtt__buf();
			uint cstype = 2;
			uint charstrings = 0;
			uint fdarrayoff = 0;
			uint fdselectoff = 0;
			uint cff = 0;
			cff = stbtt__find_table(data, (uint)fontstart, "CFF ");
			if (cff == 0)
				return 0;
			info->fontdicts = stbtt__new_buf(null, 0);
			info->fdselect = stbtt__new_buf(null, 0);
			info->cff = stbtt__new_buf(data + cff, 512 * 1024 * 1024);
			b = info->cff;
			stbtt__buf_skip(&b, 2);
			stbtt__buf_seek(&b, stbtt__buf_get8(&b));
			stbtt__cff_get_index(&b);
			topdictidx = stbtt__cff_get_index(&b);
			topdict = stbtt__cff_index_get(topdictidx, 0);
			stbtt__cff_get_index(&b);
			info->gsubrs = stbtt__cff_get_index(&b);
			stbtt__dict_get_ints(&topdict, 17, 1, &charstrings);
			stbtt__dict_get_ints(&topdict, 0x100 | 6, 1, &cstype);
			stbtt__dict_get_ints(&topdict, 0x100 | 36, 1, &fdarrayoff);
			stbtt__dict_get_ints(&topdict, 0x100 | 37, 1, &fdselectoff);
			info->subrs = stbtt__get_subrs(b, topdict);
			if (cstype != 2)
				return 0;
			if (charstrings == 0)
				return 0;
			if (fdarrayoff != 0)
			{
				if (fdselectoff == 0)
					return 0;
				stbtt__buf_seek(&b, (int)fdarrayoff);
				info->fontdicts = stbtt__cff_get_index(&b);
				info->fdselect = stbtt__buf_range(&b, (int)fdselectoff, (int)(b.size - fdselectoff));
			}

			stbtt__buf_seek(&b, (int)charstrings);
			info->charstrings = stbtt__cff_get_index(&b);
		}

		t = stbtt__find_table(data, (uint)fontstart, "maxp");
		if (t != 0)
			info->numGlyphs = ttUSHORT(data + t + 4);
		else
			info->numGlyphs = 0xffff;
		info->svg = -1;
		numTables = ttUSHORT(data + cmap + 2);
		info->index_map = 0;
		for (i = 0; i < numTables; ++i)
		{
			var encoding_record = (uint)(cmap + 4 + 8 * i);
			switch (ttUSHORT(data + encoding_record))
			{
				case STBTT_PLATFORM_ID_MICROSOFT:
					switch (ttUSHORT(data + encoding_record + 2))
					{
						case STBTT_MS_EID_UNICODE_BMP:
						case STBTT_MS_EID_UNICODE_FULL:
							info->index_map = (int)(cmap + ttULONG(data + encoding_record + 4));
							break;
					}

					break;
				case STBTT_PLATFORM_ID_UNICODE:
					info->index_map = (int)(cmap + ttULONG(data + encoding_record + 4));
					break;
			}
		}

		if (info->index_map == 0)
			throw new Exception("The font does not have a table mapping from unicode codepoints to font indices.");
		info->indexToLocFormat = ttUSHORT(data + info->head + 50);
		return 1;
	}

	static void stbtt_GetFontVMetrics(stbtt_fontinfo* info, int* ascent, int* descent, int* lineGap)
	{
		if (ascent != null)
			*ascent = ttSHORT(info->data + info->hhea + 4);
		if (descent != null)
			*descent = ttSHORT(info->data + info->hhea + 6);
		if (lineGap != null)
			*lineGap = ttSHORT(info->data + info->hhea + 8);
	}

	static int stbtt_FindGlyphIndex(stbtt_fontinfo* info, int unicode_codepoint)
	{
		var data = info->data;
		var index_map = (uint)info->index_map;
		var format = ttUSHORT(data + index_map + 0);
		if (format == 0)
		{
			int bytes = ttUSHORT(data + index_map + 2);
			if (unicode_codepoint < bytes - 6)
				return *(data + index_map + 6 + unicode_codepoint);
			return 0;
		}

		if (format == 6)
		{
			uint first = ttUSHORT(data + index_map + 6);
			uint count = ttUSHORT(data + index_map + 8);
			if ((uint)unicode_codepoint >= first && (uint)unicode_codepoint < first + count)
				return ttUSHORT(data + index_map + 10 + (unicode_codepoint - first) * 2);
			return 0;
		}

		if (format == 2) return 0;

		if (format == 4)
		{
			var segcount = (ushort)(ttUSHORT(data + index_map + 6) >> 1);
			var searchRange = (ushort)(ttUSHORT(data + index_map + 8) >> 1);
			var entrySelector = ttUSHORT(data + index_map + 10);
			var rangeShift = (ushort)(ttUSHORT(data + index_map + 12) >> 1);
			var endCount = index_map + 14;
			var search = endCount;
			if (unicode_codepoint > 0xffff)
				return 0;
			if (unicode_codepoint >= ttUSHORT(data + search + rangeShift * 2))
				search += (uint)(rangeShift * 2);
			search -= 2;
			while (entrySelector != 0)
			{
				ushort end = 0;
				searchRange >>= 1;
				end = ttUSHORT(data + search + searchRange * 2);
				if (unicode_codepoint > end)
					search += (uint)(searchRange * 2);
				--entrySelector;
			}

			search += 2;
			{
				ushort offset = 0;
				ushort start = 0;
				ushort last = 0;
				var item = (ushort)((search - endCount) >> 1);
				start = ttUSHORT(data + index_map + 14 + segcount * 2 + 2 + 2 * item);
				last = ttUSHORT(data + endCount + 2 * item);
				if (unicode_codepoint < start || unicode_codepoint > last)
					return 0;
				offset = ttUSHORT(data + index_map + 14 + segcount * 6 + 2 + 2 * item);
				if (offset == 0)
					return (ushort)(unicode_codepoint +
										ttSHORT(data + index_map + 14 + segcount * 4 + 2 + 2 * item));
				return ttUSHORT(data + offset + (unicode_codepoint - start) * 2 + index_map + 14 + segcount * 6 +
								2 + 2 * item);
			}
		}

		if (format == 12 || format == 13)
		{
			var ngroups = ttULONG(data + index_map + 12);
			var low = 0;
			var high = 0;
			low = 0;
			high = (int)ngroups;
			while (low < high)
			{
				var mid = low + ((high - low) >> 1);
				var start_char = ttULONG(data + index_map + 16 + mid * 12);
				var end_char = ttULONG(data + index_map + 16 + mid * 12 + 4);
				if ((uint)unicode_codepoint < start_char)
				{
					high = mid;
				}
				else if ((uint)unicode_codepoint > end_char)
				{
					low = mid + 1;
				}
				else
				{
					var start_glyph = ttULONG(data + index_map + 16 + mid * 12 + 8);
					if (format == 12)
						return (int)(start_glyph + unicode_codepoint - start_char);
					return (int)start_glyph;
				}
			}

			return 0;
		}

		return 0;
	}

	static float stbtt_ScaleForMappingEmToPixels(stbtt_fontinfo* info, float pixels)
	{
		int unitsPerEm = ttUSHORT(info->data + info->head + 18);
		return pixels / unitsPerEm;
	}

	static int stbtt_GetGlyphKernAdvance(stbtt_fontinfo* info, int g1, int g2)
	{
		var xAdvance = 0;
		if (info->gpos != 0)
			xAdvance += stbtt__GetGlyphGPOSInfoAdvance(info, g1, g2);
		else if (info->kern != 0)
			xAdvance += stbtt__GetGlyphKernInfoAdvance(info, g1, g2);
		return xAdvance;
	}

	static int stbtt__GetGlyphGPOSInfoAdvance(stbtt_fontinfo* info, int glyph1, int glyph2)
	{
		ushort lookupListOffset = 0;
		byte* lookupList;
		ushort lookupCount = 0;
		byte* data;
		var i = 0;
		var sti = 0;
		if (info->gpos == 0)
			return 0;
		data = info->data + info->gpos;
		if (ttUSHORT(data + 0) != 1)
			return 0;
		if (ttUSHORT(data + 2) != 0)
			return 0;
		lookupListOffset = ttUSHORT(data + 8);
		lookupList = data + lookupListOffset;
		lookupCount = ttUSHORT(lookupList);
		for (i = 0; i < lookupCount; ++i)
		{
			var lookupOffset = ttUSHORT(lookupList + 2 + 2 * i);
			var lookupTable = lookupList + lookupOffset;
			var lookupType = ttUSHORT(lookupTable);
			var subTableCount = ttUSHORT(lookupTable + 4);
			var subTableOffsets = lookupTable + 6;
			if (lookupType != 2)
				continue;
			for (sti = 0; sti < subTableCount; sti++)
			{
				var subtableOffset = ttUSHORT(subTableOffsets + 2 * sti);
				var table = lookupTable + subtableOffset;
				var posFormat = ttUSHORT(table);
				var coverageOffset = ttUSHORT(table + 2);
				var coverageIndex = stbtt__GetCoverageIndex(table + coverageOffset, glyph1);
				if (coverageIndex == -1)
					continue;
				switch (posFormat)
				{
				case 1:
				{
					var l = 0;
					var r = 0;
					var m = 0;
					var straw = 0;
					var needle = 0;
					var valueFormat1 = ttUSHORT(table + 4);
					var valueFormat2 = ttUSHORT(table + 6);
					if (valueFormat1 == 4 && valueFormat2 == 0)
					{
						var valueRecordPairSizeInBytes = 2;
						var pairSetCount = ttUSHORT(table + 8);
						var pairPosOffset = ttUSHORT(table + 10 + 2 * coverageIndex);
						var pairValueTable = table + pairPosOffset;
						var pairValueCount = ttUSHORT(pairValueTable);
						var pairValueArray = pairValueTable + 2;
						if (coverageIndex >= pairSetCount)
							return 0;
						needle = glyph2;
						r = pairValueCount - 1;
						l = 0;
						while (l <= r)
						{
							ushort secondGlyph = 0;
							byte* pairValue;
							m = (l + r) >> 1;
							pairValue = pairValueArray + (2 + valueRecordPairSizeInBytes) * m;
							secondGlyph = ttUSHORT(pairValue);
							straw = secondGlyph;
							if (needle < straw)
							{
								r = m - 1;
							}
							else if (needle > straw)
							{
								l = m + 1;
							}
							else
							{
								var xAdvance = ttSHORT(pairValue + 2);
								return xAdvance;
							}
						}
					}
					else
					{
						return 0;
					}

					break;
				}

				case 2:
				{
					var valueFormat1 = ttUSHORT(table + 4);
					var valueFormat2 = ttUSHORT(table + 6);
					if (valueFormat1 == 4 && valueFormat2 == 0)
					{
						var classDef1Offset = ttUSHORT(table + 8);
						var classDef2Offset = ttUSHORT(table + 10);
						var glyph1class = stbtt__GetGlyphClass(table + classDef1Offset, glyph1);
						var glyph2class = stbtt__GetGlyphClass(table + classDef2Offset, glyph2);
						var class1Count = ttUSHORT(table + 12);
						var class2Count = ttUSHORT(table + 14);
						byte* class1Records;
						byte* class2Records;
						short xAdvance = 0;
						if (glyph1class < 0 || glyph1class >= class1Count)
							return 0;
						if (glyph2class < 0 || glyph2class >= class2Count)
							return 0;
						class1Records = table + 16;
						class2Records = class1Records + 2 * glyph1class * class2Count;
						xAdvance = ttSHORT(class2Records + 2 * glyph2class);
						return xAdvance;
					}

					return 0;
				}

				default:
					return 0;
			}
			}
		}

		return 0;
	}

	static int stbtt__GetCoverageIndex(byte* coverageTable, int glyph)
	{
		var coverageFormat = ttUSHORT(coverageTable);
		switch (coverageFormat)
		{
		case 1:
		{
			var glyphCount = ttUSHORT(coverageTable + 2);
			var l = 0;
			var r = glyphCount - 1;
			var m = 0;
			var straw = 0;
			var needle = glyph;
			while (l <= r)
			{
				var glyphArray = coverageTable + 4;
				ushort glyphID = 0;
				m = (l + r) >> 1;
				glyphID = ttUSHORT(glyphArray + 2 * m);
				straw = glyphID;
				if (needle < straw)
					r = m - 1;
				else if (needle > straw)
					l = m + 1;
				else
					return m;
			}

			break;
		}

		case 2:
		{
			var rangeCount = ttUSHORT(coverageTable + 2);
			var rangeArray = coverageTable + 4;
			var l = 0;
			var r = rangeCount - 1;
			var m = 0;
			var strawStart = 0;
			var strawEnd = 0;
			var needle = glyph;
			while (l <= r)
			{
				byte* rangeRecord;
				m = (l + r) >> 1;
				rangeRecord = rangeArray + 6 * m;
				strawStart = ttUSHORT(rangeRecord);
				strawEnd = ttUSHORT(rangeRecord + 2);
				if (needle < strawStart)
				{
					r = m - 1;
				}
				else if (needle > strawEnd)
				{
					l = m + 1;
				}
				else
				{
					var startCoverageIndex = ttUSHORT(rangeRecord + 4);
					return startCoverageIndex + glyph - strawStart;
				}
			}

			break;
		}

		default:
			return -1;
		}

		return -1;
	}

	static int stbtt__GetGlyphKernInfoAdvance(stbtt_fontinfo* info, int glyph1, int glyph2)
	{
		var data = info->data + info->kern;
		uint needle = 0;
		uint straw = 0;
		var l = 0;
		var r = 0;
		var m = 0;
		if (info->kern == 0)
			return 0;
		if (ttUSHORT(data + 2) < 1)
			return 0;
		if (ttUSHORT(data + 8) != 1)
			return 0;
		l = 0;
		r = ttUSHORT(data + 10) - 1;
		needle = (uint)((glyph1 << 16) | glyph2);
		while (l <= r)
		{
			m = (l + r) >> 1;
			straw = ttULONG(data + 18 + m * 6);
			if (needle < straw)
				r = m - 1;
			else if (needle > straw)
				l = m + 1;
			else
				return ttSHORT(data + 22 + m * 6);
		}

		return 0;
	}

	static int stbtt__GetGlyphClass(byte* classDefTable, int glyph)
	{
		var classDefFormat = ttUSHORT(classDefTable);
		switch (classDefFormat)
		{
		case 1:
		{
			var startGlyphID = ttUSHORT(classDefTable + 2);
			var glyphCount = ttUSHORT(classDefTable + 4);
			var classDef1ValueArray = classDefTable + 6;
			if (glyph >= startGlyphID && glyph < startGlyphID + glyphCount)
				return ttUSHORT(classDef1ValueArray + 2 * (glyph - startGlyphID));
			break;
		}

		case 2:
		{
			var classRangeCount = ttUSHORT(classDefTable + 2);
			var classRangeRecords = classDefTable + 4;
			var l = 0;
			var r = classRangeCount - 1;
			var m = 0;
			var strawStart = 0;
			var strawEnd = 0;
			var needle = glyph;
			while (l <= r)
			{
				byte* classRangeRecord;
				m = (l + r) >> 1;
				classRangeRecord = classRangeRecords + 6 * m;
				strawStart = ttUSHORT(classRangeRecord);
				strawEnd = ttUSHORT(classRangeRecord + 2);
				if (needle < strawStart)
					r = m - 1;
				else if (needle > strawEnd)
					l = m + 1;
				else
					return ttUSHORT(classRangeRecord + 4);
			}

			break;
		}

		default:
			return -1;
		}

		return 0;
	}

	static void stbtt_GetGlyphHMetrics(stbtt_fontinfo* info, int glyph_index, int* advanceWidth,
		int* leftSideBearing)
	{
		var numOfLongHorMetrics = ttUSHORT(info->data + info->hhea + 34);
		if (glyph_index < numOfLongHorMetrics)
		{
			if (advanceWidth != null)
				*advanceWidth = ttSHORT(info->data + info->hmtx + 4 * glyph_index);
			if (leftSideBearing != null)
				*leftSideBearing = ttSHORT(info->data + info->hmtx + 4 * glyph_index + 2);
		}
		else
		{
			if (advanceWidth != null)
				*advanceWidth = ttSHORT(info->data + info->hmtx + 4 * (numOfLongHorMetrics - 1));
			if (leftSideBearing != null)
				*leftSideBearing = ttSHORT(info->data + info->hmtx + 4 * numOfLongHorMetrics +
											2 * (glyph_index - numOfLongHorMetrics));
		}
	}

	static void stbtt_GetGlyphBitmapBox(stbtt_fontinfo* font, int glyph, float scale_x, float scale_y,
		int* ix0, int* iy0, int* ix1, int* iy1)
	{
		stbtt_GetGlyphBitmapBoxSubpixel(font, glyph, scale_x, scale_y, 0.0f, 0.0f, ix0, iy0, ix1, iy1);
	}

	static void stbtt_GetGlyphBitmapBoxSubpixel(stbtt_fontinfo* font, int glyph, float scale_x, float scale_y,
		float shift_x, float shift_y, int* ix0, int* iy0, int* ix1, int* iy1)
	{
		var x0 = 0;
		var y0 = 0;
		var x1 = 0;
		var y1 = 0;
		if (stbtt_GetGlyphBox(font, glyph, &x0, &y0, &x1, &y1) == 0)
		{
			if (ix0 != null)
				*ix0 = 0;
			if (iy0 != null)
				*iy0 = 0;
			if (ix1 != null)
				*ix1 = 0;
			if (iy1 != null)
				*iy1 = 0;
		}
		else
		{
			if (ix0 != null)
				*ix0 = (int)Math.Floor(x0 * scale_x + shift_x);
			if (iy0 != null)
				*iy0 = (int)Math.Floor(-y1 * scale_y + shift_y);
			if (ix1 != null)
				*ix1 = (int)Math.Ceiling(x1 * scale_x + shift_x);
			if (iy1 != null)
				*iy1 = (int)Math.Ceiling(-y0 * scale_y + shift_y);
		}
	}

	static int stbtt_GetGlyphBox(stbtt_fontinfo* info, int glyph_index, int* x0, int* y0, int* x1, int* y1)
	{
		if (info->cff.size != 0)
		{
			stbtt__GetGlyphInfoT2(info, glyph_index, x0, y0, x1, y1);
		}
		else
		{
			var g = stbtt__GetGlyfOffset(info, glyph_index);
			if (g < 0)
				return 0;
			if (x0 != null)
				*x0 = ttSHORT(info->data + g + 2);
			if (y0 != null)
				*y0 = ttSHORT(info->data + g + 4);
			if (x1 != null)
				*x1 = ttSHORT(info->data + g + 6);
			if (y1 != null)
				*y1 = ttSHORT(info->data + g + 8);
		}

		return 1;
	}

	static int stbtt__GetGlyfOffset(stbtt_fontinfo* info, int glyph_index)
	{
		var g1 = 0;
		var g2 = 0;
		if (glyph_index >= info->numGlyphs)
			return -1;
		if (info->indexToLocFormat >= 2)
			return -1;
		if (info->indexToLocFormat == 0)
		{
			g1 = info->glyf + ttUSHORT(info->data + info->loca + glyph_index * 2) * 2;
			g2 = info->glyf + ttUSHORT(info->data + info->loca + glyph_index * 2 + 2) * 2;
		}
		else
		{
			g1 = (int)(info->glyf + ttULONG(info->data + info->loca + glyph_index * 4));
			g2 = (int)(info->glyf + ttULONG(info->data + info->loca + glyph_index * 4 + 4));
		}

		return g1 == g2 ? -1 : g1;
	}

	static int stbtt__GetGlyphInfoT2(stbtt_fontinfo* info, int glyph_index, int* x0, int* y0, int* x1,
		int* y1)
	{
		var c = new stbtt__csctx();
		c.bounds = 1;
		var r = stbtt__run_charstring(info, glyph_index, &c);
		if (x0 != null)
			*x0 = r != 0 ? c.min_x : 0;
		if (y0 != null)
			*y0 = r != 0 ? c.min_y : 0;
		if (x1 != null)
			*x1 = r != 0 ? c.max_x : 0;
		if (y1 != null)
			*y1 = r != 0 ? c.max_y : 0;
		return r != 0 ? c.num_vertices : 0;
	}

	static int stbtt__run_charstring(stbtt_fontinfo* info, int glyph_index, stbtt__csctx* c)
	{
		var in_header = 1;
		var maskbits = 0;
		var subr_stack_height = 0;
		var sp = 0;
		var v = 0;
		var i = 0;
		var b0 = 0;
		var has_subrs = 0;
		var clear_stack = 0;
		var s = stackalloc float[48];
		var subr_stack = stackalloc stbtt__buf[10];
		var subrs = info->subrs;
		var b = new stbtt__buf();
		float f = 0;
		b = stbtt__cff_index_get(info->charstrings, glyph_index);
		while (b.cursor < b.size)
		{
			i = 0;
			clear_stack = 1;
			b0 = stbtt__buf_get8(&b);
			switch (b0)
			{
			case 0x13:
			case 0x14:
				if (in_header != 0)
					maskbits += sp / 2;
				in_header = 0;
				stbtt__buf_skip(&b, (maskbits + 7) / 8);
				break;
			case 0x01:
			case 0x03:
			case 0x12:
			case 0x17:
				maskbits += sp / 2;
				break;
			case 0x15:
				in_header = 0;
				if (sp < 2)
					return 0;
				stbtt__csctx_rmove_to(c, s[sp - 2], s[sp - 1]);
				break;
			case 0x04:
				in_header = 0;
				if (sp < 1)
					return 0;
				stbtt__csctx_rmove_to(c, 0, s[sp - 1]);
				break;
			case 0x16:
				in_header = 0;
				if (sp < 1)
					return 0;
				stbtt__csctx_rmove_to(c, s[sp - 1], 0);
				break;
			case 0x05:
				if (sp < 2)
					return 0;
				for (; i + 1 < sp; i += 2) stbtt__csctx_rline_to(c, s[i], s[i + 1]);

				break;
			case 0x07:
			case 0x06:
				if (sp < 1)
					return 0;
				var goto_vlineto = b0 == 0x07 ? 1 : 0;
				for (; ; )
				{
					if (goto_vlineto == 0)
					{
						if (i >= sp)
							break;
						stbtt__csctx_rline_to(c, s[i], 0);
						i++;
					}

					goto_vlineto = 0;
					if (i >= sp)
						break;
					stbtt__csctx_rline_to(c, 0, s[i]);
					i++;
				}

				break;
			case 0x1F:
			case 0x1E:
				if (sp < 4)
					return 0;
				var goto_hvcurveto = b0 == 0x1F ? 1 : 0;
				for (; ; )
				{
					if (goto_hvcurveto == 0)
					{
						if (i + 3 >= sp)
							break;
						stbtt__csctx_rccurve_to(c, 0, s[i], s[i + 1], s[i + 2], s[i + 3],
							sp - i == 5 ? s[i + 4] : 0.0f);
						i += 4;
					}

					goto_hvcurveto = 0;
					if (i + 3 >= sp)
						break;
					stbtt__csctx_rccurve_to(c, s[i], 0, s[i + 1], s[i + 2], sp - i == 5 ? s[i + 4] : 0.0f,
						s[i + 3]);
					i += 4;
				}

				break;
			case 0x08:
				if (sp < 6)
					return 0;
				for (; i + 5 < sp; i += 6)
					stbtt__csctx_rccurve_to(c, s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);

				break;
			case 0x18:
				if (sp < 8)
					return 0;
				for (; i + 5 < sp - 2; i += 6)
					stbtt__csctx_rccurve_to(c, s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);

				if (i + 1 >= sp)
					return 0;
				stbtt__csctx_rline_to(c, s[i], s[i + 1]);
				break;
			case 0x19:
				if (sp < 8)
					return 0;
				for (; i + 1 < sp - 6; i += 2) stbtt__csctx_rline_to(c, s[i], s[i + 1]);

				if (i + 5 >= sp)
					return 0;
				stbtt__csctx_rccurve_to(c, s[i], s[i + 1], s[i + 2], s[i + 3], s[i + 4], s[i + 5]);
				break;
			case 0x1A:
			case 0x1B:
				if (sp < 4)
					return 0;
				f = (float)0.0;
				if ((sp & 1) != 0)
				{
					f = s[i];
					i++;
				}

				for (; i + 3 < sp; i += 4)
				{
					if (b0 == 0x1B)
						stbtt__csctx_rccurve_to(c, s[i], f, s[i + 1], s[i + 2], s[i + 3], (float)0.0);
					else
						stbtt__csctx_rccurve_to(c, f, s[i], s[i + 1], s[i + 2], (float)0.0, s[i + 3]);
					f = (float)0.0;
				}

				break;
			case 0x0A:
			case 0x1D:
				if (b0 == 0x0A && has_subrs == 0)
				{
					if (info->fdselect.size != 0)
						subrs = stbtt__cid_get_glyph_subrs(info, glyph_index);
					has_subrs = 1;
				}

				if (sp < 1)
					return 0;
				v = (int)s[--sp];
				if (subr_stack_height >= 10)
					return 0;
				subr_stack[subr_stack_height++] = b;
				b = stbtt__get_subr(b0 == 0x0A ? subrs : info->gsubrs, v);
				if (b.size == 0)
					return 0;
				b.cursor = 0;
				clear_stack = 0;
				break;
			case 0x0B:
				if (subr_stack_height <= 0)
					return 0;
				b = subr_stack[--subr_stack_height];
				clear_stack = 0;
				break;
			case 0x0E:
				stbtt__csctx_close_shape(c);
				return 1;
			case 0x0C:
				{
					float dx1 = 0;
					float dx2 = 0;
					float dx3 = 0;
					float dx4 = 0;
					float dx5 = 0;
					float dx6 = 0;
					float dy1 = 0;
					float dy2 = 0;
					float dy3 = 0;
					float dy4 = 0;
					float dy5 = 0;
					float dy6 = 0;
					float dx = 0;
					float dy = 0;
					int b1 = stbtt__buf_get8(&b);
					switch (b1)
					{
					case 0x22:
						if (sp < 7)
							return 0;
						dx1 = s[0];
						dx2 = s[1];
						dy2 = s[2];
						dx3 = s[3];
						dx4 = s[4];
						dx5 = s[5];
						dx6 = s[6];
						stbtt__csctx_rccurve_to(c, dx1, 0, dx2, dy2, dx3, 0);
						stbtt__csctx_rccurve_to(c, dx4, 0, dx5, -dy2, dx6, 0);
						break;
					case 0x23:
						if (sp < 13)
							return 0;
						dx1 = s[0];
						dy1 = s[1];
						dx2 = s[2];
						dy2 = s[3];
						dx3 = s[4];
						dy3 = s[5];
						dx4 = s[6];
						dy4 = s[7];
						dx5 = s[8];
						dy5 = s[9];
						dx6 = s[10];
						dy6 = s[11];
						stbtt__csctx_rccurve_to(c, dx1, dy1, dx2, dy2, dx3, dy3);
						stbtt__csctx_rccurve_to(c, dx4, dy4, dx5, dy5, dx6, dy6);
						break;
					case 0x24:
						if (sp < 9)
							return 0;
						dx1 = s[0];
						dy1 = s[1];
						dx2 = s[2];
						dy2 = s[3];
						dx3 = s[4];
						dx4 = s[5];
						dx5 = s[6];
						dy5 = s[7];
						dx6 = s[8];
						stbtt__csctx_rccurve_to(c, dx1, dy1, dx2, dy2, dx3, 0);
						stbtt__csctx_rccurve_to(c, dx4, 0, dx5, dy5, dx6, -(dy1 + dy2 + dy5));
						break;
					case 0x25:
						if (sp < 11)
							return 0;
						dx1 = s[0];
						dy1 = s[1];
						dx2 = s[2];
						dy2 = s[3];
						dx3 = s[4];
						dy3 = s[5];
						dx4 = s[6];
						dy4 = s[7];
						dx5 = s[8];
						dy5 = s[9];
						dx6 = dy6 = s[10];
						dx = dx1 + dx2 + dx3 + dx4 + dx5;
						dy = dy1 + dy2 + dy3 + dy4 + dy5;
						if (Math.Abs(dx) > Math.Abs(dy))
							dy6 = -dy;
						else
							dx6 = -dx;
						stbtt__csctx_rccurve_to(c, dx1, dy1, dx2, dy2, dx3, dy3);
						stbtt__csctx_rccurve_to(c, dx4, dy4, dx5, dy5, dx6, dy6);
						break;
					default:
						return 0;
					}
				}

				break;
			default:
				if (b0 != 255 && b0 != 28 && b0 < 32)
					return 0;
				if (b0 == 255)
				{
					f = (float)(int)stbtt__buf_get(&b, 4) / 0x10000;
				}
				else
				{
					stbtt__buf_skip(&b, -1);
					f = (short)stbtt__cff_int(&b);
				}

				if (sp >= 48)
					return 0;
				s[sp++] = f;
				clear_stack = 0;
				break;
			}

			if (clear_stack != 0)
				sp = 0;
		}

		return 0;
	}

	static stbtt__buf stbtt__cid_get_glyph_subrs(stbtt_fontinfo* info, int glyph_index)
	{
		var fdselect = info->fdselect;
		var nranges = 0;
		var start = 0;
		var end = 0;
		var v = 0;
		var fmt = 0;
		var fdselector = -1;
		var i = 0;
		stbtt__buf_seek(&fdselect, 0);
		fmt = stbtt__buf_get8(&fdselect);
		if (fmt == 0)
		{
			stbtt__buf_skip(&fdselect, glyph_index);
			fdselector = stbtt__buf_get8(&fdselect);
		}
		else if (fmt == 3)
		{
			nranges = (int)stbtt__buf_get(&fdselect, 2);
			start = (int)stbtt__buf_get(&fdselect, 2);
			for (i = 0; i < nranges; i++)
			{
				v = stbtt__buf_get8(&fdselect);
				end = (int)stbtt__buf_get(&fdselect, 2);
				if (glyph_index >= start && glyph_index < end)
				{
					fdselector = v;
					break;
				}

				start = end;
			}
		}

		if (fdselector == -1)
			stbtt__new_buf(null, 0);
		return stbtt__get_subrs(info->cff, stbtt__cff_index_get(info->fontdicts, fdselector));
	}

	static void stbtt__csctx_rmove_to(stbtt__csctx* ctx, float dx, float dy)
	{
		stbtt__csctx_close_shape(ctx);
		ctx->first_x = ctx->x = ctx->x + dx;
		ctx->first_y = ctx->y = ctx->y + dy;
		stbtt__csctx_v(ctx, STBTT_vmove, (int)ctx->x, (int)ctx->y, 0, 0, 0, 0);
	}

	static void stbtt__csctx_rline_to(stbtt__csctx* ctx, float dx, float dy)
	{
		ctx->x += dx;
		ctx->y += dy;
		stbtt__csctx_v(ctx, STBTT_vline, (int)ctx->x, (int)ctx->y, 0, 0, 0, 0);
	}

	static void stbtt__csctx_rccurve_to(stbtt__csctx* ctx, float dx1, float dy1, float dx2, float dy2,
		float dx3, float dy3)
	{
		var cx1 = ctx->x + dx1;
		var cy1 = ctx->y + dy1;
		var cx2 = cx1 + dx2;
		var cy2 = cy1 + dy2;
		ctx->x = cx2 + dx3;
		ctx->y = cy2 + dy3;
		stbtt__csctx_v(ctx, STBTT_vcubic, (int)ctx->x, (int)ctx->y, (int)cx1, (int)cy1, (int)cx2, (int)cy2);
	}

	static void stbtt__csctx_close_shape(stbtt__csctx* ctx)
	{
		if (ctx->first_x != ctx->x || ctx->first_y != ctx->y)
			stbtt__csctx_v(ctx, STBTT_vline, (int)ctx->first_x, (int)ctx->first_y, 0, 0, 0, 0);
	}

	static void stbtt__csctx_v(stbtt__csctx* c, byte type, int x, int y, int cx, int cy, int cx1, int cy1)
	{
		if (c->bounds != 0)
		{
			stbtt__track_vertex(c, x, y);
			if (type == STBTT_vcubic)
			{
				stbtt__track_vertex(c, cx, cy);
				stbtt__track_vertex(c, cx1, cy1);
			}
		}
		else
		{
			stbtt_setvertex(&c->pvertices[c->num_vertices], type, x, y, cx, cy);
			c->pvertices[c->num_vertices].cx1 = (short)cx1;
			c->pvertices[c->num_vertices].cy1 = (short)cy1;
		}

		c->num_vertices++;
	}

	static void stbtt__track_vertex(stbtt__csctx* c, int x, int y)
	{
		if (x > c->max_x || c->started == 0)
			c->max_x = x;
		if (y > c->max_y || c->started == 0)
			c->max_y = y;
		if (x < c->min_x || c->started == 0)
			c->min_x = x;
		if (y < c->min_y || c->started == 0)
			c->min_y = y;
		c->started = 1;
	}

	static void stbtt_setvertex(stbtt_vertex* v, byte type, int x, int y, int cx, int cy)
	{
		v->type = type;
		v->x = (short)x;
		v->y = (short)y;
		v->cx = (short)cx;
		v->cy = (short)cy;
	}

	static int stbtt_IsGlyphEmpty(stbtt_fontinfo* info, int glyph_index)
	{
		short numberOfContours = 0;
		var g = 0;
		if (info->cff.size != 0)
			return stbtt__GetGlyphInfoT2(info, glyph_index, null, null, null, null) == 0 ? 1 : 0;
		g = stbtt__GetGlyfOffset(info, glyph_index);
		if (g < 0)
			return 1;
		numberOfContours = ttSHORT(info->data + g);
		return numberOfContours == 0 ? 1 : 0;
	}

	static void stbtt_MakeGlyphBitmapSubpixel(stbtt_fontinfo* info, byte* output, int out_w, int out_h,
		int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int glyph)
	{
		var ix0 = 0;
		var iy0 = 0;
		stbtt_vertex* vertices;
		var num_verts = stbtt_GetGlyphShape(info, glyph, &vertices);
		var gbm = new stbtt__bitmap();
		stbtt_GetGlyphBitmapBoxSubpixel(info, glyph, scale_x, scale_y, shift_x, shift_y, &ix0, &iy0, null, null);
		gbm.pixels = output;
		gbm.w = out_w;
		gbm.h = out_h;
		gbm.stride = out_stride;
		if (gbm.w != 0 && gbm.h != 0)
			stbtt_Rasterize(&gbm, 0.35f, vertices, num_verts, scale_x, scale_y, shift_x, shift_y, ix0, iy0, 1);
		Free(vertices);
	}

	static int stbtt_GetGlyphShape(stbtt_fontinfo* info, int glyph_index, stbtt_vertex** pvertices)
	{
		if (info->cff.size == 0)
			return stbtt__GetGlyphShapeTT(info, glyph_index, pvertices);
		return stbtt__GetGlyphShapeT2(info, glyph_index, pvertices);
	}

	static void stbtt_Rasterize(stbtt__bitmap* result, float flatness_in_pixels, stbtt_vertex* vertices,
		int num_verts, float scale_x, float scale_y, float shift_x, float shift_y, int x_off, int y_off, int invert)
	{
		var scale = scale_x > scale_y ? scale_y : scale_x;
		var winding_count = 0;
		int* winding_lengths = null;
		var windings = stbtt_FlattenCurves(vertices, num_verts, flatness_in_pixels / scale, &winding_lengths,
			&winding_count);
		if (windings != null)
		{
			stbtt__rasterize(result, windings, winding_lengths, winding_count, scale_x, scale_y, shift_x, shift_y,
				x_off, y_off, (int)invert);
			Free(winding_lengths);
			Free(windings);
		}
	}

	static void stbtt__rasterize(stbtt__bitmap* result, stbtt__point* pts, int* wcount, int windings,
		float scale_x, float scale_y, float shift_x, float shift_y, int off_x, int off_y, int invert)
	{
		var y_scale_inv = invert != 0 ? -scale_y : scale_y;
		stbtt__edge* e;
		var n = 0;
		var i = 0;
		var j = 0;
		var k = 0;
		var m = 0;
		var vsubsample = 1;

		n = 0;
		for (i = 0; i < windings; ++i) n += wcount[i];

		e = (stbtt__edge*)Malloc((ulong)(sizeof(stbtt__edge) * (n + 1)));
		if (e == null)
			return;
		n = 0;
		m = 0;
		for (i = 0; i < windings; ++i)
		{
			var p = pts + m;
			m += wcount[i];
			j = wcount[i] - 1;
			for (k = 0; k < wcount[i]; j = k++)
			{
				var a = k;
				var b = j;
				if (p[j].y == p[k].y)
					continue;
				e[n].invert = 0;
				if (invert != 0 && p[j].y > p[k].y ||
					invert == 0 && p[j].y < p[k].y)
				{
					e[n].invert = 1;
					a = j;
					b = k;
				}

				e[n].x0 = p[a].x * scale_x + shift_x;
				e[n].y0 = (p[a].y * y_scale_inv + shift_y) * vsubsample;
				e[n].x1 = p[b].x * scale_x + shift_x;
				e[n].y1 = (p[b].y * y_scale_inv + shift_y) * vsubsample;
				++n;
			}
		}

		stbtt__sort_edges(e, n);
		stbtt__rasterize_sorted_edges(result, e, n, vsubsample, off_x, off_y);

		Free(e);
	}

	static void stbtt__rasterize_sorted_edges(stbtt__bitmap* result, stbtt__edge* e, int n, int vsubsample,
		int off_x, int off_y)
	{
		var hh = new stbtt__hheap();
		stbtt__active_edge* active = null;
		var y = 0;
		var j = 0;
		var i = 0;
		var scanline_data = stackalloc float[129];
		float* scanline;
		float* scanline2;
		if (result->w > 64)
			scanline = (float*)Malloc((ulong)((result->w * 2 + 1) * sizeof(float)));
		else
			scanline = scanline_data;
		scanline2 = scanline + result->w;
		y = off_y;
		e[n].y0 = (float)(off_y + result->h) + 1;
		while (j < result->h)
		{
			var scan_y_top = y + 0.0f;
			var scan_y_bottom = y + 1.0f;
			var step = &active;
			Zero(scanline, (ulong)(result->w * sizeof(float)));
			Zero(scanline2, (ulong)((result->w + 1) * sizeof(float)));
			while (*step != null)
			{
				var z = *step;
				if (z->ey <= scan_y_top)
				{
					*step = z->next;
					z->direction = 0;
					stbtt__hheap_free(&hh, z);
				}
				else
				{
					step = &(*step)->next;
				}
			}

			while (e->y0 <= scan_y_bottom)
			{
				if (e->y0 != e->y1)
				{
					var z = stbtt__new_active(&hh, e, off_x, scan_y_top);
					if (z != null)
					{
						if (j == 0 && off_y != 0)
							if (z->ey < scan_y_top)
								z->ey = scan_y_top;

						z->next = active;
						active = z;
					}
				}

				++e;
			}

			if (active != null)
				stbtt__fill_active_edges_new(scanline, scanline2 + 1, result->w, active, scan_y_top);
			{
				float sum = 0;
				for (i = 0; i < result->w; ++i)
				{
					float k = 0;
					var m = 0;
					sum += scanline2[i];
					k = scanline[i] + sum;
					k = Math.Abs(k) * 255 + 0.5f;
					m = (int)k;
					if (m > 255)
						m = 255;
					result->pixels[j * result->stride + i] = (byte)m;
				}
			}

			step = &active;
			while (*step != null)
			{
				var z = *step;
				z->fx += z->fdx;
				step = &(*step)->next;
			}

			++y;
			++j;
		}

		stbtt__hheap_cleanup(&hh);
		if (scanline != scanline_data)
			Free(scanline);
	}

	static void stbtt__fill_active_edges_new(float* scanline, float* scanline_fill, int len,
		stbtt__active_edge* e, float y_top)
	{
		var y_bottom = y_top + 1;
		while (e != null)
		{
			if (e->fdx == 0)
			{
				var x0 = e->fx;
				if (x0 < len)
				{
					if (x0 >= 0)
					{
						stbtt__handle_clipped_edge(scanline, (int)x0, e, x0, y_top, x0, y_bottom);
						stbtt__handle_clipped_edge(scanline_fill - 1, (int)x0 + 1, e, x0, y_top, x0, y_bottom);
					}
					else
					{
						stbtt__handle_clipped_edge(scanline_fill - 1, 0, e, x0, y_top, x0, y_bottom);
					}
				}
			}
			else
			{
				var x0 = e->fx;
				var dx = e->fdx;
				var xb = x0 + dx;
				float x_top = 0;
				float x_bottom = 0;
				float sy0 = 0;
				float sy1 = 0;
				var dy = e->fdy;
				if (e->sy > y_top)
				{
					x_top = x0 + dx * (e->sy - y_top);
					sy0 = e->sy;
				}
				else
				{
					x_top = x0;
					sy0 = y_top;
				}

				if (e->ey < y_bottom)
				{
					x_bottom = x0 + dx * (e->ey - y_top);
					sy1 = e->ey;
				}
				else
				{
					x_bottom = xb;
					sy1 = y_bottom;
				}

				if (x_top >= 0 && x_bottom >= 0 && x_top < len && x_bottom < len)
				{
					if ((int)x_top == (int)x_bottom)
					{
						float height = 0;
						var x = (int)x_top;
						height = (sy1 - sy0) * e->direction;
						scanline[x] += stbtt__position_trapezoid_area(height, x_top, x + 1.0f, x_bottom, x + 1.0f);
						scanline_fill[x] += height;
					}
					else
					{
						var x = 0;
						var x1 = 0;
						var x2 = 0;
						float y_crossing = 0;
						float y_final = 0;
						float step = 0;
						float sign = 0;
						float area = 0;
						if (x_top > x_bottom)
						{
							float t = 0;
							sy0 = y_bottom - (sy0 - y_top);
							sy1 = y_bottom - (sy1 - y_top);
							t = sy0;
							sy0 = sy1;
							sy1 = t;
							t = x_bottom;
							x_bottom = x_top;
							x_top = t;
							dx = -dx;
							dy = -dy;
							t = x0;
							x0 = xb;
							xb = t;
						}

						x1 = (int)x_top;
						x2 = (int)x_bottom;
						y_crossing = y_top + dy * (x1 + 1 - x0);
						y_final = y_top + dy * (x2 - x0);
						if (y_crossing > y_bottom)
							y_crossing = y_bottom;
						sign = e->direction;
						area = sign * (y_crossing - sy0);
						scanline[x1] += stbtt__sized_triangle_area(area, x1 + 1 - x_top);
						if (y_final > y_bottom)
						{
							y_final = y_bottom;
							dy = (y_final - y_crossing) / (x2 - (x1 + 1));
						}

						step = sign * dy * 1;
						for (x = x1 + 1; x < x2; ++x)
						{
							scanline[x] += area + step / 2;
							area += step;
						}

						scanline[x2] += area + sign *
							stbtt__position_trapezoid_area(sy1 - y_final, x2, x2 + 1.0f, x_bottom, x2 + 1.0f);
						scanline_fill[x2] += sign * (sy1 - sy0);
					}
				}
				else
				{
					var x = 0;
					for (x = 0; x < len; ++x)
					{
						var y0 = y_top;
						float x1 = x;
						float x2 = x + 1;
						var x3 = xb;
						var y3 = y_bottom;
						var y1 = (x - x0) / dx + y_top;
						var y2 = (x + 1 - x0) / dx + y_top;
						if (x0 < x1 && x3 > x2)
						{
							stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x1, y1);
							stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x2, y2);
							stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x3, y3);
						}
						else if (x3 < x1 && x0 > x2)
						{
							stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x2, y2);
							stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x1, y1);
							stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x3, y3);
						}
						else if (x0 < x1 && x3 > x1)
						{
							stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x1, y1);
							stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x3, y3);
						}
						else if (x3 < x1 && x0 > x1)
						{
							stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x1, y1);
							stbtt__handle_clipped_edge(scanline, x, e, x1, y1, x3, y3);
						}
						else if (x0 < x2 && x3 > x2)
						{
							stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x2, y2);
							stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x3, y3);
						}
						else if (x3 < x2 && x0 > x2)
						{
							stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x2, y2);
							stbtt__handle_clipped_edge(scanline, x, e, x2, y2, x3, y3);
						}
						else
						{
							stbtt__handle_clipped_edge(scanline, x, e, x0, y0, x3, y3);
						}
					}
				}
			}

			e = e->next;
		}
	}

	static float stbtt__position_trapezoid_area(float height, float tx0, float tx1, float bx0, float bx1)
	{
		return stbtt__sized_trapezoid_area(height, tx1 - tx0, bx1 - bx0);
	}

	static float stbtt__sized_trapezoid_area(float height, float top_width, float bottom_width)
	{
		return (top_width + bottom_width) / 2.0f * height;
	}

	static float stbtt__sized_triangle_area(float height, float width)
	{
		return height * width / 2;
	}

	static void stbtt__handle_clipped_edge(float* scanline, int x, stbtt__active_edge* e, float x0, float y0,
		float x1, float y1)
	{
		if (y0 == y1)
			return;
		if (y0 > e->ey)
			return;
		if (y1 < e->sy)
			return;
		if (y0 < e->sy)
		{
			x0 += (x1 - x0) * (e->sy - y0) / (y1 - y0);
			y0 = e->sy;
		}

		if (y1 > e->ey)
		{
			x1 += (x1 - x0) * (e->ey - y1) / (y1 - y0);
			y1 = e->ey;
		}

		if (x0 <= x && x1 <= x)
		{
			scanline[x] += e->direction * (y1 - y0);
		}
		else if (x0 >= x + 1 && x1 >= x + 1)
		{
		}
		else
		{
			scanline[x] += e->direction * (y1 - y0) * (1 - (x0 - x + (x1 - x)) / 2);
		}
	}

	static stbtt__active_edge* stbtt__new_active(stbtt__hheap* hh, stbtt__edge* e, int off_x,
		float start_point)
	{
		var z = (stbtt__active_edge*)stbtt__hheap_alloc(hh, (ulong)sizeof(stbtt__active_edge));
		var dxdy = (e->x1 - e->x0) / (e->y1 - e->y0);
		if (z == null)
			return z;
		z->fdx = dxdy;
		z->fdy = dxdy != 0.0f ? 1.0f / dxdy : 0.0f;
		z->fx = e->x0 + dxdy * (start_point - e->y0);
		z->fx -= off_x;
		z->direction = e->invert != 0 ? 1.0f : -1.0f;
		z->sy = e->y0;
		z->ey = e->y1;
		z->next = null;
		return z;
	}

	static void* stbtt__hheap_alloc(stbtt__hheap* hh, ulong size)
	{
		if (hh->first_free != null)
		{
			var p = hh->first_free;
			hh->first_free = *(void**)p;
			return p;
		}

		if (hh->num_remaining_in_head_chunk == 0)
		{
			var count = size < 32 ? 2000 : size < 128 ? 800 : 100;
			var c = (stbtt__hheap_chunk*)Malloc((ulong)sizeof(stbtt__hheap_chunk) +
															size * (ulong)count);
			if (c == null)
				return null;
			c->next = hh->head;
			hh->head = c;
			hh->num_remaining_in_head_chunk = count;
		}

		--hh->num_remaining_in_head_chunk;
		return (sbyte*)hh->head + sizeof(stbtt__hheap_chunk) + size * (ulong)hh->num_remaining_in_head_chunk;
	}

	static void stbtt__hheap_cleanup(stbtt__hheap* hh)
	{
		var c = hh->head;
		while (c != null)
		{
			var n = c->next;
			Free(c);
			c = n;
		}
	}

	static void stbtt__hheap_free(stbtt__hheap* hh, void* p)
	{
		*(void**)p = hh->first_free;
		hh->first_free = p;
	}

	static void stbtt__sort_edges(stbtt__edge* p, int n)
	{
		stbtt__sort_edges_quicksort(p, n);
		stbtt__sort_edges_ins_sort(p, n);
	}

	static void stbtt__sort_edges_ins_sort(stbtt__edge* p, int n)
	{
		var i = 0;
		var j = 0;
		for (i = 1; i < n; ++i)
		{
			var t = p[i];
			var a = &t;
			j = i;
			while (j > 0)
			{
				var b = &p[j - 1];
				var c = a->y0 < b->y0 ? 1 : 0;
				if (c == 0)
					break;
				p[j] = p[j - 1];
				--j;
			}

			if (i != j)
				p[j] = t;
		}
	}

	static void stbtt__sort_edges_quicksort(stbtt__edge* p, int n)
	{
		while (n > 12)
		{
			var t = new stbtt__edge();
			var c01 = 0;
			var c12 = 0;
			var c = 0;
			var m = 0;
			var i = 0;
			var j = 0;
			m = n >> 1;
			c01 = (&p[0])->y0 < (&p[m])->y0 ? 1 : 0;
			c12 = (&p[m])->y0 < (&p[n - 1])->y0 ? 1 : 0;
			if (c01 != c12)
			{
				var z = 0;
				c = (&p[0])->y0 < (&p[n - 1])->y0 ? 1 : 0;
				z = c == c12 ? 0 : n - 1;
				t = p[z];
				p[z] = p[m];
				p[m] = t;
			}

			t = p[0];
			p[0] = p[m];
			p[m] = t;
			i = 1;
			j = n - 1;
			for (; ; )
			{
				for (; ; ++i)
					if (!((&p[i])->y0 < (&p[0])->y0))
						break;

				for (; ; --j)
					if (!((&p[0])->y0 < (&p[j])->y0))
						break;

				if (i >= j)
					break;
				t = p[i];
				p[i] = p[j];
				p[j] = t;
				++i;
				--j;
			}

			if (j < n - i)
			{
				stbtt__sort_edges_quicksort(p, j);
				p = p + i;
				n = n - i;
			}
			else
			{
				stbtt__sort_edges_quicksort(p + i, n - i);
				n = j;
			}
		}
	}

	static stbtt__point* stbtt_FlattenCurves(stbtt_vertex* vertices, int num_verts, float objspace_flatness,
		int** contour_lengths, int* num_contours)
	{
		stbtt__point* points = null;
		var num_points = 0;
		var objspace_flatness_squared = objspace_flatness * objspace_flatness;
		var i = 0;
		var n = 0;
		var start = 0;
		var pass = 0;
		for (i = 0; i < num_verts; ++i)
			if (vertices[i].type == STBTT_vmove)
				++n;

		*num_contours = n;
		if (n == 0)
			return null;
		*contour_lengths = (int*)Malloc((ulong)(sizeof(int) * n));
		if (*contour_lengths == null)
		{
			*num_contours = 0;
			return null;
		}

		for (pass = 0; pass < 2; ++pass)
		{
			float x = 0;
			float y = 0;
			if (pass == 1)
			{
				points = (stbtt__point*)Malloc((ulong)(num_points * sizeof(stbtt__point)));
				if (points == null)
					goto error;
			}

			num_points = 0;
			n = -1;
			for (i = 0; i < num_verts; ++i)
				switch (vertices[i].type)
				{
					case STBTT_vmove:
						if (n >= 0)
							(*contour_lengths)[n] = num_points - start;
						++n;
						start = num_points;
						x = vertices[i].x;
						y = vertices[i].y;
						stbtt__add_point(points, num_points++, x, y);
						break;
					case STBTT_vline:
						x = vertices[i].x;
						y = vertices[i].y;
						stbtt__add_point(points, num_points++, x, y);
						break;
					case STBTT_vcurve:
						stbtt__tesselate_curve(points, &num_points, x, y, vertices[i].cx, vertices[i].cy,
							vertices[i].x, vertices[i].y, objspace_flatness_squared, 0);
						x = vertices[i].x;
						y = vertices[i].y;
						break;
					case STBTT_vcubic:
						stbtt__tesselate_cubic(points, &num_points, x, y, vertices[i].cx, vertices[i].cy,
							vertices[i].cx1, vertices[i].cy1, vertices[i].x, vertices[i].y,
							objspace_flatness_squared, 0);
						x = vertices[i].x;
						y = vertices[i].y;
						break;
				}

			(*contour_lengths)[n] = num_points - start;
		}

		return points;
	error:;
		Free(points);
		Free(*contour_lengths);
		*contour_lengths = null;
		*num_contours = 0;
		return null;

		static void stbtt__add_point(stbtt__point* points, int n, float x, float y)
		{
			if (points == null)
				return;
			points[n].x = x;
			points[n].y = y;
		}

		static int stbtt__tesselate_curve(stbtt__point* points, int* num_points, float x0, float y0, float x1,
			float y1, float x2, float y2, float objspace_flatness_squared, int n)
		{
			var mx = (x0 + 2 * x1 + x2) / 4;
			var my = (y0 + 2 * y1 + y2) / 4;
			var dx = (x0 + x2) / 2 - mx;
			var dy = (y0 + y2) / 2 - my;
			if (n > 16)
				return 1;
			if (dx * dx + dy * dy > objspace_flatness_squared)
			{
				stbtt__tesselate_curve(points, num_points, x0, y0, (x0 + x1) / 2.0f, (y0 + y1) / 2.0f, mx, my,
					objspace_flatness_squared, n + 1);
				stbtt__tesselate_curve(points, num_points, mx, my, (x1 + x2) / 2.0f, (y1 + y2) / 2.0f, x2, y2,
					objspace_flatness_squared, n + 1);
			}
			else
			{
				stbtt__add_point(points, *num_points, x2, y2);
				*num_points = *num_points + 1;
			}

			return 1;
		}

		static void stbtt__tesselate_cubic(stbtt__point* points, int* num_points, float x0, float y0, float x1,
			float y1, float x2, float y2, float x3, float y3, float objspace_flatness_squared, int n)
		{
			var dx0 = x1 - x0;
			var dy0 = y1 - y0;
			var dx1 = x2 - x1;
			var dy1 = y2 - y1;
			var dx2 = x3 - x2;
			var dy2 = y3 - y2;
			var dx = x3 - x0;
			var dy = y3 - y0;
			var longlen = (float)(Math.Sqrt(dx0 * dx0 + dy0 * dy0) + Math.Sqrt(dx1 * dx1 + dy1 * dy1) +
									Math.Sqrt(dx2 * dx2 + dy2 * dy2));
			var shortlen = (float)Math.Sqrt(dx * dx + dy * dy);
			var flatness_squared = longlen * longlen - shortlen * shortlen;
			if (n > 16)
				return;
			if (flatness_squared > objspace_flatness_squared)
			{
				var x01 = (x0 + x1) / 2;
				var y01 = (y0 + y1) / 2;
				var x12 = (x1 + x2) / 2;
				var y12 = (y1 + y2) / 2;
				var x23 = (x2 + x3) / 2;
				var y23 = (y2 + y3) / 2;
				var xa = (x01 + x12) / 2;
				var ya = (y01 + y12) / 2;
				var xb = (x12 + x23) / 2;
				var yb = (y12 + y23) / 2;
				var mx = (xa + xb) / 2;
				var my = (ya + yb) / 2;
				stbtt__tesselate_cubic(points, num_points, x0, y0, x01, y01, xa, ya, mx, my, objspace_flatness_squared,
					n + 1);
				stbtt__tesselate_cubic(points, num_points, mx, my, xb, yb, x23, y23, x3, y3, objspace_flatness_squared,
					n + 1);
			}
			else
			{
				stbtt__add_point(points, *num_points, x3, y3);
				*num_points = *num_points + 1;
			}
		}
	}

	static int stbtt__GetGlyphShapeT2(stbtt_fontinfo* info, int glyph_index, stbtt_vertex** pvertices)
	{
		var count_ctx = new stbtt__csctx();
		count_ctx.bounds = 1;
		var output_ctx = new stbtt__csctx();
		if (stbtt__run_charstring(info, glyph_index, &count_ctx) != 0)
		{
			*pvertices = (stbtt_vertex*)Malloc((ulong)(count_ctx.num_vertices * sizeof(stbtt_vertex)));
			output_ctx.pvertices = *pvertices;
			if (stbtt__run_charstring(info, glyph_index, &output_ctx) != 0) return output_ctx.num_vertices;
		}

		*pvertices = null;
		return 0;
	}

	static int stbtt__GetGlyphShapeTT(stbtt_fontinfo* info, int glyph_index, stbtt_vertex** pvertices)
	{
		short numberOfContours = 0;
		byte* endPtsOfContours;
		var data = info->data;
		stbtt_vertex* vertices = null;
		var num_vertices = 0;
		var g = stbtt__GetGlyfOffset(info, glyph_index);
		*pvertices = null;
		if (g < 0)
			return 0;
		numberOfContours = ttSHORT(data + g);
		if (numberOfContours > 0)
		{
			byte flags = 0;
			byte flagcount = 0;
			var ins = 0;
			var i = 0;
			var j = 0;
			var m = 0;
			var n = 0;
			var next_move = 0;
			var was_off = 0;
			var off = 0;
			var start_off = 0;
			var x = 0;
			var y = 0;
			var cx = 0;
			var cy = 0;
			var sx = 0;
			var sy = 0;
			var scx = 0;
			var scy = 0;
			byte* points;
			endPtsOfContours = data + g + 10;
			ins = ttUSHORT(data + g + 10 + numberOfContours * 2);
			points = data + g + 10 + numberOfContours * 2 + 2 + ins;
			n = 1 + ttUSHORT(endPtsOfContours + numberOfContours * 2 - 2);
			m = n + 2 * numberOfContours;
			vertices = (stbtt_vertex*)Malloc((ulong)(m * sizeof(stbtt_vertex)));
			if (vertices == null)
				return 0;
			next_move = 0;
			flagcount = 0;
			off = m - n;
			for (i = 0; i < n; ++i)
			{
				if (flagcount == 0)
				{
					flags = *points++;
					if ((flags & 8) != 0)
						flagcount = *points++;
				}
				else
				{
					--flagcount;
				}

				vertices[off + i].type = flags;
			}

			x = 0;
			for (i = 0; i < n; ++i)
			{
				flags = vertices[off + i].type;
				if ((flags & 2) != 0)
				{
					short dx = *points++;
					x += (flags & 16) != 0 ? dx : -dx;
				}
				else
				{
					if ((flags & 16) == 0)
					{
						x = x + (short)(points[0] * 256 + points[1]);
						points += 2;
					}
				}

				vertices[off + i].x = (short)x;
			}

			y = 0;
			for (i = 0; i < n; ++i)
			{
				flags = vertices[off + i].type;
				if ((flags & 4) != 0)
				{
					short dy = *points++;
					y += (flags & 32) != 0 ? dy : -dy;
				}
				else
				{
					if ((flags & 32) == 0)
					{
						y = y + (short)(points[0] * 256 + points[1]);
						points += 2;
					}
				}

				vertices[off + i].y = (short)y;
			}

			num_vertices = 0;
			sx = sy = cx = cy = scx = scy = 0;
			for (i = 0; i < n; ++i)
			{
				flags = vertices[off + i].type;
				x = vertices[off + i].x;
				y = vertices[off + i].y;
				if (next_move == i)
				{
					if (i != 0)
						num_vertices = stbtt__close_shape(vertices, num_vertices, was_off, start_off, sx, sy, scx,
							scy, cx, cy);
					start_off = (flags & 1) != 0 ? 0 : 1;
					if (start_off != 0)
					{
						scx = x;
						scy = y;
						if ((vertices[off + i + 1].type & 1) == 0)
						{
							sx = (x + vertices[off + i + 1].x) >> 1;
							sy = (y + vertices[off + i + 1].y) >> 1;
						}
						else
						{
							sx = vertices[off + i + 1].x;
							sy = vertices[off + i + 1].y;
							++i;
						}
					}
					else
					{
						sx = x;
						sy = y;
					}

					stbtt_setvertex(&vertices[num_vertices++], STBTT_vmove, sx, sy, 0, 0);
					was_off = 0;
					next_move = 1 + ttUSHORT(endPtsOfContours + j * 2);
					++j;
				}
				else
				{
					if ((flags & 1) == 0)
					{
						if (was_off != 0)
							stbtt_setvertex(&vertices[num_vertices++], STBTT_vcurve, (cx + x) >> 1, (cy + y) >> 1,
								cx, cy);
						cx = x;
						cy = y;
						was_off = 1;
					}
					else
					{
						if (was_off != 0)
							stbtt_setvertex(&vertices[num_vertices++], STBTT_vcurve, x, y, cx, cy);
						else
							stbtt_setvertex(&vertices[num_vertices++], STBTT_vline, x, y, 0, 0);
						was_off = 0;
					}
				}
			}

			num_vertices = stbtt__close_shape(vertices, num_vertices, was_off, start_off, sx, sy, scx, scy, cx, cy);
		}
		else if (numberOfContours < 0)
		{
			var more = 1;
			var comp = data + g + 10;
			num_vertices = 0;
			vertices = null;
			while (more != 0)
			{
				ushort flags = 0;
				ushort gidx = 0;
				var comp_num_verts = 0;
				var i = 0;
				stbtt_vertex* comp_verts = null;
				stbtt_vertex* tmp = null;
				var mtx = stackalloc float[] { 1, 0, 0, 1, 0, 0 };
				float m = 0;
				float n = 0;
				flags = (ushort)ttSHORT(comp);
				comp += 2;
				gidx = (ushort)ttSHORT(comp);
				comp += 2;
				if ((flags & 2) != 0)
				{
					if ((flags & 1) != 0)
					{
						mtx[4] = ttSHORT(comp);
						comp += 2;
						mtx[5] = ttSHORT(comp);
						comp += 2;
					}
					else
					{
						mtx[4] = *(sbyte*)comp;
						comp += 1;
						mtx[5] = *(sbyte*)comp;
						comp += 1;
					}
				}

				if ((flags & (1 << 3)) != 0)
				{
					mtx[0] = mtx[3] = ttSHORT(comp) / 16384.0f;
					comp += 2;
					mtx[1] = mtx[2] = 0;
				}
				else if ((flags & (1 << 6)) != 0)
				{
					mtx[0] = ttSHORT(comp) / 16384.0f;
					comp += 2;
					mtx[1] = mtx[2] = 0;
					mtx[3] = ttSHORT(comp) / 16384.0f;
					comp += 2;
				}
				else if ((flags & (1 << 7)) != 0)
				{
					mtx[0] = ttSHORT(comp) / 16384.0f;
					comp += 2;
					mtx[1] = ttSHORT(comp) / 16384.0f;
					comp += 2;
					mtx[2] = ttSHORT(comp) / 16384.0f;
					comp += 2;
					mtx[3] = ttSHORT(comp) / 16384.0f;
					comp += 2;
				}

				m = (float)Math.Sqrt(mtx[0] * mtx[0] + mtx[1] * mtx[1]);
				n = (float)Math.Sqrt(mtx[2] * mtx[2] + mtx[3] * mtx[3]);
				comp_num_verts = stbtt_GetGlyphShape(info, gidx, &comp_verts);
				if (comp_num_verts > 0)
				{
					for (i = 0; i < comp_num_verts; ++i)
					{
						var v = &comp_verts[i];
						short x = 0;
						short y = 0;
						x = v->x;
						y = v->y;
						v->x = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
						v->y = (short)(n * (mtx[1] * x + mtx[3] * y + mtx[5]));
						x = v->cx;
						y = v->cy;
						v->cx = (short)(m * (mtx[0] * x + mtx[2] * y + mtx[4]));
						v->cy = (short)(n * (mtx[1] * x + mtx[3] * y + mtx[5]));
					}

					tmp = (stbtt_vertex*)Malloc((ulong)((num_vertices + comp_num_verts) *
																	sizeof(stbtt_vertex)));
					if (tmp == null)
					{
						if (vertices != null)
							Free(vertices);
						if (comp_verts != null)
							Free(comp_verts);
						return 0;
					}

					if (num_vertices > 0 && vertices != null)
						MemCpy(tmp, vertices, (ulong)(num_vertices * sizeof(stbtt_vertex)));
					MemCpy(tmp + num_vertices, comp_verts,
						(ulong)(comp_num_verts * sizeof(stbtt_vertex)));
					if (vertices != null)
						Free(vertices);
					vertices = tmp;
					Free(comp_verts);
					num_vertices += comp_num_verts;
				}

				more = flags & (1 << 5);
			}
		}

		*pvertices = vertices;
		return num_vertices;

		static int stbtt__close_shape(stbtt_vertex* vertices, int num_vertices, int was_off, int start_off,
			int sx, int sy, int scx, int scy, int cx, int cy)
		{
			if (start_off != 0)
			{
				if (was_off != 0)
					stbtt_setvertex(&vertices[num_vertices++], STBTT_vcurve, (cx + scx) >> 1, (cy + scy) >> 1, cx, cy);
				stbtt_setvertex(&vertices[num_vertices++], STBTT_vcurve, sx, sy, scx, scy);
			}
			else
			{
				if (was_off != 0)
					stbtt_setvertex(&vertices[num_vertices++], STBTT_vcurve, sx, sy, cx, cy);
				else
					stbtt_setvertex(&vertices[num_vertices++], STBTT_vline, sx, sy, 0, 0);
			}

			return num_vertices;
		}
	}

	static uint stbtt__buf_get(stbtt__buf* b, int n)
	{
		uint v = 0;
		for (var i = 0; i < n; i++)
			v = (v << 8) | stbtt__buf_get8(b);
		return v;
	}

	static byte stbtt__buf_get8(stbtt__buf* b)
	{
		if (b->cursor >= b->size)
			return 0;
		return b->data[b->cursor++];
	}

	static byte stbtt__buf_peek8(stbtt__buf* b)
	{
		if (b->cursor >= b->size)
			return 0;
		return b->data[b->cursor];
	}

	static stbtt__buf stbtt__buf_range(stbtt__buf* b, int o, int s)
	{
		var r = stbtt__new_buf(null, 0);
		if (o < 0 || s < 0 || o > b->size || s > b->size - o)
			return r;
		r.data = b->data + o;
		r.size = s;
		return r;
	}

	static void stbtt__buf_seek(stbtt__buf* b, int o)
	{
		b->cursor = o > b->size || o < 0 ? b->size : o;
	}

	static void stbtt__buf_skip(stbtt__buf* b, int o)
	{
		stbtt__buf_seek(b, b->cursor + o);
	}

	static stbtt__buf stbtt__cff_get_index(stbtt__buf* b)
	{
		var count = 0;
		var start = 0;
		var offsize = 0;
		start = b->cursor;
		count = (int)stbtt__buf_get(b, 2);
		if (count != 0)
		{
			offsize = stbtt__buf_get8(b);
			stbtt__buf_skip(b, offsize * count);
			stbtt__buf_skip(b, (int)(stbtt__buf_get(b, offsize) - 1));
		}

		return stbtt__buf_range(b, start, b->cursor - start);
	}

	static int stbtt__cff_index_count(stbtt__buf* b)
	{
		stbtt__buf_seek(b, 0);
		return (int)stbtt__buf_get(b, 2);
	}

	static stbtt__buf stbtt__cff_index_get(stbtt__buf b, int i)
	{
		var count = 0;
		var offsize = 0;
		var start = 0;
		var end = 0;
		stbtt__buf_seek(&b, 0);
		count = (int)stbtt__buf_get(&b, 2);
		offsize = stbtt__buf_get8(&b);
		stbtt__buf_skip(&b, i * offsize);
		start = (int)stbtt__buf_get(&b, offsize);
		end = (int)stbtt__buf_get(&b, offsize);
		return stbtt__buf_range(&b, 2 + (count + 1) * offsize + start, end - start);
	}

	static uint stbtt__cff_int(stbtt__buf* b)
	{
		int b0 = stbtt__buf_get8(b);
		if (b0 >= 32 && b0 <= 246)
			return (uint)(b0 - 139);
		if (b0 >= 247 && b0 <= 250)
			return (uint)((b0 - 247) * 256 + stbtt__buf_get8(b) + 108);
		if (b0 >= 251 && b0 <= 254)
			return (uint)(-(b0 - 251) * 256 - stbtt__buf_get8(b) - 108);
		if (b0 == 28)
			return stbtt__buf_get(b, 2);
		if (b0 == 29)
			return stbtt__buf_get(b, 4);
		return 0;
	}

	static void stbtt__cff_skip_operand(stbtt__buf* b)
	{
		var v = 0;
		int b0 = stbtt__buf_peek8(b);
		if (b0 == 30)
		{
			stbtt__buf_skip(b, 1);
			while (b->cursor < b->size)
			{
				v = stbtt__buf_get8(b);
				if ((v & 0xF) == 0xF || v >> 4 == 0xF)
					break;
			}
		}
		else
		{
			stbtt__cff_int(b);
		}
	}

	static stbtt__buf stbtt__dict_get(stbtt__buf* b, int key)
	{
		stbtt__buf_seek(b, 0);
		while (b->cursor < b->size)
		{
			var start = b->cursor;
			var end = 0;
			var op = 0;
			while (stbtt__buf_peek8(b) >= 28) stbtt__cff_skip_operand(b);

			end = b->cursor;
			op = stbtt__buf_get8(b);
			if (op == 12)
				op = stbtt__buf_get8(b) | 0x100;
			if (op == key)
				return stbtt__buf_range(b, start, end - start);
		}

		return stbtt__buf_range(b, 0, 0);
	}

	static void stbtt__dict_get_ints(stbtt__buf* b, int key, int outcount, uint* _out_)
	{
		var i = 0;
		var operands = stbtt__dict_get(b, key);
		for (i = 0; i < outcount && operands.cursor < operands.size; i++) _out_[i] = stbtt__cff_int(&operands);
	}

	static stbtt__buf stbtt__get_subr(stbtt__buf idx, int n)
	{
		var count = stbtt__cff_index_count(&idx);
		var bias = 107;
		if (count >= 33900)
			bias = 32768;
		else if (count >= 1240)
			bias = 1131;
		n += bias;
		if (n < 0 || n >= count)
			return stbtt__new_buf(null, 0);
		return stbtt__cff_index_get(idx, n);
	}

	static stbtt__buf stbtt__get_subrs(stbtt__buf cff, stbtt__buf fontdict)
	{
		uint subrsoff = 0;
		var private_loc = stackalloc uint[] { 0, 0 };
		var pdict = new stbtt__buf();
		stbtt__dict_get_ints(&fontdict, 18, 2, private_loc);
		if (private_loc[1] == 0 || private_loc[0] == 0)
			return stbtt__new_buf(null, 0);
		pdict = stbtt__buf_range(&cff, (int)private_loc[1], (int)private_loc[0]);
		stbtt__dict_get_ints(&pdict, 19, 1, &subrsoff);
		if (subrsoff == 0)
			return stbtt__new_buf(null, 0);
		stbtt__buf_seek(&cff, (int)(private_loc[1] + subrsoff));
		return stbtt__cff_get_index(&cff);
	}

	static stbtt__buf stbtt__new_buf(void* p, ulong size)
	{
		var r = new stbtt__buf();
		r.data = (byte*)p;
		r.size = (int)size;
		r.cursor = 0;
		return r;
	}

	#endregion
}
