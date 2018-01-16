using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace VSOExp
{
	public sealed class ClassMember
	{
		public string Name = null;
		public string Type = null;
		public int Offset = -1;
		public int Size = -1;
	};

	public sealed class ClassLayout
	{
		public string Name;
		public int Size;

		public int TotalAlignmentEntries = 0;
		public int TotalAlignmentPadding = 0;

		public List<ClassMember> Members = new List<ClassMember>();
	};

	public sealed class LayoutLoaderOutput
	{
		public LayoutLoaderOutput(Dictionary<string, ClassLayout> Data)
		{
			Classes = Data;
		}
		public Dictionary<string, ClassLayout> Classes;
	};

	sealed class LayoutLoader
	{
		static readonly Regex ClassLayoutStartRegex = new Regex(
			@"[\W]*class ([^ ]+)[\W]+size\(([\d]+)\):"
			, RegexOptions.Compiled | RegexOptions.Singleline);

		//  36  | control
		//      | <alignment member> (size=3)
		static readonly Regex ExtractClassMember = new Regex(
			@"[\W]*([\d]+)[\W]*\|(.*)$"
			, RegexOptions.Compiled | RegexOptions.Singleline);
		static readonly Regex ExtractAlignmentMember = new Regex(
			@"alignment member> \(size=([\d]+)\)"
			, RegexOptions.Compiled | RegexOptions.Singleline);

		internal StringBuilder UnDecoratedSymbol = new StringBuilder(255);

		Dictionary<string, ClassLayout> Classes = new Dictionary<string, ClassLayout>(4096);
		public LayoutLoaderOutput Result
		{
			get { return new LayoutLoaderOutput(Classes); }
		}

		public bool ParseFromFile(string filename)
		{
			const int BufferSize = 1024 * 4;
			using (var fileStream = File.OpenRead(filename))
			using (var InputStreamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
			{
				string line;
				while ((line = InputStreamReader.ReadLine()) != null)
				{
					line = line.Substring(line.IndexOf('>') + 1);
					line = line.Trim();
					if (line.Length == 0)
						continue;

					var startMatch = ClassLayoutStartRegex.Match(line);
					if (startMatch.Success)
					{
						string decoratedSymbol = startMatch.Groups[1].ToString();

						Dbghelp.UnDecorateSymbolName(decoratedSymbol, UnDecoratedSymbol, UnDecoratedSymbol.Capacity, Dbghelp.UnDecorateFlags.UNDNAME_COMPLETE);
						string classSymbol = UnDecoratedSymbol.ToString();

						int classSize = int.Parse(startMatch.Groups[2].ToString());

						var classResult = new ClassLayout();

						// only save the result if its the first time we have seen the name
						if (!Classes.ContainsKey(classSymbol))
						{
							classResult.Name = classSymbol;
							classResult.Size = classSize;

							Classes.Add(classSymbol, classResult);
						}

						// consume this layout descriptor
						ParseClass(classResult, InputStreamReader);
					}
				}
			}

			return true;
		}

		void ParseClass(ClassLayout ClassResult, StreamReader InputStreamReader)
		{
			ClassMember previousMemberAlignSkip = null;
			ClassMember previousMember = null;

			string line;
			while ((line = InputStreamReader.ReadLine()) != null)
			{
				line = line.Substring(line.IndexOf('>') + 1);
				line = line.Trim();

				if (line.Length == 0)
					break;

				#region ExtractClassMember
				var classMemberMatch = ExtractClassMember.Match(line);
				if (classMemberMatch.Success)
				{
					string memberOffsetString = classMemberMatch.Groups[1].ToString();
					int memberOffset = int.Parse(memberOffsetString);

					string memberName = classMemberMatch.Groups[2].ToString().Trim();
					string memberType = null;

					if (memberName.IndexOf(' ') > 0)
					{
						string[] memberTypeAndName = memberName.Split(' ');
						string decMemberType = memberTypeAndName[0].Trim();
						memberName = memberTypeAndName[1].Trim();

						if (Dbghelp.UnDecorateSymbolName(decMemberType, UnDecoratedSymbol, UnDecoratedSymbol.Capacity, Dbghelp.UnDecorateFlags.UNDNAME_COMPLETE) > 0)
						{
							memberType = UnDecoratedSymbol.ToString();
						}
						else
						{
							memberType = decMemberType;
						}
					}

					if (previousMember != null)
					{
						if (previousMemberAlignSkip != null)
						{
							previousMemberAlignSkip.Size = memberOffset - (previousMemberAlignSkip.Offset + previousMember.Size);
						}
						else
						{
							previousMember.Size = memberOffset - previousMember.Offset;
						}
					}

					var newMember = new ClassMember()
					{
						Type = memberType,
						Name = memberName,
						Offset = memberOffset,
					};

					previousMember = newMember;
					previousMemberAlignSkip = null;

					ClassResult.Members.Add(newMember);
					continue;
				}
				#endregion

				#region ExtractAlignmentMember
				var AlignmentMemberMatch = ExtractAlignmentMember.Match(line);
				if (AlignmentMemberMatch.Success)
				{
					string memberSizeString = AlignmentMemberMatch.Groups[1].ToString();
					int memberSize = int.Parse(memberSizeString);

					var newMember = new ClassMember()
					{
						Size = memberSize,
					};

					previousMemberAlignSkip = previousMember;
					previousMember = newMember;

					ClassResult.TotalAlignmentEntries++;
					ClassResult.TotalAlignmentPadding += memberSize;

					ClassResult.Members.Add(newMember);
					continue;
				}
				#endregion
			}

			if (previousMember != null && previousMember.Name != null)
			{
				previousMember.Size = ClassResult.Size - previousMember.Offset;
			}
		}
	};
}
