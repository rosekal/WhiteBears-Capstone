﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SautinSoft.Document;
using System.IO;

namespace WhiteBears.Models
{
    public class DocumentVersionsModel
    {
        public int id { get; set; }
        public List<SelectableVersions> docList { get; set; }
    public DocumentVersionsModel()
        {
            docList = new List<SelectableVersions>();
        }
    }
    public class SelectableVersions
    {
        public int version { get; set; }
        public string timeStamp { get; set; }
        public string modifiedBy { get; set; }
    }
}