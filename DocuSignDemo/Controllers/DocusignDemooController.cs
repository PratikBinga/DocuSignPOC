using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocuSignDemo.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
//using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
//using System.Web.Mvc;
//using System.Web.Mvc;
using System.Xml;

namespace DocuSignDemo.Controllers
{
    [RoutePrefix("api/DocusignDemoo")]
    public class DocusignDemooController : ApiController
    {
        private string INTEGRATOR_KEY = "b43a7a81-bd0a-4894-9d8d-b5eebff80d66";
        public string _accountId = null;
        TestCredentials credential = new TestCredentials();


        [HttpPost]
        [Route("ReciveSignedDoc")]
        public void ReciveSignedDoc([FromBody] PayloadData payloadData)
        {
           // var addr = new System.Net.Mail.MailAddress(payloadData.UserNameFile);
            string directorypath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/" + "Files/");
            if (!Directory.Exists(directorypath))
            {
                Directory.CreateDirectory(directorypath);
            }
            var serverpath = directorypath + payloadData.UserNameFile + "_" +@DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".pdf";
            System.IO.File.WriteAllBytes(serverpath, Convert.FromBase64String(payloadData.DocumentBase64));
           // return View(serverpath);
        }


        [HttpPost]
        [Route("WebhookDocuSignResponse")]
        public async Task WebhookDocuSignResponse(HttpRequestMessage request)
        {
            string directorypath = HttpContext.Current.Server.MapPath("~/App_Data/" + "Files/");
            if (!Directory.Exists(directorypath))
            {
                Directory.CreateDirectory(directorypath);
            }
            //    StringBuilder sb = new StringBuilder();

            //  sb.Append("log something");


            // flush every 20 seconds as you do it
            //  File.AppendAllText(directorypath + "log.txt", sb.ToString());




            // to check the xml data pushed by docusign to our listener.
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(request.Content.ReadAsStreamAsync().Result);
            //       sb.Append(xmldoc.InnerXml);
            //    File.AppendAllText(directorypath + "XMLlog.txt", sb.ToString());
            //   sb.Clear();

            var mgr = new XmlNamespaceManager(xmldoc.NameTable);
            mgr.AddNamespace("a", "http://www.docusign.net/API/3.0");

            XmlNode envelopeStatus = xmldoc.SelectSingleNode("//a:EnvelopeStatus", mgr);
            XmlNode envelopeId = envelopeStatus.SelectSingleNode("//a:EnvelopeID", mgr);
            XmlNode status = envelopeStatus.SelectSingleNode("./a:Status", mgr);
            string envId = envelopeId.InnerText;
            if (envelopeId != null)
            {

                //***************  need to fetch the return url from envelopeid so that url can be dynamic not fixed.

              string retUrl =  GetReturnUrl(envId);
            }

            // Loop through the DocumentPDFs element, storing each signed document.

            //XmlNode docs = xmldoc.SelectSingleNode("//a:DocumentPDFs", mgr);
            //foreach (XmlNode doc in docs.ChildNodes)
            //{
            //    string documentName = doc.ChildNodes[0].InnerText; // pdf.SelectSingleNode("//a:Name", mgr).InnerText;
            //    string documentId = doc.ChildNodes[2].InnerText; // pdf.SelectSingleNode("//a:DocumentID", mgr).InnerText;
            //    string byteStr = doc.ChildNodes[1].InnerText; // pdf.SelectSingleNode("//a:PDFBytes", mgr).InnerText;
            //    byte[] bytArray = Encoding.ASCII.GetBytes(byteStr);




            //    // System.IO.File.WriteAllText(HttpContext.Current.Server.MapPath("~/Documents/" + envelopeId.InnerText + "_" + documentId + "_" + documentName), byteStr);
            //    System.IO.File.WriteAllText(HttpContext.Current.Server.MapPath("~/Documents/" + envelopeId.InnerText + "_" + documentId + "_" + documentName), System.Convert.ToBase64String(bytArray));
            //}

            if (status.InnerText == "Completed")
            {
                XmlNode recipientStatus = xmldoc.SelectSingleNode("//a:RecipientStatus", mgr);
                XmlNode UserName = recipientStatus.SelectSingleNode("//a:UserName", mgr);
                // purge the envelope if status is completed.
                EnvelopesApi obj = new EnvelopesApi();

                ApiClient apiClient = new ApiClient("https://demo.docusign.net/restapi");
                DocuSign.eSign.Client.Configuration.Default.ApiClient = apiClient;
                //Verify Account Details  
                string accountId = loginApi("", "");

                /// String filePath = String.Empty;
                // FileStream fs = null;

                PayloadData payLoadData = new PayloadData();
                payLoadData.DocumentBase64 = "";
                payLoadData.UserNameFile = UserName.InnerText;
                string DocumentBase64 = "";

                EnvelopeDocumentsResult docsList = obj.ListDocuments(accountId, envId);

                for (int i = 0; i < docsList.EnvelopeDocuments.Count; i++)
                {
                    if (docsList.EnvelopeDocuments[i].Type == "content")
                    {
                        MemoryStream docStream = (MemoryStream)obj.GetDocument(accountId, envId, docsList.EnvelopeDocuments[i].DocumentId);

                            byte[] fileData = docStream.ToArray();
                            payLoadData.DocumentBase64 = System.Convert.ToBase64String(fileData);
       
                    }

                }

                // method to post data to caller api i.e signed document without storing it

                using (var client = new HttpClient())
                {
                    // as of now the url is hardcoded we can get it from above call of geturl.
                   // client.PostAsync()
                    var res = await client.PostAsync("http://localhost:52427/api/DocusignDemoo/ReciveSignedDoc", new StringContent(JsonConvert.SerializeObject(payLoadData), Encoding.UTF8, "application/json"));


                    try
                    {
                        res.EnsureSuccessStatusCode();
                        //res.Result.EnsureSuccessStatusCode();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                    
                }
             //B   return Ok();



                // let's save the document to local file system
                //filePath = Path.GetTempPath() + Path.GetRandomFileName() + ".pdf";
                //fs = new FileStream(filePath, FileMode.Create);
                //docStream.Seek(0, SeekOrigin.Begin);
                //docStream.CopyTo(fs);
                //fs.Close();


                // MemStream ms = FileStream().Save()

                //Envelope envInfo = obj.GetEnvelope(accountId, envId);
                //envInfo.PurgeState = "documents_and_metadata_queued";
                //envInfo.EnvelopeId = envId;
                //// set envelope status voided forcefully to purge.
                //envInfo.Status = "voided";
                //EnvelopeUpdateSummary envelopeUpdateSummary = obj.Update(accountId, envInfo.EnvelopeId, envInfo, null);


            }


        }

        // need to place it in service class of webhook.
        public string GetReturnUrl(string envId)
        {
           // var retUrl = dbcontext.dbset.Where(a => a.ID == envId).Select(a => a.returnurl).Single();
           // _context.recipients.singleordefault(e => e.id = envId);
            return "";
        }

        // how these method will be called from client? need to place in differenct controller
        [HttpGet]
        public string GetStatusofPDFDocument(string envId)
        {
            // var retUrl = dbcontext.dbset.Where(a => a.ID == envId).Select(a => a.status).Single();
            // _context.recipients.singleordefault(e => e.id = envId);
            return "";
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
