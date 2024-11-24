namespace SiegeLib.Siege;

public unsafe struct TankHeader
	{
		public static int CopyrightTextMaxLength = 100;
		public static int BuildTextMaxLength     = 100;
		public static int TitleTextMaxLength     = 100;
		public static int AuthorTextMaxLength    = 40;
		public static int RawHeaderPad           = 16; // Used for padding between end of header and start of raw data
		public static uint ExpectedVersionDs1    = MakeVersionWord(1, 0, 2); // Version used on Dungeon Siege 1 and LOA
		public static uint ExpectedVersionDs2    = MakeVersionWord(1, 1, 0); // Version used on Dungeon Siege 2
        
         // ------ Base ------
		public byte[]         ProductId;                             // (R4) ID of product (human readable) - always ProductId
		public byte[]         TankId;                                // (R4) ID of tank (human readable) - always TankId
		public uint       HeaderVersion;                         // (WW) Version of this particular header
		public uint       DirsetOffset;                          // (HO) DirSet offset
		public uint       FilesetOffset;                         // (HO) FileSet offset
		public uint       IndexSize;                             // Total size of index (header plus all dir data - used for RAW format)
		public uint       DataOffset;                            // (HO) Offset to start of data (used for RAW format)

		// ------ V1.0 Extra - Basic ------
		public byte[] ProductVersion;                        // (#12) Product version this tank was built with
		public byte[] MinimumVersion;                        // (#12) Minimum product version required to use this tank
		public TankPriority       Priority;                              // (WW) Priority that this tank is entered into the master index (Priority enum)
		public uint       Flags;                                 // (EB) Flags regarding this tank
		public byte[] CreatorId;                             // Who created this tank (creation tool will choose, not user)
		public Guid           Guid;                                  // True GUID assigned at creation time
		public uint       IndexCrc32;                            // CRC-32 of just the index (not including the header)
		public uint       DataCrc32;                             // CRC-32 of just the data
		public byte[]     UtcBuildTime;                          // When this tank was constructed (stored in UTC)
		public string       CopyrightText; // (ZST) Copyright text
		public string       BuildText;         // (ZST) Text about how this was built

		// ------ V1.0 Extra - User Info ------
		public string       TitleText;      // (ZST) Title of this tank
		public string       AuthorText;      // (ZST) Who made this tank
		public string     DescriptionText; 
		
		private static uint MakeVersionWord(uint major, uint minor, uint build)
		{
			return ((major & 0xFF) << 16) |
			       ((minor & 0xFF) << 8)  |
			       (build & 0xFF);
		}
		
		public enum TankPriority : uint
		{
			Factory = 0x0000,
			Language = 0x1000,
			Expansion = 0x2000,
			Patch = 0x3000,
			User = 0x4000
		}
	};