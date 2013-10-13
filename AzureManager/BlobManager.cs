using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Common.Logging;
using DD.SolCat.Common;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureManager
{
    /// <summary>
    /// Utility methods for copying blobs between storage accounts. 
    /// </summary>
    public static class BlobManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BlobManager));
        
        private static CloudBlobContainer _srcContainer;
        private static CloudBlobContainer _destContainer;
        
        /// <summary>
        /// Initiates the SolCat Azure blob data sync.  
        /// </summary>
        public static void CopyBlobData()
        {
            // Authentication Credentials for Azure Storage:
            var credsSrc 
                = new StorageCredentials(
                    ConfigHelper.GetConfigValue("HubContainerName"), 
                    ConfigHelper.GetConfigValue("HubContainerKey"));
            
            var credsDest
                = new StorageCredentials(
                    ConfigHelper.GetConfigValue("NodeContainerKey"), 
                    ConfigHelper.GetConfigValue("NodeContainerKey"));
            
            // Source Container: Hub (Development) 
            _srcContainer =
                new CloudBlobContainer(
                    new Uri(ConfigHelper.GetConfigValue("HubContainerUri")),
                    credsSrc);

            // Destination Container: Node (Production)
            _destContainer =
                new CloudBlobContainer(
                    new Uri(ConfigHelper.GetConfigValue("NodeContainerUri")),
                    credsDest);

            // Set permissions on the container:
            var permissions = new BlobContainerPermissions {PublicAccess = BlobContainerPublicAccessType.Blob};
            _srcContainer.SetPermissions(permissions);
            _destContainer.SetPermissions(permissions);

            // Call the blob copy master method: 
            CopyBlobs(_srcContainer, _destContainer);
        }

        #region Private_Methods

        /// <summary>
        /// Copy all blobs from <param name="srcContainer">srcContainer</param> to the
        /// <param name="destContainer">destContainer</param>.
        /// </summary>
        /// <remarks>This method uses a Parallel ForEach to provide async copying for large files.</remarks>
        /// <param name="srcContainer">The source Blob Container</param>
        /// <param name="destContainer"></param>
        private static void CopyBlobs(
            CloudBlobContainer srcContainer,
            CloudBlobContainer destContainer)
        {
            // Get source lists from Azure (LazyLoaded):
            
            var srcBlobList
                = srcContainer.ListBlobs(string.Empty, true, BlobListingDetails.All); // set to none in prod (4perf)

            var destBlobList
                = destContainer.ListBlobs(string.Empty, true, BlobListingDetails.All); // set to none in prod (4perf)

            // Convert to IEnumerable to avoid multiple  enumeration:
 
            var srcBlobItems = srcBlobList as IList<IListBlobItem> ?? srcBlobList.ToList();
            var destBlobItems = destBlobList as IList<IListBlobItem> ?? destBlobList.ToList();
            
            // Run the core blob sort, filter, and copy method:

            BlobFilter(srcBlobItems, destBlobItems);

            // Exit Method:
            return;

        }

        /// <summary>
        /// Examines each blob and compares with its destination 
        /// counterpart to determine if the blob needs to be 
        /// copied or if it can be skipped.
        /// </summary>
        /// <remarks>We will look at the created date and test if the time span is greater than 
        /// the amount of time between syncs (every hour/day/etc)</remarks>
        /// <param name="srcBlobList">Source Blob List.</param>
        /// <param name="destBlobList">Destination Blob List.</param>
        /// <returns>A list of the blobs that qualify for sync. </returns>
        private static void BlobFilter(
        IEnumerable<IListBlobItem> srcBlobList,
        IEnumerable<IListBlobItem> destBlobList)
        {
            var srcBlobs = srcBlobList
                .Select(blobItem => blobItem as ICloudBlob)
                .Select(srcBlob => new Blob(srcBlob.Name, srcBlob.Properties.LastModified.ToString(), srcBlob.Properties.Length.ToString(CultureInfo.InvariantCulture))).ToList();
            
            var destBlobs = destBlobList
                .Select(blobItem => blobItem as ICloudBlob)
                .Select(destBlob => new Blob(destBlob.Name, destBlob.Properties.LastModified.ToString(), destBlob.Properties.Length.ToString(CultureInfo.InvariantCulture))).ToList();

            //
            // Hub (Source) Blobs:

            foreach (var blob in srcBlobs)
            {
                if (blob.Length == "0") continue; 

                Blob target = FindBlob(destBlobs, blob);
                if (BlobExists(target))
                {
                    destBlobs.Remove(target);
                    if (TestDates(target.LastModified, blob.LastModified)) // blob was updated:
                    {
                        CopyBlob(blob, true);
                    }
                }
                else // blob was deleted:
                {
                    DeleteBlob(blob, true);
                }
            }

            //
            // Node (Destination) Blobs:

            foreach (var blob in destBlobs)
            {
                if (blob.Length == "0") continue;

                Blob target = FindBlob(srcBlobs, blob);
                
                if (!BlobExists(target))
                {
                    CopyBlob(blob, false);
                }
            }

        }

        /// <summary>
        /// Locates the given blob in the <param name="blobList">blobList</param> list parameter 
        /// and returns true if the list contains this blob, otherwise returns false. 
        /// </summary>
        /// <param name="blobList"></param>
        /// <param name="blob"></param>
        /// <returns></returns>
        private static Blob FindBlob(IEnumerable<Blob> blobList, Blob blob)
        {
            return blobList.FirstOrDefault(x => x.Name.Equals(blob.Name));
        }

        /// <summary>
        /// Returns <code>true</code> if this blob is valid, 
        /// otherwise returns <code>false</code>.
        /// </summary>
        /// <remarks>Checks to determine if the blob in question 
        /// has a valid name and is not null.</remarks>
        /// <param name="db"></param>
        /// <returns></returns>
        private static bool BlobExists(Blob db)
        {
            bool isValid = db != null && !string.IsNullOrEmpty(db.Name);
            return isValid;
        }

        /// <summary>
        /// Verify's that the source blob (Hub) is not newer than the 
        /// destination (Node) blob.
        /// </summary>
        /// <remarks>True means that this blob has recently been updated.</remarks>
        /// <param name="lastModifiedSource"></param>
        /// <param name="lastModifiedDest"></param>
        /// <returns></returns>
        private static bool TestDates(string lastModifiedSource, string lastModifiedDest)
        {
            if (string.IsNullOrEmpty(lastModifiedDest) || string.IsNullOrEmpty(lastModifiedSource))
                return true; // new file...

            var dateSrc = Convert.ToDateTime(lastModifiedSource);
            var dateDest = Convert.ToDateTime(lastModifiedDest);

            if (dateSrc > dateDest)
                return true;
            else
                return false;
        }

        /// <summary>
        /// If <param name="isHub">IsHub</param> is <code>true</code>, 
        /// then this blob is copied to the hub, otherwise it is 
        /// copied to the node. 
        /// </summary>
        /// <param name="blob">The blob to be copied.</param>
        /// <param name="isHub">Whether to copy this blob to 
        /// the hub or node conatiner.</param>
        private static void CopyBlob(Blob blob, bool isHub)
        {
            CloudBlockBlob sourceBlob;
            CloudBlockBlob destinationBlob;

            if (isHub)
            {
                sourceBlob = _srcContainer.GetBlockBlobReference(blob.Name);
                destinationBlob = _destContainer.GetBlockBlobReference(blob.Name);
            }
            else
            {
                sourceBlob = _destContainer.GetBlockBlobReference(blob.Name);
                destinationBlob = _srcContainer.GetBlockBlobReference(blob.Name);
            }

            destinationBlob.StartCopyFromBlob(sourceBlob);
        }
        
        /// <summary>
        /// Remove the blob from the hub or node, depending
        /// on the <param>isHub</param> parameter. 
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="isHub"></param>
        private static void DeleteBlob(Blob blob, bool isHub)
        {
            CloudBlockBlob sourceBlob;

            if (isHub)
            {
                sourceBlob = _srcContainer.GetBlockBlobReference(blob.Name);
            }
            else
            {
                sourceBlob = _destContainer.GetBlockBlobReference(blob.Name);
            }

            sourceBlob.Delete();
        }

        #endregion
    }
}

