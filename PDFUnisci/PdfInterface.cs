using System.IO;

using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Collections.Generic;

using LogManager;
using System.Linq;
using System;

namespace PDFUnisci
{
    public static class PDFInterface
    {
        static int _DefaultDigit = 3;

        public static int DefaultDigit
        { 
            get
            {
                return _DefaultDigit;
            }

            set
            {
                if (value >= 10) _DefaultDigit = 10;
                else if (value <= 1) _DefaultDigit = 1;
                else _DefaultDigit = value;
            } 
        }

        static int _CoverFunction = 1;
        public static int CoverFunction 
        { 
            get
            {
                return _CoverFunction;
            }

            set
            { 
                if(value >= 1) _CoverFunction = 1;
                else _CoverFunction = 0;
            } 
        }
        
        static string Contain(List<string> files, string outFile, int start)
        {
            string OutFile = new Uri(outFile).ToString().Replace("file:///", "");

            if (files.Contains(OutFile))
                OutFile = OutFile.Replace(".pdf", $"({start}).pdf");

            if (files.Contains(OutFile))
                OutFile = Contain(files, OutFile, start+1);

            return OutFile;
        }

        public static void MergePDF(List<string> files, string outFile)
        {

            string OutFile = outFile;

            LogHelper.Log("Unisco tutti i file in un unico PDF", LogType.Successful);

            FileStream stream = null;
            Document doc = null;
            PdfCopy pdf = null;

            try
            {
                stream = new FileStream(OutFile, FileMode.Create);
                doc = new Document();
                pdf = new PdfCopy(doc, stream);

                doc.Open();

                foreach (string file in files)
                {
                    //todo: approfondire crash
                    LogHelper.Log($"Aggiungo il file: {file}");
                    pdf.AddDocument(new iTextSharp.text.pdf.PdfReader(file));
                }
            }
            catch (Exception e)
            {
                LogHelper.Log(e.ToString(), LogType.Error);
            }
            finally
            {
                pdf?.Dispose();
                doc?.Dispose();
                stream?.Dispose();
            }
        }

        public static void ReplaceCoverPDF(string InFile, string InCover, string OutFile)
        {
            if(CoverFunction == 0) 
            {
                List<string> Files = new List<string>
                {
                    InFile,
                    InCover
                };
                Files.Sort();
                MergePDF(Files, OutFile);
                return;
            }

            LogHelper.Log("Sostituisco la cover al file originario", LogType.Successful);

            FileStream stream = null;
            Document doc = null;
            PdfCopy pdf = null;

            try
            {
                stream = new FileStream(OutFile, FileMode.Create);
                doc = new Document();
                pdf = new PdfCopy(doc, stream);

                doc.Open();

                //Aggiungo la cover
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(InCover);
                int coverPage = reader.NumberOfPages;

                LogHelper.Log($"Aggiungo la cover: {InCover} di {coverPage} pagine");
                pdf.AddDocument(reader);
                reader.Close();

                //Aggiungo il resto del documento
                reader = new iTextSharp.text.pdf.PdfReader(InFile);
                int count = reader.NumberOfPages;
                coverPage++;
                List<int> pages = Enumerable.Range(coverPage, count - coverPage + 1).ToList();

                LogHelper.Log($"Aggiungo il file: {InFile} da pagina: {coverPage}");
                pdf.AddDocument(reader, pages);

                reader.Close();
            }
            catch(Exception e)
            {
                LogHelper.Log(e.ToString(), LogType.Error);
            }
            finally
            {
                pdf?.Dispose();
                doc?.Dispose();
                stream?.Dispose();
            }

        }

        public static void SplitPDF(string InFiles, string OutDir)
        {
            string outFiles = OutDir + Path.AltDirectorySeparatorChar + Path.GetFileNameWithoutExtension(InFiles);

            LogHelper.Log($"Creo la directory in: {OutDir}");
            Directory.CreateDirectory(OutDir);

            LogHelper.Log("Divido il file in tanti PDF singoli", LogType.Successful);

            PdfReader reader = null;

            try
            {
                reader = new PdfReader(InFiles);

                int NumPages = reader.NumberOfPages;

                int digitN = NumPages.ToString().Length;
                if (digitN < DefaultDigit) digitN = (int)DefaultDigit;

                for (int i = 1; i <= NumPages; i++)
                {
                    string outFile = string.Format("{0}_Page {1:D" + digitN + "}.pdf", outFiles, i);
                    FileStream stream = new FileStream(outFile, FileMode.Create);

                    LogHelper.Log($"Pagina: {Path.GetFileNameWithoutExtension(outFile)}");
                    Document doc = new Document();
                    PdfCopy pdf = new PdfCopy(doc, stream);

                    doc.Open();
                    PdfImportedPage page = pdf.GetImportedPage(reader, i);
                    pdf.AddPage(page);

                    pdf.Dispose();
                    doc.Dispose();
                    stream.Dispose();
                }

            }
            catch(Exception e)
            {
                LogHelper.Log(e.ToString(), LogType.Error);
            }
            finally
            {
                reader?.Dispose();
            }
        }
    }
}