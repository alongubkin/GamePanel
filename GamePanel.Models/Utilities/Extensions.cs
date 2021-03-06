﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Dynamic;
using System.IO;

namespace GamePanel.Utilities
{
    public static class SlotUtils
    {
        public static IEnumerable<SelectListItem> GenerateDropDownList(int steps, int priceDelta)
        {
            for (int x = 10; x <= 64; x += steps)
            {
                yield return new SelectListItem()
                {
                    Value = x.ToString(),
                    Text = string.Concat(x,  x < 12 ? string.Empty : " (הוסף " + ((x - 10) * priceDelta).ToString() + " קרדיטים)")
                };
            }
        }

        public static ExpandoObject ToExpando(this object anonymousObject)
        {
            IDictionary<string, object> anonymousDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var item in anonymousDictionary)
                expando.Add(item);
            return (ExpandoObject)expando;
        }

        public static void CopyTo(this DirectoryInfo source, string destDirectory, bool recursive)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destDirectory == null)
                throw new ArgumentNullException("destDirectory");
            // Compile the target.
            DirectoryInfo target = new DirectoryInfo(destDirectory);
            // If the source doesn’t exist, we have to throw an exception.
            if (!source.Exists)
                throw new DirectoryNotFoundException("Source directory not found: " + source.FullName);
            // If the target doesn’t exist, we create it.
            if (!target.Exists)
                target.Create();

            // Get all files and copy them over.
            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }

            // Return if no recursive call is required.
            if (!recursive)
                return;

            // Do the same for all sub directories.
            foreach (DirectoryInfo directory in source.GetDirectories())
            {
                CopyTo(directory, Path.Combine(target.FullName, directory.Name), recursive);
            }
        }
    }
}
