﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Land.Markup.Binding
{
	public static class FuzzyHashing
	{
		public const int MIN_TEXT_LENGTH = 25;
		public const int TLSH_HASH_LENGTH = 138;

		public static byte[] GetFuzzyHash(string text)
		{
			using (var tlshObject = new TLSH.HashObject())
			{
				var textBytes = Encoding.Unicode.GetBytes(text);
				tlshObject.final(textBytes, (uint)textBytes.Length, 1);

				return tlshObject.getHash()
					.Take(TLSH_HASH_LENGTH).ToArray();
			}
		}

		public static double CompareTexts(string txt1, string txt2)
		{
			var hash1 = GetFuzzyHash(txt1);
			var hash2 = GetFuzzyHash(txt2);

			return CompareHashes(hash1, hash2);
		}

		public static double CompareHashes(byte[] hash1, byte[] hash2)
		{
			using (var tlsh1 = new TLSH.HashObject())
			using (var tlsh2 = new TLSH.HashObject())
			{
				tlsh1.fromTlshStr(hash1);
				tlsh2.fromTlshStr(hash2);

				return Math.Max(0, (300 - tlsh1.totalDiff(tlsh2)) / 300.0);
			}
		}
	}
}
