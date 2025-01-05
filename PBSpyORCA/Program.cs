using System.Runtime.InteropServices;
using System.Text;

namespace PBSpyORCA
{
    internal class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 10; i++)
            {
                string lib = "test.pbl";
                string app = "test";
                string comments = "Hucxy";
                string sra = "forward\r\nglobal transaction sqlca\r\nglobal dynamicdescriptionarea sqlda\r\nglobal dynamicstagingarea sqlsa\r\nglobal error error\r\nglobal message message\r\nend forward\r\n\r\nglobal type test from application\r\n end type\r\nglobal test test\r\n\r\non test.create\r\nappname = \"test\"\r\nmessage = create message\r\nsqlca = create transaction\r\nsqlda = create dynamicdescriptionarea\r\nsqlsa = create dynamicstagingarea\r\nerror = create error\r\nend on\r\n\r\non test.destroy\r\ndestroy( sqlca )\r\ndestroy( sqlda )\r\ndestroy( sqlsa )\r\ndestroy( error )\r\ndestroy( message )\r\nend on\r\n\r\nevent open;messagebox(\"提示\",\"你好Hucxy\")\r\nend event";
                int len = Encoding.Unicode.GetByteCount(sra);
                StringBuilder sbSra = new StringBuilder(len + 2);
                StringBuilder sbComments = new StringBuilder(512);

                var session = PBORCA_SessionOpen(125);
                var ret = PBORCA_LibraryCreate(session, lib, comments);
                ret = PBORCA_SessionSetLibraryList(session, [lib], 1);
                ret = PBORCA_CompileEntryImport(session, lib, app, PBORCA_TYPE.PBORCA_APPLICATION, comments, sra, len, IntPtr.Zero, IntPtr.Zero);
                ret = PBORCA_SessionSetCurrentAppl(session, lib, app);
                ret = PBORCA_LibraryEntryExport(session, lib, app, PBORCA_TYPE.PBORCA_APPLICATION, sbSra, len + 2);
                ret = PBORCA_LibraryDirectory(session, lib, sbComments, 512, Marshal.GetFunctionPointerForDelegate<PBORCA_CallBack>(CallBack), IntPtr.Zero);
                PBORCA_SessionClose(session);
                File.Delete(lib);
            }
            Console.ReadKey();
        }
        internal enum PBORCA_TYPE
        {
            PBORCA_APPLICATION,
            PBORCA_DATAWINDOW,
            PBORCA_FUNCTION,
            PBORCA_MENU,
            PBORCA_QUERY,
            PBORCA_STRUCTURE,
            PBORCA_USEROBJECT,
            PBORCA_WINDOW,
            PBORCA_PIPELINE,
            PBORCA_PROJECT,
            PBORCA_PROXYOBJECT,
            PBORCA_BINARY
        }

        struct PBORCA_DIRENTRY
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] szComments;
            public int lCreateTime;
            public int lEntrySize;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszEntryName;
            public PBORCA_TYPE otEntryType;
        }


        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern void PBORCA_SessionClose(IntPtr hORCASession);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr PBORCA_SessionOpen(int pbVer);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_SessionSetLibraryList(IntPtr hORCASession, string[] pLibNames, int iNumberOfLibs);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_SessionSetCurrentAppl(IntPtr hORCASession, string lpszApplLibName, string lpszApplName);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_LibraryCreate(IntPtr hORCASession, string lpszLibraryName, string lpszLibComments);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_LibraryDirectory(IntPtr hORCASession, string lpszLibName, StringBuilder lpszLibComments, int iCmntsBuffSize, IntPtr pListProc, IntPtr pUserData);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_LibraryEntryExport(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, StringBuilder lpszExportBuffer, int lExportBufferSize);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_CompileEntryImport(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, string lpszComments, string lpszEntrySyntax, int lEntrySyntaxBuffSize, IntPtr pCompErrorProc, IntPtr pUserData);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_CompileEntryImportList(IntPtr hORCASession, string[] pLibraryNames, string[] pEntryNames, PBORCA_TYPE[] otEntryTypes, string[] pComments, string[] pEntrySyntaxBuffers, int[] pEntrySyntaxBuffSizes, int iNumberOfEntries, IntPtr pCompErrorProc, IntPtr pUserData);

        public delegate void PBORCA_CallBack(IntPtr a1, IntPtr a2);


        static void CallBack(IntPtr a1, IntPtr pUserData)
        {
            var stru = Marshal.PtrToStructure<PBORCA_DIRENTRY>(a1);
            Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", stru.lpszEntryName, stru.lEntrySize, stru.lCreateTime, stru.otEntryType, Encoding.Unicode.GetString(stru.szComments)));
        }
    }
}
