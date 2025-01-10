//最新全量代码在以下链接获取
//https://gitee.com/hucxy/pbspy-orca
//https://github.com/Hucxy/PBSpyORCA
using System.Runtime.InteropServices;
using System.Text;

namespace PBSpyORCA
{
    internal class Program
    {
#if ANSI
        const CharSet charSet = CharSet.Ansi;
#else
        const CharSet charSet = CharSet.Unicode;
#endif
        const string orcaDll = "PBSpy.dll";
        //自己测试pborca的时候注意PB版本的编码
        //const string orcaDll = "pborc90.dll";
        static void Main(/*string[] args*/)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = charSet == CharSet.Ansi ? Encoding.GetEncoding("GBK") : Encoding.Unicode;
            int charLen = charSet == CharSet.Ansi ? 1 : 2;
            int[] pbVersions;
#pragma warning disable CS0162 // 检测到无法访问的代码
            if (orcaDll == "PBSpy.dll")
            {
                pbVersions = charSet == CharSet.Ansi ? [50, 60, 70, 80, 90] : [100, 105, 110, 115, 120, 125];
            }
            else
            {
                pbVersions = [int.Parse(orcaDll[5..^4])];
            }
#pragma warning restore CS0162 // 检测到无法访问的代码
            foreach (var pbVersion in pbVersions)
            {
                string runPath = AppContext.BaseDirectory;
                string lib = string.Format("test{0}.pbl", pbVersion);
                string app = "test";
                string comments = "注释comments";
                string testSource = "forward\r\nglobal transaction sqlca\r\nglobal dynamicdescriptionarea sqlda\r\nglobal dynamicstagingarea sqlsa\r\nglobal error error\r\nglobal message message\r\nend forward\r\n\r\nglobal type test from application\r\n end type\r\nglobal test test\r\n\r\non test.create\r\nappname = \"test\"\r\nmessage = create message\r\nsqlca = create transaction\r\nsqlda = create dynamicdescriptionarea\r\nsqlsa = create dynamicstagingarea\r\nerror = create error\r\nend on\r\n\r\non test.destroy\r\ndestroy( sqlca )\r\ndestroy( sqlda )\r\ndestroy( sqlsa )\r\ndestroy( error )\r\ndestroy( message )\r\nend on\r\n\r\nevent open;messagebox(\"提示\",\"你好Hucxy\")\r\nend event";
                int testSourceLen = encoding.GetByteCount(testSource);
                string exePath = Path.Combine(runPath, string.Format("test{0}.exe", pbVersion));
                var exeInfo = new PBORCA_EXEINFO()
                {
                    lpszCompanyName = "公司名称CompanyName",
                    lpszProductName = "产品名称ProductName",
                    lpszDescription = "文件说明Description",
                    lpszCopyright = "版权Copyright",
                    lpszFileVersion = "1.0.0.0",
                    lpszFileVersionNum = "2,0,0,0",
                    lpszProductVersion = "3.0.0.0",
                    lpszProductVersionNum = "4,0,0,0",
                };

                var session = PBORCA_SessionOpen(pbVersion);
                if (session == IntPtr.Zero)
                {
                    Console.WriteLine("打开PBORCA会话失败");
                    Console.ReadKey();
                    return;
                }
                try
                {
                    int ret;
                    if (orcaDll == "PBSpy.dll")
                    {
                        //因为pborca没有默认应用不支持导入编译对象，所以测试的话先用PBSpyORCA跑一遍，等测试pborca跑的时候不删除pbl（里面已经有了默认应用）
#pragma warning disable CS0162 // 检测到无法访问的代码
                        File.Delete(lib);
                        ret = PBORCA_LibraryCreate(session, lib, comments);
                        if (ret != 0)
                        {
                            Console.WriteLine("创建库失败");
                            Console.ReadKey();
                            return;
                        }
#pragma warning restore CS0162 // 检测到无法访问的代码
                    }

                    ret = PBORCA_SessionSetLibraryList(session, [lib], 1);
                    if (ret != 0)
                    {
                        Console.WriteLine("设置库列表失败");
                        Console.ReadKey();
                        return;
                    }

                    //ret = PBORCA_SessionSetCurrentAppl(session, lib, app);
                    //if (ret != 0)
                    //{
                    //    Console.WriteLine("设置当前应用失败");
                    //    Console.ReadKey();
                    //    return;
                    //}
                    //下面的调用等价于上面的，至于为什么，需要自己思考
                    ret = PBORCA_SessionSetCurrentAppl(new SessionSetCurrentAppl() { hORCASession = session, lpszApplLibName = lib, lpszApplName = app });
                    if (ret != 0)
                    {
                        Console.WriteLine("设置当前应用失败");
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


                    var sbComments = new StringBuilder(256);
                    ret = PBORCA_LibraryDirectory(session, lib, sbComments, 256 * charLen, Marshal.GetFunctionPointerForDelegate<PBORCA_Callback>(LibraryDirectoryCallBack), IntPtr.Zero);
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

                    var testSourceBytes = new byte[testSourceInfo.lSourceSize + charLen];
                    ret = PBORCA_LibraryEntryExport(session, lib, app, PBORCA_TYPE.PBORCA_APPLICATION, testSourceBytes, testSourceBytes.Length);
                    if (ret != 0)
                    {
                        Console.WriteLine("导出对象失败");
                        Console.ReadKey();
                        return;
                    }
                    testSource = encoding.GetString(testSourceBytes);

                    if (orcaDll == "PBSpy.dll" || pbVersion >= 90)
                    {
                        //pborca只有PB9以上版本才可以设置exe信息，PBSpyORCA虽然也是一样，但是调用不会报错，只是没效果
                        ret = PBORCA_SetExeInfo(session, ref exeInfo);
                        if (ret != 0)
                        {
                            Console.WriteLine("设置编译信息失败");
                            Console.ReadKey();
                            return;
                        }
                    }
                    string? icoPath = null;
                    if (orcaDll != "PBSpy.dll")
                    {
#pragma warning disable CS0162 // 检测到无法访问的代码
                        File.Delete(exePath);//pborca如果exe文件已存在，则无法创建，所以先删除，PBSpyORCA内部会自己判断，如果存在则先删除
                        icoPath = Path.Combine(runPath, "pbdwedit.ico");//pborca没有图标不让创建exe，PBSpyORCA则没有这个限制
#pragma warning restore CS0162 // 检测到无法访问的代码
                    }
                    ret = PBORCA_ExecutableCreate(session, exePath, icoPath, null, Marshal.GetFunctionPointerForDelegate<PBORCA_Callback>(ExecutableCreateCallBack), IntPtr.Zero, [0], 1, 0);
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
            Console.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", DIRENTRY.lpszEntryName, DIRENTRY.lEntrySize, DIRENTRY.lCreateTime, DIRENTRY.otEntryType, DIRENTRY.szComments));
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

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = charSet)]
        public struct PBORCA_DIRENTRY
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szComments;
            public int lCreateTime;
            public int lEntrySize;
            public string lpszEntryName;
            public PBORCA_TYPE otEntryType;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = charSet)]
        public struct PBORCA_ENTRYINFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szComments;
            public int lCreateTime;
            public int lObjectSize;
            public int lSourceSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = charSet)]
        public struct PBORCA_COMPERR
        {
            public int iLevel;
            public string lpszMessageNumber;
            public string lpszMessageText;
            public uint iColumnNumber;
            public uint iLineNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = charSet)]
        public struct PBORCA_EXEINFO
        {
            public string lpszCompanyName;
            public string lpszProductName;
            public string lpszDescription;
            public string lpszCopyright;
            public string lpszFileVersion;
            public string lpszFileVersionNum;
            public string lpszProductVersion;
            public string lpszProductVersionNum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = charSet)]
        public struct PBORCA_LINKERR
        {
            public string lpszMessageText;
        }
        public delegate void PBORCA_Callback(IntPtr pStruct, IntPtr pUserData);
