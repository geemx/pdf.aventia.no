﻿using Microsoft.EntityFrameworkCore;
using pdf.aventia.no.Database;
using pdf.aventia.no.Interfaces;
using pdf.aventia.no.Models.Entities;
using IronPdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace pdf.aventia.no.Services
{
    public class PdfService : IPdfService
    {
        private readonly PdfDbContext context;

        public PdfService(PdfDbContext context)
        {
            this.context = context;
        }

        public async Task IndexPdf(int pdfid, CancellationToken cancellationToken = default)
        {
            var pdf = await context.Pdfs.FindAsync(pdfid);
            var pdfDoc = new IronPdf.PdfDocument(pdf.filepath);
            var extractedText = pdfDoc.ExtractAllText();

            if (extractedText != null)
            {
                // Store the whole extracted text
                pdf.text = extractedText;

                // Split the extracted text into sentences
                var sentences =
                    extractedText.Split(new string[] { ".", "!", "?" }, StringSplitOptions.RemoveEmptyEntries);


                // Save the changes to the database
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task IndexAllPdfFilesInFolder(string folderPath = pdf.aventia.no.GlobalSettings.DefaultFolderPath,
            CancellationToken cancellationToken = default)
        {
            var pdfFilePaths = Directory.GetFiles(folderPath, "*.pdf");

            foreach (var pdfFilePath in pdfFilePaths)
            {
                var pdf = new Pdf { filepath = pdfFilePath };
                context.Pdfs.Add(pdf);
                await context.SaveChangesAsync(cancellationToken);
                await IndexPdf(pdf.id, cancellationToken);
            }
        }

        public async Task IndexSinglePdfFile(string pdfid,
            string folderPath = pdf.aventia.no.GlobalSettings.DefaultFolderPath,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(folderPath, pdfid + ".pdf");
            string filePath = files.FirstOrDefault();

            if (filePath != null)
            {
                var pdf = new Pdf { filepath = filePath };
                context.Pdfs.Add(pdf);
                await context.SaveChangesAsync(cancellationToken);
                await IndexPdf(pdf.id, cancellationToken);
            }
            else
            {
                // Handle the case where the file was not found
            }
        }

        public async Task<IEnumerable<string>> SearchPdfsAsync(string word, CancellationToken cancellationToken)
        {
            var results = new List<string>();

            if (string.IsNullOrEmpty(word))
            {
                return results;
            }

            var pdfs = await context.Pdfs.ToListAsync(cancellationToken);

            foreach (var pdf in pdfs)
            {
                // Check if pdf.text is not null
                if (pdf.text != null)
                {
                    // Split the text into sentences
                    var sentences = pdf.text.Split(new[] { ".", "!", "?" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < sentences.Length; i++)
                    {
                        if (sentences[i].Contains(word))
                        {
                            var endIndex = Math.Min(i + 2, sentences.Length - 1); // get next two sentences only
                            var excerpt = string.Join(". ", sentences.Skip(i).Take(endIndex - i + 1));
                            var highlightedExcerpt = excerpt.Replace(word, "*" + word + "*");
                            results.Add($"PDF ID: {pdf.id} - Excerpt: {highlightedExcerpt}");
                        }
                    }
                }
            }

            return results;
        }




        public async Task ProcessPdfFiles(CancellationToken cancellationToken = default)
        {
            // Implement the logic for processing PDF files here
            throw new NotImplementedException();
        }
    }

}