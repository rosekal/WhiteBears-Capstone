﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Whitebears.Models;
using System.Threading.Tasks;

namespace Whitebears
{
    public interface IBlobStorageRepository
    {
        IEnumerable<BlobViewModel> GetBlobs();
        bool DeleteBlob(string file, string fileExtension);
        bool UploadBlob(HttpPostedFileBase blobFile);

        Task<bool> DownloadBlobAsync(string file, string fileExtension);
    }
}