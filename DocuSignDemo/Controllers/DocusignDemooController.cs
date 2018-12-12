using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml;

namespace DocuSignDemo.Controllers
{
    public class DocusignDemooController : ApiController
    {
        private string INTEGRATOR_KEY = "b43a7a81-bd0a-4894-9d8d-b5eebff80d66";
        public string _accountId = null;
        TestCredentials credential = new TestCredentials();

        [HttpPost]
        public void WebhookDocuSignResponse(HttpRequestMessage request)
        {
            string directorypath = HttpContext.Current.Server.MapPath("~/App_Data/" + "Files/");
            if (!Directory.Exists(directorypath))
            {
                Directory.CreateDirectory(directorypath);
            }
            StringBuilder sb = new StringBuilder();

            sb.Append("log something");


            // flush every 20 seconds as you do it
            File.AppendAllText(directorypath + "log.txt", sb.ToString());




            // to check the xml data pushed by docusign to our listener.
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(request.Content.ReadAsStreamAsync().Result);
            sb.Append(xmldoc.InnerXml);
            File.AppendAllText(directorypath + "XMLlog.txt", sb.ToString());
            sb.Clear();

            var mgr = new XmlNamespaceManager(xmldoc.NameTable);
            mgr.AddNamespace("a", "http://www.docusign.net/API/3.0");

            XmlNode envelopeStatus = xmldoc.SelectSingleNode("//a:EnvelopeStatus", mgr);
            XmlNode envelopeId = envelopeStatus.SelectSingleNode("//a:EnvelopeID", mgr);
            XmlNode status = envelopeStatus.SelectSingleNode("./a:Status", mgr);
            string envId = envelopeId.InnerText;
            if (envelopeId != null)
            {
                //System.IO.File.WriteAllText(HttpContext.Current.Server.MapPath("~/Documents/" +
                //    envelopeId.InnerText + "_" + status.InnerText + "_" + Guid.NewGuid() + ".xml"), xmldoc.OuterXml);
            }

            // Loop through the DocumentPDFs element, storing each signed document.

            XmlNode docs = xmldoc.SelectSingleNode("//a:DocumentPDFs", mgr);
            foreach (XmlNode doc in docs.ChildNodes)
            {
                string documentName = doc.ChildNodes[0].InnerText; // pdf.SelectSingleNode("//a:Name", mgr).InnerText;
                string documentId = doc.ChildNodes[2].InnerText; // pdf.SelectSingleNode("//a:DocumentID", mgr).InnerText;
                string byteStr = doc.ChildNodes[1].InnerText; // pdf.SelectSingleNode("//a:PDFBytes", mgr).InnerText;
                byte[] bytArray = Encoding.ASCII.GetBytes(byteStr);




                // System.IO.File.WriteAllText(HttpContext.Current.Server.MapPath("~/Documents/" + envelopeId.InnerText + "_" + documentId + "_" + documentName), byteStr);
                System.IO.File.WriteAllText(HttpContext.Current.Server.MapPath("~/Documents/" + envelopeId.InnerText + "_" + documentId + "_" + documentName), System.Convert.ToBase64String(bytArray));
            }

            if (status.InnerText == "Completed")
            {
                // purge the envelope if status is completed.
                EnvelopesApi obj = new EnvelopesApi();

                ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
                DocuSign.eSign.Client.Configuration.Default.ApiClient = apiClient;
                //Verify Account Details  
                string accountId = loginApi("", "");

                String filePath = String.Empty;
                FileStream fs = null;

                EnvelopeDocumentsResult docsList = obj.ListDocuments(accountId, envId);

                for (int i = 0; i < docsList.EnvelopeDocuments.Count; i++)
                {
                    MemoryStream docStream = (MemoryStream)obj.GetDocument(accountId, envId, docsList.EnvelopeDocuments[i].DocumentId);

                    // let's save the document to local file system
                    filePath = Path.GetTempPath() + Path.GetRandomFileName() + ".pdf";
                    fs = new FileStream(filePath, FileMode.Create);
                    docStream.Seek(0, SeekOrigin.Begin);
                    docStream.CopyTo(fs);
                    fs.Close();
                }

               // MemStream ms = FileStream().Save()

                Envelope envInfo = obj.GetEnvelope(accountId, envId);
                envInfo.PurgeState = "documents_and_metadata_queued";
                envInfo.EnvelopeId = envId;
                // set envelope status voided forcefully to purge.
                envInfo.Status = "voided";
                EnvelopeUpdateSummary envelopeUpdateSummary = obj.Update(accountId, envInfo.EnvelopeId, envInfo, null);


            }


        }


        public string loginApi(string usr, string pwd)
        {
            usr = "Pratikswvk@gmail.com";
            pwd = "Cns@12345";
            // we set the api client in global config when we configured the client  
            ApiClient apiClient = DocuSign.eSign.Client.Configuration.Default.ApiClient;
            string authHeader = "{\"Username\":\"" + usr + "\", \"Password\":\"" + pwd + "\", \"IntegratorKey\":\"" + INTEGRATOR_KEY + "\"}";
            DocuSign.eSign.Client.Configuration.Default.AddDefaultHeader("X-DocuSign-Authentication", authHeader);
            // we will retrieve this from the login() results  
            string accountId = null;
            // the authentication api uses the apiClient (and X-DocuSign-Authentication header) that are set in Configuration object  
            AuthenticationApi authApi = new AuthenticationApi();
            LoginInformation loginInfo = authApi.Login();
            // find the default account for this user  
            foreach (DocuSign.eSign.Model.LoginAccount loginAcct in loginInfo.LoginAccounts)
            {
                if (loginAcct.IsDefault == "true")
                {
                    accountId = loginAcct.AccountId;
                    _accountId = accountId;
                    break;
                }
            }
            if (accountId == null)
            { // if no default found set to first account  
                accountId = loginInfo.LoginAccounts[0].AccountId;
            }
            return accountId;
        }
    }
}