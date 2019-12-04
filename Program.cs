using System;
using System.Linq;

namespace IFXClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // создание soap - клиента
            var _soapClient = new SoapClient("http://services.ifx.ru/IFXService.svc/"/*Url сервиса апи*/);

            // открытие сессии
            if (_soapClient.OpenSession("your_client"/*клиент*/, "ru-RU"/*язык*/, "your_login"/*логин*/, "your_password"/*пароль*/))
            {
                // получение доступных продуктов
                var products = _soapClient.GetProductsList();

                // получение новостей по идентификатору продукта
                var news_ids = _soapClient.GetRealtimeNewsByProduct(0/*направление поиска*/, products[0].id/*идентификатор продукта*/, 1/*лимит новостей*/);

                // получение данных новости по идентификатору новости
                var news = _soapClient.GetEntireNewsByID(news_ids[0]/*идентификатор новости*/);

                // закрытие сессии
                _soapClient.CloseSession();

                // получаем имена продуктов новости
                var productNames = news.product_ids
                    .Select(pid => products.Where(p => p.id == pid).FirstOrDefault())
                    .Where(p => p != null).Select(p => p.name);

                // вывод данных новости
                Console.WriteLine($"id: {news.id}" +
                    $"{Environment.NewLine}headline: {news.headline}" +
                    $"{Environment.NewLine}publication_time: {news.publication_time}" +
                    $"{Environment.NewLine}body:{Environment.NewLine}{news.body}" +
                    $"{Environment.NewLine}products: [{string.Join(", ", productNames)}]");

                // ожидание ввода для закрытия приложения
                Console.ReadLine();
            }
        }
    }
}
