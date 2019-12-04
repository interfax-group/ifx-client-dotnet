using IFXClient.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;

namespace IFXClient
{
    /// <summary>
    /// Soap клиент
    /// </summary>
    internal class SoapClient
    {
        /// <summary>
        /// Url сервиса апи
        /// </summary>
        private readonly string _url;

        /// <summary>
        /// Сессионные куки
        /// </summary>
        private CookieContainer _sessionCookies;

        #region soap_body

        private const string SEPARATOR = "#;";

        private static readonly string _openSessionSoap = @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
    <s:Body>
        <osmreq xmlns=""http://ifx.ru/IFX3WebService"">
            <mbci>{0}</mbci>
            <mbcv>1</mbcv>
            <mbh>OnlyHeadline</mbh>
            <mbl>{1}</mbl>
		    <mbla>{2}</mbla>
		    <mbo>Windows</mbo>
		    <mbp>{3}</mbp>
	    </osmreq>
    </s:Body>
</s:Envelope>";

        private static readonly string _getProductsListSoap = @"<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"">
   <soap:Header/>
   <soap:Body/>
</soap:Envelope>";

        private static readonly string _getRealtimeNewsByProduct = @"<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ifx=""http://ifx.ru/IFX3WebService"">
   <soap:Header/>
   <soap:Body>
      <ifx:grnbpmreq>
         <ifx:direction>{0}</ifx:direction>
         <ifx:mbcid>{1}</ifx:mbcid>
         <ifx:mblnl>{2}</ifx:mblnl>
         <ifx:mbsup></ifx:mbsup>
      </ifx:grnbpmreq>
   </soap:Body>
</soap:Envelope>";

        private static readonly string _getEntireNewsById = @"<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ifx=""http://ifx.ru/IFX3WebService"">
   <soap:Header/>
   <soap:Body>
      <ifx:genmreq>
         <ifx:mbnid>{0}</ifx:mbnid>
      </ifx:genmreq>
   </soap:Body>
</soap:Envelope>";

        private static readonly string _closeSession = @"<soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ifx=""http://ifx.ru/IFX3WebService"">
   <soap:Header/>
   <soap:Body>
      <ifx:CloseSession/>
   </soap:Body>
</soap:Envelope>";

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="url">Url сервиса апи</param>
        public SoapClient(string url)
        {
            _url = url;
        }

        /// <summary>
        /// Метод открытия сессии
        /// </summary>
        /// <param name="client">Наименование клиента</param>
        /// <param name="lang">Язык</param>
        /// <param name="login">Логин</param>
        /// <param name="password">Пароль</param>
        public bool OpenSession(string client, string lang, string login, string password)
        {
            var soapBody = string.Format(_openSessionSoap, client, login, lang, password);

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = $"application/soap+xml;charset=utf-8; action=\"http://ifx.ru/IFX3WebService/IIFXService/{MethodBase.GetCurrentMethod().Name}\"";
            request.CookieContainer = new CookieContainer();

            using (var st = request.GetRequestStream())
            {
                using (var sw = new StreamWriter(st))
                {
                    sw.Write(soapBody);
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                _sessionCookies = new CookieContainer();
                _sessionCookies.Add(response.Cookies);
            }

            return _sessionCookies.Count > 0;
        }

        /// <summary>
        /// Метод получения продуктов, доступных пользователю
        /// </summary>
        public List<Product> GetProductsList()
        {
            var result = new List<Product>();

            var soapBody = _getProductsListSoap;

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = $"application/soap+xml;charset=utf-8; action=\"http://ifx.ru/IFX3WebService/IIFXService/{MethodBase.GetCurrentMethod().Name}\"";
            request.CookieContainer = _sessionCookies;

            using (var st = request.GetRequestStream())
            {
                using (var sw = new StreamWriter(st))
                {
                    sw.Write(soapBody);
                }
            }

            var xmlResult = new XmlDocument();

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    xmlResult.LoadXml(sr.ReadToEnd());
                }
            }

            var mgr = new XmlNamespaceManager(xmlResult.NameTable);
            var node = xmlResult.FirstChild;

            while (node != null && node.Name != "gpmresp")
            {
                node = node.FirstChild;
            }

