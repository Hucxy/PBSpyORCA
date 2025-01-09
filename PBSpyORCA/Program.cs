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
                string runPath = AppContext.BaseDirectory;
                string lib = "test.pbl";
                string app = "test";
                string comments = "你好Hucxy";
                string testSource = "forward\r\nglobal transaction sqlca\r\nglobal dynamicdescriptionarea sqlda\r\nglobal dynamicstagingarea sqlsa\r\nglobal error error\r\nglobal message message\r\nend forward\r\n\r\nglobal type test from application\r\n end type\r\nglobal test test\r\n\r\non test.create\r\nappname = \"test\"\r\nmessage = create message\r\nsqlca = create transaction\r\nsqlda = create dynamicdescriptionarea\r\nsqlsa = create dynamicstagingarea\r\nerror = create error\r\nend on\r\n\r\non test.destroy\r\ndestroy( sqlca )\r\ndestroy( sqlda )\r\ndestroy( sqlsa )\r\ndestroy( error )\r\ndestroy( message )\r\nend on\r\n\r\nevent open;messagebox(\"提示\",\"你好Hucxy\")\r\nend event";
                int testSourceLen = Encoding.Unicode.GetByteCount(testSource);
                string exePath = Path.Combine(runPath, "test.exe");
                var exeInfo = new PBORCA_EXEINFO()
                {
                    lpszCompanyName = "lpszCompanyName",
                    lpszProductName = "lpszProductName",
                    lpszDescription = "lpszDescription",
                    lpszCopyright = "lpszCopyright",
                    lpszFileVersion = "1.0.0.0",
                    lpszFileVersionNum = "2,0,0,0",
                    lpszProductVersion = "3.0.0.0",
                    lpszProductVersionNum = "4,0,0,0",
                };

                File.Delete(lib);
                var session = PBORCA_SessionOpen(125);
                if (session == IntPtr.Zero)
                {
                    Console.WriteLine("打开PBORCA会话失败");
                    Console.ReadKey();
                    return;
                }
                try
                {
                    var ret = PBORCA_LibraryCreate(session, lib, comments);
                    if (ret != 0)
                    {
                        Console.WriteLine("创建库失败");
                        Console.ReadKey();
                        return;
                    }
                    ret = PBORCA_SessionSetLibraryList(session, [lib], 1);
                    if (ret != 0)
                    {
                        Console.WriteLine("设置库列表失败");
                        Console.ReadKey();
                        return;
                    }

                    ret = PBORCA_CompileEntryImport(session, lib, app, PBORCA_TYPE.PBORCA_APPLICATION, comments, testSource, testSourceLen, Marshal.GetFunctionPointerForDelegate<PBORCA_Callback>(CompilingObjectsCallBack), IntPtr.Zero);
                    if (ret != 0)
                    {
                        Console.WriteLine("导入编译对象失败");
                        Console.ReadKey();
                        return;
                    }
                    ret = PBORCA_SessionSetCurrentAppl(session, lib, app);
                    if (ret != 0)
                    {
                        Console.WriteLine("设置当前应用失败");
                        Console.ReadKey();
                        return;
                    }

                    var sbComments = new StringBuilder(256);
                    ret = PBORCA_LibraryDirectory(session, lib, sbComments, 512, Marshal.GetFunctionPointerForDelegate<PBORCA_Callback>(LibraryDirectoryCallBack), IntPtr.Zero);
                    if (ret != 0)
                    {
                        Console.WriteLine("获取库文件对象列表失败");
                        Console.ReadKey();
                        return;
                    }
                    comments = sbComments.ToString();

                    var testSourceInfo = new PBORCA_ENTRYINFO();
                    ret = PBORCA_LibraryEntryInformation(session, lib, app, PBORCA_TYPE.PBORCA_APPLICATION, ref testSourceInfo);
                    if (ret != 0)
                    {
                        Console.WriteLine("获取对象信息失败");
                        Console.ReadKey();
                        return;
                    }

                    var testSourceSB = new StringBuilder(testSourceInfo.lSourceSize + 2);
                    ret = PBORCA_LibraryEntryExport(session, lib, app, PBORCA_TYPE.PBORCA_APPLICATION, testSourceSB, testSourceInfo.lSourceSize + 2);
                    if (ret != 0)
                    {
                        Console.WriteLine("导出对象失败");
                        Console.ReadKey();
                        return;
                    }
                    testSource = testSourceSB.ToString();

                    ret = PBORCA_SetExeInfo(session, ref exeInfo);
                    if (ret != 0)
                    {
                        Console.WriteLine("设置编译信息失败");
                        Console.ReadKey();
                        return;
                    }

                    ret = PBORCA_ExecutableCreate(session, exePath, null, null, Marshal.GetFunctionPointerForDelegate<PBORCA_Callback>(ExecutableCreateCallBack), IntPtr.Zero, [0], 1, 0);
                    if (ret != 0)
                    {
                        Console.WriteLine("创建可执行文件失败");
                        Console.ReadKey();
                        return;
                    }
                }
                finally
                {
                    PBORCA_SessionClose(session);
                }
            }
            Console.WriteLine("执行完成，按任意键退出...");
            Console.ReadKey();
        }
        static void LibraryDirectoryCallBack(IntPtr pPBORCA_DIRENTRY, IntPtr pUserData)
        {
            var DIRENTRY = Marshal.PtrToStructure<PBORCA_DIRENTRY>(pPBORCA_DIRENTRY);
            Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", DIRENTRY.lpszEntryName, DIRENTRY.lEntrySize, DIRENTRY.lCreateTime, DIRENTRY.otEntryType, Encoding.Unicode.GetString(DIRENTRY.szComments)));
        }

        static void CompilingObjectsCallBack(IntPtr pPBORCA_COMPERR, IntPtr pUserData)
        {
            var COMPERR = Marshal.PtrToStructure<PBORCA_COMPERR>(pPBORCA_COMPERR);
            Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", COMPERR.iLevel, COMPERR.lpszMessageNumber, COMPERR.lpszMessageText, COMPERR.iColumnNumber, COMPERR.iLineNumber));
        }

        static void ExecutableCreateCallBack(IntPtr pPBORCA_LINKERR, IntPtr pUserData)
        {
            var LINKERR = Marshal.PtrToStructure<PBORCA_LINKERR>(pPBORCA_LINKERR);
            Console.WriteLine(LINKERR.lpszMessageText);
        }

        public enum PBORCA_TYPE
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PBORCA_DIRENTRY
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] szComments;
            public int lCreateTime;
            public int lEntrySize;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszEntryName;
            public PBORCA_TYPE otEntryType;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PBORCA_ENTRYINFO
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] szComments;
            public int lCreateTime;
            public int lObjectSize;
            public int lSourceSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PBORCA_COMPERR
        {
            public int iLevel;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszMessageNumber;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszMessageText;
            public uint iColumnNumber;
            public uint iLineNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PBORCA_EXEINFO
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszCompanyName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszProductName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszDescription;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszCopyright;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszFileVersion;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszFileVersionNum;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszProductVersion;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszProductVersionNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PBORCA_LINKERR
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszMessageText;
        }

        public delegate void PBORCA_Callback(IntPtr pStruct, IntPtr pUserData);

        #region managing the ORCA session
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr PBORCA_SessionOpen(int pbVer);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern void PBORCA_SessionClose(IntPtr hORCASession);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_SessionSetLibraryList(IntPtr hORCASession, string[] pLibNames, int iNumberOfLibs);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_SessionSetCurrentAppl(IntPtr hORCASession, string lpszApplLibName, string lpszApplName);
        #endregion

        #region managing PowerBuilder libraries
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_LibraryCreate(IntPtr hORCASession, string lpszLibraryName, string lpszLibComments);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_LibraryDirectory(IntPtr hORCASession, string lpszLibName, StringBuilder lpszLibComments, int iCmntsBuffSize, IntPtr pListProc, IntPtr pUserData);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_LibraryEntryInformation(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, ref PBORCA_ENTRYINFO pEntryInformationBlock);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_LibraryEntryExport(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, StringBuilder lpszExportBuffer, int lExportBufferSize);
        #endregion

        #region importing and compiling PowerBuilder objects
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_CompileEntryImport(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, string lpszComments, string lpszEntrySyntax, int lEntrySyntaxBuffSize, IntPtr pCompErrorProc, IntPtr pUserData);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_CompileEntryImportList(IntPtr hORCASession, string[] pLibraryNames, string[] pEntryNames, PBORCA_TYPE[] otEntryTypes, string[] pComments, string[] pEntrySyntaxBuffers, int[] pEntrySyntaxBuffSizes, int iNumberOfEntries, IntPtr pCompErrorProc, IntPtr pUserData);
        #endregion

        #region creating executables and dynamic libraries
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_SetExeInfo(IntPtr hORCASession, ref PBORCA_EXEINFO pExeInfo);
        [DllImport("PBSpy.dll", CharSet = CharSet.Unicode)]
        public static extern int PBORCA_ExecutableCreate(IntPtr hORCASession, string lpszExeName, string? lpszIconName, string? lpszPBRName, IntPtr pLinkErrProc, IntPtr pUserData, int[] iPBDFlags, int iNumberOfPBDFlags, int lFlags);
        #endregion
    }
}
