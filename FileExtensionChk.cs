using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CheckFileExtension
{
    public static class FileExtensionChk
    {
        public static string whiteList = string.Empty;
        public static string blackList = string.Empty;

        public static string[] whiteAry = new string[] { };
        public static string[] blackAry = new string[] { };

        /// <summary>
        /// 확장자 검사 버튼 클릭
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool CheckFileExtension(string filePath)
        {
            Form1.WriteRichTextBoxText($"FullName: {filePath}");

            whiteList = Form1.whiteList;
            blackList = Form1.blackList;

            whiteAry = whiteList.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            blackAry = blackList.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            string fileExt = Path.GetExtension(filePath).ToLower();

            /// 1. 파일 확장자 whiteList 포함 및 blackList 불포함 확인
            if (!CheckExtension(fileExt))
                return false;

            /// 2. .zip 파일 내부 확인
            if (".zip".Equals(fileExt))
            {
                return CheckZipFileExtension(filePath);
            }

            return true;
        }

        
        /// <summary>
        /// 1. 확장자 whiteList 포함 및 blackList 불포함 확인
        /// </summary>
        /// <param name="fileExt"></param>
        /// <returns></returns>
        public static bool CheckExtension(string fileExt)
        {
            //whiteList Check
            if (whiteAry.Count(s => s.Equals("*")) == 0 && whiteAry.Count(s => s.ToLower().Equals(fileExt)) == 0)
            {
                return false;
            }
            //blackList Check
            if (blackAry.Count(s => s.Equals("*")) > 0 || blackAry.Count(s => s.ToLower().Equals(fileExt)) > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 2. .zip 파일 내부 확인
        /// </summary>
        /// <param name="zipPath"></param>
        /// <returns></returns>
        public static bool CheckZipFileExtension(string zipPath)
        {
            string extractPath = string.Empty;
            string destinationPath = string.Empty;
            string extractZipPath = Path.Combine(Path.GetDirectoryName(zipPath), Path.GetFileNameWithoutExtension(zipPath));

            try {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    //파일 내부 요소
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fileName = entry.FullName;
                        string fileExt = Path.GetExtension(fileName).ToLower();

                        Form1.WriteRichTextBoxText($"FullName: {zipPath}\\{fileName}");

                        //1. 폴더는 마지막에 "/" 문자 (생략)
                        if (entry.FullName.EndsWith(@"/", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        //2. 파일 확장자 whiteList 포함 및 blackList 불포함 확인
                        if (!CheckExtension(fileExt))
                            return false;

                        //3. .zip 파일 항목 추출
                        if (".zip".Equals(fileExt.ToLower()))
                        {
                            #region .zip이라면 압축해제
                            extractPath = Path.Combine(extractZipPath, Path.GetDirectoryName(fileName));
                            destinationPath = Path.Combine(extractPath, entry.Name);

                            //압축 해제 임시 폴더 생성 막는 파일 삭제
                            //임시 폴더와 동일한 이름의 파일이 있으면 오류 발생
                            if (File.Exists(extractPath))
                                File.Delete(extractPath);

                            //압축 해제 임시 폴더 생성
                            if (!Directory.Exists(extractPath))
                                Directory.CreateDirectory(extractPath);

                            //압축 파일 삭제
                            if (File.Exists(destinationPath))
                                File.Delete(destinationPath);

                            //항목 추출
                            entry.ExtractToFile(destinationPath);

                            //.zip 파일 내부 확인 (재귀)
                            bool rtnVal = CheckZipFileExtension(destinationPath);
                            
                            //검사 종료 후 폴더 및 파일 삭제
                            if (Directory.Exists(extractZipPath))
                                Directory.Delete(extractZipPath, true);

                            if (!rtnVal)
                                return false;
                            #endregion
                        }
                    }
                }
            }catch(Exception e)
            {
                Form1.WriteRichTextBoxText(e.ToString());

                //검사 종료 후 폴더 및 파일 삭제
                if (Directory.Exists(extractZipPath))
                    Directory.Delete(extractZipPath, true);
            }

            return true;
        }

        /// <summary>
        /// 변조 검사 버튼 클릭
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool CheckFileExtensionFromSignatures(string filePath)
        {
            Form1.WriteRichTextBoxText($"FullName: {filePath}");

            string fileExt = Path.GetExtension(filePath).ToLower();

            //1. .text MimeType 검사
            if (".txt|.ini|.csv|.log".Contains(fileExt))            
            {
                return GetMimeTypeFromFile(filePath).Count > 0;
            }

            //2. 시그니쳐와 확장자가 매치되는지 검사
            List<string> Signatures = ReadSignaturesFromStream(filePath);
            List<string> Extensions = MatchExtensionsFromSignatures(Signatures);

            if (Extensions.Count(s => s.ToLower().Equals(fileExt)) == 0)
            {
                return false;
            }

            //3. .zip과 같은 Signatures [50-4B-03-04] == [.DOCX, .PPTX, .XLSX, .XPS, .ZIP] 내부검사
            if (Signatures.Count(s => s.Equals("50-4B-03-04")) > 0)
            {
                #region .zip일 가능성이 있는 파일 검사
                if (".zip".Equals(fileExt))
                {   
                    using (ZipArchive archive = ZipFile.OpenRead(filePath))
                    {   //.zip 이라면 Entries[0] 값 ![Content_Types].xml 
                        if (archive.Entries[0].ToString().Contains("[Content_Types].xml"))
                            return false;
                        // 4. .zip 파일 내부 확인
                        return CheckZipFileExtensionFromSignatures(filePath);

                    }
                }
                else
                {
                    using (ZipArchive archive = ZipFile.OpenRead(filePath))
                    {   //.zip 아니라면 Entries[0] 값 [Content_Types].xml
                        if (!archive.Entries[0].ToString().Contains("[Content_Types].xml"))
                            return false;
                    }
                }
                #endregion 
            }

            return true;
        }
        
        ///.zip 파일 내부 확인
        public static bool CheckZipFileExtensionFromSignatures(string zipPath)
        {
            string extractPath = string.Empty;
            string destinationPath = string.Empty;
            string extractZipPath = Path.Combine(Path.GetDirectoryName(zipPath), Path.GetFileNameWithoutExtension(zipPath));

            try {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    //파일 내부 요소
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fileName = entry.FullName;
                        string fileExt = Path.GetExtension(fileName).ToLower();

                        Form1.WriteRichTextBoxText($"FullName: {zipPath}\\{fileName}");
                                                
                        using (Stream source = entry.Open())
                        {
                            //1. 폴더는 마지막에 "/" 문자 (생략)
                            if (entry.FullName.EndsWith(@"/", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            //2. .text MimeType 검사
                            if (".txt|.ini|.csv|.log".Contains(fileExt))
                            {
                                if(GetMimeTypeFromFile(source).Count > 0)
                                    continue;
                                return false;
                            }

                            List<string> Signatures = ReadSignaturesFromStream(source);
                            List<string> Extensions = MatchExtensionsFromSignatures(Signatures);
                                                        
                            //3. 시그니쳐와 확장자가 매치되는지 검사
                            if (Extensions.Count(s => s.ToLower().Equals(fileExt)) == 0)
                                return false;

                            //4. .zip과 같은 Signatures [50-4B-03-04] == [.DOCX, .PPTX, .XLSX, .XPS, .ZIP] 내부검사
                            if (Signatures.Count(s => s.Equals("50-4B-03-04")) > 0)
                            {
                                #region .zip일 가능성이 있는 파일값 검사
                                extractPath = Path.Combine(extractZipPath, Path.GetDirectoryName(fileName));
                                destinationPath = Path.Combine(extractPath, entry.Name);

                                //압축 해제 임시 폴더 생성 막는 파일 삭제
                                //임시 폴더와 동일한 이름의 파일이 있으면 오류 발생
                                if (File.Exists(extractPath))
                                    File.Delete(extractPath);

                                //압축 해제 임시 폴더 생성
                                if (!Directory.Exists(extractPath))
                                    Directory.CreateDirectory(extractPath);

                                //압축 파일 삭제
                                if (File.Exists(destinationPath))
                                    File.Delete(destinationPath);

                                if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                    entry.ExtractToFile(destinationPath);

                                // 5. .zip 파일 내부 확인(재귀)
                                bool rtnVal = CheckFileExtensionFromSignatures(destinationPath);

                                //검사 종료 후 폴더 및 파일 삭제
                                if (Directory.Exists(extractZipPath))
                                    Directory.Delete(extractZipPath, true);

                                if (!rtnVal)
                                    return false;
                                continue;
                                #endregion
                            }
                        }
                    }
                }
            }catch(Exception e)
            {
                Form1.WriteRichTextBoxText(e.ToString());

                //검사 종료 후 폴더 및 파일 삭제
                if (Directory.Exists(extractZipPath))
                    Directory.Delete(extractZipPath, true);
                return false;
            }

            return true;
        }
         
        
        public static List<string> GetMimeTypeFromFile(string filename)
        {
            List<string> extensions = null;

            using (Stream stream = new FileStream(filename, FileMode.Open))
            {
                extensions = GetMimeTypeFromFile(stream);
            }

            return extensions;
        }

        public static List<string> GetMimeTypeFromFile(Stream stream)
        {
            List<string> extensions = null;
            System.UInt32 mimetype;
            string mine = string.Empty;

            byte[] buffer = null;

            if (stream.Length >= 256)
            {
                buffer = new byte[256];
                stream.Read(buffer, 0, 256);
            }
            else
            {
                buffer = new byte[(int)stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
            }

            try
            {
                FindMimeFromData(0, null, buffer, (uint)buffer.Length, null, 0, out mimetype, 0);

                System.IntPtr mimeTypePtr = new IntPtr(mimetype);

                string _mine = Marshal.PtrToStringUni(mimeTypePtr);
                Marshal.FreeCoTaskMem(mimeTypePtr);
                Form1.WriteRichTextBoxText("_mine :" + _mine);
                extensions = MIMETypesDictionary.Where(t => t.Value == _mine).Select(t => t.Key.Remove(0, 1).ToUpper()).ToList();
                mine = _mine;
            }
            catch (Exception e)
            {
                Form1.WriteRichTextBoxText(e.ToString());
                mine = null;
            }

            return extensions;
        }

        public static List<string> ReadSignaturesFromStream(string filepath)
        {
            List<string> byteArr = null;

            try
            {
                using (Stream source = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    byteArr = ReadSignaturesFromStream(source);
                }
            }
            catch (Exception e)
            {
                Form1.WriteRichTextBoxText(e.ToString());
            }

            return byteArr;
        }

        public static List<string> ReadSignaturesFromStream(Stream source)
        {
            List<string> byteArr = null;

            try
            {
                byteArr = new List<string>();
                byte[] bytes9 = new byte[9];
                int n9 = source.Read(bytes9, 0, 9);

                string t_maginNumber = BitConverter.ToString(bytes9);

                byteArr.Add(t_maginNumber);                     // 00-00-00-00-00-00-00-00-00   9Bytes
                byteArr.Add(t_maginNumber.Substring(0, 23));    // 00-00-00-00-00-00-00-00      8Bytes
                byteArr.Add(t_maginNumber.Substring(0, 17));    // 00-00-00-00-00-00            6Bytes
                byteArr.Add(t_maginNumber.Substring(0, 14));    // 00-00-00-00-00               5Bytes
                byteArr.Add(t_maginNumber.Substring(0, 11));    // 00-00-00-00                  4Bytes
                byteArr.Add(t_maginNumber.Substring(0, 5));     // 00-00                        2Bytes
                byteArr.Add(t_maginNumber.Substring(0, 2));     // 00                           1Bytes

                Form1.WriteRichTextBoxText($"Signatures : {t_maginNumber}");
            }
            catch (Exception e)
            {
                Form1.WriteRichTextBoxText(e.ToString());
            }

            return byteArr;
        }

        public static List<string> MatchExtensionsFromSignatures(List<string> signaturesList)
        {
            List<string> Extensions = new List<string>();

            foreach (string signature in signaturesList)
            {
                if (signature != null)
                {
                    Extensions.AddRange(FileSignaturesList.Where(t => t.Signatures.Equals(signature)).Select(t => t.Extension.ToLower()));
                }
            }

            return Extensions;
        }
        
        private static List<FileExtSignatures> FileSignaturesList = new List<FileExtSignatures>()
        {
        #region [Extension - Signatures list]
            //시그니쳐 존재
            //.DAT|.LOG|.BMP|.GIF|.JPEG|.JPG|.PDF|.PNG|.DOC|.DOCX|.PPT|.PPTX|.XLS|.XLSX|.XML|.XPS|.ZIP
            //스그니쳐 부재
            //.TXT|.INI|.CSV
            new FileExtSignatures() { Extension = ".DAT", Signatures = "52-49-46-46"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "A9-0D-00-00-00-00-00-00"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "73-6C-68-21"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "73-6C-68-2E"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "41-56-47-36-5F-49-6E-74"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "03"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "45-52-46-53-53-41-56-45"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "43-6C-69-65-6E-74-20-55"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "49-6E-6E-6F-20-53-65-74"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "50-4E-43-49-55-4E-44-4F"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "50-45-53-54"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "1A-52-54-53-20-43-4F-4D"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "52-41-5A-41-54-44-42-31"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "4E-41-56-54-52-41-46-46"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "55-46-4F-4F-72-62-69-74"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "57-4D-4D-50"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "43-52-45-47"}
            , new FileExtSignatures() { Extension = ".DAT", Signatures = "72-65-67-66"}
            , new FileExtSignatures() { Extension = ".LOG", Signatures = "2A-2A-2A-20-20-49-6E-73"}
            , new FileExtSignatures() { Extension = ".BMP", Signatures = "42-4D"}
            , new FileExtSignatures() { Extension = ".GIF", Signatures = "47-49-46-38"}
            , new FileExtSignatures() { Extension = ".JPEG", Signatures = "FF-D8-FF-E0"}
            , new FileExtSignatures() { Extension = ".JPEG", Signatures = "FF-D8-FF-E2"}
            , new FileExtSignatures() { Extension = ".JPEG", Signatures = "FF-D8-FF-E3"}
            , new FileExtSignatures() { Extension = ".JPG", Signatures = "FF-D8-FF-E0"}
            , new FileExtSignatures() { Extension = ".JPG", Signatures = "FF-D8-FF-E1"}
            , new FileExtSignatures() { Extension = ".JPG", Signatures = "FF-D8-FF-E8"}
            , new FileExtSignatures() { Extension = ".PDF", Signatures = "25-50-44-46"}
            , new FileExtSignatures() { Extension = ".PNG", Signatures = "89-50-4E-47-0D-0A-1A-0A"}
            , new FileExtSignatures() { Extension = ".DOC", Signatures = "D0-CF-11-E0-A1-B1-1A-E1"}
            , new FileExtSignatures() { Extension = ".DOC", Signatures = "0D-44-4F-43"}
            , new FileExtSignatures() { Extension = ".DOC", Signatures = "CF-11-E0-A1-B1-1A-E1-00"}
            , new FileExtSignatures() { Extension = ".DOC", Signatures = "DB-A5-2D-00"}
            , new FileExtSignatures() { Extension = ".DOC", Signatures = "EC-A5-C1-00"}
            , new FileExtSignatures() { Extension = ".DOCX", Signatures = "50-4B-03-04"}
            , new FileExtSignatures() { Extension = ".DOCX", Signatures = "50-4B-03-04-14-00-06-00"}
            , new FileExtSignatures() { Extension = ".PPT", Signatures = "D0-CF-11-E0-A1-B1-1A-E1"}
            , new FileExtSignatures() { Extension = ".PPT", Signatures = "00-6E-1E-F0"}
            , new FileExtSignatures() { Extension = ".PPT", Signatures = "0F-00-E8-03"}
            , new FileExtSignatures() { Extension = ".PPT", Signatures = "A0-46-1D-F0"}
            , new FileExtSignatures() { Extension = ".PPT", Signatures = "FD-FF-FF-FF-0E-00-00-00"}
            , new FileExtSignatures() { Extension = ".PPT", Signatures = "FD-FF-FF-FF-1C-00-00-00"}
            , new FileExtSignatures() { Extension = ".PPT", Signatures = "FD-FF-FF-FF-43-00-00-00"}
            , new FileExtSignatures() { Extension = ".PPTX", Signatures = "50-4B-03-04"}
            , new FileExtSignatures() { Extension = ".PPTX", Signatures = "50-4B-03-04-14-00-06-00"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "D0-CF-11-E0-A1-B1-1A-E1"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "09-08-10-00-00-06-05-00"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "FD-FF-FF-FF-10"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "FD-FF-FF-FF-28"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "FD-FF-FF-FF-1F"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "FD-FF-FF-FF-22"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "FD-FF-FF-FF-23"}
            , new FileExtSignatures() { Extension = ".XLS", Signatures = "FD-FF-FF-FF-29"}
            , new FileExtSignatures() { Extension = ".XLSX", Signatures = "50-4B-03-04"}
            , new FileExtSignatures() { Extension = ".XLSX", Signatures = "50-4B-03-04-14-00-06-00"}
            , new FileExtSignatures() { Extension = ".XML", Signatures = "3C-3F-78-6D-6C-20-76-65"}
            , new FileExtSignatures() { Extension = ".XPS", Signatures = "50-4B-03-04"}
            , new FileExtSignatures() { Extension = ".ZIP", Signatures = "50-4B-03-04"}
            , new FileExtSignatures() { Extension = ".ZIP", Signatures = "50-4B-4C-49-54-45"}
            , new FileExtSignatures() { Extension = ".ZIP", Signatures = "50-4B-53-70-58"}
            , new FileExtSignatures() { Extension = ".ZIP", Signatures = "50-4B-05-06"}
            , new FileExtSignatures() { Extension = ".ZIP", Signatures = "50-4B-07-08"}
            , new FileExtSignatures() { Extension = ".ZIP", Signatures = "57-69-6E-5A-69-70"}
            , new FileExtSignatures() { Extension = ".ZIP", Signatures = "50-4B-03-04-14-00-01-00"}
            #endregion
        };

        [DllImport(@"urlmon.dll", CharSet = CharSet.Auto)]
        private extern static System.UInt32 FindMimeFromData(
            System.UInt32 pBC,
            [MarshalAs(UnmanagedType.LPStr)] System.String pwzUrl,
            [MarshalAs(UnmanagedType.LPArray)] byte[] pBuffer,
            System.UInt32 cbSize,
            [MarshalAs(UnmanagedType.LPStr)] System.String pwzMimeProposed,
            System.UInt32 dwMimeFlags,
            out System.UInt32 ppwzMimeOut,
            System.UInt32 dwReserverd
        );


        private static IDictionary<string, string> MIMETypesDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        #region Big freaking list of mime types
        
        // combination of values from Windows 7 Registry and 
        // from C:\Windows\System32\inetsrv\config\applicationHost.config
        // some added, including .7z and .dat

        { "1.bin", "application/octet-stream"},
        { "1.dat", "application/octet-stream"},
        { "1.log", "text/plain"},
        { "2.log", "application/octet-stream"},

        { "1.csv", "text/csv"},
        { "2.csv", "text/plain"},
        { "1.ini", "application/octet-stream"},
        { "2.ini", "text/plain"},
        { "1.txt", "text/plain"}
        #endregion
        };

    }
}
