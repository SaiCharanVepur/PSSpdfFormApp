using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Florentis;
using iTextSharp;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.interfaces;
using iTextSharp.text.io;
using PdfUtils;
using PdfiumViewer;




namespace PSSpdfFormApp
{
    public partial class PSSpdfFormApp : Form
    {
        public string defaultPdf = "Empty_PDF.pdf";
        public bool defaultLoaded = false;

        public string originalFile = "";
        public string currentFile = "";

        public string name = "Name";
        public string reason = "Reason";

        private DateTime signatureDateTime;
        public PSSpdfFormApp()
        {
            InitializeComponent();
        }

        private void loadDefaultPDF()
        {
            loadPdf(defaultPdf);
            defaultLoaded = true;
        }

        private void loadPdf(string pdfPath)
        {
            loadPdf(pdfPath, true);
        }

        private void loadPdf(string pdfPath, bool changeOriginal)
        {

            //pdfRenderer1.Load(PdfiumViewer.PdfDocument.Load(pdfPath));
            pdfRenderer1.Document = PdfiumViewer.PdfDocument.Load(pdfPath);
            pdfRenderer1.Show();

            currentFile = pdfPath;

            if (changeOriginal)
            {
                originalFile = pdfPath;
            }
            defaultLoaded = false;
            this.Text = "PSSpdfFormApp - " + originalFile;

        }

        private void PsspdfFormApp_Load(object sender, EventArgs e)
        {
            loadDefaultPDF();
        }

        private void signToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sign();
        }

        public void sign()
        {
            SigCtl sigCtl = new SigCtl();
            sigCtl.Licence = Properties.Settings.Default.License;
            DynamicCapture dc = new DynamicCaptureClass();
            DynamicCaptureResult res = dc.Capture(sigCtl, name, reason, null, null);

            if (res == DynamicCaptureResult.DynCaptOK)
            {
                SigObj sigObj = (SigObj)sigCtl.Signature;
                

                String filename = System.IO.Path.GetTempFileName();
                try
                {
                    sigObj.RenderBitmap(filename, 400, 200, "image/png", 0.5f, 0x000000, 0xffffff, 10.0f, 10.0f, RBFlags.RenderOutputFilename | RBFlags.RenderColor32BPP | RBFlags.RenderEncodeData | RBFlags.RenderBackgroundTransparent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                string newFile = InsertSignatureImageToPdf(filename);
                loadPdf(newFile, false);
            }
        }

        public void save()
        {
            save(originalFile);
        }

        public void save(string path)
        {
            FileStream fsOut = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            FileStream fsIn = new FileStream(currentFile, FileMode.Open, FileAccess.Read);
            fsIn.CopyTo(fsOut);

            fsIn.Close();
            fsOut.Close();
        }

        public void saveAs()
        {
            saveFileDialog1.Filter = "PDF Documents | *.pdf";
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                save(saveFileDialog1.FileName);
                loadPdf(saveFileDialog1.FileName, true);
            }
        }


        private string InsertSignatureImageToPdf(string imageFileName)
        {
            string signedFile = System.IO.Path.GetTempFileName();

            PdfReader reader = new PdfReader(currentFile);
            using (FileStream fs = new FileStream(signedFile, FileMode.Create, FileAccess.Write))
            {
                using (PdfStamper stamper = new PdfStamper(reader, fs))
                {
                    PdfContentByte cb = stamper.GetOverContent(1);

                    iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imageFileName);
                    // Change signature points
                    image.SetAbsolutePosition(90, 220);
                    image.ScalePercent(30f, 30f);

                    cb.AddImage(image);

                    // Add date and time
                    cb.BeginText();
                    cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 12);

                    // Set the new position for the date and time text
                    float textX = 90; // Adjust the X coordinate
                    float textY = 210; // Adjust the Y coordinate
                    cb.SetTextMatrix(textX, textY);

                    cb.ShowText($"Signed on: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                    cb.EndText();
                }
            }

            currentFile = signedFile;

            return signedFile;
        }

        private void getSignatureFromPdf(string pdf_filename)
        {
            bool imagesFound = PdfImageExtractor.PageContainsImages(pdf_filename, 1);

            if (imagesFound)
            {
                Dictionary<string, System.Drawing.Image> dict = PdfImageExtractor.ExtractImages(pdf_filename, 1);
                foreach (string key in dict.Keys)
                {
                    System.Drawing.Image img = dict[key];
                    img.Save("sign.png");

                    SigObj sig = new SigObj();
                    ReadEncodedBitmapResult result = sig.ReadEncodedBitmap("sign.png");
                    if (result == ReadEncodedBitmapResult.ReadEncodedBitmapOK)
                    {
                        MessageBox.Show(sig.Who + " " + sig.Why + " " + sig.When);
                    }
                }
            }
        }

        private void mnuOpen_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "PDF Documents | *.pdf";
            openFileDialog1.Multiselect = false;
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    loadPdf(openFileDialog1.FileName);
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("File not found");
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        private void mnuInfo_Click(object sender, EventArgs e)
        {
            frmSignatureInfo info = new frmSignatureInfo();
            info.loadSignInfo(currentFile);
            info.ShowDialog();

        }

        private void mnuSave_Click(object sender, EventArgs e)
        {
            save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAs();
        }
    }
}
