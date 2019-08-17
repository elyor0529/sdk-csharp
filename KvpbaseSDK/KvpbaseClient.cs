﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using RestWrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KvpbaseSDK
{
	/// <summary>
	/// Kvpbase object storage client.
	/// </summary>
	public class KvpbaseClient
    {
        #region Public-Members

        /// <summary>
        /// Specify whether or not to ignore SSL certificate errors (default is true).
        /// </summary>
        public bool IgnoreCertificateErrors { get; set; }

        /// <summary>
        /// Specify the maximum transfer size in bytes (default is 536870912).
        /// </summary>
        public long MaxTransferSize
        {
            get
            {
                return _MaxTransferSize;
            }
            set
            {
                if (value < 1) throw new ArgumentException("MaxTransferSize must be greater than zero.");
                _MaxTransferSize = value;
            }
        }

        /// <summary>
        /// Buffer size to use when uploading files for file APIs or using stream, default 1MB.
        /// </summary>
        public int UploadStreamBufferSize
        {
            get
            {
                return _UploadStreamBufferSize;
            }
            set
            {
                if (value < 1) throw new ArgumentException("UploadStreamBufferSize must be greater than zero.");
                _UploadStreamBufferSize = value;
            }
        }

        /// <summary>
        /// Buffer size to use when downloading files for file APIs or using stream, default 1MB.
        /// </summary>
        public int DownloadStreamBufferSize
        {
            get
            {
                return _DownloadStreamBufferSize;
            }
            set
            {
                if (value < 1) throw new ArgumentException("DownloadStreamBufferSize must be greater than zero.");
                _DownloadStreamBufferSize = value;
            }
        }

        /// <summary>
        /// Retrieve the user GUID for the client.
        /// </summary>
        public string UserGuid
        {
            get { return _UserGuid; }
            private set { _UserGuid = value; }
        }

        /// <summary>
        /// Retrieve the endpoint for the client.
        /// </summary>
        public string Endpoint
        {
            get { return _Endpoint; }
            private set { _Endpoint = value; }
        }

        #endregion

        #region Private-Members

        private string _UserGuid = null;
        private string _Email = null;
        private string _Password = null;
        private string _ApiKey = null;
        private string _Token = null;
        private string _Endpoint = null;

        private Dictionary<string, string> _AuthHeaders = null;

        private long _MaxTransferSize = 536870912;
        private int _UploadStreamBufferSize = (1024 * 1024);
        private int _DownloadStreamBufferSize = (1024 * 1024);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the Kvpbase client.
        /// </summary>
        /// <param name="userGuid">The GUID of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <param name="endpointUrl">The Kvpbase server endpoint (e.g. http://api1.kvpbase.com:8001/, or, https://hostname.com:443/).</param>
        public KvpbaseClient(string userGuid, string email, string password, string endpointUrl)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (String.IsNullOrEmpty(endpointUrl)) throw new ArgumentNullException(nameof(endpointUrl));

            _UserGuid = userGuid;
            _Email = email;
            _Password = password;
            _Endpoint = AppendSlash(endpointUrl);

            IgnoreCertificateErrors = true;
            MaxTransferSize = 536870912;
            UploadStreamBufferSize = 1048576;
            DownloadStreamBufferSize = 1048576;

            SetAuthHeaders();
        }

        /// <summary>
        /// Initializes a new instance of the Kvpbase client.
        /// </summary>
        /// <param name="userGuid">The GUID of the user.</param>
        /// <param name="apiKey">The API key of the user.</param>
        /// <param name="endpointUrl">The Kvpbase server endpoint (e.g. http://api1.kvpbase.com:8001/, or, https://hostname.com:443/).</param>
        public KvpbaseClient(string userGuid, string apiKey, string endpointUrl)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (String.IsNullOrEmpty(endpointUrl)) throw new ArgumentNullException(nameof(endpointUrl));

            _UserGuid = userGuid;
            _ApiKey = apiKey;
            _Endpoint = AppendSlash(endpointUrl);

            IgnoreCertificateErrors = true;
            MaxTransferSize = 536870912;
            UploadStreamBufferSize = 1048576;
            DownloadStreamBufferSize = 1048576;

            SetAuthHeaders();
        }

        #endregion

        #region Public-Methods

        #region General

        /// <summary>
        /// Verify connectivity to Kvpbase.
        /// </summary>
        /// <returns>True if connectivity exists.</returns>
        public bool VerifyConnectivity()
        {
            RestRequest req = new RestRequest(_Endpoint, HttpMethod.GET, null, null, true);
            req.IgnoreCertificateErrors = IgnoreCertificateErrors; 
            RestResponse resp = req.Send();

            if (resp != null && resp.StatusCode >= 200 && resp.StatusCode <= 299)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Authenticate to Kvpbase.
        /// </summary>
        /// <returns>True if able to successfully authenticate.</returns>
        public bool Authenticate()
        {
            RestRequest req = new RestRequest(
                _Endpoint + "token", 
                HttpMethod.GET, 
                _AuthHeaders, 
                null, 
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp != null && resp.StatusCode >= 200 && resp.StatusCode <= 299)
            {
                if (resp.Data != null && resp.Data.Length > 0)
                {
                    _Token = Encoding.UTF8.GetString(resp.Data);
                }

                return true;
            }
            return false;
        }

        #endregion
         
        #region Node

        /// <summary>
        /// Get the version number of the endpoint.
        /// </summary>
        /// <param name="version">Kvpbase version.</param>
        /// <returns>True if successful.</returns>
        public bool GetVersion(out string version)
        {
            version = null;
            string url = _Endpoint + "version";

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200 || resp.Data == null || resp.Data.Length < 1)
            {
                return false;
            }

            version = Encoding.UTF8.GetString(resp.Data);
            return true;
        }

        #endregion

        #region Containers

        /// <summary>
        /// List the names of the existing containers.
        /// </summary> 
        /// <param name="settings">List of container settings.</param>
        /// <returns>True if successful.</returns>
        public bool ListContainers(out List<ContainerSettings> settings)
        {
            settings = new List<ContainerSettings>();

            string url = _Endpoint + _UserGuid + "?_container=true&_stats=true";

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200 || resp.Data == null || resp.Data.Length < 1)
            {
                return false;
            }

            settings = KvpbaseCommon.DeserializeJson<List<ContainerSettings>>(resp.Data);
            return true;
        }

        /// <summary>
        /// Create a container.
        /// </summary> 
        /// <param name="container">The container.</param>
        /// <param name="publicRead">True if available for read by unauthenticated users.</param>
        /// <param name="publicWrite">True if available for write by unauthenticated users.</param>
        /// <param name="auditLogging">True if audit logging should be enabled.</param>
        /// <param name="replication">Replication mode for the container.</param>
        /// <returns>True if successful.</returns>
        public bool CreateContainer(string container, bool publicRead, bool publicWrite, bool auditLogging, ReplicationMode replication)
        {
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));

            ContainerSettings settings = new ContainerSettings();
            settings.User = _UserGuid;
            settings.Name = container;
            settings.IsPublicRead = publicRead;
            settings.IsPublicWrite = publicWrite;
            settings.EnableAuditLogging = auditLogging;
            settings.Replication = replication;

            string url = _Endpoint + _UserGuid + "/" + container + "?_container=true";

            RestRequest req = new RestRequest(
                url,
                HttpMethod.POST,
                _AuthHeaders,
                "application/json",
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send(Encoding.UTF8.GetBytes(KvpbaseCommon.SerializeJson(settings, false)));

            if (resp == null || resp.StatusCode != 201)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieve a container's settings.
        /// </summary> 
        /// <param name="container">The container.</param>
        /// <param name="settings">Settings for the container.</param>
        /// <returns>True if successful.</returns>
        public bool GetContainerSettings(string container, out ContainerSettings settings)
        {
            settings = null;

            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));

            string url = _Endpoint + _UserGuid + "/" + container + "?_container=true&_config=true";

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200 || resp.Data == null || resp.Data.Length < 1)
            {
                return false;
            }

            settings = KvpbaseCommon.DeserializeJson<ContainerSettings>(resp.Data);
            return true;
        }

        /// <summary>
        /// Update a container's settings.
        /// </summary>
        /// <param name="settings">Settings for the container.</param>
        /// <returns>True if successful.</returns>
        public bool UpdateContainer(ContainerSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string url = _Endpoint + _UserGuid + "/" + settings.Name + "?_container=true";

            RestRequest req = new RestRequest(
                url,
                HttpMethod.PUT,
                _AuthHeaders,
                "application/json",
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send(Encoding.UTF8.GetBytes(KvpbaseCommon.SerializeJson(settings, false)));

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Enumerate container statistics and objects within the container.
        /// </summary> 
        /// <param name="container">The container.</param>
        /// <param name="startIndex">Begin object enumeration from this position.</param>
        /// <param name="maxResults">Maximum number of objects to return.</param>
        /// <param name="metadata">The container's metadata.</param>
        /// <returns>True if successful.</returns>
        public bool EnumerateContainer(string container, long? startIndex, long? maxResults, out ContainerMetadata metadata)
        {
            return EnumerateContainer(null, container, startIndex, maxResults, out metadata); 
        }

        /// <summary>
        /// Enumerate container statistics and objects within the container.
        /// </summary> 
        /// <param name="prefix">Prefix to the object key.</param>
        /// <param name="container">The container.</param>
        /// <param name="startIndex">Begin object enumeration from this position.</param>
        /// <param name="maxResults">Maximum number of objects to return.</param>
        /// <param name="metadata">The container's metadata.</param>
        /// <returns>True if successful.</returns>
        public bool EnumerateContainer(string prefix, string container, long? startIndex, long? maxResults, out ContainerMetadata metadata)
        {
            metadata = null;

            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));

            string url = _Endpoint + _UserGuid + "/" + container + "?_container=true";
            if (startIndex != null) url += "&_index=" + startIndex;
            if (maxResults != null) url += "&_count=" + maxResults;
            if (!String.IsNullOrEmpty(prefix)) url += "&_prefix=" + prefix;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200 || resp.Data == null || resp.Data.Length < 1)
            {
                return false;
            }

            metadata = KvpbaseCommon.DeserializeJson<ContainerMetadata>(resp.Data);
            return true;
        }

        /// <summary>
        /// Delete a container.
        /// </summary> 
        /// <param name="container">The container.</param>
        /// <returns>True if successful.</returns>
        public bool DeleteContainer(string container)
        {
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));

            string url = _Endpoint + _UserGuid + "/" + container + "?_container=true";

            RestRequest req = new RestRequest(
                url,
                HttpMethod.DELETE,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 204)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if a container exists.
        /// </summary> 
        /// <param name="container">The container.</param>
        /// <returns>True if the container exists.</returns>
        public bool ContainerExists(string container)
        {
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));

            string url = _Endpoint + _UserGuid + "/" + container + "?_container=true";

            RestRequest req = new RestRequest(
                url,
                HttpMethod.HEAD,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Objects

        /// <summary>
        /// Write an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="contentType">The content type for the object.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if successful.</returns>
        public bool WriteObject(string container, string objectKey, string contentType, byte[] data)
        { 
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            
            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.POST,
                _AuthHeaders,
                contentType,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send(data);

            if (resp == null || resp.StatusCode != 201)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Write an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="contentType">The content type for the object.</param>
        /// <param name="contentLength">The length of the data in the stream.</param>
        /// <param name="stream">The stream containing the data.</param>
        /// <returns>True if successful.</returns>
        public bool WriteObject(string container, string objectKey, string contentType, long contentLength, Stream stream)
        {
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.POST,
                _AuthHeaders,
                contentType,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send(stream, contentLength);

            if (resp == null || resp.StatusCode != 201)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Write a range of bytes to an existing object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="startIndex">The byte position at which to write the data.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>True if successful.</returns>
        public bool WriteObjectRange(string container, string objectKey, long startIndex, byte[] data)
        { 
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            if (startIndex < 0) throw new ArgumentException("Invalid value for startIndex.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey + "?_index=" + startIndex;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.PUT,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send(data);

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Write a range of bytes to an existing object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="startIndex">The byte position at which to write the data.</param>
        /// <param name="contentLength">The length of the data in the stream.</param>
        /// <param name="stream">The stream containing the data.</param>
        /// <returns>True if successful.</returns>
        public bool WriteObjectRange(string container, string objectKey, long startIndex, long contentLength, Stream stream)
        {
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            if (startIndex < 0) throw new ArgumentException("Invalid value for startIndex.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey + "?_index=" + startIndex;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.PUT,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send(stream, contentLength);

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="data">Data from the object.</param>
        /// <returns>True if successful.</returns>
        public bool ReadObject(string container, string objectKey, out byte[] data)
        {
            data = null;
             
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }
            
            if (resp.Data != null && resp.Data.Length > 0)
            {
                data = new byte[resp.Data.Length];
                Buffer.BlockCopy(resp.Data, 0, data, 0, resp.Data.Length);
            }

            return true;
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="contentLength">The length of the data in the stream.</param>
        /// <param name="stream">The stream containing the data.</param>
        /// <returns>True if successful.</returns>
        public bool ReadObject(string container, string objectKey, out long contentLength, out Stream stream)
        {
            contentLength = 0;
            stream = null;

            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                false);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            if (resp.DataStream != null && resp.ContentLength > 0)
            {
                contentLength = resp.ContentLength;
                stream = resp.DataStream;
            }

            return true;
        }

        /// <summary>
        /// Read a range of bytes from an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="startIndex">The byte position from which to read the data.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="data">Data from the object.</param>
        /// <returns>True if successful.</returns>
        public bool ReadObjectRange(string container, string objectKey, long startIndex, long count, out byte[] data)
        {
            data = null;
             
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            if (startIndex < 0) throw new ArgumentException("Invalid value for startIndex.");
            if (count <= 0) throw new ArgumentException("Invalid value for count.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey + "?_index=" + startIndex + "&_count=" + count;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            if (resp.Data != null && resp.Data.Length > 0)
            {
                data = new byte[resp.Data.Length];
                Buffer.BlockCopy(resp.Data, 0, data, 0, resp.Data.Length);
            }

            return true;
        }

        /// <summary>
        /// Read a range of bytes from an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="startIndex">The byte position from which to read the data.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="stream">The stream containing the data.</param>
        /// <returns>True if successful.</returns>
        public bool ReadObjectRange(string container, string objectKey, long startIndex, long count, out Stream stream)
        {
            stream = null;

            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            if (startIndex < 0) throw new ArgumentException("Invalid value for startIndex.");
            if (count <= 0) throw new ArgumentException("Invalid value for count.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey + "?_index=" + startIndex + "&_count=" + count;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                false);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            if (resp.DataStream != null && resp.ContentLength > 0)
            {
                stream = resp.DataStream;
            }

            return true;

        }

        /// <summary>
        /// Rename an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="originalObjectKey">The original object key.</param>
        /// <param name="newObjectKey">The desired object key.</param>
        /// <returns>True if successful.</returns>
        public bool RenameObject(string container, string originalObjectKey, string newObjectKey)
        { 
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(originalObjectKey)) throw new ArgumentNullException(nameof(originalObjectKey));
            if (String.IsNullOrEmpty(newObjectKey)) throw new ArgumentNullException(nameof(newObjectKey));

            string url = _Endpoint + _UserGuid + "/" + container + "/" + originalObjectKey + "?_rename=" + newObjectKey;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.PUT,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }
             
            return true;
        }

        /// <summary>
        /// Delete an object.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <returns>True if successful.</returns>
        public bool DeleteObject(string container, string objectKey)
        {  
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.DELETE,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 204)
            {
                return false;
            }
             
            return true;
        }

        /// <summary>
        /// Check if an object exists.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <returns>True if the object exists.</returns>
        public bool ObjectExists(string container, string objectKey)
        { 
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            RestRequest req = new RestRequest(
                url,
                HttpMethod.HEAD,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieve an object's metadata.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="metadata">The object's metadata.</param>
        /// <returns>True if successful.</returns>
        public bool GetObjectMetadata(string container, string objectKey, out ObjectMetadata metadata)
        {
            metadata = null;
             
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey + "?_metadata=true"; 

            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();
             
            if (resp == null || resp.StatusCode != 200 || resp.Data == null || resp.Data.Length < 1)
            {
                return false;
            }

            metadata = KvpbaseCommon.DeserializeJson<ObjectMetadata>(resp.Data);
            return true;
        }

        /// <summary>
        /// Upload from a file to an object.
        /// </summary>
        /// <param name="filename">The filename of the file to upload.</param>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="metadata">The object's metadata.</param>
        /// <returns>True if successful.</returns>
        public bool UploadFile(string filename, string container, string objectKey, string contentType, out ObjectMetadata metadata)
        {
            metadata = null;

            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            if (!ContainerExists(container)) throw new IOException("Container does not exist.");
            if (!File.Exists(filename)) throw new IOException("File specified does not exist.");
            if (ObjectExists(container, objectKey)) throw new IOException("Object specified already exists.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            long fileLength = new FileInfo(filename).Length;
            RestResponse resp = null;

            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                RestRequest req = new RestRequest(
                    url,
                    HttpMethod.POST,
                    _AuthHeaders,
                    contentType,
                    true);

                req.IgnoreCertificateErrors = IgnoreCertificateErrors;

                resp = req.Send(fs, fileLength);
            }

            if (resp == null || resp.StatusCode != 201)
            {
                return false;
            }
             
            return GetObjectMetadata(container, objectKey, out metadata);
        }

        /// <summary>
        /// Upload from a stream to an object.
        /// </summary>
        /// <param name="stream">The input stream from which to read.</param>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="metadata">The object's metadata.</param>
        /// <returns>True if successful.</returns>
        public bool UploadFromStream(Stream stream, long contentLength, string container, string objectKey, string contentType, out ObjectMetadata metadata)
        {
            metadata = null;

            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Stream cannot be read.");
            if (contentLength < 0) throw new ArgumentException("Content length must be zero or greater.");
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            if (!ContainerExists(container)) throw new IOException("Container does not exist.");
            if (ObjectExists(container, objectKey)) throw new IOException("Object specified already exists.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;

            RestResponse resp = null;
             
            RestRequest req = new RestRequest(
                url,
                HttpMethod.POST,
                _AuthHeaders,
                contentType,
                true);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            resp = req.Send(stream, contentLength); 

            if (resp == null || resp.StatusCode != 201)
            {
                return false;
            }

            return GetObjectMetadata(container, objectKey, out metadata);
        }

        /// <summary>
        /// Download an object to a file.
        /// </summary>
        /// <param name="filename">The filename of the file to upload.</param>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <returns>True if successful.</returns>
        public bool DownloadFile(string filename, string container, string objectKey)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            if (!ContainerExists(container)) throw new IOException("Container does not exist.");
            if (File.Exists(filename)) throw new IOException("File specified already exists.");
            if (!ObjectExists(container, objectKey)) throw new IOException("Object specified does not exist.");

            ObjectMetadata metadata = null;
            if (!GetObjectMetadata(container, objectKey, out metadata)) throw new IOException("Unable to retrieve object metadata.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;
             
            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
            {
                RestRequest req = new RestRequest(
                    url,
                    HttpMethod.GET,
                    _AuthHeaders,
                    null,
                    false);

                req.IgnoreCertificateErrors = IgnoreCertificateErrors;

                RestResponse resp = req.Send();

                if (resp == null || resp.StatusCode != 200)
                {
                    return false;
                }

                long bytesRemaining = resp.ContentLength;
                byte[] buffer = new byte[DownloadStreamBufferSize];

                if (bytesRemaining > 0)
                {
                    while (bytesRemaining > 0)
                    {
                        int bytesRead = resp.DataStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            bytesRemaining -= bytesRead;
                            fs.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Download an object to a stream.
        /// </summary>
        /// <param name="stream">The output stream into which data read will be written.</param>
        /// <param name="container">The container.</param>
        /// <param name="objectKey">The object key.</param>
        /// <param name="contentLength">Content length of the data in the stream.</param>
        /// <returns>True if successful.</returns>
        public bool DownloadToStream(Stream stream, string container, string objectKey, out long contentLength)
        {
            contentLength = 0;

            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite) throw new ArgumentException("Stream cannot be written.");             
            if (String.IsNullOrEmpty(container)) throw new ArgumentNullException(nameof(container));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            if (!ContainerExists(container)) throw new IOException("Container does not exist.");
            if (!ObjectExists(container, objectKey)) throw new IOException("Object specified does not exist.");

            ObjectMetadata metadata = null;
            if (!GetObjectMetadata(container, objectKey, out metadata)) throw new IOException("Unable to retrieve object metadata.");

            string url = _Endpoint + _UserGuid + "/" + container + "/" + objectKey;
             
            RestRequest req = new RestRequest(
                url,
                HttpMethod.GET,
                _AuthHeaders,
                null,
                false);

            req.IgnoreCertificateErrors = IgnoreCertificateErrors;

            RestResponse resp = req.Send();

            if (resp == null || resp.StatusCode != 200)
            {
                return false;
            }

            contentLength = resp.ContentLength;
            long bytesRemaining = resp.ContentLength;
            byte[] buffer = new byte[DownloadStreamBufferSize];

            if (bytesRemaining > 0)
            {
                while (bytesRemaining > 0)
                {
                    int bytesRead = resp.DataStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        bytesRemaining -= bytesRead;
                        stream.Write(buffer, 0, bytesRead);
                    }
                }

                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
            } 

            return true;
        }

        #endregion

        #endregion

        #region Private-Methods

        private void SetAuthHeaders()
        {
            _AuthHeaders = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(_Email) && !String.IsNullOrEmpty(_Password))
            {
                #region Password-Auth

                _AuthHeaders.Add("x-email", _Email);
                _AuthHeaders.Add("x-password", _Password);
                return;

                #endregion
            }
            else if (!String.IsNullOrEmpty(_ApiKey))
            {
                #region Api-Key-Auth

                _AuthHeaders.Add("x-api-key", _ApiKey);
                return;

                #endregion
            }
            else
            {
                throw new Exception("No authentication material configured.");
            } 
        }

        private string AppendSlash(string s)
        {
            if (String.IsNullOrEmpty(s)) return "/";
            if (s.EndsWith("/", StringComparison.InvariantCulture)) return s;
            return s + "/";
        }
         
        #endregion 
    }
}