#pragma warning disable SYSLIB1054 // 使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码
        #region managing the ORCA session
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern IntPtr PBORCA_SessionOpen(int pbVer);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern void PBORCA_SessionClose(IntPtr hORCASession);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_SessionSetLibraryList(IntPtr hORCASession, string[] pLibNames, int iNumberOfLibs);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_SessionSetCurrentAppl(IntPtr hORCASession, string lpszApplLibName, string lpszApplName);
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = charSet)]
        public struct SessionSetCurrentAppl
        {
            public IntPtr hORCASession;
            public string lpszApplLibName;
            public string lpszApplName;
        }
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_SessionSetCurrentAppl(SessionSetCurrentAppl stru);
        #endregion

        #region managing PowerBuilder libraries
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_LibraryCreate(IntPtr hORCASession, string lpszLibraryName, string lpszLibComments);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_LibraryDirectory(IntPtr hORCASession, string lpszLibName, StringBuilder lpszLibComments, int iCmntsBuffSize, IntPtr pListProc, IntPtr pUserData);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_LibraryEntryInformation(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, ref PBORCA_ENTRYINFO pEntryInformationBlock);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_LibraryEntryExport(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, byte[] lpszExportBuffer, int lExportBufferSize);
        #endregion

        #region importing and compiling PowerBuilder objects
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_CompileEntryImport(IntPtr hORCASession, string lpszLibraryName, string lpszEntryName, PBORCA_TYPE otEntryType, string lpszComments, string lpszEntrySyntax, int lEntrySyntaxBuffSize, IntPtr pCompErrorProc, IntPtr pUserData);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_CompileEntryImportList(IntPtr hORCASession, string[] pLibraryNames, string[] pEntryNames, PBORCA_TYPE[] otEntryTypes, string[] pComments, string[] pEntrySyntaxBuffers, int[] pEntrySyntaxBuffSizes, int iNumberOfEntries, IntPtr pCompErrorProc, IntPtr pUserData);
        #endregion

        #region creating executables and dynamic libraries
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_SetExeInfo(IntPtr hORCASession, ref PBORCA_EXEINFO pExeInfo);
        [DllImport(orcaDll, CharSet = charSet)]
        public static extern int PBORCA_ExecutableCreate(IntPtr hORCASession, string lpszExeName, string? lpszIconName, string? lpszPBRName, IntPtr pLinkErrProc, IntPtr pUserData, int[] iPBDFlags, int iNumberOfPBDFlags, int lFlags);
        #endregion
#pragma warning restore SYSLIB1054 // 使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码
    }
}