            if (node != null)
            {
                mgr.AddNamespace("s", node.NamespaceURI);
                var newsParentNode = node.SelectSingleNode("descendant::s:mbpl", mgr);
                XmlNodeList newsNodes = null;

                if (newsParentNode != null)
                {
                    newsNodes = newsParentNode.ChildNodes;
                }

                if (newsNodes != null)
                {
                    foreach (XmlNode newsNode in newsNodes)
                    {
                        var titleNode = newsNode.SelectSingleNode("descendant::s:n", mgr);
                        var idNode = newsNode.SelectSingleNode("descendant::s:i", mgr);

                        if (idNode != null)
                        {
                            result.Add(new Product { id = idNode.InnerText, name = titleNode.InnerText });
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Метод получения последних новостей по идентификатору продукта
        /// </summary>
        /// <param name="direction">Направление поиска: 0 - вперед, 1 - назад</param>
        /// <param name="productId">Идентификатор продукта</param>
        /// <param name="limit">Количество новостей в ответе</param>
        public List<string> GetRealtimeNewsByProduct(int direction, string productId, int limit)
        {
            var result = new List<string>();

            var soapBody = string.Format(_getRealtimeNewsByProduct, direction, productId, limit);

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = $"application/soap+xml;charset=utf-8; action=\"http://ifx.ru/IFX3WebService/IIFXService/{MethodBase.GetCurrentMethod().Name}\"";
            request.CookieContainer = _sessionCookies;

            using (var st = request.GetRequestStream())
            {
                using (var sw = new StreamWriter(st))
                {
                    sw.Write(soapBody);
                }
            }

            var xmlResult = new XmlDocument();

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    xmlResult.LoadXml(sr.ReadToEnd());
                }
            }

            XmlNamespaceManager mgr = new XmlNamespaceManager(xmlResult.NameTable);

            XmlNode node = xmlResult.FirstChild;

            while (node != null && node.Name != "grnmresp")
            {
                node = node.FirstChild;
            }

            if (node != null)
            {
                mgr.AddNamespace("s", node.NamespaceURI);
                var newsParentNode = node.SelectSingleNode("descendant::s:mbnl", mgr);
                XmlNodeList newsNodes = null;

                if (newsParentNode != null)
                {
                    newsNodes = newsParentNode.ChildNodes;
                }

                if (newsNodes != null)
                {
                    foreach (XmlNode newsNode in newsNodes)
                    {
                        var idNode = newsNode.SelectSingleNode("descendant::s:i", mgr);
                        var sidsNode = newsNode.SelectSingleNode("descendant::s:sids", mgr);

                        XmlNodeList sidNodes = null;
                        if (sidsNode != null)
                        {
                            sidNodes = sidsNode.ChildNodes;
                        }

                        var sb = new StringBuilder();

                        if (idNode != null)
                        {
                            sb.Append(idNode.InnerText);

                            if (sidNodes != null)
                            {
                                foreach (XmlNode fit in sidNodes)
                                {
                                    sb.Append(SEPARATOR + fit.InnerText);
                                }
                            }

                            result.Add(sb.ToString());
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Метод получения данных новости по идентификатору новости
        /// </summary>
        /// <param name="newsId">Идентификатор новости</param>
        public News GetEntireNewsByID(string newsId)
        {
            var result = new News
            {
                product_ids = new List<string>()
            };

            var soapBody = string.Format(_getEntireNewsById, newsId);

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = $"application/soap+xml;charset=utf-8; action=\"http://ifx.ru/IFX3WebService/IIFXService/{MethodBase.GetCurrentMethod().Name}\"";
            request.CookieContainer = _sessionCookies;

            using (var st = request.GetRequestStream())
            {
                using (var sw = new StreamWriter(st))
                {
                    sw.Write(soapBody);
                }
            }

            var xmlResult = new XmlDocument();

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    xmlResult.LoadXml(sr.ReadToEnd());
                }
            }

            var node = xmlResult.FirstChild;

            while (node != null && node.Name != "mbn")
            {
                node = node.FirstChild;
            }

            if (node != null)
            {
                var childs = node.ChildNodes;

                foreach (XmlNode child in childs)
                {
                    if (child.Name == "p")
                    {
                        foreach (XmlNode nrh in child.ChildNodes)
                        {
                            result.product_ids.Add(nrh.InnerText);
                        }
                    }
                }

                result.id = newsId.Split(new string[] { SEPARATOR }, StringSplitOptions.None)[0];
                result.headline = GetSiblingNodeValue("h", node);
                result.publication_time = DateTime.Parse(GetSiblingNodeValue("pd", node));
                result.body = GetSiblingNodeValue("c", node);
            }

            return result;
        }

        /// <summary>
        /// Метод закрытия сессии
        /// </summary>
        public void CloseSession()
        {
            var soapBody = _closeSession;

            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = $"application/soap+xml;charset=utf-8; action=\"http://ifx.ru/IFX3WebService/IIFXService/{MethodBase.GetCurrentMethod().Name}\"";
            request.CookieContainer = _sessionCookies;

            using (var st = request.GetRequestStream())
            {
                using (var sw = new StreamWriter(st))
                {
                    sw.Write(soapBody);
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                _sessionCookies = null;
            }
        }

        #region helpers

        private string GetSiblingNodeValue(string siblingNodeName, XmlNode newsNode)
        {
            if (string.IsNullOrEmpty(siblingNodeName))
            {
                throw new ArgumentNullException("siblingNodeName");
            }

            if (newsNode == null || newsNode.ChildNodes.Count == 0)
            {
                return null;
            }

            var node = newsNode.FirstChild;

            while (node != null && node.Name != siblingNodeName)
            {
                node = node.NextSibling;
            }

            return node?.InnerText;
        }

        #endregion
    }
}
