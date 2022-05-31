using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        public static bool CheckFileExtension(string filePath)
        {
            Form1.WriteRichTextBoxText($"FullName: {filePath}");

            whiteList = Form1.whiteList;
            blackList = Form1.blackList;

            whiteAry = whiteList.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            blackAry = blackList.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            string fileExt = Path.GetExtension(filePath).ToLower();

            if (!CheckExtension(fileExt))
                return false;

            if (".zip".Equals(fileExt))
            {
                return CheckZipFileExtension(filePath);
            }

            return true;
        }
        
        public static bool CheckZipFileExtension(string zipPath)
        {
            string extractPath = string.Empty;
            string destinationPath = string.Empty;
            string extractZipPath = Path.Combine(Path.GetDirectoryName(zipPath), Path.GetFileNameWithoutExtension(zipPath));

            try {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fileName = entry.FullName;
                        string fileExt = Path.GetExtension(fileName).ToLower();

                        Form1.WriteRichTextBoxText($"FullName: {zipPath}\\{fileName}");

                        //폴더는 마지막에 "/" 문자
                        if (entry.FullName.EndsWith(@"/", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!CheckExtension(fileExt))
                            return false;

                        //.zip이라면 압축해제
                        if (".zip".Equals(fileExt.ToLower()))
                        {
                            #region 파일 확장자 .zip이라면 압축해제
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


        public static bool CheckFileExtensionFromSignatures(string filePath)
        {
            Form1.WriteRichTextBoxText($"FullName: {filePath}");

            string fileExt = Path.GetExtension(filePath).ToLower();

            List<string> Signatures = ReadSignaturesFromStream(filePath);
                                     
            List<string> Extensions = MatchExtensionsFromSignatures(Signatures);
             
            if (Extensions.Count(s => s.ToLower().Equals(fileExt)) == 0)
            {
                return false;
            }

            //.zip과 같은 Signatures [50-4B-03-04] == [.DOCX, .PPTX, .XLSX, .XPS, .ZIP]
            //.zip일 가능성이 있는 파일값 검사
            if (Signatures.Count(s => s.Equals("50-4B-03-04")) > 0)
            {
                if (".zip".Equals(fileExt))
                {   
                    using (ZipArchive archive = ZipFile.OpenRead(filePath))
                    {   //.zip 이라면 Entries[0] 값 ![Content_Types].xml 
                        if (archive.Entries[0].ToString().Contains("[Content_Types].xml"))
                            return false;

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
            }
                        
            //if (".zip".Equals(fileExt.ToLower()))
            //{
            //    foreach (List<string> strList in ReadZipFileEntriesSignaturesBytes(filePath))
            //    {
            //        foreach (string str in strList)
            //        {
            //            //Form1.WriteRichTextBoxText(str);
            //        }
            //    }
            //}

            return true;
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
                //, new FileExtSignatures() { Extension = ".XML", Signatures = "EF-BB"}
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

    }
}
